using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;
using TradingBot.Services;
using TradingBot.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;


var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        IConfiguration config = context.Configuration;

        string? botToken = config["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(botToken))
            throw new Exception("В конфигурации не указан Telegram Bot Token.");

        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

        string connection = config.GetConnectionString("Default") ?? "Data Source=trades.db";

        services.AddDbContext<TradeContext>(options =>
        {
            options.UseSqlite(connection);

            // Временно подавляем PendingModelChangesWarning, чтобы запуск не падал,
            // если миграции ещё не созданы. Рекомендую всё же создать миграцию.
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        // Общие зависимости
        services.AddSingleton<PnLService>();
        services.AddSingleton<UIManager>();
        services.AddMemoryCache();
        services.AddScoped<TradeRepository>();

        bool useNotion = bool.TryParse(config["UseNotion"], out var flag) && flag;

        if (useNotion)
        {
            services.AddHttpClient<NotionService>((provider, client) =>
            {
                string? notionToken = config["Notion:ApiToken"];
                if (string.IsNullOrWhiteSpace(notionToken))
                    throw new Exception("В конфигурации не указан Notion API Token.");

                client.BaseAddress = new Uri("https://api.notion.com/v1/");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {notionToken}");
                client.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");
            });
            services.AddScoped<ITradeStorage, NotionTradeStorage>();
        }
        else
        {
            // Локальное хранилище SQLite
            services.AddScoped<ITradeStorage, SQLiteTradeStorage>();
        }

        // UpdateHandler с зависимостями
        services.AddScoped<UpdateHandler>(provider =>
        {
            var tradeStorage = provider.GetRequiredService<ITradeStorage>();
            var pnlService = provider.GetRequiredService<PnLService>();
            var uiManager = provider.GetRequiredService<UIManager>();
            var logger = provider.GetRequiredService<ILogger<UpdateHandler>>();
            var cache = provider.GetRequiredService<IMemoryCache>();
            string botId = botToken.Contains(':') ? botToken.Split(':')[0] : "bot";
            string connectionString = connection;
            return new UpdateHandler(tradeStorage, pnlService, uiManager, logger, cache, connectionString, botId);
        });

        services.AddHostedService<BotService>();
        services.AddHostedService<ReportService>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("Program");

    // 1) Снимаем webhook перед запуском polling (чинит 409 Conflict)
    try
    {
        var bot = sp.GetRequiredService<ITelegramBotClient>();
        // Если в вашей версии пакета доступен флаг dropPendingUpdates — можете вызвать так:
        // await bot.DeleteWebhookAsync(dropPendingUpdates: true);
        await bot.DeleteWebhook(); // совместимо со старыми версиями клиента
        logger.LogInformation("Telegram webhook удалён перед стартом polling.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Не удалось удалить webhook. Продолжаем запуск.");
    }

    // 2) Пробуем применить миграции (не падаем, если что-то не так)
    try
    {
        var db = sp.GetRequiredService<TradeContext>();
        await db.Database.MigrateAsync();
        logger.LogInformation("EF Core миграции применены успешно.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Ошибка при миграции базы данных. Пытаемся синхронизировать схему вручную.");
        
        // Попробуем синхронизировать схему вручную
        try
        {
            var db = sp.GetRequiredService<TradeContext>();
            await EnsureDatabaseSchemaAsync(db, logger);
            logger.LogInformation("Схема базы данных синхронизирована вручную.");
        }
        catch (Exception schemaEx)
        {
            logger.LogError(schemaEx, "Не удалось синхронизировать схему базы данных. Бот может работать некорректно.");
        }
    }


}

await host.RunAsync();

static async Task EnsureDatabaseSchemaAsync(TradeContext ctx, ILogger logger)
{
    var requiredColumns = new Dictionary<string, string>
    {
        {"Account", "TEXT"},
        {"Session", "TEXT"},
        {"Position", "TEXT"},
        {"Direction", "TEXT"},
        {"Context", "TEXT"},
        {"Setup", "TEXT"},
        {"Emotions", "TEXT"},
        {"EntryDetails", "TEXT"},
        {"Note", "TEXT"},
        {"Result", "TEXT"},
        {"RR", "TEXT"},
        {"Risk", "REAL"},
        {"PnL", "REAL"},
        {"NotionPageId", "TEXT"}
    };

    await using var conn = (SqliteConnection)ctx.Database.GetDbConnection();
    if (conn.State != System.Data.ConnectionState.Open)
        await conn.OpenAsync();

    // Получаем список существующих колонок
    var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    await using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "PRAGMA table_info('Trades')";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var name = reader.GetString(1);
            existingColumns.Add(name);
        }
    }

    // Добавляем недостающие колонки
    foreach (var column in requiredColumns)
    {
        if (existingColumns.Contains(column.Key))
        {
            logger.LogDebug($"Колонка {column.Key} уже существует, пропускаем.");
            continue;
        }

        try
        {
            var alterSql = $"ALTER TABLE \"Trades\" ADD COLUMN \"{column.Key}\" {column.Value}";
            if (column.Key == "PnL")
                alterSql += " NOT NULL DEFAULT 0.0";
            else
                alterSql += " NULL";

            await using var alterCmd = conn.CreateCommand();
            alterCmd.CommandText = alterSql;
            await alterCmd.ExecuteNonQueryAsync();
            logger.LogInformation($"Добавлена колонка {column.Key} типа {column.Value}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, $"Не удалось добавить колонку {column.Key}");
        }
    }
}




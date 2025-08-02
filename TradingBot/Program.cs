using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;
using TradingBot.Services;
using TradingBot.Models;

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
        if (string.IsNullOrEmpty(botToken))
            throw new Exception("В конфигурации не указан Telegram Bot Token.");

        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

        string connection = config.GetConnectionString("Default") ?? "Data Source=trades.db";
        services.AddDbContext<TradeContext>(options => options.UseSqlite(connection));

        services.AddSingleton<PnLService>();
        services.AddSingleton<UIManager>();
        services.AddMemoryCache();

        bool useNotion = bool.Parse(config["UseNotion"] ?? "false");

        if (useNotion)
        {
            // Регистрация HttpClient для NotionService с нужными заголовками
            services.AddHttpClient<NotionService>((provider, client) =>
            {
                string? notionToken = config["Notion:ApiToken"];
                if (string.IsNullOrEmpty(notionToken))
                    throw new Exception("В конфигурации не указан Notion API Token.");
                client.BaseAddress = new Uri("https://api.notion.com/v1/");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {notionToken}");
                client.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");
            });
            services.AddScoped<ITradeStorage, NotionTradeStorage>();
        }
        else
        {
            services.AddScoped<ITradeStorage, SQLiteTradeStorage>();
        }

// Регистрация прочих сервисов бота
        services.AddSingleton<PnLService>();
        services.AddSingleton<UIManager>();
        services.AddMemoryCache();

// Внедрение UpdateHandler с зависимостями
        services.AddScoped<UpdateHandler>(provider =>
        {
            var tradeStorage = provider.GetRequiredService<ITradeStorage>();
            var pnlService  = provider.GetRequiredService<PnLService>();
            var uiManager   = provider.GetRequiredService<UIManager>();
            var logger      = provider.GetRequiredService<ILogger<UpdateHandler>>();
            var cache       = provider.GetRequiredService<IMemoryCache>();
            string botId    = botToken.Contains(":") ? botToken.Split(':')[0] : "bot";
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
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<TradeContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Program");
        logger.LogError(ex, "Ошибка при миграции базы данных.");
        throw;
    }
}

await host.RunAsync();

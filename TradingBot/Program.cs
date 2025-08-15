using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

using Telegram.Bot;
using TradingBot.Services;
using TradingBot.Services.Interfaces;
using TradingBot.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using Prometheus;


var host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseUrls("http://localhost:5000");
        webBuilder.Configure(app =>
        {
            // Логируем запуск веб-сервера
            var logger = app.ApplicationServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("🌐 Настройка веб-сервера...");
            
            // Статические файлы для веб-интерфейса
            app.UseDefaultFiles();
            app.UseStaticFiles();
            
            logger.LogInformation("🌐 Статические файлы настроены");
            
            app.UseRouting();
            
            app.UseEndpoints(endpoints =>
            {
                // Простые эндпоинты для тестирования
                endpoints.MapGet("/test", () => "Test endpoint works!");
                
                // Эндпоинт для метрик Prometheus
                endpoints.MapGet("/metrics", async context =>
                {
                    logger.LogInformation("📊 Запрос метрик от {RemoteIpAddress}", context.Connection.RemoteIpAddress);
                    context.Response.ContentType = "text/plain; version=0.0.4; charset=utf-8";
                    var metrics = Metrics.DefaultRegistry.ToString() ?? string.Empty;
                    await context.Response.WriteAsync(metrics);
                    logger.LogInformation("📊 Метрики отправлены, размер: {Size} символов", metrics.Length);
                });
                
                // Эндпоинт для health checks
                endpoints.MapGet("/health", async context =>
                {
                    logger.LogInformation("🏥 Health check от {RemoteIpAddress}", context.Connection.RemoteIpAddress);
                    context.Response.ContentType = "application/json";
                    var healthResponse = "{\"status\":\"healthy\",\"timestamp\":\"" + DateTime.UtcNow.ToString("O") + "\"}";
                    await context.Response.WriteAsync(healthResponse);
                    logger.LogInformation("🏥 Health check отправлен");
                });
                
                // Корневой эндпоинт - перенаправляем на дашборд
                endpoints.MapGet("/", async context =>
                {
                    logger.LogInformation("🏠 Главная страница от {RemoteIpAddress}", context.Connection.RemoteIpAddress);
                    context.Response.Redirect("/index.html");
                });
            });
            
            logger.LogInformation("🌐 Веб-сервер настроен и запущен на http://localhost:5000");
        });
    })
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
        });

        // Общие зависимости
        services.AddSingleton<PnLService>();
        services.AddSingleton<UIManager>();
        services.AddSingleton<KeyboardService>();
        services.AddMemoryCache();
        services.AddScoped<TradeRepository>();
        services.AddScoped<UserSettingsService>();
        services.AddScoped<ErrorHandlingService>();
        
        // HTTP клиенты для Notion API с Polly политиками
        services.AddNotionHttpClients();
        
        // Фабрика HTTP клиентов для персональных настроек Notion
        services.AddSingleton<NotionHttpClientFactory>();
        
        // Сервисы для работы с Notion
        services.AddScoped<PersonalNotionService>();
        services.AddScoped<NotionSettingsService>();
        services.AddScoped<NotionSchemaCacheService>();
        
        // Добавляем NotionService с конфигурацией
        services.AddScoped<NotionService>(provider =>
        {
            var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("NotionClient");
            var databaseId = config["Notion:DatabaseId"] ?? throw new Exception("Notion DatabaseId not configured.");
            var logger = provider.GetRequiredService<ILogger<NotionService>>();
            return new NotionService(httpClient, databaseId, logger);
        });
        
        // Сервис фоновых задач
        services.AddSingleton<BackgroundTaskService>();
        
        // Сервис валидации
        services.AddScoped<ValidationService>();
        
        // Сервис ограничения частоты запросов
        services.AddScoped<RateLimitingService>();
        
        // Глобальный обработчик исключений
        services.AddScoped<GlobalExceptionHandler>();
        
        // Сервис мониторинга здоровья системы
        services.AddScoped<HealthCheckService>();
        
        // Сервис метрик Prometheus
        services.AddSingleton<IMetricsService, PrometheusMetricsService>();
        
        // Сервис мониторинга здоровья системы (фоновый)
        services.AddScoped<IHealthMonitoringService, HealthMonitoringService>();
        services.AddHostedService<HealthMonitoringService>();
        
        // Сервис сбора системных метрик
        services.AddHostedService<SystemMetricsCollector>();
        services.AddHostedService<MetricsUpdateService>();
        
        // Новые сервисы мониторинга и уведомлений
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPerformanceMetricsService, PerformanceMetricsService>();
        services.AddScoped<IAdvancedMonitoringService, AdvancedMonitoringService>();
        
        // Конфигурация новых сервисов
        services.Configure<NotificationSettings>(
            config.GetSection("NotificationSettings"));
        services.Configure<MonitoringSettings>(
            config.GetSection("MonitoringSettings"));
        
        // Добавляем health checks
        services.AddHealthChecks()
            .AddCheck<HealthCheckService>("database_health", tags: new[] { "database", "critical" })
            .AddDbContextCheck<TradeContext>("ef_core_health", tags: new[] { "database", "ef_core" });

        bool useNotion = bool.TryParse(config["UseNotion"], out var flag) && flag;

        if (useNotion)
        {
            // Глобальное хранилище Notion для общих справочников
            services.AddScoped<ITradeStorage, NotionTradeStorage>();
        }
        else
        {
            // Локальное хранилище SQLite
            services.AddScoped<ITradeStorage, SQLiteTradeStorage>();
        }

        // UpdateHandler с зависимостями
        services.AddSingleton<UpdateHandler>(provider =>
        {
            var tradeStorage = provider.GetRequiredService<ITradeStorage>();
            var pnlService = provider.GetRequiredService<PnLService>();
            var uiManager = provider.GetRequiredService<UIManager>();
            var logger = provider.GetRequiredService<ILogger<UpdateHandler>>();
            var cache = provider.GetRequiredService<IMemoryCache>();
            var validationService = provider.GetRequiredService<ValidationService>();
            var rateLimitingService = provider.GetRequiredService<RateLimitingService>();
            var metricsService = provider.GetRequiredService<TradingBot.Services.Interfaces.IMetricsService>();
            var exceptionHandler = provider.GetRequiredService<GlobalExceptionHandler>();
            string botId = botToken.Contains(':') ? botToken.Split(':')[0] : "bot";
            string connectionString = connection;
            return new UpdateHandler(tradeStorage, pnlService, uiManager, logger, cache, validationService, rateLimitingService, metricsService, exceptionHandler, connectionString, botId);
        });

        services.AddHostedService<BotService>();
        services.AddHostedService<ReportService>();
        services.AddHostedService<AdvancedMonitoringService>();
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
        
        // Проверяем, существует ли база данных
        if (db.Database.CanConnect())
        {
            // Если база существует, проверяем схему
            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                try
                {
                    await db.Database.MigrateAsync();
                    logger.LogInformation("EF Core миграции применены успешно.");
                }
                catch (Exception migrationEx) when (migrationEx.Message.Contains("duplicate column name"))
                {
                    logger.LogWarning("Обнаружены конфликты миграций. Переходим к ручной синхронизации схемы.");
                    await EnsureDatabaseSchemaAsync(db, logger);
                    logger.LogInformation("Схема базы данных синхронизирована вручную после конфликта миграций.");
                }
            }
            else
            {
                logger.LogInformation("База данных уже актуальна, миграции не требуются.");
            }
        }
        else
        {
            // Если база не существует, создаем её
            await db.Database.EnsureCreatedAsync();
            logger.LogInformation("База данных создана успешно.");
        }
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
        catch (Exception ex) when (ex.Message.Contains("duplicate column name"))
        {
            logger.LogInformation($"Колонка {column.Key} уже существует (игнорируем ошибку дублирования)");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, $"Не удалось добавить колонку {column.Key}");
        }
    }
}




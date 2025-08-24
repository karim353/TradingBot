using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

using Telegram.Bot;
using TradingBot.Services;
using TradingBot.Services.Interfaces;
using TradingBot.Models;
using TradingBot.Middleware;
using Microsoft.Data.Sqlite;
using Prometheus;
using Serilog;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;


var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, cfg) => cfg.ReadFrom.Configuration(context.Configuration))
    .ConfigureWebHostDefaults(webBuilder =>
    {
        var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://0.0.0.0:5000";
        webBuilder.UseUrls(urls);
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
            app.UseHttpMetrics();
            
            // Добавляем middleware для сбора метрик HTTP-запросов
            app.UseMetricsMiddleware();
            
            app.UseEndpoints(endpoints =>
            {
                // Простые эндпоинты для тестирования
                endpoints.MapGet("/test", () => "Test endpoint works!");
                
                // Метрики Prometheus
                endpoints.MapMetrics();
                
                // Health checks
                endpoints.MapHealthChecks("/health");
                
                // Расширенный дашборд метрик
                endpoints.MapGet("/metrics-dashboard", context =>
                {
                    context.Response.Redirect("/metrics-dashboard.html");
                    return Task.CompletedTask;
                });
                
                // Корневой эндпоинт - перенаправляем на дашборд
                endpoints.MapGet("/", context =>
                {
                    logger.LogInformation("🏠 Главная страница от {RemoteIpAddress}", context.Connection.RemoteIpAddress);
                    context.Response.Redirect("/index.html");
                    return Task.CompletedTask;
                });
            });
            
            logger.LogInformation("🌐 Веб-сервер настроен и запущен на {Urls}", urls);
        });
    })
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
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
        // Redis кеширование
        bool redisEnabled = config.GetValue<bool>("Caching:Redis:Enabled", false);
        if (redisEnabled)
        {
            string redisConnection = config.GetConnectionString("Redis") ?? "localhost:6379";
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
            });
            services.AddScoped<IRedisCacheService, RedisCacheService>();
            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {
            // Fallback на MemoryCache
            services.AddScoped<ICacheService, MemoryCacheService>();
        }
        
        // Всегда регистрируем IMemoryCache для совместимости
        services.AddMemoryCache();
        
        services.AddScoped<TradeRepository>();
        services.AddScoped<UserSettingsService>();

        // HTTP клиенты для Notion API с Polly политиками
        services.AddNotionHttpClients();
        
        // Фабрика HTTP клиентов для персональных настроек Notion
        services.AddSingleton<NotionHttpClientFactory>();
        
        // Сервисы для работы с Notion
        services.AddScoped<PersonalNotionService>();
        services.AddScoped<NotionSettingsService>();

        
        // Добавляем NotionService с конфигурацией
        services.AddScoped<NotionService>(provider =>
        {
            var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("NotionClient");
            var databaseId = config["Notion:DatabaseId"] ?? throw new Exception("Notion DatabaseId not configured.");
            var logger = provider.GetRequiredService<ILogger<NotionService>>();
            return new NotionService(httpClient, databaseId, logger);
        });
        

        
        // Сервис валидации
        services.AddScoped<ValidationService>();
        
        // Сервис ограничения частоты запросов
        services.AddScoped<RateLimitingService>();
        
        // Глобальный обработчик исключений
        services.AddScoped<GlobalExceptionHandler>();
        
        // Сервис метрик Prometheus
        services.AddSingleton<IMetricsService, PrometheusMetricsService>();
        
        // Расширенный сборщик метрик
        services.AddHostedService<AdvancedMetricsCollector>();
        
        // Сервис мониторинга здоровья системы (фоновый)
        services.AddScoped<IHealthMonitoringService, HealthMonitoringService>();
        services.AddHostedService<HealthMonitoringService>();
        
        // Регистрация объединенных сервисов
        services.AddHostedService<BotService>();
        services.AddHostedService<ReportService>();
        
        // Сервис сбора системных метрик
        services.AddHostedService<SystemMetricsCollector>();
        services.AddHostedService<MetricsUpdateService>();
        services.AddHostedService<DailySummaryService>();
        
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

        // UpdateHandler с зависимостями (scoped, чтобы не захватывать scoped-зависимости в singleton)
        services.AddScoped<UpdateHandler>(provider =>
        {
            var tradeStorage = provider.GetRequiredService<ITradeStorage>();
            var pnlService = provider.GetRequiredService<PnLService>();
            var uiManager = provider.GetRequiredService<UIManager>();
            var logger = provider.GetRequiredService<ILogger<UpdateHandler>>();
            var cache = provider.GetRequiredService<ICacheService>();
            var validationService = provider.GetRequiredService<ValidationService>();
            var rateLimitingService = provider.GetRequiredService<RateLimitingService>();
            var metricsService = provider.GetRequiredService<TradingBot.Services.Interfaces.IMetricsService>();
            var exceptionHandler = provider.GetRequiredService<GlobalExceptionHandler>();
            string botId = botToken.Contains(':') ? botToken.Split(':')[0] : "bot";
            string connectionString = connection;
            return new UpdateHandler(tradeStorage, pnlService, uiManager, logger, cache, validationService, rateLimitingService, metricsService, exceptionHandler, connectionString, botId);
        });

        services.AddHostedService<AdvancedMonitoringService>();
    })
    // Логирование перенесено на Serilog через UseSerilog выше
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

static async Task EnsureDatabaseSchemaAsync(TradeContext ctx, Microsoft.Extensions.Logging.ILogger logger)
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




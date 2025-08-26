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
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Microsoft.AspNetCore.ResponseCompression;


var host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        // URL задаётся через ASPNETCORE_URLS или Kestrel-конфиг; localhost по умолчанию
        webBuilder.Configure(app =>
        {
            // Логируем запуск веб-сервера
            var logger = app.ApplicationServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("🌐 Настройка веб-сервера...");
            
            // Сжатие ответов
            app.UseResponseCompression();

            // Статические файлы для веб-интерфейса
            app.UseDefaultFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=86400";
                }
            });
            
            logger.LogInformation("🌐 Статические файлы настроены");
            
            app.UseHttpMetrics();
            app.UseRouting();
            
            app.UseEndpoints(endpoints =>
            {
                // Простые эндпоинты для тестирования
                endpoints.MapGet("/test", () => "Test endpoint works!");
                
                // Экспорт метрик Prometheus
                endpoints.MapMetrics();
                
                // Liveness (просто жив)
                endpoints.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                {
                    Predicate = _ => false
                });
                // Readiness (готовность зависимостей)
                endpoints.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                {
                    Predicate = reg => reg.Tags.Contains("readiness")
                });
                
                // Back-compat общий health
                endpoints.MapGet("/health", async context =>
                {
                    context.Response.Redirect("/health/ready");
                });
                
                // Telegram webhook endpoint (prod)
                var config = app.ApplicationServices.GetRequiredService<IConfiguration>();
                if (bool.TryParse(config["Telegram:UseWebhook"], out var useWebhook) && useWebhook)
                {
                    endpoints.MapPost("/telegram/webhook", async context =>
                    {
                        var bot = context.RequestServices.GetRequiredService<Telegram.Bot.ITelegramBotClient>();
                        using var scope = context.RequestServices.CreateScope();
                        var handler = scope.ServiceProvider.GetRequiredService<UpdateHandler>();
                        var update = await System.Text.Json.JsonSerializer.DeserializeAsync<Telegram.Bot.Types.Update>(context.Request.Body);
                        if (update != null)
                        {
                            await handler.HandleUpdateAsync(bot, update, context.RequestAborted);
                        }
                        context.Response.StatusCode = 200;
                    });
                }

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
        // OpenTelemetry (Traces + Metrics)
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("TradingBot"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter());

        // Response compression
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<GzipCompressionProvider>();
        });

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
        // Всегда регистрируем IMemoryCache (нужно для RateLimitingService и др.)
        services.AddMemoryCache();
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
        
        services.AddScoped<TradeRepository>();
        services.AddScoped<UserSettingsService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<UserSettingsService>>();
            var personalNotion = provider.GetRequiredService<PersonalNotionService>();
            return new UserSettingsService(logger, connection, personalNotion);
        });

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
        
        // Сервис мониторинга здоровья системы (фоновый)
        services.AddScoped<IHealthMonitoringService, HealthMonitoringService>();
        services.AddHostedService<HealthMonitoringService>();
        
        // Регистрация объединенных сервисов
        bool useWebhook = bool.TryParse(config["Telegram:UseWebhook"], out var wh) && wh;
        if (!useWebhook)
        {
            services.AddHostedService<BotService>();
        }
        services.AddHostedService<ReportService>();
        
        // Сервис сбора системных метрик
        services.AddHostedService<SystemMetricsCollector>();
        services.AddHostedService<MetricsUpdateService>();
        
        // Новые сервисы мониторинга и уведомлений
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IPerformanceMetricsService, PerformanceMetricsService>();
        services.AddScoped<IAdvancedMonitoringService, AdvancedMonitoringService>();
        
        // Конфигурация новых сервисов
        services.Configure<NotificationSettings>(
            config.GetSection("NotificationSettings"));
        services.Configure<MonitoringSettings>(
            config.GetSection("MonitoringSettings"));
        
        // Добавляем health checks: liveness / readiness
        services.AddHealthChecks()
            .AddCheck<HealthCheckService>("database_health", tags: new[] { "readiness", "database" })
            .AddDbContextCheck<TradeContext>("ef_core_health", tags: new[] { "readiness", "ef_core" });

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

        // UpdateHandler со scoped временем жизни
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

    // 2) Применяем миграции единообразно
    try
    {
        var db = sp.GetRequiredService<TradeContext>();
        await db.Database.MigrateAsync();
        logger.LogInformation("EF Core миграции применены успешно (или отсутствуют).");
    }
    catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("duplicate column name"))
    {
        logger.LogWarning(ex, "Обнаружены дублирующиеся колонки при миграции. Продолжаем запуск приложения.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Критическая ошибка при применении миграций EF Core.");
        throw;
    }


}

await host.RunAsync();

// Удалена ручная синхронизация схемы (EnsureDatabaseSchemaAsync)




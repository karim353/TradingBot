using System;
using System.Threading.Tasks;
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
        services.AddHttpClient<NotionService>(client =>
        {
            string? notionToken = config["Notion:ApiToken"];
            if (string.IsNullOrEmpty(notionToken))
                throw new Exception("В конфигурации не указан Notion API Token.");
            client.BaseAddress = new Uri("https://api.notion.com/v1/");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {notionToken}");
            client.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");
        });
        services.AddScoped<TradeRepository>();
        services.AddSingleton<UIManager>();
        services.AddMemoryCache();

        services.AddScoped<UpdateHandler>(provider =>
        {
            var repo = provider.GetRequiredService<TradeRepository>();
            var pnlService = provider.GetRequiredService<PnLService>();
            var notionService = provider.GetRequiredService<NotionService>();
            var uiManager = provider.GetRequiredService<UIManager>();
            var logger = provider.GetRequiredService<ILogger<UpdateHandler>>();
            var cache = provider.GetRequiredService<IMemoryCache>();
            string sqliteConnectionString = connection;
            string botId = botToken.Contains(":") ? botToken.Split(':')[0] : "bot";
            return new UpdateHandler(repo, pnlService, notionService, uiManager, logger, cache, sqliteConnectionString, botId);
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

// Выполни миграции (а не EnsureCreated), иначе будут проблемы при обновлении схемы!
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

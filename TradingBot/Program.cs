using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http; // Добавлено для AddHttpClient
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
        services.AddScoped<UpdateHandler>();

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
    var db = scope.ServiceProvider.GetRequiredService<TradeContext>();
    db.Database.EnsureCreated();
}

await host.RunAsync();
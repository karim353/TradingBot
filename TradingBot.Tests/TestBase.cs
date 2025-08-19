using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradingBot.Models;
using TradingBot.Services;
using TradingBot.Services.Interfaces;
using Moq;
using Telegram.Bot;

namespace TradingBot.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected readonly IServiceProvider ServiceProvider;
        protected readonly TradeContext DbContext;
        protected readonly ILoggerFactory LoggerFactory;

        protected TestBase()
        {
            var services = new ServiceCollection();

            // Конфигурация
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"ConnectionStrings:Default", "Data Source=:memory:"},
                    {"Caching:Redis:Enabled", "false"},
                    {"Telegram:BotToken", "test_token"},
                    {"Notion:ApiToken", "test_token"},
                    {"Notion:DatabaseId", "test_db"}
                })
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            // Логирование
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            services.AddSingleton<ILoggerFactory>(LoggerFactory);

            // База данных в памяти
            services.AddDbContext<TradeContext>(options =>
            {
                options.UseInMemoryDatabase(Guid.NewGuid().ToString());
            });

            // Кеширование
            services.AddMemoryCache();
            services.AddScoped<ICacheService, MemoryCacheService>();

            // Моки для внешних сервисов
            services.AddScoped<ITelegramBotClient>(provider => Mock.Of<ITelegramBotClient>());
            services.AddScoped<IMetricsService>(provider => Mock.Of<IMetricsService>());

            // Реальные сервисы для тестирования
            services.AddScoped<TradeRepository>();
            services.AddScoped<UserSettingsService>();
            services.AddScoped<ValidationService>();

            services.AddScoped<UIManager>();

            services.AddScoped<PnLService>();

            ServiceProvider = services.BuildServiceProvider();
            DbContext = ServiceProvider.GetRequiredService<TradeContext>();
            DbContext.Database.EnsureCreated();
        }

        protected T GetService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        protected Mock<T> GetMock<T>() where T : class
        {
            return Mock.Get(ServiceProvider.GetRequiredService<T>());
        }

        protected async Task<Trade> CreateTestTradeAsync(long userId = 12345)
        {
            var trade = new Trade
            {
                UserId = userId,
                Ticker = "AAPL",
                Account = "Test Account",
                Session = "Morning",
                Position = "Long",
                Direction = "Buy",
                Context = new List<string> { "Test Context" },
                Setup = new List<string> { "Test Setup" },
                Result = "Win",
                RR = "2:1",
                Risk = 1.0m,
                PnL = 100.0m,
                Emotions = new List<string> { "Confident" },
                EntryDetails = "Test Entry",
                Note = "Test Note",
                Date = DateTime.UtcNow
            };

            DbContext.Trades.Add(trade);
            await DbContext.SaveChangesAsync();
            return trade;
        }

        protected async Task<UserSettings> CreateTestUserSettingsAsync(long userId = 12345)
        {
            var settings = new UserSettings
            {
                Language = "ru",
                NotificationsEnabled = true,
                FavoriteTickers = new List<string> { "AAPL", "GOOGL", "MSFT" },
                NotionEnabled = false
            };

            // UserSettings не имеет UserId, поэтому просто возвращаем объект
            return settings;
        }

        public void Dispose()
        {
            DbContext?.Dispose();
            LoggerFactory?.Dispose();
        }
    }
}

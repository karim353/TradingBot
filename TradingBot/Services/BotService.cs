using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace TradingBot.Services
{
    /// <summary>
    /// Фоновый сервис, отвечающий за запуск Telegram-бота и получение сообщений.
    /// </summary>
    public class BotService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BotService> _logger;

        public BotService(ITelegramBotClient botClient, IServiceProvider serviceProvider, ILogger<BotService> logger)
        {
            _botClient = botClient;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Начинаем приём обновлений от Telegram
            _botClient.StartReceiving(
                // Обработчик входящего обновления
                async (bot, update, ct) =>
                {
                    // Создаём новый scope для обработки (чтобы получить свои экземпляры зависимостей на каждый апдейт)
                    using var scope = _serviceProvider.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<UpdateHandler>();
                    await handler.HandleUpdateAsync(bot, update, ct);
                },
                // Обработчик ошибок при получении обновлений
                async (bot, exception, ct) =>
                {
                    _logger.LogError(exception, "Ошибка при получении обновления от Telegram.");
                    // Здесь можно логировать ошибки, повторно запускать получение и пр.
                },
                cancellationToken: stoppingToken
            );

            _logger.LogInformation("Telegram bot started receiving updates.");
            return Task.CompletedTask; // сервис продолжает работать, StartReceiving использует фоновые задачи
        }
    }
}

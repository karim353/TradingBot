// BotService.cs
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Регистрация команд
            await _botClient.SetMyCommands(new[]
            {
                new BotCommand { Command = "start", Description = "🚀 Запуск бота и обучение" },
                new BotCommand { Command = "menu", Description = "📋 Главное меню" },
                new BotCommand { Command = "help", Description = "🆘 Помощь" }
            }, cancellationToken: stoppingToken);

            // Получаем один экземпляр UpdateHandler
            var handler = _serviceProvider.GetRequiredService<UpdateHandler>();

            _botClient.StartReceiving(
                // Обработчик входящего обновления
                async (bot, update, ct) =>
                {
                    try
                    {
                        await handler.HandleUpdateAsync(bot, update, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при обработке обновления");
                    }
                },
                // Обработчик ошибок при получении обновлений
                (bot, exception, ct) =>
                {
                    _logger.LogError(exception, "Ошибка при получении обновления от Telegram.");
                    return Task.CompletedTask;
                },
                cancellationToken: stoppingToken
            );

            _logger.LogInformation("Telegram bot started receiving updates.");

            // Держим сервис активным, пока не будет отмены.
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
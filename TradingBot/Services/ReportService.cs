using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace TradingBot.Services
{
    /// <summary>
    /// Фоновый сервис для отправки еженедельных отчётов (каждый понедельник).
    /// </summary>
    public class ReportService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReportService> _logger;

        public ReportService(IServiceProvider serviceProvider, ILogger<ReportService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Weekly report service is running.");
            while (!stoppingToken.IsCancellationRequested)
            {
                // Вычисляем время до ближайшего понедельника 09:00
                TimeSpan delay;
                try
                {
                    var now = DateTime.Now;
                    int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
                    if (daysUntilMonday == 0) daysUntilMonday = 7;
                    var nextMonday = now.Date.AddDays(daysUntilMonday).AddHours(9);
                    delay = nextMonday - now;
                    // Ожидаем до наступления запланированного времени или отмены
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Выход из цикла, если сервис останавливается
                    break;
                }

                try
                {
                    // Создаём scope для получения зависимостей (репозиторий и бот клиент)
                    using var scope = _serviceProvider.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<TradeRepository>();
                    var bot = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

                    // Получаем всех пользователей, у которых есть сделки
                    var userIds = await repo.GetAllUserIdsAsync();
                    foreach (var uid in userIds)
                    {
                        // Считаем статистику за прошедшую неделю для каждого пользователя
                        DateTime from = DateTime.Now.AddDays(-7);
                        DateTime to = DateTime.Now;
                        var trades = await repo.GetTradesInDateRangeAsync(uid, from, to);

                        string reportMessage;
                        if (trades.Count == 0)
                        {
                            reportMessage = "Отчет: На прошлой неделе у вас не было сделок.";
                        }
                        else
                        {
                            decimal total = 0;
                            foreach (var t in trades) total += t.PnL;
                            reportMessage = $"Отчет за прошлую неделю:\nСделок: {trades.Count}\nСуммарный PnL: {total}";
                        }

                        // Отправляем личное сообщение с отчётом
                        try
                        {
                            await bot.SendMessage(uid, reportMessage);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Не удалось отправить еженедельный отчет пользователю {uid}.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при формировании/отправке еженедельных отчётов.");
                }
                // Цикл повторяется, снова вычисляем задержку до следующего понедельника
            }
        }
    }
}

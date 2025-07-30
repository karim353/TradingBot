using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace TradingBot.Services
{
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
                TimeSpan delay;
                try
                {
                    var now = DateTime.Now;
                    int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
                    if (daysUntilMonday == 0) daysUntilMonday = 7;
                    var nextMonday = now.Date.AddDays(daysUntilMonday).AddHours(9);
                    delay = nextMonday - now;
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<TradeRepository>();
                    var bot = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

                    var userIds = await repo.GetAllUserIdsAsync();
                    foreach (var uid in userIds)
                    {
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
                            decimal totalPnL = trades.Sum(t => t.PnL);
                            int totalTrades = trades.Count;
                            int profitableTrades = trades.Count(t => t.PnL > 0);
                            decimal winRate = totalTrades > 0 ? (decimal)profitableTrades / totalTrades * 100 : 0;
                            reportMessage = $"Отчет за прошлую неделю:\nСделок: {totalTrades}\nСуммарный PnL: {totalPnL:F2}%\nПрибыльных сделок: {profitableTrades}\nWinrate: {winRate:F2}%";


                            if (winRate > 50)
                                reportMessage += "\nПоздравляем! Ваш winrate выше 50%! Продолжайте в том же духе!";
                            else if (winRate < 30)
                                reportMessage += "\nWinrate низкий. Возможно, стоит пройти обучение?";
                        }

                        try
                        {
                            await bot.SendMessage(uid, reportMessage, cancellationToken: stoppingToken);
                            _logger.LogInformation("Weekly report sent to user {UserId}", uid);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to send weekly report to user {UserId}", uid);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating/sending weekly reports");
                }
            }
        }
    }
}
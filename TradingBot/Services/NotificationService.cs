using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TradingBot.Models;

namespace TradingBot.Services
{
    public class NotificationSettings
    {
        public bool EnableTradeNotifications { get; set; } = true;
        public bool EnableErrorNotifications { get; set; } = true;
        public bool EnablePerformanceNotifications { get; set; } = true;
        public bool EnableWeeklyReports { get; set; } = true;
        public TimeSpan OptimalNotificationTime { get; set; } = TimeSpan.FromHours(9); // 9:00 утра
        public int MaxNotificationsPerHour { get; set; } = 10;
    }

    public interface INotificationService
    {
        Task SendTradeNotificationAsync(Trade trade, string message, NotificationType type);
        Task SendErrorNotificationAsync(string error, string context, long userId);
        Task SendPerformanceNotificationAsync(string metric, double value, string threshold);
        Task SendWeeklyReportAsync(long userId, WeeklyReport report);
        Task<bool> RequestNotificationPermissionAsync(long userId);
        Task UpdateNotificationPreferencesAsync(long userId, NotificationSettings settings);
    }

    public enum NotificationType
    {
        TradeCreated,
        TradeUpdated,
        TradeClosed,
        ProfitTarget,
        StopLoss,
        WeeklySummary,
        Error,
        Performance
    }

    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly NotificationSettings _settings;
        private readonly Dictionary<long, DateTime> _lastNotificationTime = new();
        private readonly Dictionary<long, int> _notificationsThisHour = new();

        public NotificationService(ILogger<NotificationService> logger, IOptions<NotificationSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task SendTradeNotificationAsync(Trade trade, string message, NotificationType type)
        {
            try
            {
                if (!_settings.EnableTradeNotifications)
                    return;

                if (!await CanSendNotificationAsync(trade.UserId))
                    return;

                var notification = CreateTradeNotification(trade, message, type);
                await SendTelegramNotificationAsync(trade.UserId, notification);
                
                UpdateNotificationCounters(trade.UserId);
                _logger.LogInformation("Уведомление о сделке отправлено пользователю {UserId}: {Message}", trade.UserId, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки уведомления о сделке пользователю {UserId}", trade.UserId);
            }
        }

        public async Task SendErrorNotificationAsync(string error, string context, long userId)
        {
            try
            {
                if (!_settings.EnableErrorNotifications)
                    return;

                var notification = $"🚨 Ошибка в {context}\n\n{error}\n\n⏰ {DateTime.Now:HH:mm:ss}";
                await SendTelegramNotificationAsync(userId, notification);
                
                _logger.LogWarning("Уведомление об ошибке отправлено пользователю {UserId}: {Error}", userId, error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки уведомления об ошибке пользователю {UserId}", userId);
            }
        }

        public async Task SendPerformanceNotificationAsync(string metric, double value, string threshold)
        {
            try
            {
                if (!_settings.EnablePerformanceNotifications)
                    return;

                var notification = $"⚡ Уведомление о производительности\n\n" +
                                $"Метрика: {metric}\n" +
                                $"Значение: {value:F2}\n" +
                                $"Порог: {threshold}\n" +
                                $"⏰ {DateTime.Now:HH:mm:ss}";

                // Отправляем администраторам
                var adminUserIds = GetAdminUserIds();
                foreach (var adminId in adminUserIds)
                {
                    await SendTelegramNotificationAsync(adminId, notification);
                }

                _logger.LogInformation("Уведомление о производительности отправлено: {Metric} = {Value}", metric, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки уведомления о производительности");
            }
        }

        public async Task SendWeeklyReportAsync(long userId, WeeklyReport report)
        {
            try
            {
                if (!_settings.EnableWeeklyReports)
                    return;

                var notification = CreateWeeklyReportNotification(report);
                await SendTelegramNotificationAsync(userId, notification);
                
                _logger.LogInformation("Еженедельный отчет отправлен пользователю {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки еженедельного отчета пользователю {UserId}", userId);
            }
        }

        public async Task<bool> RequestNotificationPermissionAsync(long userId)
        {
            try
            {
                var message = "🔔 Хотите получать уведомления о важных событиях?\n\n" +
                             "✅ Торговые сделки\n" +
                             "📊 Еженедельные отчеты\n" +
                             "⚡ Уведомления о производительности\n\n" +
                             "Нажмите /notifications_on для включения";

                await SendTelegramNotificationAsync(userId, message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка запроса разрешения на уведомления для пользователя {UserId}", userId);
                return false;
            }
        }

        public async Task UpdateNotificationPreferencesAsync(long userId, NotificationSettings settings)
        {
            try
            {
                var message = "⚙️ Настройки уведомлений обновлены:\n\n" +
                             $"🔔 Торговые сделки: {(settings.EnableTradeNotifications ? "✅" : "❌")}\n" +
                             $"🚨 Ошибки: {(settings.EnableErrorNotifications ? "✅" : "❌")}\n" +
                             $"⚡ Производительность: {(settings.EnablePerformanceNotifications ? "✅" : "❌")}\n" +
                             $"📊 Еженедельные отчеты: {(settings.EnableWeeklyReports ? "✅" : "❌")}\n\n" +
                             $"⏰ Оптимальное время: {settings.OptimalNotificationTime:HH:mm}";

                await SendTelegramNotificationAsync(userId, message);
                
                _logger.LogInformation("Настройки уведомлений обновлены для пользователя {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления настроек уведомлений для пользователя {UserId}", userId);
            }
        }

        private string CreateTradeNotification(Trade trade, string message, NotificationType type)
        {
            var emoji = type switch
            {
                NotificationType.TradeCreated => "🆕",
                NotificationType.TradeUpdated => "✏️",
                NotificationType.TradeClosed => "🔒",
                NotificationType.ProfitTarget => "🎯",
                NotificationType.StopLoss => "🛑",
                _ => "📊"
            };

            return $"{emoji} {message}\n\n" +
                   $"Тикер: {trade.Ticker}\n" +
                   $"Направление: {trade.Direction}\n" +
                   $"PnL: {trade.PnL:C}\n" +
                   $"Риск: {trade.Risk}%\n" +
                   $"Дата: {trade.Date:dd.MM.yyyy HH:mm}\n\n" +
                   $"⏰ {DateTime.Now:HH:mm:ss}";
        }

        private string CreateWeeklyReportNotification(WeeklyReport report)
        {
            return $"📊 Еженедельный отчет {report.WeekStart:dd.MM.yyyy} - {report.WeekEnd:dd.MM.yyyy}\n\n" +
                   $"📈 Всего сделок: {report.TotalTrades}\n" +
                   $"✅ Прибыльных: {report.ProfitableTrades}\n" +
                   $"❌ Убыточных: {report.LossMakingTrades}\n" +
                   $"💰 Общий PnL: {report.TotalPnL:C}\n" +
                   $"📊 Винрейт: {report.WinRate:P1}\n" +
                   $"⚡ Средний PnL: {report.AveragePnL:C}\n\n" +
                   $"🔝 Лучшая сделка: {report.BestTrade?.Ticker} ({report.BestTrade?.PnL:C})\n" +
                   $"📉 Худшая сделка: {report.WorstTrade?.Ticker} ({report.WorstTrade?.PnL:C})\n\n" +
                   $"⏰ {DateTime.Now:HH:mm:ss}";
        }

        private async Task SendTelegramNotificationAsync(long userId, string message)
        {
            // Здесь будет интеграция с Telegram Bot API
            // Пока просто логируем
            _logger.LogInformation("Telegram уведомление для {UserId}: {Message}", userId, message);
            await Task.Delay(100); // Имитация отправки
        }

        private async Task<bool> CanSendNotificationAsync(long userId)
        {
            var now = DateTime.Now;
            
            // Проверяем лимит уведомлений в час
            if (_notificationsThisHour.TryGetValue(userId, out var count) && count >= _settings.MaxNotificationsPerHour)
            {
                _logger.LogDebug("Достигнут лимит уведомлений в час для пользователя {UserId}", userId);
                return false;
            }

            // Проверяем время последнего уведомления (минимум 1 минута между уведомлениями)
            if (_lastNotificationTime.TryGetValue(userId, out var lastTime))
            {
                if (now - lastTime < TimeSpan.FromMinutes(1))
                {
                    _logger.LogDebug("Слишком частое уведомление для пользователя {UserId}", userId);
                    return false;
                }
            }

            return true;
        }

        private void UpdateNotificationCounters(long userId)
        {
            var now = DateTime.Now;
            
            // Обновляем время последнего уведомления
            _lastNotificationTime[userId] = now;
            
            // Обновляем счетчик уведомлений в час
            if (!_notificationsThisHour.ContainsKey(userId))
                _notificationsThisHour[userId] = 0;
            
            _notificationsThisHour[userId]++;
            
            // Сбрасываем счетчик каждый час
            if (now.Minute == 0)
            {
                _notificationsThisHour[userId] = 1;
            }
        }

        private List<long> GetAdminUserIds()
        {
            // Здесь можно получить список администраторов из конфигурации или БД
            return new List<long> { 123456789 }; // Пример
        }
    }

    public class WeeklyReport
    {
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
        public int TotalTrades { get; set; }
        public int ProfitableTrades { get; set; }
        public int LossMakingTrades { get; set; }
        public decimal TotalPnL { get; set; }
        public double WinRate { get; set; }
        public decimal AveragePnL { get; set; }
        public Trade? BestTrade { get; set; }
        public Trade? WorstTrade { get; set; }
    }
}

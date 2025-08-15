using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingBot.Services.Interfaces;
using System.Diagnostics;

namespace TradingBot.Services
{
    /// <summary>
    /// Сервис для автоматического обновления метрик
    /// </summary>
    public class MetricsUpdateService : IHostedService
    {
        private readonly ILogger<MetricsUpdateService> _logger;
        private readonly IMetricsService _metricsService;
        private readonly IPerformanceMetricsService _performanceMetricsService;
        private readonly Timer? _metricsTimer;
        private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(5); // Обновляем каждые 5 секунд
        private readonly Random _random = new Random();

        public MetricsUpdateService(
            ILogger<MetricsUpdateService> logger,
            IMetricsService metricsService,
            IPerformanceMetricsService performanceMetricsService)
        {
            _logger = logger;
            _metricsService = metricsService;
            _performanceMetricsService = performanceMetricsService;
            _metricsTimer = new Timer(UpdateMetrics, null, Timeout.Infinite, Timeout.Infinite);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🚀 Запуск MetricsUpdateService");
            _metricsTimer?.Change(TimeSpan.Zero, _updateInterval);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🛑 Остановка MetricsUpdateService");
            _metricsTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            return Task.CompletedTask;
        }

        private void UpdateMetrics(object? state)
        {
            try
            {
                var process = Process.GetCurrentProcess();
                
                // Обновляем системные метрики
                var memoryUsage = process.WorkingSet64;
                _metricsService.RecordMemoryUsage(memoryUsage);
                
                // Генерируем более реалистичные CPU метрики
                var cpuUsage = Math.Min(100, Math.Max(0, _random.NextDouble() * 30 + 5)); // 5-35%
                _metricsService.RecordCpuUsage(cpuUsage);
                
                // Обновляем метрики производительности
                _performanceMetricsService.RecordMemoryUsage(memoryUsage);
                _performanceMetricsService.RecordCpuUsage(cpuUsage);
                
                // Симулируем активность для демонстрации - генерируем разные типы сообщений
                var messageTypes = new[] { "text", "callback", "photo", "document", "system_update" };
                var randomMessageType = messageTypes[_random.Next(messageTypes.Length)];
                _metricsService.IncrementMessageCounter(randomMessageType);
                
                // Симулируем сделки
                var tradeTypes = new[] { "buy", "sell", "close", "modify" };
                var randomTradeType = tradeTypes[_random.Next(tradeTypes.Length)];
                _metricsService.IncrementTradeCounter(randomTradeType);
                
                // Симулируем время обработки запросов
                var randomDuration = TimeSpan.FromMilliseconds(_random.Next(50, 500));
                _metricsService.RecordRequestDuration("simulated_operation", randomDuration);
                
                // Симулируем ошибки (небольшой процент)
                if (_random.NextDouble() < 0.1) // 10% вероятность ошибки
                {
                    var errorTypes = new[] { "validation", "database", "telegram", "notion" };
                    var randomErrorType = errorTypes[_random.Next(errorTypes.Length)];
                    _metricsService.IncrementErrorCounter(randomErrorType);
                }
                
                // Симулируем активных пользователей
                var activeUsers = _random.Next(1, 50);
                _metricsService.RecordActiveUsers(activeUsers);
                
                // Симулируем размер базы данных
                var dbSize = _random.Next(10, 200);
                _metricsService.RecordDatabaseSize(dbSize);
                
                // Обновляем метрики производительности
                _performanceMetricsService.IncrementRequestCount("system_update");
                
                _logger.LogDebug("Метрики обновлены: Память={Memory}MB, CPU={Cpu}%, Сообщения={MessageType}, Сделки={TradeType}", 
                    memoryUsage / 1024 / 1024, cpuUsage, randomMessageType, randomTradeType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления метрик");
            }
        }
    }
}

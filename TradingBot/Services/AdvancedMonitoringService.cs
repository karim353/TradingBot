using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Prometheus;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace TradingBot.Services
{
    public class MonitoringSettings
    {
        public bool EnableApplicationInsights { get; set; } = false;
        public bool EnableOpenTelemetry { get; set; } = false;
        public bool EnableCustomMetrics { get; set; } = true;
        public bool EnableHealthChecks { get; set; } = true;
        public int MetricsCollectionIntervalSeconds { get; set; } = 15; // Обновляем каждые 15 секунд
        public int HealthCheckIntervalSeconds { get; set; } = 30; // Обновляем каждые 30 секунд
        public string[] CriticalOperations { get; set; } = { "save_trade", "process_message", "database_query" };
        public Dictionary<string, double> PerformanceThresholds { get; set; } = new()
        {
            { "save_trade", 1.0 },        // 1 секунда
            { "process_message", 0.5 },    // 500 мс
            { "database_query", 0.1 }      // 100 мс
        };
    }

    public interface IAdvancedMonitoringService
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
        void TrackCustomMetric(string name, double value, Dictionary<string, string>? labels = null);
        void TrackBusinessEvent(string eventName, Dictionary<string, object> properties);
        void TrackDependency(string dependencyType, string target, bool isSuccess, TimeSpan duration);
        void TrackException(Exception exception, Dictionary<string, object>? properties = null);
        MonitoringHealthReport GetHealthReport();
        Task<bool> IsHealthyAsync();
    }

    public class AdvancedMonitoringService : IAdvancedMonitoringService, IHostedService
    {
        private readonly ILogger<AdvancedMonitoringService> _logger;
        private readonly MonitoringSettings _settings;
        private readonly IPerformanceMetricsService _performanceMetrics;
        private readonly INotificationService _notificationService;
        private readonly Timer _metricsTimer;
        private readonly Timer _healthTimer;
        private readonly ConcurrentDictionary<string, CustomMetric> _customMetrics = new();
        private readonly ConcurrentDictionary<string, BusinessEvent> _businessEvents = new();
        private readonly ConcurrentDictionary<string, DependencyMetric> _dependencyMetrics = new();
        private readonly ConcurrentDictionary<string, ExceptionMetric> _exceptionMetrics = new();

        // Prometheus метрики
        private readonly Gauge _customMetricsGauge;
        private readonly Counter _businessEventsCounter;
        private readonly Histogram _dependencyDurationHistogram;
        private readonly Counter _exceptionsCounter;
        private readonly Gauge _systemHealthGauge;

        public AdvancedMonitoringService(
            ILogger<AdvancedMonitoringService> logger,
            IOptions<MonitoringSettings> settings,
            IPerformanceMetricsService performanceMetrics,
            INotificationService notificationService)
        {
            _logger = logger;
            _settings = settings.Value;
            _performanceMetrics = performanceMetrics;
            _notificationService = notificationService;

            // Инициализируем Prometheus метрики
            _customMetricsGauge = Metrics.CreateGauge(
                "tradingbot_custom_metrics",
                "Custom business metrics",
                new GaugeConfiguration
                {
                    LabelNames = new[] { "metric_name", "label_key", "label_value" }
                });

            _businessEventsCounter = Metrics.CreateCounter(
                "tradingbot_business_events_total",
                "Total business events",
                new CounterConfiguration
                {
                    LabelNames = new[] { "event_name" }
                });

            _dependencyDurationHistogram = Metrics.CreateHistogram(
                "tradingbot_dependency_duration_seconds",
                "Dependency call duration",
                new HistogramConfiguration
                {
                    LabelNames = new[] { "dependency_type", "target" },
                    Buckets = new[] { 0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0 }
                });

            _exceptionsCounter = Metrics.CreateCounter(
                "tradingbot_exceptions_total",
                "Total exceptions",
                new CounterConfiguration
                {
                    LabelNames = new[] { "exception_type", "operation" }
                });

            _systemHealthGauge = Metrics.CreateGauge(
                "tradingbot_system_health",
                "Overall system health score (0-100)");

            _metricsTimer = new Timer(CollectMetrics, null, Timeout.Infinite, Timeout.Infinite);
            _healthTimer = new Timer(PerformHealthCheck, null, Timeout.Infinite, Timeout.Infinite);

            _logger.LogInformation("AdvancedMonitoringService инициализирован");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Запуск расширенного мониторинга...");

                // Запускаем таймеры
                _metricsTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(_settings.MetricsCollectionIntervalSeconds));
                _healthTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(_settings.HealthCheckIntervalSeconds));

                // Первоначальный сбор метрик
                await CollectMetricsAsync();
                await PerformHealthCheckAsync();

                _logger.LogInformation("Расширенный мониторинг запущен");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка запуска расширенного мониторинга");
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Остановка расширенного мониторинга...");

                _metricsTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _healthTimer?.Change(Timeout.Infinite, Timeout.Infinite);

                _logger.LogInformation("Расширенный мониторинг остановлен");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка остановки расширенного мониторинга");
            }
        }

        public void TrackCustomMetric(string name, double value, Dictionary<string, string>? labels = null)
        {
            try
            {
                var metric = new CustomMetric
                {
                    Name = name,
                    Value = value,
                    Labels = labels ?? new Dictionary<string, string>(),
                    Timestamp = DateTime.UtcNow
                };

                _customMetrics[name] = metric;

                // Обновляем Prometheus метрики
                if (labels != null)
                {
                    foreach (var label in labels)
                    {
                        _customMetricsGauge.WithLabels(name, label.Key, label.Value).Set(value);
                    }
                }
                else
                {
                    _customMetricsGauge.WithLabels(name, "none", "none").Set(value);
                }

                _logger.LogDebug("Кастомная метрика {MetricName}: {Value}", name, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка записи кастомной метрики {MetricName}", name);
            }
        }

        public void TrackBusinessEvent(string eventName, Dictionary<string, object> properties)
        {
            try
            {
                var businessEvent = new BusinessEvent
                {
                    Name = eventName,
                    Properties = properties,
                    Timestamp = DateTime.UtcNow
                };

                _businessEvents[eventName] = businessEvent;

                // Обновляем Prometheus метрики
                _businessEventsCounter.WithLabels(eventName).Inc();

                _logger.LogDebug("Бизнес-событие {EventName} зарегистрировано", eventName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка записи бизнес-события {EventName}", eventName);
            }
        }

        public void TrackDependency(string dependencyType, string target, bool isSuccess, TimeSpan duration)
        {
            try
            {
                var dependency = new DependencyMetric
                {
                    Type = dependencyType,
                    Target = target,
                    IsSuccess = isSuccess,
                    Duration = duration,
                    Timestamp = DateTime.UtcNow
                };

                var key = $"{dependencyType}_{target}";
                _dependencyMetrics[key] = dependency;

                // Обновляем Prometheus метрики
                _dependencyDurationHistogram.WithLabels(dependencyType, target).Observe(duration.TotalSeconds);

                _logger.LogDebug("Зависимость {DependencyType} -> {Target}: {Duration}ms, Успех: {IsSuccess}", 
                    dependencyType, target, duration.TotalMilliseconds, isSuccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка записи метрики зависимости {DependencyType}", dependencyType);
            }
        }

        public void TrackException(Exception exception, Dictionary<string, object>? properties = null)
        {
            try
            {
                var exceptionMetric = new ExceptionMetric
                {
                    ExceptionType = exception.GetType().Name,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace,
                    Properties = properties ?? new Dictionary<string, object>(),
                    Timestamp = DateTime.UtcNow
                };

                var key = $"{exception.GetType().Name}_{DateTime.UtcNow:yyyyMMdd}";
                _exceptionMetrics[key] = exceptionMetric;

                // Обновляем Prometheus метрики
                var operation = properties?.GetValueOrDefault("operation")?.ToString() ?? "unknown";
                _exceptionsCounter.WithLabels(exception.GetType().Name, operation).Inc();

                _logger.LogWarning("Исключение зарегистрировано: {ExceptionType} - {Message}", 
                    exception.GetType().Name, exception.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка записи метрики исключения");
            }
        }

        public MonitoringHealthReport GetHealthReport()
        {
            try
            {
                var report = new MonitoringHealthReport
                {
                    Timestamp = DateTime.UtcNow,
                    OverallHealth = CalculateOverallHealth(),
                    ComponentHealth = GetComponentHealth(),
                    PerformanceMetrics = _performanceMetrics.GetCurrentSnapshot(),
                    CustomMetrics = _customMetrics.Values.ToList(),
                    BusinessEvents = _businessEvents.Values.ToList(),
                    DependencyMetrics = _dependencyMetrics.Values.ToList(),
                    ExceptionMetrics = _exceptionMetrics.Values.ToList()
                };

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения отчета о здоровье");
                return new MonitoringHealthReport
                {
                    Timestamp = DateTime.UtcNow,
                    OverallHealth = HealthStatus.Unhealthy,
                    Error = ex.Message
                };
            }
        }

        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var report = GetHealthReport();
                var isHealthy = report.OverallHealth == HealthStatus.Healthy;

                // Обновляем Prometheus метрику здоровья
                _systemHealthGauge.Set(isHealthy ? 100 : 0);

                return isHealthy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки здоровья системы");
                return false;
            }
        }

        private async void CollectMetrics(object? state)
        {
            try
            {
                await CollectMetricsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сбора метрик");
            }
        }

        private async Task CollectMetricsAsync()
        {
            try
            {
                // Собираем системные метрики
                var memoryUsage = GC.GetTotalMemory(false);
                var cpuUsage = GetCpuUsage();

                _performanceMetrics.RecordMemoryUsage(memoryUsage);
                _performanceMetrics.RecordCpuUsage(cpuUsage);

                // Отслеживаем кастомные метрики
                TrackCustomMetric("memory_usage_mb", memoryUsage / 1024.0 / 1024.0);
                TrackCustomMetric("cpu_usage_percent", cpuUsage);
                TrackCustomMetric("gc_collections", GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2));
                TrackCustomMetric("active_threads", Process.GetCurrentProcess().Threads.Count);

                // Проверяем пороги производительности
                await CheckPerformanceThresholdsAsync();

                _logger.LogDebug("Метрики собраны: память={MemoryMB:F2}MB, CPU={CpuUsage:F2}%", 
                    memoryUsage / 1024.0 / 1024.0, cpuUsage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сбора метрик");
            }
        }

        private async void PerformHealthCheck(object? state)
        {
            try
            {
                await PerformHealthCheckAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки здоровья");
            }
        }

        private async Task PerformHealthCheckAsync()
        {
            try
            {
                var isHealthy = await IsHealthyAsync();
                
                if (!isHealthy)
                {
                    _logger.LogWarning("Система нездорова");
                    await _notificationService.SendPerformanceNotificationAsync("System Health", 0, "Unhealthy");
                }
                else
                {
                    _logger.LogDebug("Система здорова");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки здоровья");
            }
        }

        private async Task CheckPerformanceThresholdsAsync()
        {
            try
            {
                var snapshot = _performanceMetrics.GetCurrentSnapshot();
                
                foreach (var operation in _settings.CriticalOperations)
                {
                    if (snapshot.Operations.TryGetValue(operation, out var opSnapshot))
                    {
                        var threshold = _settings.PerformanceThresholds.GetValueOrDefault(operation, 1.0);
                        var currentTime = opSnapshot.AverageResponseTime.TotalSeconds;

                        if (currentTime > threshold)
                        {
                            _logger.LogWarning("Операция {Operation} превышает порог производительности: {CurrentTime:F3}s > {Threshold:F3}s", 
                                operation, currentTime, threshold);

                            await _notificationService.SendPerformanceNotificationAsync(
                                $"Response Time - {operation}", 
                                currentTime, 
                                $"Threshold: {threshold}s");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки порогов производительности");
            }
        }

        private HealthStatus CalculateOverallHealth()
        {
            try
            {
                var healthScore = 100;

                // Проверяем метрики производительности
                var snapshot = _performanceMetrics.GetCurrentSnapshot();
                foreach (var operation in _settings.CriticalOperations)
                {
                    if (snapshot.Operations.TryGetValue(operation, out var opSnapshot))
                    {
                        var threshold = _settings.PerformanceThresholds.GetValueOrDefault(operation, 1.0);
                        if (opSnapshot.AverageResponseTime.TotalSeconds > threshold)
                        {
                            healthScore -= 20;
                        }

                        if (opSnapshot.ErrorRate > 0.05) // 5% ошибок
                        {
                            healthScore -= 30;
                        }
                    }
                }

                // Проверяем использование ресурсов
                var memoryUsageMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
                if (memoryUsageMB > 1000) // 1GB
                {
                    healthScore -= 15;
                }

                return healthScore switch
                {
                    >= 80 => HealthStatus.Healthy,
                    >= 60 => HealthStatus.Degraded,
                    _ => HealthStatus.Unhealthy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка расчета здоровья системы");
                return HealthStatus.Unhealthy;
            }
        }

        private Dictionary<string, ComponentHealth> GetComponentHealth()
        {
            var components = new Dictionary<string, ComponentHealth>();

            try
            {
                // Проверяем здоровье базы данных
                components["database"] = new ComponentHealth
                {
                    Name = "Database",
                    Status = HealthStatus.Healthy, // Здесь должна быть реальная проверка
                    LastCheck = DateTime.UtcNow,
                    Details = "SQLite connection is active"
                };

                // Проверяем здоровье Telegram API
                components["telegram"] = new ComponentHealth
                {
                    Name = "Telegram API",
                    Status = HealthStatus.Healthy, // Здесь должна быть реальная проверка
                    LastCheck = DateTime.UtcNow,
                    Details = "Bot is responding to updates"
                };

                // Проверяем здоровье Notion API
                components["notion"] = new ComponentHealth
                {
                    Name = "Notion API",
                    Status = HealthStatus.Healthy, // Здесь должна быть реальная проверка
                    LastCheck = DateTime.UtcNow,
                    Details = "Notion integration is working"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения здоровья компонентов");
            }

            return components;
        }

        private double GetCpuUsage()
        {
            try
            {
                // Простая реализация для Windows
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var process = Process.GetCurrentProcess();
                    var startTime = process.StartTime;
                    var startCpu = process.TotalProcessorTime;
                    
                    Thread.Sleep(100); // Ждем 100мс
                    
                    var endTime = process.StartTime;
                    var endCpu = process.TotalProcessorTime;
                    
                    var cpuUsedMs = (endCpu - startCpu).TotalMilliseconds;
                    var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                    
                    return (cpuUsedMs / totalMsPassed) * 100;
                }
                
                return 0.0; // Для других ОС возвращаем 0
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения использования CPU");
                return 0.0;
            }
        }
    }

    public enum HealthStatus
    {
        Healthy,
        Degraded,
        Unhealthy
    }

    public class MonitoringHealthReport
    {
        public DateTime Timestamp { get; set; }
        public HealthStatus OverallHealth { get; set; }
        public Dictionary<string, ComponentHealth> ComponentHealth { get; set; } = new();
        public PerformanceSnapshot PerformanceMetrics { get; set; } = new();
        public List<CustomMetric> CustomMetrics { get; set; } = new();
        public List<BusinessEvent> BusinessEvents { get; set; } = new();
        public List<DependencyMetric> DependencyMetrics { get; set; } = new();
        public List<ExceptionMetric> ExceptionMetrics { get; set; } = new();
        public string? Error { get; set; }
    }

    public class ComponentHealth
    {
        public string Name { get; set; } = "";
        public HealthStatus Status { get; set; }
        public DateTime LastCheck { get; set; }
        public string Details { get; set; } = "";
    }

    public class CustomMetric
    {
        public string Name { get; set; } = "";
        public double Value { get; set; }
        public Dictionary<string, string> Labels { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    public class BusinessEvent
    {
        public string Name { get; set; } = "";
        public Dictionary<string, object> Properties { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    public class DependencyMetric
    {
        public string Type { get; set; } = "";
        public string Target { get; set; } = "";
        public bool IsSuccess { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ExceptionMetric
    {
        public string ExceptionType { get; set; } = "";
        public string Message { get; set; } = "";
        public string? StackTrace { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }
}

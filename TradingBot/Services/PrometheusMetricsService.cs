using Prometheus;
using TradingBot.Services.Interfaces;
using System.Diagnostics;

namespace TradingBot.Services;

/// <summary>
/// Сервис метрик Prometheus для TradingBot
/// </summary>
public class PrometheusMetricsService : IMetricsService
{
    // Счетчики
    private readonly Counter _messageCounter;
    private readonly Counter _tradeCounter;
    private readonly Counter _errorCounter;
    
    // Гистограммы для времени
    private readonly Histogram _requestDuration;
    private readonly Histogram _databaseResponseTime;
    
    // Gauge для текущих значений
    private readonly Gauge _databaseSizeGauge;
    private readonly Gauge _activeUsersGauge;
    private readonly Gauge _memoryUsageGauge;
    private readonly Gauge _cpuUsageGauge;
    
    // Метки для категоризации
    private readonly string[] _messageTypes = { "text", "callback", "photo", "document" };
    private readonly string[] _tradeTypes = { "buy", "sell", "close", "modify" };
    private readonly string[] _errorTypes = { "validation", "database", "telegram", "notion" };
    private readonly string[] _operations = { "save_trade", "get_trades", "update_user", "send_message" };

    public PrometheusMetricsService()
    {
        // Инициализируем метрики
        _messageCounter = Metrics.CreateCounter(
            "tradingbot_messages_total",
            "Total number of messages processed",
            new CounterConfiguration
            {
                LabelNames = new[] { "type" }
            });

        _tradeCounter = Metrics.CreateCounter(
            "tradingbot_trades_total",
            "Total number of trades processed",
            new CounterConfiguration
            {
                LabelNames = new[] { "type" }
            });

        _errorCounter = Metrics.CreateCounter(
            "tradingbot_errors_total",
            "Total number of errors",
            new CounterConfiguration
            {
                LabelNames = new[] { "type" }
            });

        _requestDuration = Metrics.CreateHistogram(
            "tradingbot_request_duration_seconds",
            "Request duration in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "operation" },
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 10) // от 1ms до 1s
            });

        _databaseResponseTime = Metrics.CreateHistogram(
            "tradingbot_database_response_time_seconds",
            "Database response time in seconds",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 10)
            });

        _databaseSizeGauge = Metrics.CreateGauge(
            "tradingbot_database_size_mb",
            "Database size in megabytes");

        _activeUsersGauge = Metrics.CreateGauge(
            "tradingbot_active_users",
            "Number of active users");

        _memoryUsageGauge = Metrics.CreateGauge(
            "tradingbot_memory_usage_bytes",
            "Memory usage in bytes");

        _cpuUsageGauge = Metrics.CreateGauge(
            "tradingbot_cpu_usage_percentage",
            "CPU usage percentage");
    }

    public void IncrementMessageCounter(string messageType)
    {
        if (_messageTypes.Contains(messageType))
        {
            _messageCounter.WithLabels(messageType).Inc();
        }
        else
        {
            _messageCounter.WithLabels("other").Inc();
        }
    }

    public void IncrementTradeCounter(string tradeType)
    {
        if (_tradeTypes.Contains(tradeType))
        {
            _tradeCounter.WithLabels(tradeType).Inc();
        }
        else
        {
            _tradeCounter.WithLabels("other").Inc();
        }
    }

    public void RecordRequestDuration(string operation, TimeSpan duration)
    {
        if (_operations.Contains(operation))
        {
            _requestDuration.WithLabels(operation).Observe(duration.TotalSeconds);
        }
        else
        {
            _requestDuration.WithLabels("other").Observe(duration.TotalSeconds);
        }
    }

    public void RecordDatabaseSize(double sizeMB)
    {
        _databaseSizeGauge.Set(sizeMB);
    }

    public void RecordActiveUsers(int count)
    {
        _activeUsersGauge.Set(count);
    }

    public void IncrementErrorCounter(string errorType)
    {
        if (_errorTypes.Contains(errorType))
        {
            _errorCounter.WithLabels(errorType).Inc();
        }
        else
        {
            _errorCounter.WithLabels("other").Inc();
        }
    }

    public void RecordDatabaseResponseTime(TimeSpan responseTime)
    {
        _databaseResponseTime.Observe(responseTime.TotalSeconds);
    }

    public void RecordMemoryUsage(long bytesUsed)
    {
        _memoryUsageGauge.Set(bytesUsed);
    }

    public void RecordCpuUsage(double percentage)
    {
        _cpuUsageGauge.Set(percentage);
    }

    /// <summary>
    /// Получить текущие метрики системы
    /// </summary>
    public void RecordSystemMetrics()
    {
        try
        {
            // Записываем использование памяти
            var process = Process.GetCurrentProcess();
            RecordMemoryUsage(process.WorkingSet64);
            
            // Записываем использование CPU (примерное)
            var cpuUsage = GetCpuUsage();
            RecordCpuUsage(cpuUsage);
        }
        catch
        {
            // Игнорируем ошибки при сборе системных метрик
        }
    }

    private double GetCpuUsage()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var startTime = process.StartTime;
            var startCpuUsage = process.TotalProcessorTime;
            
            // Небольшая задержка для измерения
            Thread.Sleep(100);
            
            var endTime = DateTime.Now;
            var endCpuUsage = process.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            
            return Math.Min(100.0, (cpuUsedMs / totalMsPassed) * 100);
        }
        catch
        {
            return 0.0;
        }
    }
}

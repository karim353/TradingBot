using Prometheus;
using TradingBot.Services.Interfaces;
using System.Diagnostics;

namespace TradingBot.Services;

/// <summary>
/// Расширенный сервис метрик Prometheus для TradingBot
/// </summary>
public class PrometheusMetricsService : IMetricsService
{
    // ==================== БАЗОВЫЕ СЧЕТЧИКИ ====================
    private readonly Counter _messageCounter;
    private readonly Counter _tradeCounter;
    private readonly Counter _errorCounter;
    
    // ==================== ГИСТОГРАММЫ ДЛЯ ВРЕМЕНИ ====================
    private readonly Histogram _requestDuration;
    private readonly Histogram _databaseResponseTime;
    private readonly Histogram _telegramApiLatency;
    private readonly Histogram _notionApiLatency;
    private readonly Histogram _redisCacheLatency;
    private readonly Histogram _commandExecutionTime;
    private readonly Histogram _notificationDeliveryTime;
    private readonly Histogram _queueProcessingTime;
    private readonly Histogram _externalApiLatency;
    
    // ==================== GAUGE ДЛЯ ТЕКУЩИХ ЗНАЧЕНИЙ ====================
    private readonly Gauge _databaseSizeGauge;
    private readonly Gauge _activeUsersGauge;
    private readonly Gauge _memoryUsageGauge;
    private readonly Gauge _cpuUsageGauge;
    private readonly Gauge _concurrentRequestsGauge;
    private readonly Gauge _queueSizeGauge;
    private readonly Gauge _networkConnectionsGauge;
    private readonly Gauge _diskUsageGauge;
    private readonly Gauge _componentHealthGauge;
    
    // ==================== ТОРГОВЫЕ МЕТРИКИ ====================
    private readonly Histogram _tradePnLHistogram;
    private readonly Histogram _positionSizeHistogram;
    private readonly Histogram _riskRewardRatioHistogram;
    private readonly Histogram _positionHoldTimeHistogram;
    private readonly Gauge _winRateGauge;
    private readonly Gauge _averagePnLGauge;
    private readonly Gauge _maxDrawdownGauge;
    private readonly Gauge _consecutiveWinsGauge;
    private readonly Gauge _consecutiveLossesGauge;
    
    // ==================== КЭШ МЕТРИКИ ====================
    private readonly Counter _cacheHitCounter;
    private readonly Counter _cacheMissCounter;
    
    // ==================== СИСТЕМНЫЕ МЕТРИКИ ====================
    private readonly Gauge _systemUptimeGauge;
    private readonly Counter _systemRestartCounter;
    private readonly Gauge _applicationVersionGauge;
    private readonly Gauge _lastUpdateTimeGauge;
    private readonly Counter _exceptionCounter;
    private readonly Histogram _garbageCollectionTimeHistogram;
    
    // ==================== ПОЛЬЗОВАТЕЛЬСКИЕ МЕТРИКИ ====================
    private readonly Counter _userActivityCounter;
    private readonly Histogram _userSessionDurationHistogram;
    private readonly Counter _notificationCounter;
    private readonly Counter _cancelledOperationCounter;

    // ==================== МЕТКИ ДЛЯ КАТЕГОРИЗАЦИИ ====================
    private readonly string[] _messageTypes = { "text", "callback", "photo", "document", "video", "audio", "sticker", "location" };
    private readonly string[] _tradeTypes = { "buy", "sell", "close", "modify", "cancel", "partial" };
    private readonly string[] _errorTypes = { "validation", "database", "telegram", "notion", "redis", "network", "timeout", "rate_limit" };
    private readonly string[] _operations = { "save_trade", "get_trades", "update_user", "send_message", "process_callback", "sync_notion" };
    private readonly string[] _notificationTypes = { "trade_executed", "price_alert", "news_update", "system_maintenance", "error_notification" };
    private readonly string[] _componentNames = { "database", "redis", "telegram_api", "notion_api", "metrics_service", "health_check" };

    public PrometheusMetricsService()
    {
        // ==================== ИНИЦИАЛИЗАЦИЯ БАЗОВЫХ МЕТРИК ====================
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

        // ==================== ИНИЦИАЛИЗАЦИЯ ГИСТОГРАММ ====================
        _requestDuration = Metrics.CreateHistogram(
            "tradingbot_request_duration_seconds",
            "Request duration in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "operation" },
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 15) // от 1ms до 16s
            });

        _databaseResponseTime = Metrics.CreateHistogram(
            "tradingbot_database_response_time_seconds",
            "Database response time in seconds",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 12)
            });

        _telegramApiLatency = Metrics.CreateHistogram(
            "tradingbot_telegram_api_latency_seconds",
            "Telegram API latency in seconds",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 12)
            });

        _notionApiLatency = Metrics.CreateHistogram(
            "tradingbot_notion_api_latency_seconds",
            "Notion API latency in seconds",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 12)
            });

        _redisCacheLatency = Metrics.CreateHistogram(
            "tradingbot_redis_cache_latency_seconds",
            "Redis cache latency in seconds",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 10)
            });

        _commandExecutionTime = Metrics.CreateHistogram(
            "tradingbot_command_execution_time_seconds",
            "Command execution time in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "command" },
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 12)
            });

        _notificationDeliveryTime = Metrics.CreateHistogram(
            "tradingbot_notification_delivery_time_seconds",
            "Notification delivery time in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "type" },
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 10)
            });

        _queueProcessingTime = Metrics.CreateHistogram(
            "tradingbot_queue_processing_time_seconds",
            "Queue processing time in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "queue_name" },
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 10)
            });

        _externalApiLatency = Metrics.CreateHistogram(
            "tradingbot_external_api_latency_seconds",
            "External API latency in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "api_name" },
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 12)
            });

        // ==================== ИНИЦИАЛИЗАЦИЯ GAUGE ====================
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

        _concurrentRequestsGauge = Metrics.CreateGauge(
            "tradingbot_concurrent_requests",
            "Number of concurrent requests");

        _queueSizeGauge = Metrics.CreateGauge(
            "tradingbot_queue_size",
            "Queue size",
            new GaugeConfiguration
            {
                LabelNames = new[] { "queue_name" }
            });

        _networkConnectionsGauge = Metrics.CreateGauge(
            "tradingbot_network_connections",
            "Number of active network connections");

        _diskUsageGauge = Metrics.CreateGauge(
            "tradingbot_disk_usage_bytes",
            "Disk usage in bytes",
            new GaugeConfiguration
            {
                LabelNames = new[] { "path" }
            });

        _componentHealthGauge = Metrics.CreateGauge(
            "tradingbot_component_health",
            "Component health status (1 = healthy, 0 = unhealthy)",
            new GaugeConfiguration
            {
                LabelNames = new[] { "component_name" }
            });

        // ==================== ИНИЦИАЛИЗАЦИЯ ТОРГОВЫХ МЕТРИК ====================
        _tradePnLHistogram = Metrics.CreateHistogram(
            "tradingbot_trade_pnl",
            "Trade PnL distribution",
            new HistogramConfiguration
            {
                LabelNames = new[] { "ticker", "direction" },
                Buckets = Histogram.LinearBuckets(-10000, 1000, 21) // от -10k до +10k
            });

        _positionSizeHistogram = Metrics.CreateHistogram(
            "tradingbot_position_size",
            "Position size distribution",
            new HistogramConfiguration
            {
                LabelNames = new[] { "ticker" },
                Buckets = Histogram.ExponentialBuckets(0.01, 2, 15)
            });

        _riskRewardRatioHistogram = Metrics.CreateHistogram(
            "tradingbot_risk_reward_ratio",
            "Risk/Reward ratio distribution",
            new HistogramConfiguration
            {
                LabelNames = new[] { "ticker" },
                Buckets = Histogram.LinearBuckets(0, 0.5, 21) // от 0 до 10
            });

        _positionHoldTimeHistogram = Metrics.CreateHistogram(
            "tradingbot_position_hold_time_seconds",
            "Position hold time in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "ticker" },
                Buckets = Histogram.ExponentialBuckets(1, 2, 15) // от 1s до 16h
            });

        _winRateGauge = Metrics.CreateGauge(
            "tradingbot_win_rate_percentage",
            "Win rate percentage");

        _averagePnLGauge = Metrics.CreateGauge(
            "tradingbot_average_pnl",
            "Average PnL per trade");

        _maxDrawdownGauge = Metrics.CreateGauge(
            "tradingbot_max_drawdown",
            "Maximum drawdown");

        _consecutiveWinsGauge = Metrics.CreateGauge(
            "tradingbot_consecutive_wins",
            "Consecutive wins");

        _consecutiveLossesGauge = Metrics.CreateGauge(
            "tradingbot_consecutive_losses",
            "Consecutive losses");

        // ==================== ИНИЦИАЛИЗАЦИЯ КЭШ МЕТРИК ====================
        _cacheHitCounter = Metrics.CreateCounter(
            "tradingbot_cache_hits_total",
            "Total cache hits",
            new CounterConfiguration
            {
                LabelNames = new[] { "operation" }
            });

        _cacheMissCounter = Metrics.CreateCounter(
            "tradingbot_cache_misses_total",
            "Total cache misses",
            new CounterConfiguration
            {
                LabelNames = new[] { "operation" }
            });

        // ==================== ИНИЦИАЛИЗАЦИЯ СИСТЕМНЫХ МЕТРИК ====================
        _systemUptimeGauge = Metrics.CreateGauge(
            "tradingbot_system_uptime_seconds",
            "System uptime in seconds");

        _systemRestartCounter = Metrics.CreateCounter(
            "tradingbot_system_restarts_total",
            "Total system restarts");

        _applicationVersionGauge = Metrics.CreateGauge(
            "tradingbot_application_version",
            "Application version number");

        _lastUpdateTimeGauge = Metrics.CreateGauge(
            "tradingbot_last_update_timestamp",
            "Last update timestamp");

        _exceptionCounter = Metrics.CreateCounter(
            "tradingbot_exceptions_total",
            "Total exceptions",
            new CounterConfiguration
            {
                LabelNames = new[] { "exception_type", "source" }
            });

        _garbageCollectionTimeHistogram = Metrics.CreateHistogram(
            "tradingbot_gc_time_seconds",
            "Garbage collection time in seconds",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 10)
            });

        // ==================== ИНИЦИАЛИЗАЦИЯ ПОЛЬЗОВАТЕЛЬСКИХ МЕТРИК ====================
        _userActivityCounter = Metrics.CreateCounter(
            "tradingbot_user_activity_total",
            "Total user activities",
            new CounterConfiguration
            {
                LabelNames = new[] { "user_id", "action" }
            });

        _userSessionDurationHistogram = Metrics.CreateHistogram(
            "tradingbot_user_session_duration_seconds",
            "User session duration in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "user_id" },
                Buckets = Histogram.ExponentialBuckets(1, 2, 15)
            });

        _notificationCounter = Metrics.CreateCounter(
            "tradingbot_notifications_total",
            "Total notifications sent",
            new CounterConfiguration
            {
                LabelNames = new[] { "type" }
            });

        _cancelledOperationCounter = Metrics.CreateCounter(
            "tradingbot_cancelled_operations_total",
            "Total cancelled operations",
            new CounterConfiguration
            {
                LabelNames = new[] { "operation_type" }
            });

        // ==================== ИНИЦИАЛИЗАЦИЯ СИСТЕМЫ ====================
        InitializeSystemMetrics();
    }

    private void InitializeSystemMetrics()
    {
        // Устанавливаем начальные значения
        _systemUptimeGauge.Set(0);
        _systemRestartCounter.Inc(0);
        _applicationVersionGauge.Set(1.0); // Версия 1.0
        _lastUpdateTimeGauge.Set(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        
        // Устанавливаем здоровье компонентов
        foreach (var component in _componentNames)
        {
            _componentHealthGauge.WithLabels(component).Set(1); // Все компоненты здоровы
        }
    }

    // ==================== РЕАЛИЗАЦИЯ БАЗОВЫХ МЕТОДОВ ====================
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

    // ==================== РЕАЛИЗАЦИЯ ТОРГОВЫХ МЕТРИК ====================
    public void RecordTradePnL(string ticker, decimal pnl, string direction)
    {
        _tradePnLHistogram.WithLabels(ticker, direction).Observe((double)pnl);
    }

    public void RecordPositionSize(string ticker, decimal size)
    {
        _positionSizeHistogram.WithLabels(ticker).Observe((double)size);
    }

    public void RecordRiskRewardRatio(string ticker, double ratio)
    {
        _riskRewardRatioHistogram.WithLabels(ticker).Observe(ratio);
    }

    public void RecordPositionHoldTime(string ticker, TimeSpan holdTime)
    {
        _positionHoldTimeHistogram.WithLabels(ticker).Observe(holdTime.TotalSeconds);
    }

    public void RecordWinLossStreak(int consecutiveWins, int consecutiveLosses)
    {
        _consecutiveWinsGauge.Set(consecutiveWins);
        _consecutiveLossesGauge.Set(consecutiveLosses);
    }

    public void RecordWinRate(double winRate)
    {
        _winRateGauge.Set(winRate);
    }

    public void RecordAveragePnL(double averagePnL)
    {
        _averagePnLGauge.Set(averagePnL);
    }

    public void RecordMaxDrawdown(double maxDrawdown)
    {
        _maxDrawdownGauge.Set(maxDrawdown);
    }

    // ==================== РЕАЛИЗАЦИЯ МЕТРИК ПРОИЗВОДИТЕЛЬНОСТИ ====================
    public void RecordTelegramApiLatency(TimeSpan latency)
    {
        _telegramApiLatency.Observe(latency.TotalSeconds);
    }

    public void RecordNotionApiLatency(TimeSpan latency)
    {
        _notionApiLatency.Observe(latency.TotalSeconds);
    }

    public void RecordRedisCacheLatency(TimeSpan latency)
    {
        _redisCacheLatency.Observe(latency.TotalSeconds);
    }

    public void RecordCacheOperations(string operation, bool hit)
    {
        if (hit)
        {
            _cacheHitCounter.WithLabels(operation).Inc();
        }
        else
        {
            _cacheMissCounter.WithLabels(operation).Inc();
        }
    }

    public void RecordCommandExecutionTime(string command, TimeSpan executionTime)
    {
        _commandExecutionTime.WithLabels(command).Observe(executionTime.TotalSeconds);
    }

    public void RecordConcurrentRequests(int count)
    {
        _concurrentRequestsGauge.Set(count);
    }

    public void RecordGarbageCollectionTime(TimeSpan gcTime)
    {
        _garbageCollectionTimeHistogram.Observe(gcTime.TotalSeconds);
    }

    public void RecordExceptionCount(string exceptionType, string source)
    {
        _exceptionCounter.WithLabels(exceptionType, source).Inc();
    }

    // ==================== РЕАЛИЗАЦИЯ МЕТРИК БИЗНЕС-ЛОГИКИ ====================
    public void RecordUserActivity(long userId, string action)
    {
        _userActivityCounter.WithLabels(userId.ToString(), action).Inc();
    }

    public void RecordUserSessionDuration(long userId, TimeSpan duration)
    {
        _userSessionDurationHistogram.WithLabels(userId.ToString()).Observe(duration.TotalSeconds);
    }

    public void RecordNotificationCount(string notificationType)
    {
        if (_notificationTypes.Contains(notificationType))
        {
            _notificationCounter.WithLabels(notificationType).Inc();
        }
        else
        {
            _notificationCounter.WithLabels("other").Inc();
        }
    }

    public void RecordNotificationDeliveryTime(string notificationType, TimeSpan deliveryTime)
    {
        _notificationDeliveryTime.WithLabels(notificationType).Observe(deliveryTime.TotalSeconds);
    }

    public void RecordCancelledOperationCount(string operationType)
    {
        _cancelledOperationCounter.WithLabels(operationType).Inc();
    }

    public void RecordQueueProcessingTime(string queueName, TimeSpan processingTime)
    {
        _queueProcessingTime.WithLabels(queueName).Observe(processingTime.TotalSeconds);
    }

    public void RecordQueueSize(string queueName, int size)
    {
        _queueSizeGauge.WithLabels(queueName).Set(size);
    }

    // ==================== РЕАЛИЗАЦИЯ СИСТЕМНЫХ МЕТРИК ====================
    public void RecordSystemUptime(TimeSpan uptime)
    {
        _systemUptimeGauge.Set(uptime.TotalSeconds);
    }

    public void RecordSystemRestartCount()
    {
        _systemRestartCounter.Inc();
    }

    public void RecordDiskUsage(string path, long bytesUsed, long bytesTotal)
    {
        _diskUsageGauge.WithLabels(path).Set(bytesUsed);
    }

    public void RecordNetworkConnections(int activeConnections)
    {
        _networkConnectionsGauge.Set(activeConnections);
    }

    public void RecordExternalApiLatency(string apiName, TimeSpan latency)
    {
        _externalApiLatency.WithLabels(apiName).Observe(latency.TotalSeconds);
    }

    public void RecordComponentHealth(string componentName, bool isHealthy)
    {
        _componentHealthGauge.WithLabels(componentName).Set(isHealthy ? 1 : 0);
    }

    public void RecordApplicationVersion(string version)
    {
        if (double.TryParse(version, out double versionNumber))
        {
            _applicationVersionGauge.Set(versionNumber);
        }
    }

    public void RecordLastUpdateTime(DateTime lastUpdate)
    {
        _lastUpdateTimeGauge.Set(new DateTimeOffset(lastUpdate).ToUnixTimeSeconds());
    }
}

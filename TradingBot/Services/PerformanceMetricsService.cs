using Microsoft.Extensions.Logging;
using Prometheus;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace TradingBot.Services
{
    public interface IPerformanceMetricsService
    {
        IDisposable TrackRequest(string operation);
        void RecordResponseTime(string operation, TimeSpan duration);
        void IncrementRequestCount(string operation);
        void IncrementErrorCount(string operation, string errorType);
        void RecordThroughput(string operation, int count);
        void RecordMemoryUsage(long bytes);
        void RecordCpuUsage(double percentage);
        void RecordDatabaseQueryTime(string queryType, TimeSpan duration);
        PerformanceSnapshot GetCurrentSnapshot();
    }

    public class PerformanceMetricsService : IPerformanceMetricsService
    {
        private readonly ILogger<PerformanceMetricsService> _logger;
        private readonly ConcurrentDictionary<string, OperationMetrics> _operationMetrics = new();
        private readonly Histogram _responseTimeHistogram;
        private readonly Counter _requestCounter;
        private readonly Counter _errorCounter;
        private readonly Gauge _throughputGauge;
        private readonly Gauge _memoryUsageGauge;
        private readonly Gauge _cpuUsageGauge;
        private readonly Histogram _databaseQueryHistogram;

        public PerformanceMetricsService(ILogger<PerformanceMetricsService> logger)
        {
            _logger = logger;

            // Инициализируем Prometheus метрики
            _responseTimeHistogram = Metrics.CreateHistogram(
                "tradingbot_request_duration_seconds",
                "Request duration in seconds",
                new HistogramConfiguration
                {
                    LabelNames = new[] { "operation" },
                    Buckets = new[] { 0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0 }
                });

            _requestCounter = Metrics.CreateCounter(
                "tradingbot_requests_total",
                "Total number of requests",
                new CounterConfiguration
                {
                    LabelNames = new[] { "operation" }
                });

            _errorCounter = Metrics.CreateCounter(
                "tradingbot_errors_total",
                "Total number of errors",
                new CounterConfiguration
                {
                    LabelNames = new[] { "operation", "error_type" }
                });

            _throughputGauge = Metrics.CreateGauge(
                "tradingbot_throughput_requests_per_second",
                "Requests per second",
                new GaugeConfiguration
                {
                    LabelNames = new[] { "operation" }
                });

            _memoryUsageGauge = Metrics.CreateGauge(
                "tradingbot_memory_usage_bytes",
                "Memory usage in bytes");

            _cpuUsageGauge = Metrics.CreateGauge(
                "tradingbot_cpu_usage_percentage",
                "CPU usage percentage");

            _databaseQueryHistogram = Metrics.CreateHistogram(
                "tradingbot_database_query_duration_seconds",
                "Database query duration in seconds",
                new HistogramConfiguration
                {
                    LabelNames = new[] { "query_type" },
                    Buckets = new[] { 0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0 }
                });

            _logger.LogInformation("PerformanceMetricsService инициализирован");
        }

        public IDisposable TrackRequest(string operation)
        {
            return new RequestTracker(this, operation);
        }

        public void RecordResponseTime(string operation, TimeSpan duration)
        {
            try
            {
                // Обновляем Prometheus метрики
                _responseTimeHistogram.WithLabels(operation).Observe(duration.TotalSeconds);

                // Обновляем локальные метрики
                var metrics = _operationMetrics.GetOrAdd(operation, _ => new OperationMetrics());
                metrics.RecordResponseTime(duration);

                _logger.LogDebug("Время ответа для операции {Operation}: {Duration}ms", operation, duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка записи времени ответа для операции {Operation}", operation);
            }
        }

        public void IncrementRequestCount(string operation)
        {
            try
            {
                // Обновляем Prometheus метрики
                _requestCounter.WithLabels(operation).Inc();

                // Обновляем локальные метрики
                var metrics = _operationMetrics.GetOrAdd(operation, _ => new OperationMetrics());
                metrics.IncrementRequestCount();

                _logger.LogDebug("Счетчик запросов увеличен для операции {Operation}", operation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка увеличения счетчика запросов для операции {Operation}", operation);
            }
        }

        public void IncrementErrorCount(string operation, string errorType)
        {
            try
            {
                // Обновляем Prometheus метрики
                _errorCounter.WithLabels(operation, errorType).Inc();

                // Обновляем локальные метрики
                var metrics = _operationMetrics.GetOrAdd(operation, _ => new OperationMetrics());
                metrics.IncrementErrorCount(errorType);

                _logger.LogWarning("Счетчик ошибок увеличен для операции {Operation}, тип: {ErrorType}", operation, errorType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка увеличения счетчика ошибок для операции {Operation}", operation);
            }
        }

        public void RecordThroughput(string operation, int count)
        {
            try
            {
                // Обновляем Prometheus метрики
                _throughputGauge.WithLabels(operation).Set(count);

                // Обновляем локальные метрики
                var metrics = _operationMetrics.GetOrAdd(operation, _ => new OperationMetrics());
                metrics.RecordThroughput(count);

                _logger.LogDebug("Throughput для операции {Operation}: {Count} req/s", operation, count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка записи throughput для операции {Operation}", operation);
            }
        }

        public void RecordMemoryUsage(long bytes)
        {
            try
            {
                _memoryUsageGauge.Set(bytes);
                _logger.LogDebug("Использование памяти: {Bytes} bytes ({MB:F2} MB)", bytes, bytes / 1024.0 / 1024.0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка записи использования памяти");
            }
        }

        public void RecordCpuUsage(double percentage)
        {
            try
            {
                _cpuUsageGauge.Set(percentage);
                _logger.LogDebug("Использование CPU: {Percentage:F2}%", percentage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка записи использования CPU");
            }
        }

        public void RecordDatabaseQueryTime(string queryType, TimeSpan duration)
        {
            try
            {
                _databaseQueryHistogram.WithLabels(queryType).Observe(duration.TotalSeconds);
                _logger.LogDebug("Время выполнения запроса БД {QueryType}: {Duration}ms", queryType, duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка записи времени выполнения запроса БД {QueryType}", queryType);
            }
        }

        public PerformanceSnapshot GetCurrentSnapshot()
        {
            var snapshot = new PerformanceSnapshot
            {
                Timestamp = DateTime.UtcNow,
                Operations = new Dictionary<string, OperationSnapshot>()
            };

            foreach (var kvp in _operationMetrics)
            {
                snapshot.Operations[kvp.Key] = kvp.Value.GetSnapshot();
            }

            return snapshot;
        }

        private class RequestTracker : IDisposable
        {
            private readonly PerformanceMetricsService _service;
            private readonly string _operation;
            private readonly Stopwatch _stopwatch;

            public RequestTracker(PerformanceMetricsService service, string operation)
            {
                _service = service;
                _operation = operation;
                _stopwatch = Stopwatch.StartNew();
                
                _service.IncrementRequestCount(operation);
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                _service.RecordResponseTime(_operation, _stopwatch.Elapsed);
            }
        }

        private class OperationMetrics
        {
            private readonly object _lock = new object();
            private long _totalRequests;
            private long _totalErrors;
            private readonly List<TimeSpan> _responseTimes = new();
            private readonly List<int> _throughputHistory = new();
            private readonly Dictionary<string, long> _errorCounts = new();

            public void RecordResponseTime(TimeSpan duration)
            {
                lock (_lock)
                {
                    _responseTimes.Add(duration);
                    
                    // Ограничиваем историю последними 1000 значениями
                    if (_responseTimes.Count > 1000)
                    {
                        _responseTimes.RemoveAt(0);
                    }
                }
            }

            public void IncrementRequestCount()
            {
                Interlocked.Increment(ref _totalRequests);
            }

            public void IncrementErrorCount(string errorType)
            {
                Interlocked.Increment(ref _totalErrors);
                
                lock (_lock)
                {
                    if (!_errorCounts.ContainsKey(errorType))
                        _errorCounts[errorType] = 0;
                    _errorCounts[errorType]++;
                }
            }

            public void RecordThroughput(int count)
            {
                lock (_lock)
                {
                    _throughputHistory.Add(count);
                    
                    // Ограничиваем историю последними 100 значениями
                    if (_throughputHistory.Count > 100)
                    {
                        _throughputHistory.RemoveAt(0);
                    }
                }
            }

            public OperationSnapshot GetSnapshot()
            {
                lock (_lock)
                {
                    var responseTimes = _responseTimes.ToArray();
                    var throughputHistory = _throughputHistory.ToArray();

                    return new OperationSnapshot
                    {
                        TotalRequests = _totalRequests,
                        TotalErrors = _totalErrors,
                        ErrorRate = _totalRequests > 0 ? (double)_totalErrors / _totalRequests : 0,
                        AverageResponseTime = responseTimes.Length > 0 ? TimeSpan.FromTicks((long)responseTimes.Average(t => t.Ticks)) : TimeSpan.Zero,
                        MinResponseTime = responseTimes.Length > 0 ? responseTimes.Min() : TimeSpan.Zero,
                        MaxResponseTime = responseTimes.Length > 0 ? responseTimes.Max() : TimeSpan.Zero,
                        P95ResponseTime = CalculatePercentile(responseTimes, 95),
                        P99ResponseTime = CalculatePercentile(responseTimes, 99),
                        CurrentThroughput = throughputHistory.Length > 0 ? throughputHistory.Last() : 0,
                        AverageThroughput = throughputHistory.Length > 0 ? throughputHistory.Average() : 0,
                        ErrorCounts = new Dictionary<string, long>(_errorCounts)
                    };
                }
            }

            private static TimeSpan CalculatePercentile(TimeSpan[] values, int percentile)
            {
                if (values.Length == 0) return TimeSpan.Zero;
                
                var sorted = values.OrderBy(v => v).ToArray();
                var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Length) - 1;
                return sorted[Math.Max(0, Math.Min(index, sorted.Length - 1))];
            }
        }
    }

    public class PerformanceSnapshot
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<string, OperationSnapshot> Operations { get; set; } = new();
    }

    public class OperationSnapshot
    {
        public long TotalRequests { get; set; }
        public long TotalErrors { get; set; }
        public double ErrorRate { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public TimeSpan MinResponseTime { get; set; }
        public TimeSpan MaxResponseTime { get; set; }
        public TimeSpan P95ResponseTime { get; set; }
        public TimeSpan P99ResponseTime { get; set; }
        public int CurrentThroughput { get; set; }
        public double AverageThroughput { get; set; }
        public Dictionary<string, long> ErrorCounts { get; set; } = new();
    }
}

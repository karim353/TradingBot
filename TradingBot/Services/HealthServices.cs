using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace TradingBot.Services
{
    /// <summary>
    /// Интерфейс для сервиса мониторинга здоровья
    /// </summary>
    public interface IHealthMonitoringService
    {
        Task<bool> IsHealthyAsync();
        Task<Dictionary<string, HealthStatus>> GetComponentHealthAsync();
        Task<HealthStatus> GetOverallHealthAsync();
        void RegisterComponent(string name, Func<Task<bool>> healthCheck);
        void UnregisterComponent(string name);
    }

    /// <summary>
    /// Сервис проверки здоровья системы
    /// </summary>
    public class HealthCheckService : IHealthCheck
    {
        private readonly ILogger<HealthCheckService> _logger;
        private readonly string _connectionString;

        public HealthCheckService(ILogger<HealthCheckService> logger, string connectionString)
        {
            _logger = logger;
            _connectionString = connectionString;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Проверка подключения к базе данных
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                
                // Проверка доступности таблиц
                var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table'";
                var tableCount = await command.ExecuteScalarAsync(cancellationToken);
                
                if (tableCount == null || Convert.ToInt32(tableCount) < 3)
                {
                    _logger.LogWarning("Недостаточно таблиц в базе данных: {TableCount}", tableCount);
                    return HealthCheckResult.Degraded("Недостаточно таблиц в базе данных");
                }

                // Проверка размера базы данных
                var fileInfo = new FileInfo(_connectionString.Replace("Data Source=", ""));
                if (fileInfo.Exists && fileInfo.Length > 100 * 1024 * 1024) // 100 MB
                {
                    _logger.LogWarning("База данных превышает рекомендуемый размер: {Size} MB", fileInfo.Length / (1024 * 1024));
                    return HealthCheckResult.Degraded("База данных превышает рекомендуемый размер");
                }

                _logger.LogInformation("Health check пройден успешно");
                return HealthCheckResult.Healthy("Система работает нормально");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check не пройден");
                return HealthCheckResult.Unhealthy("Ошибка подключения к базе данных", ex);
            }
        }
    }

    /// <summary>
    /// Сервис мониторинга здоровья системы
    /// </summary>
    public class HealthMonitoringService : IHealthMonitoringService, IHostedService
    {
        private readonly ILogger<HealthMonitoringService> _logger;
        private readonly ConcurrentDictionary<string, Func<Task<bool>>> _healthChecks;
        private readonly Timer _healthCheckTimer;
        private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(1);

        public HealthMonitoringService(ILogger<HealthMonitoringService> logger)
        {
            _logger = logger;
            _healthChecks = new ConcurrentDictionary<string, Func<Task<bool>>>();
            _healthCheckTimer = new Timer(PerformHealthCheck, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void RegisterComponent(string name, Func<Task<bool>> healthCheck)
        {
            _healthChecks.TryAdd(name, healthCheck);
            _logger.LogInformation("Зарегистрирован компонент для мониторинга здоровья: {ComponentName}", name);
        }

        public void UnregisterComponent(string name)
        {
            _healthChecks.TryRemove(name, out _);
            _logger.LogInformation("Удален компонент из мониторинга здоровья: {ComponentName}", name);
        }

        public async Task<bool> IsHealthyAsync()
        {
            var overallHealth = await GetOverallHealthAsync();
            return overallHealth == HealthStatus.Healthy;
        }

        public async Task<Dictionary<string, HealthStatus>> GetComponentHealthAsync()
        {
            var results = new Dictionary<string, HealthStatus>();

            foreach (var kvp in _healthChecks)
            {
                try
                {
                    var isHealthy = await kvp.Value();
                    results[kvp.Key] = isHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при проверке здоровья компонента: {ComponentName}", kvp.Key);
                    results[kvp.Key] = HealthStatus.Unhealthy;
                }
            }

            return results;
        }

        public async Task<HealthStatus> GetOverallHealthAsync()
        {
            var componentHealth = await GetComponentHealthAsync();
            
            if (!componentHealth.Any())
                return HealthStatus.Healthy;

            var unhealthyCount = componentHealth.Count(x => x.Value == HealthStatus.Unhealthy);
            var totalCount = componentHealth.Count;

            if (unhealthyCount == 0)
                return HealthStatus.Healthy;
            
            if (unhealthyCount < totalCount / 2)
                return HealthStatus.Degraded;
            
            return HealthStatus.Unhealthy;
        }

        private async void PerformHealthCheck(object? state)
        {
            try
            {
                var overallHealth = await GetOverallHealthAsync();
                var componentHealth = await GetComponentHealthAsync();
                
                var healthyCount = componentHealth.Count(x => x.Value == HealthStatus.Healthy);
                var totalCount = componentHealth.Count;

                if (overallHealth == HealthStatus.Healthy)
                {
                    _logger.LogInformation("✅ Система здорова. Компонентов: {TotalCount}, Здоровых: {HealthyCount}", totalCount, healthyCount);
                }
                else if (overallHealth == HealthStatus.Degraded)
                {
                    _logger.LogWarning("⚠️ Система работает с ограничениями. Компонентов: {TotalCount}, Здоровых: {HealthyCount}", totalCount, healthyCount);
                }
                else
                {
                    _logger.LogError("❌ Система нездорова. Компонентов: {TotalCount}, Здоровых: {HealthyCount}", totalCount, healthyCount);
                }

                // Логируем детали по каждому компоненту
                foreach (var kvp in componentHealth)
                {
                    if (kvp.Value == HealthStatus.Unhealthy)
                    {
                        _logger.LogWarning("Компонент {ComponentName} нездоров", kvp.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении проверки здоровья");
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Запуск мониторинга здоровья системы");
            _healthCheckTimer.Change(TimeSpan.Zero, _healthCheckInterval);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Остановка мониторинга здоровья системы");
            _healthCheckTimer.Change(Timeout.Infinite, Timeout.Infinite);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _healthCheckTimer?.Dispose();
        }
    }
}

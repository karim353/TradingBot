using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TradingBot.Services.Interfaces;
using TradingBot.Models;
using System.Diagnostics;

namespace TradingBot.Services;

/// <summary>
/// Сервис мониторинга здоровья системы
/// </summary>
public class HealthMonitoringService : IHealthMonitoringService, IHostedService
{
    private readonly ILogger<HealthMonitoringService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMetricsService _metricsService;
    private readonly Timer? _monitoringTimer;
    private readonly TimeSpan _monitoringInterval = TimeSpan.FromMinutes(5);
    
    private SystemHealthInfo _lastHealthInfo = new();
    private bool _isMonitoring = false;

    public HealthMonitoringService(
        ILogger<HealthMonitoringService> logger,
        IServiceProvider serviceProvider,
        IMetricsService metricsService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _metricsService = metricsService;
        _monitoringTimer = new Timer(PerformHealthCheck, null, Timeout.Infinite, Timeout.Infinite);
    }

    public async Task<SystemHealthStatus> GetSystemHealthAsync()
    {
        try
        {
            var healthInfo = await GetDetailedHealthInfoAsync();
            return healthInfo.Status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении статуса здоровья системы");
            return SystemHealthStatus.Unhealthy;
        }
    }

    public async Task<SystemHealthInfo> GetDetailedHealthInfoAsync()
    {
        var healthInfo = new SystemHealthInfo
        {
            Timestamp = DateTime.UtcNow,
            Components = new List<Interfaces.ComponentHealth>(),
            Metrics = new Dictionary<string, object>(),
            Warnings = new List<string>(),
            Errors = new List<string>()
        };

        try
        {
            // Проверяем здоровье базы данных
            var dbHealth = await CheckDatabaseHealthAsync();
            healthInfo.Components.Add(dbHealth);

            // Проверяем размер базы данных
            var dbSize = await GetDatabaseSizeAsync();
            if (dbSize > 0)
            {
                healthInfo.Metrics["DatabaseSizeMB"] = dbSize;
            }

            // Определяем общий статус на основе компонентов
            if (healthInfo.Components.Any(c => c.Status == SystemHealthStatus.Unhealthy))
            {
                healthInfo.Status = SystemHealthStatus.Unhealthy;
            }
            else if (healthInfo.Components.Any(c => c.Status == SystemHealthStatus.Degraded))
            {
                healthInfo.Status = SystemHealthStatus.Degraded;
            }
            else
            {
                healthInfo.Status = SystemHealthStatus.Healthy;
            }

            // Добавляем метрики
            healthInfo.Metrics["TotalComponents"] = healthInfo.Components.Count;
            healthInfo.Metrics["HealthyComponents"] = healthInfo.Components.Count(c => c.Status == SystemHealthStatus.Healthy);
            healthInfo.Metrics["DegradedComponents"] = healthInfo.Components.Count(c => c.Status == SystemHealthStatus.Degraded);
            healthInfo.Metrics["UnhealthyComponents"] = healthInfo.Components.Count(c => c.Status == SystemHealthStatus.Unhealthy);

            // Добавляем предупреждения и ошибки
            foreach (var component in healthInfo.Components)
            {
                if (component.Status == SystemHealthStatus.Degraded)
                {
                    healthInfo.Warnings.Add($"{component.Name}: {component.Description}");
                }
                else if (component.Status == SystemHealthStatus.Unhealthy)
                {
                    healthInfo.Errors.Add($"{component.Name}: {component.Description}");
                }
            }

            _lastHealthInfo = healthInfo;
            return healthInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении детальной информации о здоровье системы");
            healthInfo.Status = SystemHealthStatus.Unhealthy;
            healthInfo.Errors.Add($"Ошибка мониторинга: {ex.Message}");
            return healthInfo;
        }
    }

    private async Task<Interfaces.ComponentHealth> CheckDatabaseHealthAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var dbContext = _serviceProvider.GetRequiredService<TradeContext>();
            var canConnect = await dbContext.Database.CanConnectAsync();
            stopwatch.Stop();

            if (canConnect)
            {
                // Записываем метрики в Prometheus
                _metricsService.RecordDatabaseResponseTime(stopwatch.Elapsed);
                
                return new Interfaces.ComponentHealth
                {
                    Name = "Database",
                    Status = SystemHealthStatus.Healthy,
                    Description = "База данных доступна",
                    ResponseTime = stopwatch.Elapsed,
                    LastCheck = DateTime.UtcNow
                };
            }
            else
            {
                // Записываем ошибку в метрики
                _metricsService.IncrementErrorCounter("database");
                
                return new Interfaces.ComponentHealth
                {
                    Name = "Database",
                    Status = SystemHealthStatus.Unhealthy,
                    Description = "Не удается подключиться к базе данных",
                    ResponseTime = stopwatch.Elapsed,
                    LastCheck = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Ошибка при проверке здоровья базы данных");
            return new Interfaces.ComponentHealth
            {
                Name = "Database",
                Status = SystemHealthStatus.Unhealthy,
                Description = $"Ошибка: {ex.Message}",
                ResponseTime = stopwatch.Elapsed,
                LastCheck = DateTime.UtcNow
            };
        }
    }

    private Task<double> GetDatabaseSizeAsync()
    {
        try
        {
            var dbContext = _serviceProvider.GetRequiredService<TradeContext>();
            // Используем стандартный путь к базе данных
            var dbPath = "trades.db";
            if (File.Exists(dbPath))
            {
                var fileInfo = new FileInfo(dbPath);
                return Task.FromResult(Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось получить информацию о размере базы данных");
        }
        return Task.FromResult(0.0);
    }

    public Task StartMonitoringAsync()
    {
        if (_isMonitoring)
        {
            _logger.LogInformation("Мониторинг уже запущен");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Запуск мониторинга здоровья системы");
        _isMonitoring = true;
        
        // Запускаем мониторинг с задержкой, чтобы дать время на инициализацию DbContext
        _monitoringTimer?.Change(TimeSpan.FromSeconds(10), _monitoringInterval);
        
        return Task.CompletedTask;
    }

    public Task StopMonitoringAsync()
    {
        if (!_isMonitoring)
        {
            _logger.LogInformation("Мониторинг уже остановлен");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Остановка мониторинга здоровья системы");
        _isMonitoring = false;
        _monitoringTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        return Task.CompletedTask;
    }

    private void PerformHealthCheck(object? state)
    {
        _ = PerformHealthCheckAsync();
    }

    private async Task PerformHealthCheckAsync()
    {
        try
        {
            var healthInfo = await GetDetailedHealthInfoAsync();
            
            // Логируем результаты
            if (healthInfo.Status == SystemHealthStatus.Healthy)
            {
                _logger.LogInformation("✅ Система здорова. Компонентов: {Total}, Здоровых: {Healthy}", 
                    healthInfo.Metrics["TotalComponents"], healthInfo.Metrics["HealthyComponents"]);
            }
            else if (healthInfo.Status == SystemHealthStatus.Degraded)
            {
                _logger.LogWarning("⚠️ Система работает с ограничениями. Предупреждения: {Warnings}", 
                    string.Join("; ", healthInfo.Warnings));
            }
            else
            {
                _logger.LogError("❌ Система нездорова. Ошибки: {Errors}", 
                    string.Join("; ", healthInfo.Errors));
            }

            // Логируем метрики
            if (healthInfo.Metrics.ContainsKey("DatabaseSizeMB"))
            {
                _logger.LogDebug("Размер базы данных: {Size} MB", healthInfo.Metrics["DatabaseSizeMB"]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при выполнении проверки здоровья системы");
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await StartMonitoringAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await StopMonitoringAsync();
        _monitoringTimer?.Dispose();
    }
}

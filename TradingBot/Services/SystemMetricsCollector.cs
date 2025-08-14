using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingBot.Services.Interfaces;
using System.Diagnostics;

namespace TradingBot.Services;

/// <summary>
/// Сервис для сбора системных метрик
/// </summary>
public class SystemMetricsCollector : IHostedService
{
    private readonly ILogger<SystemMetricsCollector> _logger;
    private readonly IMetricsService _metricsService;
    private readonly Timer? _metricsTimer;
    private readonly TimeSpan _collectionInterval = TimeSpan.FromMinutes(1);
    
    private bool _isCollecting = false;

    public SystemMetricsCollector(
        ILogger<SystemMetricsCollector> logger,
        IMetricsService metricsService)
    {
        _logger = logger;
        _metricsService = metricsService;
        _metricsTimer = new Timer(CollectMetrics, null, Timeout.Infinite, Timeout.Infinite);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_isCollecting)
        {
            _logger.LogInformation("Сбор метрик уже запущен");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Запуск сбора системных метрик");
        _isCollecting = true;
        
        // Запускаем сбор метрик с задержкой
        _metricsTimer?.Change(TimeSpan.FromSeconds(30), _collectionInterval);
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_isCollecting)
        {
            _logger.LogInformation("Сбор метрик уже остановлен");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Остановка сбора системных метрик");
        _isCollecting = false;
        _metricsTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        
        return Task.CompletedTask;
    }

    private void CollectMetrics(object? state)
    {
        CollectMetricsAsync();
    }

    private Task CollectMetricsAsync()
    {
        try
        {
            _logger.LogDebug("Сбор системных метрик...");
            
            // Собираем базовые системные метрики
            CollectSystemMetrics();
            
            // Собираем метрики базы данных
            CollectDatabaseMetricsAsync();
            
            // Собираем метрики пользователей
            CollectUserMetrics();
            
            _logger.LogDebug("Системные метрики собраны успешно");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сборе системных метрик");
        }
        
        return Task.CompletedTask;
    }

    private void CollectSystemMetrics()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            
            // Использование памяти
            var memoryUsage = process.WorkingSet64;
            _metricsService.RecordMemoryUsage(memoryUsage);
            
            // Использование CPU
            var cpuUsage = GetCpuUsage();
            _metricsService.RecordCpuUsage(cpuUsage);
            
            _logger.LogTrace("Системные метрики: Память={Memory}MB, CPU={Cpu}%", 
                Math.Round(memoryUsage / (1024.0 * 1024.0), 2), 
                Math.Round(cpuUsage, 2));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось собрать системные метрики");
        }
    }

    private Task CollectDatabaseMetricsAsync()
    {
        try
        {
            // Размер базы данных
            var dbPath = "trades.db";
            if (File.Exists(dbPath))
            {
                var fileInfo = new FileInfo(dbPath);
                var sizeMB = Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2);
                _metricsService.RecordDatabaseSize(sizeMB);
                
                _logger.LogTrace("Размер базы данных: {Size} MB", sizeMB);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось собрать метрики базы данных");
        }
        
        return Task.CompletedTask;
    }

    private void CollectUserMetrics()
    {
        try
        {
            // Здесь можно добавить логику подсчета активных пользователей
            // Пока устанавливаем базовое значение
            var activeUsers = 1; // Минимальное значение
            _metricsService.RecordActiveUsers(activeUsers);
            
            _logger.LogTrace("Активных пользователей: {Count}", activeUsers);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось собрать метрики пользователей");
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

namespace TradingBot.Services.Interfaces;

/// <summary>
/// Интерфейс для сервиса мониторинга здоровья системы
/// </summary>
public interface IHealthMonitoringService
{
    /// <summary>
    /// Получить текущий статус здоровья системы
    /// </summary>
    Task<SystemHealthStatus> GetSystemHealthAsync();
    
    /// <summary>
    /// Получить детальную информацию о здоровье системы
    /// </summary>
    Task<SystemHealthInfo> GetDetailedHealthInfoAsync();
    
    /// <summary>
    /// Запустить мониторинг в фоновом режиме
    /// </summary>
    Task StartMonitoringAsync();
    
    /// <summary>
    /// Остановить мониторинг
    /// </summary>
    Task StopMonitoringAsync();
}

/// <summary>
/// Статус здоровья системы
/// </summary>
public enum SystemHealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

/// <summary>
/// Детальная информация о здоровье системы
/// </summary>
public class SystemHealthInfo
{
    public SystemHealthStatus Status { get; set; }
    public DateTime Timestamp { get; set; }
    public List<ComponentHealth> Components { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Здоровье компонента системы
/// </summary>
public class ComponentHealth
{
    public string Name { get; set; } = string.Empty;
    public SystemHealthStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public DateTime LastCheck { get; set; }
}

namespace TradingBot.Services.Interfaces;

/// <summary>
/// Интерфейс для сервиса метрик Prometheus
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Увеличить счетчик сообщений
    /// </summary>
    void IncrementMessageCounter(string messageType);
    
    /// <summary>
    /// Увеличить счетчик сделок
    /// </summary>
    void IncrementTradeCounter(string tradeType);
    
    /// <summary>
    /// Записать время обработки запроса
    /// </summary>
    void RecordRequestDuration(string operation, TimeSpan duration);
    
    /// <summary>
    /// Записать размер базы данных
    /// </summary>
    void RecordDatabaseSize(double sizeMB);
    
    /// <summary>
    /// Записать количество активных пользователей
    /// </summary>
    void RecordActiveUsers(int count);
    
    /// <summary>
    /// Записать количество ошибок
    /// </summary>
    void IncrementErrorCounter(string errorType);
    
    /// <summary>
    /// Записать время отклика базы данных
    /// </summary>
    void RecordDatabaseResponseTime(TimeSpan responseTime);
    
    /// <summary>
    /// Записать использование памяти
    /// </summary>
    void RecordMemoryUsage(long bytesUsed);
    
    /// <summary>
    /// Записать использование CPU
    /// </summary>
    void RecordCpuUsage(double percentage);
}

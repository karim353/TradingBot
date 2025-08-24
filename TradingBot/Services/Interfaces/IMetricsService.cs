namespace TradingBot.Services.Interfaces;

/// <summary>
/// Расширенный интерфейс для сервиса метрик Prometheus
/// </summary>
public interface IMetricsService
{
    // ==================== БАЗОВЫЕ СЧЕТЧИКИ ====================
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

    // ==================== РАСШИРЕННЫЕ ТОРГОВЫЕ МЕТРИКИ ====================
    /// <summary>
    /// Записать PnL сделку
    /// </summary>
    void RecordTradePnL(string ticker, decimal pnl, string direction);
    
    /// <summary>
    /// Записать размер позиции
    /// </summary>
    void RecordPositionSize(string ticker, decimal size);
    
    /// <summary>
    /// Записать риск/доходность
    /// </summary>
    void RecordRiskRewardRatio(string ticker, double ratio);
    
    /// <summary>
    /// Записать время удержания позиции
    /// </summary>
    void RecordPositionHoldTime(string ticker, TimeSpan holdTime);
    
    /// <summary>
    /// Записать серию выигрышей/проигрышей
    /// </summary>
    void RecordWinLossStreak(int consecutiveWins, int consecutiveLosses);
    
    /// <summary>
    /// Записать процент выигрышных сделок
    /// </summary>
    void RecordWinRate(double winRate);
    
    /// <summary>
    /// Записать средний PnL
    /// </summary>
    void RecordAveragePnL(double averagePnL);
    
    /// <summary>
    /// Записать максимальный проигрыш
    /// </summary>
    void RecordMaxDrawdown(double maxDrawdown);

    // ==================== МЕТРИКИ ПРОИЗВОДИТЕЛЬНОСТИ ====================
    /// <summary>
    /// Записать время отклика Telegram API
    /// </summary>
    void RecordTelegramApiLatency(TimeSpan latency);
    
    /// <summary>
    /// Записать время обработки Notion API
    /// </summary>
    void RecordNotionApiLatency(TimeSpan latency);
    
    /// <summary>
    /// Записать время кэширования Redis
    /// </summary>
    void RecordRedisCacheLatency(TimeSpan latency);
    
    /// <summary>
    /// Записать количество операций кэша
    /// </summary>
    void RecordCacheOperations(string operation, bool hit);
    
    /// <summary>
    /// Записать время выполнения команд
    /// </summary>
    void RecordCommandExecutionTime(string command, TimeSpan executionTime);
    
    /// <summary>
    /// Записать количество параллельных запросов
    /// </summary>
    void RecordConcurrentRequests(int count);
    
    /// <summary>
    /// Записать время сборки мусора
    /// </summary>
    void RecordGarbageCollectionTime(TimeSpan gcTime);
    
    /// <summary>
    /// Записать количество исключений
    /// </summary>
    void RecordExceptionCount(string exceptionType, string source);

    // ==================== МЕТРИКИ БИЗНЕС-ЛОГИКИ ====================
    /// <summary>
    /// Записать активность пользователя
    /// </summary>
    void RecordUserActivity(long userId, string action);
    
    /// <summary>
    /// Записать время сессии пользователя
    /// </summary>
    void RecordUserSessionDuration(long userId, TimeSpan duration);
    
    /// <summary>
    /// Записать количество уведомлений
    /// </summary>
    void RecordNotificationCount(string notificationType);
    
    /// <summary>
    /// Записать время доставки уведомлений
    /// </summary>
    void RecordNotificationDeliveryTime(string notificationType, TimeSpan deliveryTime);
    
    /// <summary>
    /// Записать количество отмененных операций
    /// </summary>
    void RecordCancelledOperationCount(string operationType);
    
    /// <summary>
    /// Записать время обработки очереди
    /// </summary>
    void RecordQueueProcessingTime(string queueName, TimeSpan processingTime);
    
    /// <summary>
    /// Записать размер очереди
    /// </summary>
    void RecordQueueSize(string queueName, int size);

    // ==================== СИСТЕМНЫЕ МЕТРИКИ ====================
    /// <summary>
    /// Записать время работы системы
    /// </summary>
    void RecordSystemUptime(TimeSpan uptime);
    
    /// <summary>
    /// Записать количество перезапусков
    /// </summary>
    void RecordSystemRestartCount();
    
    /// <summary>
    /// Записать использование диска
    /// </summary>
    void RecordDiskUsage(string path, long bytesUsed, long bytesTotal);
    
    /// <summary>
    /// Записать количество сетевых соединений
    /// </summary>
    void RecordNetworkConnections(int activeConnections);
    
    /// <summary>
    /// Записать время отклика внешних API
    /// </summary>
    void RecordExternalApiLatency(string apiName, TimeSpan latency);
    
    /// <summary>
    /// Записать статус здоровья компонентов
    /// </summary>
    void RecordComponentHealth(string componentName, bool isHealthy);
    
    /// <summary>
    /// Записать версию приложения
    /// </summary>
    void RecordApplicationVersion(string version);
    
    /// <summary>
    /// Записать время последнего обновления
    /// </summary>
    void RecordLastUpdateTime(DateTime lastUpdate);
}

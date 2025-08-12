using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingBot.Models;

namespace TradingBot.Services
{
    /// <summary>
    /// Сервис для выполнения фоновых задач
    /// Обрабатывает тяжелые операции (например, создание страниц в Notion) в очереди
    /// </summary>
    public class BackgroundTaskService : BackgroundService
    {
        private readonly ILogger<BackgroundTaskService> _logger;
        private readonly ConcurrentQueue<BackgroundTask> _taskQueue;
        private readonly SemaphoreSlim _semaphore;
        private readonly int _maxConcurrentTasks;
        private readonly int _maxQueueSize;

        public BackgroundTaskService(ILogger<BackgroundTaskService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _taskQueue = new ConcurrentQueue<BackgroundTask>();
            
            _maxConcurrentTasks = configuration.GetValue<int>("BackgroundTasks:MaxConcurrent", 3);
            _maxQueueSize = configuration.GetValue<int>("BackgroundTasks:MaxQueueSize", 100);
            
            _semaphore = new SemaphoreSlim(_maxConcurrentTasks, _maxConcurrentTasks);
            
            _logger.LogInformation("BackgroundTaskService initialized with {MaxConcurrent} concurrent tasks and {MaxQueueSize} queue size", 
                _maxConcurrentTasks, _maxQueueSize);
        }

        /// <summary>
        /// Добавляет задачу в очередь для выполнения в фоновом режиме
        /// </summary>
        /// <param name="task">Задача для выполнения</param>
        /// <returns>True если задача добавлена, false если очередь переполнена</returns>
        public bool EnqueueTask(BackgroundTask task)
        {
            if (_taskQueue.Count >= _maxQueueSize)
            {
                _logger.LogWarning("Task queue is full ({Count}/{MaxSize}). Task {TaskType} for user {UserId} rejected.", 
                    _taskQueue.Count, _maxQueueSize, task.TaskType, task.UserId);
                return false;
            }

            _taskQueue.Enqueue(task);
            _logger.LogDebug("Task {TaskType} for user {UserId} added to queue. Queue size: {QueueSize}", 
                task.TaskType, task.UserId, _taskQueue.Count);
            return true;
        }

        /// <summary>
        /// Добавляет задачу с приоритетом (выполняется в первую очередь)
        /// </summary>
        /// <param name="task">Задача для выполнения</param>
        /// <returns>True если задача добавлена</returns>
        public bool EnqueuePriorityTask(BackgroundTask task)
        {
            if (_taskQueue.Count >= _maxQueueSize)
            {
                _logger.LogWarning("Task queue is full. Priority task {TaskType} for user {UserId} rejected.", 
                    task.TaskType, task.UserId);
                return false;
            }

            // Для приоритетных задач используем специальную логику
            // В реальной реализации можно использовать PriorityQueue
            _taskQueue.Enqueue(task);
            _logger.LogInformation("Priority task {TaskType} for user {UserId} added to queue", 
                task.TaskType, task.UserId);
            return true;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BackgroundTaskService started. Processing tasks...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_taskQueue.TryDequeue(out var task))
                    {
                        await ProcessTaskAsync(task, stoppingToken);
                    }
                    else
                    {
                        // Если очередь пуста, ждем немного
                        await Task.Delay(100, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in BackgroundTaskService main loop");
                    await Task.Delay(1000, stoppingToken); // Пауза перед продолжением
                }
            }

            _logger.LogInformation("BackgroundTaskService stopped");
        }

        private async Task ProcessTaskAsync(BackgroundTask task, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            
            try
            {
                _logger.LogInformation("Processing task {TaskType} for user {UserId}", task.TaskType, task.UserId);
                
                var startTime = DateTime.UtcNow;
                
                await task.ExecuteAsync(cancellationToken);
                
                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("Task {TaskType} for user {UserId} completed in {Duration:g}", 
                    task.TaskType, task.UserId, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing task {TaskType} for user {UserId}", task.TaskType, task.UserId);
                
                // Уведомляем пользователя об ошибке, если возможно
                try
                {
                    await task.HandleErrorAsync(ex);
                }
                catch (Exception errorHandlerEx)
                {
                    _logger.LogError(errorHandlerEx, "Error in error handler for task {TaskType}", task.TaskType);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping BackgroundTaskService. Queue size: {QueueSize}", _taskQueue.Count);
            
            // Ждем завершения текущих задач
            var waitTime = TimeSpan.FromSeconds(30);
            var waitTask = Task.Delay(waitTime, cancellationToken);
            
            try
            {
                await waitTask;
            }
            catch (OperationCanceledException)
            {
                // Таймаут или отмена
            }
            
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _semaphore?.Dispose();
            base.Dispose();
        }
    }

    /// <summary>
    /// Абстракция фоновой задачи
    /// </summary>
    public abstract class BackgroundTask
    {
        public string TaskType { get; }
        public long UserId { get; }
        public DateTime CreatedAt { get; }
        public int RetryCount { get; private set; }
        public int MaxRetries { get; }

        protected BackgroundTask(string taskType, long userId, int maxRetries = 3)
        {
            TaskType = taskType;
            UserId = userId;
            CreatedAt = DateTime.UtcNow;
            MaxRetries = maxRetries;
        }

        /// <summary>
        /// Выполняет основную логику задачи
        /// </summary>
        public abstract Task ExecuteAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Обрабатывает ошибки выполнения задачи
        /// </summary>
        public virtual Task HandleErrorAsync(Exception exception)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Увеличивает счетчик попыток
        /// </summary>
        public bool IncrementRetryCount()
        {
            RetryCount++;
            return RetryCount <= MaxRetries;
        }

        /// <summary>
        /// Проверяет, можно ли повторить задачу
        /// </summary>
        public bool CanRetry => RetryCount < MaxRetries;
    }

    /// <summary>
    /// Задача создания страницы в Notion
    /// </summary>
    public class NotionPageCreationTask : BackgroundTask
    {
        private readonly Trade _trade;
        private readonly NotionService _notionService;
        private readonly ILogger<NotionPageCreationTask> _logger;

        public NotionPageCreationTask(Trade trade, NotionService notionService, ILogger<NotionPageCreationTask> logger)
            : base("NotionPageCreation", trade.UserId)
        {
            _trade = trade;
            _notionService = notionService;
            _logger = logger;
        }

        public override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating Notion page for trade {TradeId} of user {UserId}", _trade.Id, UserId);
            
            try
            {
                // Здесь будет создание страницы в Notion
                // Пока что просто логируем
                _logger.LogInformation("Notion page creation task queued for trade {TradeId}", _trade.Id);
                
                // TODO: Реализовать создание страницы в Notion
                // var pageId = await _notionService.CreateTradePageAsync(_trade);
                // _logger.LogInformation("Notion page created successfully: {PageId}", pageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Notion page for trade {TradeId}", _trade.Id);
                throw;
            }
            
            return Task.CompletedTask;
        }

        public override Task HandleErrorAsync(Exception exception)
        {
            _logger.LogWarning("Handling error for Notion page creation task. Trade: {TradeId}, User: {UserId}", 
                _trade.Id, UserId);
            
            // Можно добавить логику уведомления пользователя об ошибке
            // или сохранения задачи для повторной попытки
            return Task.CompletedTask;
        }
    }
}

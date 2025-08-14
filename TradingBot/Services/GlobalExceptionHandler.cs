using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace TradingBot.Services
{
    /// <summary>
    /// Глобальный обработчик исключений для безопасного выполнения операций
    /// </summary>
    public class GlobalExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Безопасно выполняет операцию без возвращаемого значения
        /// </summary>
        public async Task HandleAsync(Func<Task> operation, string operationName)
        {
            try
            {
                await operation();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении операции: {OperationName}", operationName);
                throw;
            }
        }

        /// <summary>
        /// Безопасно выполняет операцию с возвращаемым значением
        /// </summary>
        public async Task<T> HandleAsync<T>(Func<Task<T>> operation, string operationName, T defaultValue = default!)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении операции: {OperationName}", operationName);
                return defaultValue;
            }
        }

        /// <summary>
        /// Безопасно выполняет операцию с возможностью повторных попыток
        /// </summary>
        public async Task<T> HandleWithRetryAsync<T>(Func<Task<T>> operation, string operationName, int maxRetries = 3, T defaultValue = default!)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "Попытка {Attempt} из {MaxRetries} не удалась для операции {OperationName}. Повторяем...", 
                        attempt, maxRetries, operationName);
                    
                    // Экспоненциальная задержка перед повторной попыткой
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Все {MaxRetries} попыток не удались для операции {OperationName}", maxRetries, operationName);
                    return defaultValue;
                }
            }
            
            return defaultValue;
        }

        /// <summary>
        /// Безопасно выполняет операцию с логированием результата
        /// </summary>
        public async Task<T> HandleWithLoggingAsync<T>(Func<Task<T>> operation, string operationName, T defaultValue = default!)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                var result = await operation();
                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("Операция {OperationName} выполнена успешно за {Duration}ms", operationName, duration.TotalMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "Операция {OperationName} завершилась с ошибкой через {Duration}ms", operationName, duration.TotalMilliseconds);
                return defaultValue;
            }
        }
    }
}

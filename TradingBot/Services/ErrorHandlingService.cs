using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace TradingBot.Services
{
    /// <summary>
    /// Сервис для централизованной обработки ошибок с пользовательскими сообщениями
    /// </summary>
    public class ErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;
        private readonly Dictionary<Type, string> _userFriendlyMessages;

        public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
        {
            _logger = logger;
            _userFriendlyMessages = InitializeUserFriendlyMessages();
        }

        /// <summary>
        /// Инициализирует словарь пользовательских сообщений для различных типов ошибок
        /// </summary>
        private Dictionary<Type, string> InitializeUserFriendlyMessages()
        {
            return new Dictionary<Type, string>
            {
                { typeof(ArgumentException), "⚠️ Некорректные данные. Проверьте введенную информацию." },
                { typeof(InvalidOperationException), "⚠️ Операция не может быть выполнена. Попробуйте позже." },
                { typeof(UnauthorizedAccessException), "🔒 Доступ запрещен. Проверьте права доступа." },
                { typeof(System.Net.Http.HttpRequestException), "🌐 Ошибка сети. Проверьте подключение к интернету." },
                { typeof(System.Threading.Tasks.TaskCanceledException), "⏰ Превышено время ожидания. Попробуйте еще раз." },
                { typeof(System.Text.Json.JsonException), "📝 Ошибка обработки данных. Обратитесь к поддержке." },
                { typeof(System.IO.IOException), "💾 Ошибка ввода-вывода. Проверьте доступность файлов." },
                { typeof(Microsoft.Data.Sqlite.SqliteException), "🗄️ Ошибка базы данных. Попробуйте позже." }
            };
        }

        /// <summary>
        /// Получает пользовательское сообщение об ошибке
        /// </summary>
        public string GetUserFriendlyMessage(Exception exception, string language = "ru")
        {
            if (exception == null) return GetDefaultMessage(language);

            // Логируем ошибку для разработчиков
            _logger.LogError(exception, "Ошибка для пользователя: {Message}", exception.Message);

            // Ищем специфичное сообщение для типа ошибки
            var exceptionType = exception.GetType();
            if (_userFriendlyMessages.TryGetValue(exceptionType, out var message))
            {
                return message;
            }

            // Проверяем базовые типы
            foreach (var kvp in _userFriendlyMessages)
            {
                if (exceptionType.IsSubclassOf(kvp.Key))
                {
                    return kvp.Value;
                }
            }

            // Проверяем сообщение на наличие ключевых слов
            var errorMessage = exception.Message.ToLowerInvariant();
            if (errorMessage.Contains("notion") || errorMessage.Contains("api"))
            {
                return language == "ru" 
                    ? "🌐 Ошибка интеграции с Notion. Проверьте настройки подключения." 
                    : "🌐 Notion integration error. Check connection settings.";
            }
            
            if (errorMessage.Contains("database") || errorMessage.Contains("база"))
            {
                return language == "ru" 
                    ? "🗄️ Ошибка базы данных. Попробуйте позже." 
                    : "🗄️ Database error. Try again later.";
            }
            
            if (errorMessage.Contains("network") || errorMessage.Contains("сеть"))
            {
                return language == "ru" 
                    ? "🌐 Сетевая ошибка. Проверьте подключение к интернету." 
                    : "🌐 Network error. Check internet connection.";
            }

            // Возвращаем общее сообщение
            return GetDefaultMessage(language);
        }

        /// <summary>
        /// Получает сообщение по умолчанию на указанном языке
        /// </summary>
        private string GetDefaultMessage(string language)
        {
            return language == "ru" 
                ? "⚠️ Произошла ошибка. Попробуйте еще раз или обратитесь к поддержке." 
                : "⚠️ An error occurred. Try again or contact support.";
        }

        /// <summary>
        /// Обрабатывает исключение и возвращает пользовательское сообщение
        /// </summary>
        public string HandleException(Exception exception, string language = "ru", bool includeTechnicalDetails = false)
        {
            var userMessage = GetUserFriendlyMessage(exception, language);
            
            if (includeTechnicalDetails && exception != null)
            {
                // Добавляем технические детали для отладки (только в режиме разработки)
                var technicalInfo = $"\n\n🔧 Техническая информация: {exception.GetType().Name}";
                if (!string.IsNullOrEmpty(exception.Message))
                {
                    technicalInfo += $"\n💬 {exception.Message}";
                }
                userMessage += technicalInfo;
            }
            
            return userMessage;
        }

        /// <summary>
        /// Проверяет, является ли ошибка критической
        /// </summary>
        public bool IsCriticalError(Exception exception)
        {
            if (exception == null) return false;

            var criticalTypes = new[]
            {
                typeof(System.OutOfMemoryException),
                typeof(System.StackOverflowException),
                typeof(System.Threading.ThreadAbortException)
            };

            var exceptionType = exception.GetType();
            return Array.Exists(criticalTypes, t => t == exceptionType || exceptionType.IsSubclassOf(t));
        }

        /// <summary>
        /// Получает рекомендации по устранению ошибки
        /// </summary>
        public List<string> GetTroubleshootingTips(Exception exception, string language = "ru")
        {
            var tips = new List<string>();
            
            if (exception == null) return tips;

            var errorMessage = exception.Message.ToLowerInvariant();
            
            if (errorMessage.Contains("notion") || errorMessage.Contains("api"))
            {
                if (language == "ru")
                {
                    tips.Add("🔑 Проверьте правильность токена интеграции");
                    tips.Add("🗄️ Убедитесь, что база данных существует и доступна");
                    tips.Add("🔗 Проверьте права доступа интеграции к базе данных");
                }
                else
                {
                    tips.Add("🔑 Check integration token validity");
                    tips.Add("🗄️ Ensure database exists and is accessible");
                    tips.Add("🔗 Verify integration access rights to database");
                }
            }
            else if (errorMessage.Contains("database") || errorMessage.Contains("база"))
            {
                if (language == "ru")
                {
                    tips.Add("💾 Проверьте доступность файла базы данных");
                    tips.Add("🔒 Убедитесь в правах доступа к файлу");
                    tips.Add("🔄 Попробуйте перезапустить приложение");
                }
                else
                {
                    tips.Add("💾 Check database file accessibility");
                    tips.Add("🔒 Verify file access permissions");
                    tips.Add("🔄 Try restarting the application");
                }
            }
            else if (errorMessage.Contains("network") || errorMessage.Contains("сеть"))
            {
                if (language == "ru")
                {
                    tips.Add("🌐 Проверьте подключение к интернету");
                    tips.Add("🔄 Попробуйте обновить страницу");
                    tips.Add("⏰ Подождите несколько минут и попробуйте снова");
                }
                else
                {
                    tips.Add("🌐 Check internet connection");
                    tips.Add("🔄 Try refreshing the page");
                    tips.Add("⏰ Wait a few minutes and try again");
                }
            }

            return tips;
        }
    }
}

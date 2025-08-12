using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace TradingBot.Services
{
    /// <summary>
    /// Фабрика для создания HTTP-клиентов с персональными заголовками для каждого пользователя
    /// Решает проблему гонок между потоками при изменении DefaultRequestHeaders
    /// </summary>
    public class NotionHttpClientFactory
    {
        private readonly ILogger<NotionHttpClientFactory> _logger;
        private readonly HttpClient _baseClient;

        public NotionHttpClientFactory(ILogger<NotionHttpClientFactory> logger, HttpClient baseClient)
        {
            _logger = logger;
            _baseClient = baseClient;
        }

        /// <summary>
        /// Создает HTTP-клиент с персональными заголовками для конкретного пользователя
        /// </summary>
        public HttpClient CreateClient(string integrationToken, string? notionVersion = "2022-06-28")
        {
            try
            {
                // Создаем новый экземпляр клиента для каждого запроса
                var client = new HttpClient();
                
                // Копируем базовые настройки
                client.Timeout = _baseClient.Timeout;
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {integrationToken}");
                client.DefaultRequestHeaders.Add("Notion-Version", notionVersion ?? "2022-06-28");
                
                _logger.LogDebug("Создан HTTP-клиент для пользователя с токеном: {TokenPrefix}...", 
                    integrationToken.Length > 8 ? integrationToken.Substring(0, 8) : "short");
                
                return client;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании HTTP-клиента для пользователя");
                throw;
            }
        }

        /// <summary>
        /// Создает HTTP-клиент с персональными заголовками и автоматическим освобождением ресурсов
        /// </summary>
        public async Task<T> UseClientAsync<T>(string integrationToken, Func<HttpClient, Task<T>> operation, string? notionVersion = "2022-06-28")
        {
            using var client = CreateClient(integrationToken, notionVersion);
            try
            {
                return await operation(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении операции с HTTP-клиентом");
                throw;
            }
        }

        /// <summary>
        /// Создает HTTP-клиент с персональными заголовками для длительных операций
        /// </summary>
        public HttpClient CreateLongLivedClient(string integrationToken, string? notionVersion = "2022-06-28")
        {
            try
            {
                var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(5); // Увеличенный таймаут для длительных операций
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {integrationToken}");
                client.DefaultRequestHeaders.Add("Notion-Version", notionVersion ?? "2022-06-28");
                
                _logger.LogDebug("Создан долгоживущий HTTP-клиент для пользователя");
                return client;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании долгоживущего HTTP-клиента");
                throw;
            }
        }
    }
}

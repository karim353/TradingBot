using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http.Headers;

namespace TradingBot.Services
{
    /// <summary>
    /// Фабрика для создания HTTP-клиентов Notion с политиками повторов
    /// </summary>
    public class NotionHttpClientFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<NotionHttpClientFactory> _logger;

        public NotionHttpClientFactory(IHttpClientFactory httpClientFactory, ILogger<NotionHttpClientFactory> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Создает HTTP-клиент для Notion с настройками по умолчанию
        /// </summary>
        public HttpClient CreateClient(string integrationToken)
        {
            var client = _httpClientFactory.CreateClient("NotionClient");
            ConfigureClient(client, integrationToken);
            return client;
        }

        /// <summary>
        /// Создает HTTP-клиент для долгих операций
        /// </summary>
        public HttpClient CreateLongRunningClient(string integrationToken)
        {
            var client = _httpClientFactory.CreateClient("NotionLongRunningClient");
            ConfigureClient(client, integrationToken);
            return client;
        }

        /// <summary>
        /// Выполняет операцию с HTTP-клиентом
        /// </summary>
        public async Task<T> UseClientAsync<T>(string integrationToken, Func<HttpClient, Task<T>> operation)
        {
            using var client = CreateClient(integrationToken);
            try
            {
                return await operation(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении операции с Notion API");
                throw;
            }
        }

        /// <summary>
        /// Настраивает HTTP-клиент для работы с Notion
        /// </summary>
        private void ConfigureClient(HttpClient client, string integrationToken)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {integrationToken}");
            client.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");
            client.DefaultRequestHeaders.Add("User-Agent", "TradingBot/1.0");
        }
    }

    /// <summary>
    /// Расширения для настройки HTTP-клиентов в DI
    /// </summary>
    public static class NotionHttpClientFactoryExtensions
    {
        /// <summary>
        /// Добавляет HTTP-клиенты для Notion с политиками повторов
        /// </summary>
        public static IServiceCollection AddNotionHttpClients(this IServiceCollection services)
        {
            // Политика повторов с экспоненциальным бэк-оффом
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)));

            // Политика для долгих операций
            var longRunningRetryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(5, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)));

            // Политика для таймаутов
            var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(30);

            // Комбинированная политика
            var combinedPolicy = Policy.WrapAsync(retryPolicy, timeoutPolicy);
            var longRunningCombinedPolicy = Policy.WrapAsync(longRunningRetryPolicy, timeoutPolicy);

            services.AddHttpClient("NotionClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddPolicyHandler(combinedPolicy);

            services.AddHttpClient("NotionLongRunningClient", client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5);
            })
            .AddPolicyHandler(longRunningCombinedPolicy);

            return services;
        }
    }
}

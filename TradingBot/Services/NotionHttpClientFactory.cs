using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.CircuitBreaker;
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
            // Политика повторов с джиттером
            var jitterDelays = Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(200), retryCount: 5);
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => (int)msg.StatusCode == 429)
                .WaitAndRetryAsync(jitterDelays);

            // Политика для долгих операций
            var longRunningRetryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => (int)msg.StatusCode == 429)
                .WaitAndRetryAsync(jitterDelays);

            // Политика для таймаутов
            var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(30);

            // Circuit breaker
            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => (int)msg.StatusCode == 429)
                .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 5, durationOfBreak: TimeSpan.FromSeconds(30));

            // Комбинированная политика
            var combinedPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
            var longRunningCombinedPolicy = Policy.WrapAsync(longRunningRetryPolicy, circuitBreakerPolicy, timeoutPolicy);

            services.AddHttpClient("NotionClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddPolicyHandler(combinedPolicy)
            .AddHttpMessageHandler(() => new MetricsDelegatingHandler("notion_client"));

            services.AddHttpClient("NotionLongRunningClient", client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5);
            })
            .AddPolicyHandler(longRunningCombinedPolicy)
            .AddHttpMessageHandler(() => new MetricsDelegatingHandler("notion_long_client"));

            return services;
        }
    }
    
    public class MetricsDelegatingHandler : DelegatingHandler
    {
        private readonly string _clientName;
        private static readonly Prometheus.Histogram Duration = Prometheus.Metrics.CreateHistogram(
            "tradingbot_http_client_duration_seconds",
            "HTTP client duration",
            new Prometheus.HistogramConfiguration { LabelNames = new[] { "client", "status" } });

        public MetricsDelegatingHandler(string clientName)
        {
            _clientName = clientName;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();
            Duration.WithLabels(_clientName, ((int)response.StatusCode).ToString()).Observe(sw.Elapsed.TotalSeconds);
            return response;
        }
    }
}

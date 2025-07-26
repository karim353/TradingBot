using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TradingBot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TradingBot.Services
{
    /// <summary>
    /// Сервис для интеграции с Notion: создание страницы сделки в базе данных Notion.
    /// </summary>
    public class NotionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _databaseId;
        private readonly ILogger<NotionService> _logger;

        public NotionService(HttpClient httpClient, IConfiguration config, ILogger<NotionService> logger)
        {
            _httpClient = httpClient;
            _databaseId = config["Notion:DatabaseId"];
            _logger = logger;
        }

        /// <summary>
        /// Создаёт новую страницу в базе Notion для переданной сделки.
        /// Возвращает идентификатор созданной страницы.
        /// </summary>
        public async Task<string> CreatePageForTradeAsync(Trade trade)
        {
            // Формируем объект с данными для Notion API
            var newPage = new
            {
                parent = new { database_id = _databaseId },
                properties = new
                {
                    Date = new { date = new { start = trade.Date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") } },
                    Ticker = new { title = new object[] { new { text = new { content = trade.Ticker } } } },
                    Direction = new { select = new { name = trade.Direction } },
                    PnL = new { number = trade.PnL },
                    Comment = new { rich_text = new object[] { new { text = new { content = trade.Comment ?? "" } } } },
                    Entry = new { number = trade.Entry },
                    SL = new { number = trade.SL },
                    TP = new { number = trade.TP },
                    Volume = new { number = trade.Volume }
                }
            };

            string json = JsonSerializer.Serialize(newPage);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("pages", content);
                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Notion API вернул ошибку: {0}", errorBody);
                    throw new Exception($"Notion API error: {response.StatusCode}");
                }
                // Получаем содержимое ответа и извлекаем ID созданной страницы
                string responseBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("id", out JsonElement idElem))
                {
                    string pageId = idElem.GetString();
                    return pageId;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось создать страницу в Notion");
                throw;
            }
        }
    }
}

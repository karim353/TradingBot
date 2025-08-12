using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TradingBot.Models;

namespace TradingBot.Services
{
    public class NotionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _databaseId;
        private readonly ILogger<NotionService> _logger;

        public NotionService(HttpClient httpClient, IConfiguration config, ILogger<NotionService> logger)
        {
            _httpClient = httpClient;
            _databaseId = config["Notion:DatabaseId"] ?? throw new Exception("Notion DatabaseId not configured.");
            _logger = logger;
        }

        public async Task<string?> CreatePageForTradeAsync(Trade trade)
        {
            try
            {
                var properties = TradeToProperties(trade);
                // Отключаем сохранение скриншота в свойствах, так как поле "Скриншот" убрано в новой базе
                /*
                if (!string.IsNullOrEmpty(trade.ScreenshotPath))
                {
                    // Скриншот: логика отключена - вместо сохранения в Notion поле остается пустым
                    string screenshotText = trade.ScreenshotPath;
                    properties["Скриншот"] = new
                    {
                        rich_text = new[] { new { text = new { content = screenshotText } } }
                    };
                }
                */
                var pagePayload = new { parent = new { database_id = _databaseId }, properties };
                var content = new StringContent(JsonSerializer.Serialize(pagePayload), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync("https://api.notion.com/v1/pages", content);
                if (response.IsSuccessStatusCode)
                {
                    string resultJson = await response.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(resultJson);
                    string pageId = doc.RootElement.GetProperty("id").GetString() ?? string.Empty;
                    _logger.LogInformation("Создана страница сделки в Notion, PageID={0}", pageId);
                    return pageId;
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Ошибка при создании страницы в Notion: {0}", errorBody);
                    throw new Exception("Не удалось создать сделку в Notion: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось выполнить CreatePageForTradeAsync для сделки");
                throw;
            }
        }

        public async Task UpdatePageForTradeAsync(Trade trade)
        {
            try
            {
                var properties = TradeToProperties(trade);
                // Скриншот: отключаем обновление поля "Скриншот" (новая база его не использует)
                /*
                if (!string.IsNullOrEmpty(trade.ScreenshotPath))
                {
                    string screenshotText = trade.ScreenshotPath;
                    properties["Скриншот"] = new
                    {
                        rich_text = new[] { new { text = new { content = screenshotText } } }
                    };
                }
                else
                {
                    properties["Скриншот"] = new { rich_text = Array.Empty<object>() };
                }
                */
                var payload = new { properties };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                string uri = $"https://api.notion.com/v1/pages/{trade.NotionPageId}";
                HttpResponseMessage response = await _httpClient.PatchAsync(uri, content);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Страница сделки {0} обновлена в Notion", trade.NotionPageId);
                }
                else
                {
                    string errorMsg = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Ошибка при обновлении сделки в Notion: {0}", errorMsg);
                    throw new Exception("Не удалось обновить сделку в Notion: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось выполнить UpdatePageForTradeAsync для сделки {0}", trade.NotionPageId);
                throw;
            }
        }

        public async Task DeletePageForTradeAsync(string pageId)
        {
            try
            {
                var payload = new { archived = true };
                var req = new HttpRequestMessage(HttpMethod.Patch, $"https://api.notion.com/v1/pages/{pageId}")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                HttpResponseMessage response = await _httpClient.SendAsync(req);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Страница сделки {0} помечена как удалённая (архив) в Notion", pageId);
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Ошибка при удалении страницы {0} в Notion: {1}", pageId, error);
                    throw new Exception("Не удалось удалить сделку в Notion");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось выполнить DeletePageForTradeAsync для PageID={0}", pageId);
                throw;
            }
        }

        public async Task<List<Trade>> GetTradesFromNotionAsync()
        {
            var trades = new List<Trade>();
            string? nextCursor = null;
            do
            {
                object payload = nextCursor == null 
                    ? new { page_size = 100 } 
                    : new { page_size = 100, start_cursor = nextCursor };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PostAsync($"https://api.notion.com/v1/databases/{_databaseId}/query", content);
                if (!response.IsSuccessStatusCode)
                {
                    string err = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Ошибка при получении сделок из Notion: {0}", err);
                    throw new Exception("Не удалось загрузить список сделок из Notion");
                }
                string json = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(json);
                foreach (JsonElement page in doc.RootElement.GetProperty("results").EnumerateArray())
                {
                    trades.Add(ParseTradeFromPage(page));
                }
                bool hasMore = doc.RootElement.GetProperty("has_more").GetBoolean();
                nextCursor = hasMore ? doc.RootElement.GetProperty("next_cursor").GetString() : null;
            }
            while (nextCursor != null);
            _logger.LogInformation("Загружено {0} сделок из Notion.", trades.Count);
            return trades;
        }

        public async Task<Dictionary<string, List<string>>> GetSelectOptionsAsync()
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"https://api.notion.com/v1/databases/{_databaseId}");
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ошибка при получении схемы базы Notion: {0}", error);
                throw new Exception("Не удалось загрузить справочные данные из Notion");
            }
            string json = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(json);
            var optionsByField = new Dictionary<string, List<string>>();
            JsonElement props = doc.RootElement.GetProperty("properties");
            foreach (var prop in props.EnumerateObject())
            {
                if (prop.Value.GetProperty("type").GetString() is string type && (type == "select" || type == "multi_select"))
                {
                    var options = new List<string>();
                    foreach (var opt in prop.Value.GetProperty(type).GetProperty("options").EnumerateArray())
                    {
                        string? name = opt.GetProperty("name").GetString();
                        if (!string.IsNullOrWhiteSpace(name))
                            options.Add(name);
                    }
                    optionsByField[prop.Name] = options;
                }
            }
            return optionsByField;
        }

        public async Task<List<string>> GetSelectOptionsAsync(string propertyName)
        {
            var allOptions = await GetSelectOptionsAsync();
            return allOptions.TryGetValue(propertyName, out var options) ? options : new List<string>();
        }

        private Dictionary<string, object> TradeToProperties(Trade trade)
        {
            // Формируем словарь свойств Notion для сделки по новой структуре
            var properties = new Dictionary<string, object>
            {
                ["Pair"] = new { title = new[] { new { text = new { content = trade.Ticker } } } },
                ["Date"] = new { date = new { start = trade.Date.ToString("yyyy-MM-dd") } }
            };
            if (!string.IsNullOrWhiteSpace(trade.Account))
            {
                properties["Account"] = new { select = new { name = trade.Account } };
            }
            if (!string.IsNullOrWhiteSpace(trade.Session))
            {
                properties["Session"] = new { select = new { name = trade.Session } };
            }
            if (!string.IsNullOrWhiteSpace(trade.Position))
            {
                properties["Position"] = new { select = new { name = trade.Position } };
            }
            if (!string.IsNullOrWhiteSpace(trade.Direction))
            {
                properties["Direction"] = new { select = new { name = trade.Direction } };
            }
            if (trade.Context != null && trade.Context.Count > 0)
            {
                properties["Context"] = new { multi_select = trade.Context.Select(val => new { name = val }).ToArray() };
            }
            if (trade.Setup != null && trade.Setup.Count > 0)
            {
                properties["Setup"] = new { multi_select = trade.Setup.Select(val => new { name = val }).ToArray() };
            }
            if (!string.IsNullOrWhiteSpace(trade.Result))
            {
                properties["Result"] = new { select = new { name = trade.Result } };
            }
            if (!string.IsNullOrWhiteSpace(trade.RR))
            {
                properties["RR"] = new { rich_text = new[] { new { text = new { content = trade.RR } } } };
            }
            if (trade.Risk.HasValue)
            {
                properties["Risk"] = new { number = trade.Risk.Value };
            }
            if (!string.IsNullOrWhiteSpace(trade.EntryDetails))
            {
                properties["Entry Details"] = new 
                {
                    rich_text = new[] { new { text = new { content = trade.EntryDetails } } }
                };
            }
            if (!string.IsNullOrWhiteSpace(trade.Note))
            {
                properties["Note"] = new 
                {
                    rich_text = new[] { new { text = new { content = trade.Note } } }
                };
            }
            if (trade.Emotions != null && trade.Emotions.Count > 0)
            {
                properties["Emotions"] = new { multi_select = trade.Emotions.Select(val => new { name = val }).ToArray() };
            }
            // % Profit – процент прибыли
            properties["% Profit"] = new { number = trade.PnL };
            return properties;
        }

        private Trade ParseTradeFromPage(JsonElement page)
        {
            var props = page.GetProperty("properties");
            Trade trade = new Trade
            {
                NotionPageId = page.GetProperty("id").GetString() ?? string.Empty,
                Ticker = props.GetProperty("Pair").GetProperty("title").EnumerateArray()
                                 .FirstOrDefault().GetProperty("text").GetProperty("content").GetString() ?? string.Empty,
                Date = DateTime.Parse(props.GetProperty("Date").GetProperty("date").GetProperty("start").GetString() ?? DateTime.UtcNow.ToString())
            };
            if (props.TryGetProperty("Account", out var accElem) && accElem.TryGetProperty("select", out var accVal) && accVal.ValueKind != JsonValueKind.Null)
            {
                trade.Account = accVal.GetProperty("name").GetString();
            }
            if (props.TryGetProperty("Session", out var sessElem) && sessElem.TryGetProperty("select", out var sessVal) && sessVal.ValueKind != JsonValueKind.Null)
            {
                trade.Session = sessVal.GetProperty("name").GetString();
            }
            if (props.TryGetProperty("Position", out var posElem) && posElem.TryGetProperty("select", out var posVal) && posVal.ValueKind != JsonValueKind.Null)
            {
                trade.Position = posVal.GetProperty("name").GetString() ?? string.Empty;
            }
            if (props.TryGetProperty("Direction", out var dirElem) && dirElem.TryGetProperty("select", out var dirVal) && dirVal.ValueKind != JsonValueKind.Null)
            {
                trade.Direction = dirVal.GetProperty("name").GetString() ?? string.Empty;
            }
            if (props.TryGetProperty("Context", out var ctxElem) && ctxElem.TryGetProperty("multi_select", out var ctxArray))
            {
                trade.Context = ctxArray.EnumerateArray()
                                .Select(r => r.GetProperty("name").GetString() ?? "")
                                .Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            }
            if (props.TryGetProperty("Setup", out var setupElem) && setupElem.TryGetProperty("multi_select", out var setupArray))
            {
                trade.Setup = setupArray.EnumerateArray()
                                .Select(r => r.GetProperty("name").GetString() ?? "")
                                .Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            }
            if (props.TryGetProperty("Result", out var resElem) && resElem.TryGetProperty("select", out var resVal) && resVal.ValueKind != JsonValueKind.Null)
            {
                trade.Result = resVal.GetProperty("name").GetString();
            }
            if (props.TryGetProperty("RR", out var rrElem) && rrElem.TryGetProperty("rich_text", out var rrText))
            {
                trade.RR = string.Concat(rrText.EnumerateArray()
                                         .Select(r => r.GetProperty("text").GetProperty("content").GetString()));
            }
            if (props.TryGetProperty("Risk", out var riskElem) && riskElem.TryGetProperty("number", out var riskNum) && riskNum.ValueKind != JsonValueKind.Null)
            {
                trade.Risk = riskNum.GetDecimal();
            }
            if (props.TryGetProperty("Entry Details", out var entryElem) && entryElem.TryGetProperty("rich_text", out var entryText))
            {
                trade.EntryDetails = string.Concat(entryText.EnumerateArray()
                                             .Select(r => r.GetProperty("text").GetProperty("content").GetString()));
            }
            if (props.TryGetProperty("Note", out var noteElem) && noteElem.TryGetProperty("rich_text", out var noteText))
            {
                trade.Note = string.Concat(noteText.EnumerateArray()
                                         .Select(r => r.GetProperty("text").GetProperty("content").GetString()));
            }
            if (props.TryGetProperty("Emotions", out var emoElem) && emoElem.TryGetProperty("multi_select", out var emoArray))
            {
                trade.Emotions = emoArray.EnumerateArray()
                                  .Select(r => r.GetProperty("name").GetString() ?? "")
                                  .Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            }
            if (props.TryGetProperty("% Profit", out var profitElem) && profitElem.TryGetProperty("number", out var profitNum) && profitNum.ValueKind != JsonValueKind.Null)
            {
                trade.PnL = profitNum.GetDecimal();
            }
            return trade;
        }

        private async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> sendFunc)
        {
            int attempt = 0;
            while (true)
            {
                attempt++;
                try
                {
                    HttpResponseMessage response = await sendFunc();
                    if (response.IsSuccessStatusCode)
                        return response;
                    if ((int)response.StatusCode >= 500 && attempt < 3)
                    {
                        _logger.LogWarning("Notion API вернул ошибку {0}, попытка {1}", response.StatusCode, attempt);
                    }
                    else
                    {
                        return response;
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "Сеть недоступна, попытка {0}", attempt);
                    if (attempt >= 3) throw;
                }
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
        }
    }
}
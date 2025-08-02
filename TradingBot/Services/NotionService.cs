using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TradingBot.Models;

namespace TradingBot.Services;

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

    public async Task<string?> CreatePageForTradeAsync(Trade trade)
{
    try
    {
        var properties = TradeToProperties(trade);

        // Если есть путь к файлу, загружаем файл, но в поле "Скриншот" пишем ссылку (или просто строку)
        if (!string.IsNullOrEmpty(trade.ScreenshotPath))
        {
            // Можно загрузить файл на сторонний хостинг и сюда писать ссылку,
            // или просто сохранять имя файла как текст, если хостинга нет.
            // Если хочешь загрузить в Notion и получить ссылку — тут должна быть твоя логика.
            string screenshotText = trade.ScreenshotPath; // или ссылка, если есть
            properties["Скриншот"] = new
            {
                rich_text = new[] { new { text = new { content = screenshotText } } }
            };
        }

        var pagePayload = new { parent = new { database_id = _databaseId }, properties };
        var content = new StringContent(JsonSerializer.Serialize(pagePayload), Encoding.UTF8, "application/json");

        HttpResponseMessage response = await SendWithRetryAsync(() => _httpClient.PostAsync("https://api.notion.com/v1/pages", content));
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
            var payload = new { properties };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            string uri = $"https://api.notion.com/v1/pages/{trade.NotionPageId}";
            HttpResponseMessage response = await SendWithRetryAsync(() => _httpClient.PatchAsync(uri, content));
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
            HttpResponseMessage response = await SendWithRetryAsync(() =>
            {
                var req = new HttpRequestMessage(HttpMethod.Patch, $"https://api.notion.com/v1/pages/{pageId}")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                return _httpClient.SendAsync(req);
            });
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
            object payload;
            if (nextCursor == null)
            {
                payload = new { page_size = 100 };
            }
            else
            {
                payload = new { page_size = 100, start_cursor = nextCursor };
            }

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await SendWithRetryAsync(() =>
                _httpClient.PostAsync($"https://api.notion.com/v1/databases/{_databaseId}/query", content));
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
            nextCursor = doc.RootElement.GetProperty("has_more").GetBoolean()
                ? doc.RootElement.GetProperty("next_cursor").GetString()
                : null;
        }
        while (nextCursor != null);
        _logger.LogInformation("Загружено {0} сделок из Notion.", trades.Count);
        return trades;
    }


    public async Task<Dictionary<string, List<string>>> GetSelectOptionsAsync()
    {
        HttpResponseMessage response = await SendWithRetryAsync(() => _httpClient.GetAsync($"https://api.notion.com/v1/databases/{_databaseId}"));
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

    public async Task<string> UploadScreenshotAsync(string filePath)
{
    // 1. Запрос на создание upload slot
    var createReq = new HttpRequestMessage(HttpMethod.Post, "https://api.notion.com/v1/file_uploads")
    {
        Content = new StringContent("{\"mode\":\"single_part\"}", Encoding.UTF8, "application/json")
    };
    var createResp = await _httpClient.SendAsync(createReq);
    if (!createResp.IsSuccessStatusCode)
    {
        var err = await createResp.Content.ReadAsStringAsync();
        _logger.LogError("Ошибка инициации загрузки файла: {0}", err);
        throw new Exception("Не удалось инициировать загрузку файла в Notion");
    }
    var createJson = await createResp.Content.ReadAsStringAsync();
    using var createDoc = JsonDocument.Parse(createJson);
    var uploadId = createDoc.RootElement.GetProperty("id").GetString();
    var uploadUrl = createDoc.RootElement.GetProperty("upload_url").GetString();

    // 2. Загружаем файл RAW (НЕ multipart!!!)
    using var fileStream = File.OpenRead(filePath);
    var fileContent = new StreamContent(fileStream);
    string ext = Path.GetExtension(filePath).ToLowerInvariant();
    fileContent.Headers.ContentType = new MediaTypeHeaderValue(ext == ".png" ? "image/png" : ext == ".gif" ? "image/gif" : "image/jpeg");

    // ОЧЕНЬ ВАЖНО: НЕ multipart, просто fileContent!
    var sendReq = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
    {
        Content = fileContent
    };

    // Notion требует НИКАКИХ лишних заголовков (Authorization, Notion-Version и т.д.) для upload_url!
    // Поэтому используем новый HttpClient без default headers:
    using (var uploadClient = new HttpClient())
    {
        var sendResp = await uploadClient.SendAsync(sendReq);
        if (!sendResp.IsSuccessStatusCode)
        {
            var err = await sendResp.Content.ReadAsStringAsync();
            _logger.LogError("Ошибка при загрузке файла в Notion: {0}", err);
            throw new Exception("Не удалось загрузить файл-скриншот в Notion");
        }
    }

    _logger.LogInformation("Файл {0} загружен в Notion (FileUpload ID = {1})", Path.GetFileName(filePath), uploadId);
    return uploadId;
}



    private Dictionary<string, object> TradeToProperties(Trade trade)
    {
        var properties = new Dictionary<string, object>
        {
            ["Актив"] = new { title = new[] { new { text = new { content = trade.Ticker } } } },
            ["Дата"] = new { date = new { start = trade.Date.ToString("yyyy-MM-dd") } }
        };

        // Вход — именно Entry/Open Price
        if (trade.Entry.HasValue)
            properties["Вход"] = new { number = trade.Entry.Value };

        // Выход — Exit/Close Price
        if (trade.Exit.HasValue)
            properties["Выход"] = new { number = trade.Exit.Value };

        // PnL ($) — только если это абсолютная прибыль, иначе исправь!
        if (trade.PnL != 0m)
            properties["PnL ($)"] = new { number = trade.PnL };

        // % Депозита — только если ты действительно считаешь % (иначе убери!)
        if (trade.PercentDeposit.HasValue)
            properties["% Депозита"] = new { number = trade.PercentDeposit.Value / 100m }; // Notion percent!

        if (trade.Volume.HasValue)
            properties["Лот"] = new { number = trade.Volume.Value };

        if (!string.IsNullOrWhiteSpace(trade.Comment))
            properties["Комментарии"] = new { rich_text = new[] { new { text = new { content = trade.Comment } } } };

        // Ошибки — rich_text
        if (trade.Mistakes != null && trade.Mistakes.Count > 0)
            properties["Ошибки"] = new { rich_text = new[] { new { text = new { content = string.Join(", ", trade.Mistakes) } } } };

        // Скриншот добавляй отдельно (см. ниже)
        return properties;
    }



    private Trade ParseTradeFromPage(JsonElement page)
{
    var props = page.GetProperty("properties");
    Trade trade = new Trade
    {
        NotionPageId = page.GetProperty("id").GetString() ?? string.Empty,
        Ticker = props.GetProperty("Актив").GetProperty("title").EnumerateArray()
                    .FirstOrDefault().GetProperty("text").GetProperty("content").GetString() ?? string.Empty,
        Date = DateTime.Parse(props.GetProperty("Дата").GetProperty("date").GetProperty("start").GetString() ?? DateTime.UtcNow.ToString())
    };

    if (props.TryGetProperty("Вход", out var entryElem) && entryElem.TryGetProperty("number", out var entryNum) && entryNum.ValueKind != JsonValueKind.Null)
        trade.Entry = entryNum.GetDecimal();

    if (props.TryGetProperty("Выход", out var exitElem) && exitElem.TryGetProperty("number", out var exitNum) && exitNum.ValueKind != JsonValueKind.Null)
        trade.Exit = exitNum.GetDecimal();

    if (props.TryGetProperty("% Депозита", out var pctElem) && pctElem.TryGetProperty("number", out var pctNum) && pctNum.ValueKind != JsonValueKind.Null)
        trade.PnL = pctNum.GetDecimal();

    if (props.TryGetProperty("PnL ($)", out var profitElem) && profitElem.TryGetProperty("number", out var profitNum) && profitNum.ValueKind != JsonValueKind.Null)
        trade.Profit = profitNum.GetDecimal();

    if (props.TryGetProperty("Лот", out var lotElem) && lotElem.TryGetProperty("number", out var lotNum) && lotNum.ValueKind != JsonValueKind.Null)
        trade.Volume = lotNum.GetDecimal();

    if (props.TryGetProperty("Комментарии", out var commElem) && commElem.TryGetProperty("rich_text", out var richTextElem))
        trade.Comment = string.Concat(richTextElem.EnumerateArray().Select(r => r.GetProperty("text").GetProperty("content").GetString()));

    // Ошибки — из rich_text, а не multi_select
    if (props.TryGetProperty("Ошибки", out var mistakesElem) && mistakesElem.TryGetProperty("rich_text", out var richTextMistake))
        trade.Mistakes = richTextMistake.EnumerateArray().Select(r => r.GetProperty("text").GetProperty("content").GetString() ?? "")
                         .Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

    // Скриншот можно читать по аналогии, если будешь использовать rich_text

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

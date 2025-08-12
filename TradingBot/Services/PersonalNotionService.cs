using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TradingBot.Models;

namespace TradingBot.Services
{
    /// <summary>
    /// Базовый класс для общих методов работы с Notion API
    /// </summary>
    public abstract class NotionServiceBase
    {
        protected readonly ILogger _logger;

        protected NotionServiceBase(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Безопасно извлекает опции из свойства Notion
        /// </summary>
        protected List<string> ExtractOptionsFromProperty(JsonElement property, string propertyType)
        {
            var options = new List<string>();
            
            try
            {
                if (property.TryGetProperty(propertyType, out var typeElement) && 
                    typeElement.TryGetProperty("options", out var optionsElement))
                {
                    foreach (var opt in optionsElement.EnumerateArray())
                    {
                        if (opt.TryGetProperty("name", out var nameElement))
                        {
                            var name = nameElement.GetString();
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                options.Add(name);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка при извлечении опций из свойства {PropertyType}", propertyType);
            }
            
            return options;
        }

        /// <summary>
        /// Безопасно извлекает схему базы данных Notion
        /// </summary>
        protected Dictionary<string, List<string>> ExtractDatabaseSchema(JsonElement propertiesElement)
        {
            var optionsByField = new Dictionary<string, List<string>>();
            
            try
            {
                foreach (var prop in propertiesElement.EnumerateObject())
                {
                    if (prop.Value.TryGetProperty("type", out var typeElement))
                    {
                        var type = typeElement.GetString();
                        if (type == "select" || type == "multi_select")
                        {
                            var options = ExtractOptionsFromProperty(prop.Value, type);
                            if (options.Any())
                            {
                                optionsByField[prop.Name] = options;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка при извлечении схемы базы данных");
            }
            
            return optionsByField;
        }
    }

    public class PersonalNotionService : NotionServiceBase
    {
        private readonly NotionHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public PersonalNotionService(NotionHttpClientFactory httpClientFactory, ILogger<PersonalNotionService> logger, IConfiguration configuration)
            : base(logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        /// <summary>
        /// Получает готовые ответы из персональной базы данных пользователя
        /// </summary>
        public async Task<Dictionary<string, List<string>>> GetPersonalOptionsAsync(UserSettings userSettings)
        {
            if (!userSettings.NotionEnabled || string.IsNullOrEmpty(userSettings.NotionDatabaseId))
            {
                return new Dictionary<string, List<string>>();
            }

            // Логируем источник настроек для отладки
            var isDeveloperSettings = userSettings.NotionDatabaseId == _configuration["Notion:DatabaseId"];
            if (isDeveloperSettings)
            {
                _logger.LogInformation("Используются настройки разработчика из appsettings.json");
            }

            try
            {
                // Используем фабрику для создания безопасного HTTP-клиента
                return await _httpClientFactory.UseClientAsync(
                    userSettings.NotionIntegrationToken!,
                    async (client) =>
                    {
                        // Получаем схему базы данных
                        var response = await client.GetAsync($"https://api.notion.com/v1/databases/{userSettings.NotionDatabaseId}");
                        
                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("Не удалось получить схему базы данных Notion для пользователя. Status: {Status}", response.StatusCode);
                            return new Dictionary<string, List<string>>();
                        }

                        string json = await response.Content.ReadAsStringAsync();
                        using JsonDocument doc = JsonDocument.Parse(json);
                        
                        if (doc.RootElement.TryGetProperty("properties", out var props))
                        {
                            var optionsByField = ExtractDatabaseSchema(props);
                            _logger.LogInformation("Получено {Count} полей с опциями из персональной БД Notion пользователя", optionsByField.Count);
                            return optionsByField;
                        }
                        
                        return new Dictionary<string, List<string>>();
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении опций из персональной БД Notion");
                return new Dictionary<string, List<string>>();
            }
        }

        /// <summary>
        /// Получает опции для конкретного поля из персональной БД
        /// </summary>
        public async Task<List<string>> GetPersonalOptionsAsync(UserSettings userSettings, string fieldName)
        {
            var allOptions = await GetPersonalOptionsAsync(userSettings);
            return allOptions.TryGetValue(fieldName, out var options) ? options : new List<string>();
        }

        /// <summary>
        /// Проверяет доступность персональной базы данных Notion
        /// </summary>
        public async Task<bool> TestPersonalNotionConnectionAsync(UserSettings userSettings)
        {
            if (!userSettings.NotionEnabled || string.IsNullOrEmpty(userSettings.NotionIntegrationToken) || string.IsNullOrEmpty(userSettings.NotionDatabaseId))
            {
                return false;
            }

            try
            {
                return await _httpClientFactory.UseClientAsync(
                    userSettings.NotionIntegrationToken,
                    async (client) =>
                    {
                        var response = await client.GetAsync($"https://api.notion.com/v1/databases/{userSettings.NotionDatabaseId}");
                        return response.IsSuccessStatusCode;
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке подключения к персональной БД Notion");
                return false;
            }
        }

        /// <summary>
        /// Извлекает Database ID из URL Notion
        /// </summary>
        public string? ExtractDatabaseIdFromUrl(string notionUrl)
        {
            try
            {
                // Пример URL: https://emphasized-aardvark-dab.notion.site/Defaust-Trader-13158ba9850f8067bfc5d349521b0fd8
                var uri = new Uri(notionUrl);
                var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                
                if (pathSegments.Length >= 2)
                {
                    // Последний сегмент содержит Database ID
                    return pathSegments[^1];
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при извлечении Database ID из URL: {Url}", notionUrl);
                return null;
            }
        }

        /// <summary>
        /// Получает готовые ответы для конкретного поля с fallback на стандартные
        /// </summary>
        public async Task<List<string>> GetOptionsWithFallbackAsync(
            UserSettings userSettings,
            string fieldName,
            List<string> defaultOptions,
            string userId)
        {
            if (userSettings.NotionEnabled && !string.IsNullOrEmpty(userSettings.NotionDatabaseId))
            {
                var personalOptions = await GetPersonalOptionsAsync(userSettings, fieldName);
                if (personalOptions.Any())
                {
                    // Объединяем персональные опции с дефолтными, убирая дубликаты
                    var combined = new List<string>(personalOptions);
                    foreach (var option in defaultOptions)
                    {
                        if (!combined.Contains(option, StringComparer.OrdinalIgnoreCase))
                        {
                            combined.Add(option);
                        }
                    }
                    return combined;
                }
            }
            // Fallback на настройки разработчика только для указанного разработчика
            var developerUserId = _configuration["Developer:UserId"];
            if (!string.IsNullOrEmpty(developerUserId) && string.Equals(developerUserId, userId, StringComparison.Ordinal))
            {
                var developerOptions = await GetDeveloperOptionsAsync(fieldName);
                if (developerOptions.Any())
                {
                    var combined = new List<string>(developerOptions);
                    foreach (var option in defaultOptions)
                    {
                        if (!combined.Contains(option, StringComparer.OrdinalIgnoreCase))
                        {
                            combined.Add(option);
                        }
                    }
                    return combined;
                }
            }
            
            return defaultOptions;
        }

        /// <summary>
        /// Получает опции из настроек разработчика (appsettings.json)
        /// </summary>
        private async Task<List<string>> GetDeveloperOptionsAsync(string fieldName)
        {
            try
            {
                var notionToken = _configuration["Notion:ApiToken"];
                var notionDbId = _configuration["Notion:DatabaseId"];
                
                if (string.IsNullOrEmpty(notionToken) || string.IsNullOrEmpty(notionDbId))
                {
                    return new List<string>();
                }

                // Создаем временные настройки разработчика
                var developerSettings = new UserSettings
                {
                    NotionEnabled = true,
                    NotionDatabaseId = notionDbId,
                    NotionIntegrationToken = notionToken
                };

                var options = await GetPersonalOptionsAsync(developerSettings, fieldName);
                _logger.LogInformation("Получено {Count} опций из настроек разработчика для поля {Field}", options.Count, fieldName);
                return options;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка при получении опций разработчика для поля {Field}", fieldName);
                return new List<string>();
            }
        }
    }
}

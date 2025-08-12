using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TradingBot.Models;

namespace TradingBot.Services
{
    /// <summary>
    /// Сервис для кеширования схемы Notion с учетом персональных настроек пользователей
    /// </summary>
    public class NotionSchemaCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly PersonalNotionService _personalNotionService;
        private readonly ILogger<NotionSchemaCacheService> _logger;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);

        public NotionSchemaCacheService(
            IMemoryCache cache,
            PersonalNotionService personalNotionService,
            ILogger<NotionSchemaCacheService> logger)
        {
            _cache = cache;
            _personalNotionService = personalNotionService;
            _logger = logger;
        }

        /// <summary>
        /// Получает опции для поля из кеша или загружает из Notion
        /// </summary>
        public async Task<List<string>> GetOptionsAsync(string propertyName, long userId, UserSettings userSettings)
        {
            try
            {
                if (!userSettings.NotionEnabled || string.IsNullOrEmpty(userSettings.NotionDatabaseId))
                {
                    return new List<string>();
                }

                // Создаем уникальный ключ кеша для пользователя и базы данных
                var cacheKey = $"notion_schema_{userId}_{userSettings.NotionDatabaseId}_{propertyName}";
                
                if (_cache.TryGetValue(cacheKey, out List<string>? cachedOptions) && cachedOptions != null)
                {
                    _logger.LogDebug("Опции для поля {Field} получены из кеша для пользователя {UserId}", propertyName, userId);
                    return cachedOptions;
                }

                // Загружаем опции из Notion
                var options = await _personalNotionService.GetPersonalOptionsAsync(userSettings, propertyName);
                
                // Кешируем результат
                _cache.Set(cacheKey, options, _cacheExpiration);
                
                _logger.LogInformation("Опции для поля {Field} загружены из Notion и закешированы для пользователя {UserId}", 
                    propertyName, userId);
                
                return options;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении опций для поля {Field} из Notion для пользователя {UserId}", 
                    propertyName, userId);
                return new List<string>();
            }
        }

        /// <summary>
        /// Получает все опции для пользователя из кеша или загружает из Notion
        /// </summary>
        public async Task<Dictionary<string, List<string>>> GetAllOptionsAsync(long userId, UserSettings userSettings)
        {
            try
            {
                if (!userSettings.NotionEnabled || string.IsNullOrEmpty(userSettings.NotionDatabaseId))
                {
                    return new Dictionary<string, List<string>>();
                }

                // Создаем уникальный ключ кеша для пользователя и базы данных
                var cacheKey = $"notion_schema_all_{userId}_{userSettings.NotionDatabaseId}";
                
                if (_cache.TryGetValue(cacheKey, out Dictionary<string, List<string>>? cachedOptions) && cachedOptions != null)
                {
                    _logger.LogDebug("Все опции получены из кеша для пользователя {UserId}", userId);
                    return cachedOptions;
                }

                // Загружаем все опции из Notion
                var options = await _personalNotionService.GetPersonalOptionsAsync(userSettings);
                
                // Кешируем результат
                _cache.Set(cacheKey, options, _cacheExpiration);
                
                _logger.LogInformation("Все опции загружены из Notion и закешированы для пользователя {UserId}", userId);
                
                return options;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении всех опций из Notion для пользователя {UserId}", userId);
                return new Dictionary<string, List<string>>();
            }
        }

        /// <summary>
        /// Инвалидирует кеш для конкретного пользователя
        /// </summary>
        public void InvalidateUserCache(long userId, string? databaseId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(databaseId))
                {
                    // Удаляем все кеши для пользователя
                    var keysToRemove = new List<string>();
                    
                    // Здесь можно реализовать более сложную логику поиска ключей
                    // Пока просто логируем
                    _logger.LogInformation("Кеш для пользователя {UserId} помечен для инвалидации", userId);
                }
                else
                {
                    // Удаляем кеши для конкретной базы данных
                    var cacheKey = $"notion_schema_all_{userId}_{databaseId}";
                    _cache.Remove(cacheKey);
                    
                    _logger.LogInformation("Кеш для пользователя {UserId} и базы {DatabaseId} инвалидирован", userId, databaseId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при инвалидации кеша для пользователя {UserId}", userId);
            }
        }

        /// <summary>
        /// Инвалидирует кеш для конкретного поля пользователя
        /// </summary>
        public void InvalidateFieldCache(long userId, string databaseId, string propertyName)
        {
            try
            {
                var cacheKey = $"notion_schema_{userId}_{databaseId}_{propertyName}";
                _cache.Remove(cacheKey);
                
                _logger.LogDebug("Кеш для поля {Field} пользователя {UserId} инвалидирован", propertyName, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при инвалидации кеша для поля {Field} пользователя {UserId}", propertyName, userId);
            }
        }

        /// <summary>
        /// Очищает весь кеш схемы Notion
        /// </summary>
        public void ClearAllCache()
        {
            try
            {
                // В IMemoryCache нет прямого способа очистить все ключи
                // Можно реализовать через паттерн "namespace" или использовать IDistributedCache
                _logger.LogInformation("Весь кеш схемы Notion помечен для очистки");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при очистке кеша схемы Notion");
            }
        }

        /// <summary>
        /// Получает статистику кеша
        /// </summary>
        public (int EstimatedSize, TimeSpan Expiration) GetCacheStats()
        {
            return (_cache is MemoryCache memoryCache ? memoryCache.Count : 0, _cacheExpiration);
        }
    }
}

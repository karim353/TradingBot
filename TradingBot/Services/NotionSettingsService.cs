using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TradingBot.Models;

namespace TradingBot.Services
{
    /// <summary>
    /// Сервис для управления настройками Notion пользователей
    /// </summary>
    public class NotionSettingsService
    {
        private readonly PersonalNotionService _personalNotionService;
        private readonly ILogger<NotionSettingsService> _logger;

        public NotionSettingsService(PersonalNotionService personalNotionService, ILogger<NotionSettingsService> logger)
        {
            _personalNotionService = personalNotionService;
            _logger = logger;
        }

        /// <summary>
        /// Включает интеграцию с Notion для пользователя
        /// </summary>
        public async Task<bool> EnableNotionAsync(UserSettings userSettings, string integrationToken, string databaseId)
        {
            try
            {
                // Проверяем валидность токена и базы данных
                var testSettings = new UserSettings
                {
                    NotionEnabled = true,
                    NotionIntegrationToken = integrationToken,
                    NotionDatabaseId = databaseId
                };

                var isConnectionValid = await _personalNotionService.TestNotionConnectionAsync(testSettings);
                if (!isConnectionValid)
                {
                    _logger.LogWarning("Не удалось подключиться к Notion с указанными настройками для пользователя");
                    return false;
                }

                // Обновляем настройки пользователя
                userSettings.NotionEnabled = true;
                userSettings.NotionIntegrationToken = integrationToken;
                userSettings.NotionDatabaseId = databaseId;

                _logger.LogInformation("Интеграция с Notion успешно включена для пользователя");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при включении интеграции с Notion");
                return false;
            }
        }

        /// <summary>
        /// Отключает интеграцию с Notion для пользователя
        /// </summary>
        public void DisableNotion(UserSettings userSettings)
        {
            try
            {
                userSettings.NotionEnabled = false;
                userSettings.NotionIntegrationToken = null;
                userSettings.NotionDatabaseId = null;

                _logger.LogInformation("Интеграция с Notion отключена для пользователя");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отключении интеграции с Notion");
            }
        }

        /// <summary>
        /// Обновляет токен интеграции
        /// </summary>
        public async Task<bool> UpdateIntegrationTokenAsync(UserSettings userSettings, string newToken)
        {
            try
            {
                if (!userSettings.NotionEnabled || string.IsNullOrEmpty(userSettings.NotionDatabaseId))
                {
                    _logger.LogWarning("Нельзя обновить токен: интеграция с Notion не включена");
                    return false;
                }

                // Проверяем новый токен
                var testSettings = new UserSettings
                {
                    NotionEnabled = true,
                    NotionIntegrationToken = newToken,
                    NotionDatabaseId = userSettings.NotionDatabaseId
                };

                var isConnectionValid = await _personalNotionService.TestNotionConnectionAsync(testSettings);
                if (!isConnectionValid)
                {
                    _logger.LogWarning("Новый токен недействителен");
                    return false;
                }

                userSettings.NotionIntegrationToken = newToken;
                _logger.LogInformation("Токен интеграции успешно обновлен");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении токена интеграции");
                return false;
            }
        }

        /// <summary>
        /// Обновляет ID базы данных
        /// </summary>
        public async Task<bool> UpdateDatabaseIdAsync(UserSettings userSettings, string newDatabaseId)
        {
            try
            {
                if (!userSettings.NotionEnabled || string.IsNullOrEmpty(userSettings.NotionIntegrationToken))
                {
                    _logger.LogWarning("Нельзя обновить ID базы: интеграция с Notion не включена");
                    return false;
                }

                // Проверяем новый ID базы данных
                var testSettings = new UserSettings
                {
                    NotionEnabled = true,
                    NotionIntegrationToken = userSettings.NotionIntegrationToken,
                    NotionDatabaseId = newDatabaseId
                };

                var isConnectionValid = await _personalNotionService.TestNotionConnectionAsync(testSettings);
                if (!isConnectionValid)
                {
                    _logger.LogWarning("Новый ID базы данных недействителен");
                    return false;
                }

                userSettings.NotionDatabaseId = newDatabaseId;
                _logger.LogInformation("ID базы данных успешно обновлен");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении ID базы данных");
                return false;
            }
        }

        /// <summary>
        /// Проверяет статус интеграции с Notion
        /// </summary>
        public async Task<(bool IsEnabled, bool IsConnected, string? ErrorMessage)> GetNotionStatusAsync(UserSettings userSettings)
        {
            try
            {
                if (!userSettings.NotionEnabled)
                {
                    return (false, false, null);
                }

                if (string.IsNullOrEmpty(userSettings.NotionIntegrationToken) || string.IsNullOrEmpty(userSettings.NotionDatabaseId))
                {
                    return (true, false, "Неполные настройки интеграции");
                }

                var isConnected = await _personalNotionService.TestNotionConnectionAsync(userSettings);
                if (!isConnected)
                {
                    return (true, false, "Нет подключения к Notion API");
                }

                return (true, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке статуса интеграции с Notion");
                return (true, false, "Ошибка проверки подключения");
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
    }
}

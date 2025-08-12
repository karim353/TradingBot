using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TradingBot.Models;
using Microsoft.Data.Sqlite;
using System.Linq;

namespace TradingBot.Services
{
    /// <summary>
    /// Сервис для управления настройками пользователей
    /// </summary>
    public class UserSettingsService
    {
        private readonly ILogger<UserSettingsService> _logger;
        private readonly string _connectionString;
        private readonly PersonalNotionService _personalNotionService;

        public UserSettingsService(ILogger<UserSettingsService> logger, string connectionString, PersonalNotionService personalNotionService)
        {
            _logger = logger;
            _connectionString = connectionString;
            _personalNotionService = personalNotionService;
        }

        /// <summary>
        /// Получает настройки пользователя
        /// </summary>
        public async Task<UserSettings?> GetUserSettingsAsync(long userId)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT Language, NotificationsEnabled, NotionEnabled, NotionDatabaseId, NotionIntegrationToken,
                           FavoriteTickers, RecentTickers, RecentDirections, RecentComments,
                           RecentAccounts, RecentSessions, RecentPositions, RecentResults, RecentSetups, RecentContexts, RecentEmotions
                    FROM UserSettings 
                    WHERE UserId = @UserId";

                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new UserSettings
                    {
                        Language = reader.GetString(0),
                        NotificationsEnabled = reader.GetBoolean(1),
                        NotionEnabled = reader.GetBoolean(2),
                        NotionDatabaseId = reader.IsDBNull(3) ? null : reader.GetString(3),
                        NotionIntegrationToken = reader.IsDBNull(4) ? null : reader.GetString(4),
                        FavoriteTickers = ParseJsonArray(reader.GetString(5)),
                        RecentTickers = ParseJsonArray(reader.GetString(6)),
                        RecentDirections = ParseJsonArray(reader.GetString(7)),
                        RecentComments = ParseJsonArray(reader.GetString(8)),
                        RecentAccounts = ParseJsonArray(reader.GetString(9)),
                        RecentSessions = ParseJsonArray(reader.GetString(10)),
                        RecentPositions = ParseJsonArray(reader.GetString(11)),
                        RecentResults = ParseJsonArray(reader.GetString(12)),
                        RecentSetups = ParseJsonArray(reader.GetString(13)),
                        RecentContexts = ParseJsonArray(reader.GetString(14)),
                        RecentEmotions = ParseJsonArray(reader.GetString(15))
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении настроек пользователя {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Сохраняет настройки пользователя
        /// </summary>
        public async Task<bool> SaveUserSettingsAsync(long userId, UserSettings settings)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT OR REPLACE INTO UserSettings 
                    (UserId, Language, NotificationsEnabled, NotionEnabled, NotionDatabaseId, NotionIntegrationToken,
                     FavoriteTickers, RecentTickers, RecentDirections, RecentComments,
                     RecentAccounts, RecentSessions, RecentPositions, RecentResults, RecentSetups, RecentContexts, RecentEmotions)
                    VALUES 
                    (@UserId, @Language, @NotificationsEnabled, @NotionEnabled, @NotionDatabaseId, @NotionIntegrationToken,
                     @FavoriteTickers, @RecentTickers, @RecentDirections, @RecentComments,
                     @RecentAccounts, @RecentSessions, @RecentPositions, @RecentResults, @RecentSetups, @RecentContexts, @RecentEmotions)";

                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@Language", settings.Language);
                command.Parameters.AddWithValue("@NotificationsEnabled", settings.NotificationsEnabled);
                command.Parameters.AddWithValue("@NotionEnabled", settings.NotionEnabled);
                command.Parameters.AddWithValue("@NotionDatabaseId", settings.NotionDatabaseId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@NotionIntegrationToken", settings.NotionIntegrationToken ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@FavoriteTickers", SerializeJsonArray(settings.FavoriteTickers));
                command.Parameters.AddWithValue("@RecentTickers", SerializeJsonArray(settings.RecentTickers));
                command.Parameters.AddWithValue("@RecentDirections", SerializeJsonArray(settings.RecentDirections));
                command.Parameters.AddWithValue("@RecentComments", SerializeJsonArray(settings.RecentComments));
                command.Parameters.AddWithValue("@RecentAccounts", SerializeJsonArray(settings.RecentAccounts));
                command.Parameters.AddWithValue("@RecentSessions", SerializeJsonArray(settings.RecentSessions));
                command.Parameters.AddWithValue("@RecentPositions", SerializeJsonArray(settings.RecentPositions));
                command.Parameters.AddWithValue("@RecentResults", SerializeJsonArray(settings.RecentResults));
                command.Parameters.AddWithValue("@RecentSetups", SerializeJsonArray(settings.RecentSetups));
                command.Parameters.AddWithValue("@RecentContexts", SerializeJsonArray(settings.RecentContexts));
                command.Parameters.AddWithValue("@RecentEmotions", SerializeJsonArray(settings.RecentEmotions));

                await command.ExecuteNonQueryAsync();
                
                _logger.LogInformation("Настройки пользователя {UserId} сохранены", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении настроек пользователя {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Обновляет настройки Notion для пользователя
        /// </summary>
        public async Task<bool> UpdateNotionSettingsAsync(long userId, bool enabled, string? databaseId, string? integrationToken)
        {
            try
            {
                var settings = await GetUserSettingsAsync(userId) ?? new UserSettings();
                settings.NotionEnabled = enabled;
                settings.NotionDatabaseId = databaseId;
                settings.NotionIntegrationToken = integrationToken;

                return await SaveUserSettingsAsync(userId, settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении настроек Notion для пользователя {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Проверяет подключение к Notion для пользователя
        /// </summary>
        public async Task<bool> TestNotionConnectionAsync(long userId)
        {
            try
            {
                var settings = await GetUserSettingsAsync(userId);
                if (settings?.NotionEnabled != true || string.IsNullOrEmpty(settings.NotionDatabaseId))
                {
                    return false;
                }

                return await _personalNotionService.TestPersonalNotionConnectionAsync(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке подключения к Notion для пользователя {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Добавляет элемент в список недавних значений
        /// </summary>
        public async Task<bool> AddToRecentAsync(long userId, string propertyName, string value, int maxItems = 10)
        {
            try
            {
                var settings = await GetUserSettingsAsync(userId) ?? new UserSettings();
                var property = GetPropertyByName(settings, propertyName);
                
                if (property != null)
                {
                    // Удаляем существующее значение, если есть
                    property.RemoveAll(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));
                    
                    // Добавляем в начало списка
                    property.Insert(0, value);
                    
                    // Ограничиваем размер списка
                    if (property.Count > maxItems)
                    {
                        property.RemoveRange(maxItems, property.Count - maxItems);
                    }
                    
                    return await SaveUserSettingsAsync(userId, settings);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении в недавние для пользователя {UserId}, свойство {PropertyName}", userId, propertyName);
                return false;
            }
        }

        /// <summary>
        /// Переключает избранный тикер
        /// </summary>
        public async Task<bool> ToggleFavoriteTickerAsync(long userId, string ticker)
        {
            try
            {
                var settings = await GetUserSettingsAsync(userId) ?? new UserSettings();
                
                if (settings.FavoriteTickers.Contains(ticker, StringComparer.OrdinalIgnoreCase))
                {
                    settings.FavoriteTickers.RemoveAll(x => string.Equals(x, ticker, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    settings.FavoriteTickers.Add(ticker);
                }
                
                return await SaveUserSettingsAsync(userId, settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при переключении избранного тикера для пользователя {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Получает список недавних значений для поля
        /// </summary>
        public async Task<List<string>> GetRecentValuesAsync(long userId, string propertyName)
        {
            try
            {
                var settings = await GetUserSettingsAsync(userId);
                if (settings == null) return new List<string>();

                var property = GetPropertyByName(settings, propertyName);
                return property ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении недавних значений для пользователя {UserId}, свойство {PropertyName}", userId, propertyName);
                return new List<string>();
            }
        }

        /// <summary>
        /// Получает избранные тикеры пользователя
        /// </summary>
        public async Task<List<string>> GetFavoriteTickersAsync(long userId)
        {
            try
            {
                var settings = await GetUserSettingsAsync(userId);
                return settings?.FavoriteTickers ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении избранных тикеров для пользователя {UserId}", userId);
                return new List<string>();
            }
        }

        private List<string>? GetPropertyByName(UserSettings settings, string propertyName)
        {
            return propertyName.ToLowerInvariant() switch
            {
                "ticker" => settings.RecentTickers,
                "account" => settings.RecentAccounts,
                "session" => settings.RecentSessions,
                "position" => settings.RecentPositions,
                "direction" => settings.RecentDirections,
                "context" => settings.RecentContexts,
                "setup" => settings.RecentSetups,
                "result" => settings.RecentResults,
                "emotions" => settings.RecentEmotions,
                "comment" => settings.RecentComments,
                _ => null
            };
        }

        private List<string> ParseJsonArray(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json) || json == "[]")
                    return new List<string>();

                // Простой парсинг JSON массива
                var result = new List<string>();
                var trimmed = json.Trim('[', ']');
                if (string.IsNullOrEmpty(trimmed))
                    return result;

                var items = trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in items)
                {
                    var cleanItem = item.Trim('"', ' ', '\t', '\n', '\r');
                    if (!string.IsNullOrEmpty(cleanItem))
                        result.Add(cleanItem);
                }

                return result;
            }
            catch
            {
                return new List<string>();
            }
        }

        private string SerializeJsonArray(List<string> list)
        {
            if (list == null || list.Count == 0)
                return "[]";

            return "[" + string.Join(",", list.Select(x => $"\"{x}\"")) + "]";
        }
    }
}

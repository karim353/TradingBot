using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using TradingBot.Models;

namespace TradingBot.Services
{
    /// <summary>
    /// Хранилище сделок для персональных баз данных Notion пользователей
    /// </summary>
    public class PersonalNotionTradeStorage : ITradeStorage
    {
        private readonly TradeRepository _repo;
        private readonly PersonalNotionService _personalNotionService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PersonalNotionTradeStorage> _logger;

        public PersonalNotionTradeStorage(
            TradeRepository repo,
            PersonalNotionService personalNotionService,
            IMemoryCache cache,
            ILogger<PersonalNotionTradeStorage> logger)
        {
            _repo = repo;
            _personalNotionService = personalNotionService;
            _cache = cache;
            _logger = logger;
        }

        public async Task AddTradeAsync(Trade trade)
        {
            try
            {
                // Сначала сохраняем локально
                await _repo.AddTradeAsync(trade);
                
                // Затем пытаемся сохранить в персональную базу Notion (если включена)
                if (trade.UserId > 0)
                {
                    // TODO: Добавить метод GetUserSettingsAsync в TradeRepository
                    // var userSettings = await _repo.GetUserSettingsAsync(trade.UserId);
                    // if (userSettings?.NotionEnabled == true && !string.IsNullOrEmpty(userSettings.NotionDatabaseId))
                    // {
                    //     try
                    //     {
                    //         // Здесь можно добавить логику создания страницы в персональной базе Notion
                    //         // Пока просто логируем
                    //         _logger.LogInformation("Сделка {TradeId} будет синхронизирована с персональной базой Notion пользователя {UserId}", 
                    //             trade.Id, trade.UserId);
                    //     }
                    //     catch (Exception ex)
                    //     {
                    //         _logger.LogWarning(ex, "Не удалось синхронизировать сделку {TradeId} с персональной базой Notion", trade.Id);
                    //     }
                    // }
                    
                    _logger.LogInformation("Сделка {TradeId} будет синхронизирована с персональной базой Notion пользователя {UserId}", 
                        trade.Id, trade.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении сделки {TradeId}", trade.Id);
                throw;
            }
        }

        public async Task UpdateTradeAsync(Trade trade)
        {
            try
            {
                await _repo.UpdateTradeAsync(trade);
                
                // Обновляем в персональной базе Notion (если включена)
                if (trade.UserId > 0)
                {
                    // TODO: Добавить метод GetUserSettingsAsync в TradeRepository
                    _logger.LogInformation("Сделка {TradeId} будет обновлена в персональной базе Notion", trade.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении сделки {TradeId}", trade.Id);
                throw;
            }
        }

        public async Task DeleteTradeAsync(Trade trade)
        {
            try
            {
                await _repo.DeleteTradeAsync(trade);
                
                // Удаляем из персональной базы Notion (если включена)
                if (trade.UserId > 0)
                {
                    // TODO: Добавить метод GetUserSettingsAsync в TradeRepository
                    _logger.LogInformation("Сделка {TradeId} будет удалена из персональной базы Notion", trade.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении сделки {TradeId}", trade.Id);
                throw;
            }
        }

        public async Task<Trade?> GetLastTradeAsync(long userId)
        {
            return await _repo.GetLastTradeAsync(userId);
        }

        public async Task<List<Trade>> GetTradesAsync(long userId)
        {
            return await _repo.GetTradesAsync(userId);
        }

        public async Task<List<Trade>> GetTradesInDateRangeAsync(long userId, DateTime from, DateTime to)
        {
            return await _repo.GetTradesInDateRangeAsync(userId, from, to);
        }

        public async Task<List<string>> GetSelectOptionsAsync(string propertyName, Trade? current = null)
        {
            try
            {
                if (current?.UserId == 0)
                {
                    return new List<string>();
                }

                // TODO: Добавить метод GetUserSettingsAsync в TradeRepository
                // var userSettings = await _repo.GetUserSettingsAsync(current.UserId);
                // if (userSettings?.NotionEnabled == true && !string.IsNullOrEmpty(userSettings.NotionDatabaseId))
                // {
                //     // Получаем опции из персональной базы Notion
                //     var personalOptions = await _personalNotionService.GetPersonalOptionsAsync(userSettings, propertyName);
                //     if (personalOptions.Any())
                //     {
                //         _logger.LogDebug("Получено {Count} опций для поля {Field} из персональной базы Notion", 
                //             personalOptions.Count, propertyName);
                //         return personalOptions;
                //     }
                // }

                // Fallback на локальные опции
                return await GetLocalOptionsAsync(propertyName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка при получении опций для поля {Field}, используем локальные", propertyName);
                return await GetLocalOptionsAsync(propertyName);
            }
        }

        public async Task<List<string>> GetSuggestedOptionsAsync(string propertyName, long userId, Trade? current = null, int topN = 12)
        {
            try
            {
                var suggestions = new List<string>();
                
                // TODO: Добавить метод GetUserSettingsAsync в TradeRepository
                // var userSettings = await _repo.GetUserSettingsAsync(userId);
                // if (userSettings?.NotionEnabled == true && !string.IsNullOrEmpty(userSettings.NotionDatabaseId))
                // {
                //     var personalOptions = await _personalNotionService.GetPersonalOptionsAsync(userSettings, propertyName);
                //     suggestions.AddRange(personalOptions.Take(topN / 2));
                // }
                
                // Дополняем локальными опциями
                var localOptions = await GetLocalOptionsAsync(propertyName);
                var remainingCount = topN - suggestions.Count;
                if (remainingCount > 0)
                {
                    suggestions.AddRange(localOptions.Take(remainingCount));
                }
                
                return suggestions.Distinct().Take(topN).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка при получении предложений для поля {Field}, используем локальные", propertyName);
                return await GetLocalOptionsAsync(propertyName);
            }
        }

        /// <summary>
        /// Получает локальные опции для поля (fallback)
        /// </summary>
        private Task<List<string>> GetLocalOptionsAsync(string propertyName)
        {
            // Кешируем локальные опции
            var cacheKey = $"local_options_{propertyName}";
            if (_cache.TryGetValue(cacheKey, out List<string>? cachedOptions) && cachedOptions != null)
            {
                return Task.FromResult(cachedOptions);
            }

            var options = propertyName.ToLowerInvariant() switch
            {
                "account" => new List<string> { "🏦 BingX", "🏦 Binance", "🏦 MEXC", "🏦 Bybit", "🧪 Demo" },
                "session" => new List<string> { "ASIA", "LONDON", "NEW YORK", "FRANKFURT" },
                "position" => new List<string> { "⚡ Scalp", "⏱ Intraday", "📅 Swing", "🏋️ Position" },
                "direction" => new List<string> { "Long", "Short" },
                "context" => new List<string> { "📈 Uptrend", "📉 Downtrend", "➖ Range" },
                "setup" => new List<string> { "↗️ Continuation (CONT)", "📈 Breakout", "🔄 Reversal (REVR)", "🔁 Double Top/Bottom", "👤 Head & Shoulders" },
                "result" => new List<string> { "TP", "SL", "BE" },
                "emotions" => new List<string> { "😌 Calm", "🎯 Focused", "😨 Fear", "😵‍💫 FOMO" },
                _ => new List<string>()
            };

            // Кешируем на 1 час
            _cache.Set(cacheKey, options, TimeSpan.FromHours(1));
            return Task.FromResult(options);
        }
    }
}

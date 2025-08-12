using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using TradingBot.Models;

namespace TradingBot.Services
{
    public class NotionTradeStorage : ITradeStorage
    {
        private readonly TradeContext _db;
        private readonly TradeRepository _repo;
        private readonly NotionService _notion;
        private readonly IMemoryCache _cache;

        public NotionTradeStorage(TradeContext dbContext, NotionService notionService, IMemoryCache cache)
        {
            _db = dbContext;
            _repo = new TradeRepository(_db);
            _notion = notionService;
            _cache = cache;
        }

        public async Task AddTradeAsync(Trade trade)
        {
            // При добавлении сделки сохраняем в Notion и локально
            string? pageId = null;
            try
            {
                pageId = await _notion.CreatePageForTradeAsync(trade);
                trade.NotionPageId = pageId ?? string.Empty;
            }
            catch (Exception ex)
            {
                // Ошибка при отправке в Notion – пробрасываем исключение, чтобы выдать пользователю предупреждение
                throw new Exception("Notion error", ex);
            }
            // Сохраняем сделку в локальной базе (SQLite)
            await _repo.AddTradeAsync(trade);
        }

        public async Task UpdateTradeAsync(Trade trade)
        {
            if (!string.IsNullOrEmpty(trade.NotionPageId))
            {
                await _notion.UpdatePageForTradeAsync(trade);
            }
            await _repo.UpdateTradeAsync(trade);
        }

        public async Task DeleteTradeAsync(Trade trade)
        {
            if (!string.IsNullOrEmpty(trade.NotionPageId))
            {
                try
                {
                    await _notion.DeletePageForTradeAsync(trade.NotionPageId);
                }
                catch (Exception)
                {
                    _ = Task.CompletedTask;
                }
            }
            _db.Trades.Remove(trade);
            await _db.SaveChangesAsync();
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
            const string cacheKey = "notion_schema_options";
            Dictionary<string, List<string>> optionsByField;

            if (!_cache.TryGetValue(cacheKey, out optionsByField!))
            {
                try
                {
                    optionsByField = await _notion.GetSelectOptionsAsync();
                    _cache.Set(cacheKey, optionsByField, TimeSpan.FromMinutes(20));
                }
                catch
                {
                    if (!_cache.TryGetValue(cacheKey, out optionsByField!))
                        optionsByField = new Dictionary<string, List<string>>();
                }
            }

            var list = optionsByField.TryGetValue(propertyName, out var opts) ? new List<string>(opts) : new List<string>();

            // Достаём UserSettings для приоритезации частых опций
            // (инжектировать нельзя здесь, поэтому берём последние сохранённые из кэша, если есть)
            var userSettings = _cache.Get<ModelUserSettingsProxy>("last_user_settings")?.Settings; // см. UpdateHandler будет класть актуальные
            var priority = new List<string>();
            if (userSettings != null)
            {
                switch (propertyName)
                {
                    case "Account": priority = userSettings.RecentAccounts; break;
                    case "Session": priority = userSettings.RecentSessions; break;
                    case "Position": priority = userSettings.RecentPositions; break;
                    case "Direction": priority = userSettings.RecentDirections; break;
                    case "Result": priority = userSettings.RecentResults; break;
                    case "Context": priority = userSettings.RecentContexts; break;
                    case "Setup": priority = userSettings.RecentSetups; break;
                    case "Emotions": priority = userSettings.RecentEmotions; break;
                }
            }
            var prioritySet = new HashSet<string>(priority ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            if (prioritySet.Count > 0)
            {
                list = list
                    .OrderByDescending(v => prioritySet.Contains(v))
                    .ThenBy(v => v)
                    .ToList();
            }

            // Доп. приоритезация от текущей сделки
            if (current != null)
            {
                var curr = current;
                var currSet = new HashSet<string>(new[]
                {
                    propertyName == "Account" ? curr.Account : null,
                    propertyName == "Session" ? curr.Session : null,
                    propertyName == "Position" ? curr.Position : null,
                    propertyName == "Direction" ? curr.Direction : null,
                    propertyName == "Result" ? curr.Result : null
                }.Where(s => !string.IsNullOrWhiteSpace(s))!, StringComparer.OrdinalIgnoreCase);

                if (propertyName == "Setup" && curr.Setup != null) foreach (var s in curr.Setup) currSet.Add(s);
                if (propertyName == "Context" && curr.Context != null) foreach (var s in curr.Context) currSet.Add(s);
                if (propertyName == "Emotions" && curr.Emotions != null) foreach (var s in curr.Emotions) currSet.Add(s);

                if (currSet.Count > 0)
                    list = list.OrderByDescending(v => currSet.Contains(v)).ThenBy(v => v).ToList();
            }

            return list;
        }

        public async Task<List<string>> GetSuggestedOptionsAsync(string propertyName, long userId, Trade? current = null, int topN = 12)
        {
            // Кэш подсказок на 45 сек per-user+field
            string suggestKey = $"suggest_{userId}_{propertyName}";
            if (_cache.TryGetValue(suggestKey, out List<string>? cached) && cached != null && cached.Count > 0)
                return cached.Take(topN).ToList();

            // База опций: из схемы (кэш 20 мин)
            List<string> all;
            try { all = await GetSelectOptionsAsync(propertyName, current); }
            catch { all = new List<string>(); }

            // История пользователя из локальной БД
            List<Trade> userTrades;
            try { userTrades = await _repo.GetTradesAsync(userId); }
            catch { userTrades = new List<Trade>(); }

            // Глобальная популярность: по всем сделкам в локальной БД
            List<Trade> globalTrades;
            try { globalTrades = _db.Trades.ToList(); }
            catch { globalTrades = new List<Trade>(); }

            var scores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            DateTime now = DateTime.UtcNow;

            void bump(Dictionary<string, double> dict, string? val, double weight)
            {
                if (string.IsNullOrWhiteSpace(val)) return;
                dict[val] = dict.TryGetValue(val, out var s) ? s + weight : weight;
            }

            // Частота и свежесть по пользователю
            foreach (var t in userTrades)
            {
                double ageDays = Math.Max(1, (now - t.Date).TotalDays);
                double freshness = 1.0 / ageDays;        // β = 0.3
                double baseHit = 1.0;                    // α = 0.7
                double weight = baseHit * 0.7 + freshness * 0.3;
                switch (propertyName)
                {
                    case "Account": bump(scores, t.Account, weight); break;
                    case "Session": bump(scores, t.Session, weight); break;
                    case "Position": bump(scores, t.Position, weight); break;
                    case "Direction": bump(scores, t.Direction, weight); break;
                    case "Result": bump(scores, t.Result, weight); break;
                    case "Context": if (t.Context != null) foreach (var v in t.Context) bump(scores, v, weight); break;
                    case "Setup": if (t.Setup != null) foreach (var v in t.Setup) bump(scores, v, weight); break;
                    case "Emotions": if (t.Emotions != null) foreach (var v in t.Emotions) bump(scores, v, weight); break;
                }
            }

            // Глобальная популярность γ = 0.2 на попадание
            foreach (var t in globalTrades)
            {
                const double gamma = 0.2;
                switch (propertyName)
                {
                    case "Account": bump(scores, t.Account, gamma); break;
                    case "Session": bump(scores, t.Session, gamma); break;
                    case "Position": bump(scores, t.Position, gamma); break;
                    case "Direction": bump(scores, t.Direction, gamma); break;
                    case "Result": bump(scores, t.Result, gamma); break;
                    case "Context": if (t.Context != null) foreach (var v in t.Context) bump(scores, v, gamma); break;
                    case "Setup": if (t.Setup != null) foreach (var v in t.Setup) bump(scores, v, gamma); break;
                    case "Emotions": if (t.Emotions != null) foreach (var v in t.Emotions) bump(scores, v, gamma); break;
                }
            }

            var ranked = all
                .OrderByDescending(v => scores.TryGetValue(v, out var s) ? s : 0.0)
                .ThenBy(v => v)
                .Take(topN)
                .ToList();

            // Приоритезация по текущей сделке (контекстная релевантность)
            if (current != null)
            {
                var currSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (propertyName == "Account" && !string.IsNullOrWhiteSpace(current.Account)) currSet.Add(current.Account);
                if (propertyName == "Session" && !string.IsNullOrWhiteSpace(current.Session)) currSet.Add(current.Session);
                if (propertyName == "Position" && !string.IsNullOrWhiteSpace(current.Position)) currSet.Add(current.Position);
                if (propertyName == "Direction" && !string.IsNullOrWhiteSpace(current.Direction)) currSet.Add(current.Direction);
                if (propertyName == "Result" && !string.IsNullOrWhiteSpace(current.Result)) currSet.Add(current.Result);
                if (propertyName == "Context" && current.Context != null) foreach (var v in current.Context) currSet.Add(v);
                if (propertyName == "Setup" && current.Setup != null) foreach (var v in current.Setup) currSet.Add(v);
                if (propertyName == "Emotions" && current.Emotions != null) foreach (var v in current.Emotions) currSet.Add(v);

                if (currSet.Count > 0)
                    ranked = ranked.OrderByDescending(v => currSet.Contains(v)).ThenBy(v => v).ToList();
            }

            // Кэшируем результат на 45 секунд
            _cache.Set(suggestKey, ranked, TimeSpan.FromSeconds(45));
            return ranked;
        }

        // Прокси-модель для кэша (имя intentionally уникально, чтобы избежать конфликтов)
        public class ModelUserSettingsProxy
        {
            public Models.UserSettings Settings { get; set; }
            public ModelUserSettingsProxy(Models.UserSettings s) { Settings = s; }
        }
    }
}
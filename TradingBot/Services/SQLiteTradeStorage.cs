using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradingBot.Models;

namespace TradingBot.Services
{
    /// <summary>
    /// Локальное хранилище сделок на SQLite через EF Core под новую модель.
    /// </summary>
    public class SQLiteTradeStorage : ITradeStorage
    {
        private readonly TradeRepository _repo;
        private ITradeStorage _tradeStorageImplementation;

        public SQLiteTradeStorage(TradeContext db)
        {
            _repo = new TradeRepository(db);
        }

        public Task AddTradeAsync(Trade trade) => _repo.AddTradeAsync(trade);
        public Task UpdateTradeAsync(Trade trade) => _repo.UpdateTradeAsync(trade);
        public Task DeleteTradeAsync(Trade trade) => _repo.DeleteTradeAsync(trade);
        public Task<Trade?> GetLastTradeAsync(long userId) => _repo.GetLastTradeAsync(userId);
        public Task<List<Trade>> GetTradesAsync(long userId) => _repo.GetTradesAsync(userId);
        public Task<List<Trade>> GetTradesInDateRangeAsync(long userId, DateTime from, DateTime to) => _repo.GetTradesInDateRangeAsync(userId, from, to);

        public Task<List<string>> GetSelectOptionsAsync(string propertyName, Trade? current = null)
        {
            // Локальной схеме нечего возвращать — отдаём базовый набор или пусто.
            var defaults = propertyName switch
            {
                "Position" => new List<string> { "Long", "Short" },
                "Direction" => new List<string> { "Long", "Short" },
                "Session" => new List<string> { "ASIA", "FRANKFURT", "LONDON", "NEW YORK" },
                _ => new List<string>()
            };
            return Task.FromResult(defaults);
        }

        public async Task<List<string>> GetSuggestedOptionsAsync(string propertyName, long userId, Trade? current = null, int topN = 12)
        {
            var trades = await _repo.GetTradesAsync(userId);
            if (!trades.Any())
                return (await GetSelectOptionsAsync(propertyName, current)).Take(topN).ToList();

            // Подсчёт частоты и свежести
            var scores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            DateTime now = DateTime.UtcNow;
            foreach (var t in trades)
            {
                void bump(string? val)
                {
                    if (string.IsNullOrWhiteSpace(val)) return;
                    double ageDays = Math.Max(1, (now - t.Date).TotalDays);
                    double freshness = 1.0 / ageDays; // чем свежее, тем больше
                    double freq = 1.0;                 // по одному за попадание
                    double scoreDelta = freq * 0.7 + freshness * 0.3;
                    scores[val] = scores.TryGetValue(val, out var s) ? s + scoreDelta : scoreDelta;
                }

                switch (propertyName)
                {
                    case "Account": bump(t.Account); break;
                    case "Session": bump(t.Session); break;
                    case "Position": bump(t.Position); break;
                    case "Direction": bump(t.Direction); break;
                    case "Result": bump(t.Result); break;
                    case "Context": if (t.Context != null) foreach (var v in t.Context) bump(v); break;
                    case "Setup": if (t.Setup != null) foreach (var v in t.Setup) bump(v); break;
                    case "Emotions": if (t.Emotions != null) foreach (var v in t.Emotions) bump(v); break;
                }
            }

            var all = await GetSelectOptionsAsync(propertyName, current);
            // Глобальная популярность имитируем частотой в общей истории (для локального режима это та же история)
            var ranked = all
                .OrderByDescending(v => scores.TryGetValue(v, out var s) ? s : 0.0)
                .ThenBy(v => v)
                .Take(topN)
                .ToList();

            // Дополнительная приоритезация по текущей сделке (если указана)
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

            return ranked;
        }
    }
}
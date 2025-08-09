using System;
using System.Linq;
using System.Threading.Tasks;
using TradingBot.Models;

namespace TradingBot.Services
{
    /// <summary>
    /// Расширения для ITradeStorage, чтобы не менять интерфейс/реализации.
    /// </summary>
    public static class TradeStorageExtensions
    {
        /// <summary>
        /// Универсальный поиск сделки по Id через GetTradesAsync.
        /// Не ломает существующие реализации ITradeStorage.
        /// </summary>
        public static async Task<Trade?> GetTradeByIdAsync(this ITradeStorage storage, long userId, int id)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            var trades = await storage.GetTradesAsync(userId);
            if (trades == null || trades.Count == 0) return null;

            return trades.FirstOrDefault(t => t.Id == id);
        }
    }
}
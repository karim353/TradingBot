using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TradingBot.Models;
using Microsoft.EntityFrameworkCore;

namespace TradingBot.Services
{
    /// <summary>
    /// Репозиторий для операций с данными Trade в базе данных.
    /// </summary>
    public class TradeRepository
    {
        private readonly TradeContext _context;

        public TradeRepository(TradeContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Добавляет новую сделку в базу данных.
        /// </summary>
        public async Task<Trade> AddTradeAsync(Trade trade)
        {
            _context.Trades.Add(trade);
            await _context.SaveChangesAsync();
            return trade;
        }

        /// <summary>
        /// Обновляет существующую сделку (например, после добавления NotionPageId).
        /// </summary>
        public async Task UpdateTradeAsync(Trade trade)
        {
            _context.Trades.Update(trade);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Возвращает все сделки пользователя (по chatId), отсортированные по дате.
        /// </summary>
        public async Task<List<Trade>> GetTradesAsync(long chatId)
        {
            return await _context.Trades
                .Where(t => t.ChatId == chatId)
                .OrderBy(t => t.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Возвращает сделки пользователя за указанный период [from, to].
        /// </summary>
        public async Task<List<Trade>> GetTradesInDateRangeAsync(long chatId, DateTime from, DateTime to)
        {
            return await _context.Trades
                .Where(t => t.ChatId == chatId && t.Date >= from && t.Date <= to)
                .OrderBy(t => t.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Получает последнюю (самую свежую по дате) сделку пользователя.
        /// </summary>
        public async Task<Trade> GetLastTradeAsync(long chatId)
        {
            return await _context.Trades
                .Where(t => t.ChatId == chatId)
                .OrderByDescending(t => t.Date)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Удаляет заданную сделку из базы данных.
        /// </summary>
        public async Task DeleteTradeAsync(Trade trade)
        {
            if (trade != null)
            {
                _context.Trades.Remove(trade);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Получает список всех уникальных идентификаторов пользователей, для которых есть сделки.
        /// </summary>
        public async Task<List<long>> GetAllUserIdsAsync()
        {
            return await _context.Trades
                .Select(t => t.ChatId)
                .Distinct()
                .ToListAsync();
        }
    }
}

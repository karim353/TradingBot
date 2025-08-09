// TradeRepository.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TradingBot.Models;

namespace TradingBot.Services
{
    public class TradeRepository
    {
        private readonly TradeContext _context;

        public TradeRepository(TradeContext context)
        {
            _context = context;
        }

        public async Task AddTradeAsync(Trade trade)
        {
            _context.Trades.Add(trade);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTradeAsync(Trade trade)
        {
            _context.Trades.Update(trade);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTradeAsync(Trade trade)
        {
            _context.Trades.Remove(trade);
            await _context.SaveChangesAsync();
        }

        public async Task<Trade?> GetLastTradeAsync(long userId)
        {
            return await _context.Trades
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Trade>> GetTradesAsync(long userId)
        {
            return await _context.Trades
                .Where(t => t.UserId == userId)
                .ToListAsync();
        }

        public async Task<List<Trade>> GetTradesInDateRangeAsync(long userId, DateTime from, DateTime to)
        {
            return await _context.Trades
                .Where(t => t.UserId == userId && t.Date >= from && t.Date <= to)
                .ToListAsync();
        }

        public async Task<List<long>> GetAllUserIdsAsync()
        {
            return await _context.Trades
                .Select(t => t.UserId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<Trade?> GetTradeByIdAsync(long userId, int tradeId)
        {
            return await _context.Trades
                .FirstOrDefaultAsync(t => t.UserId == userId && t.Id == tradeId);
        }
    }
}
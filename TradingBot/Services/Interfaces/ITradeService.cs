using TradingBot.Models;

namespace TradingBot.Services.Interfaces;

public interface ITradeService
{
    Task<Trade> CreateTradeAsync(Trade trade, long userId, CancellationToken cancellationToken = default);
    Task<Trade?> GetTradeAsync(string tradeId, long userId, CancellationToken cancellationToken = default);
    Task<List<Trade>> GetTradesAsync(long userId, CancellationToken cancellationToken = default);
    Task<List<Trade>> GetTradesInDateRangeAsync(long userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<bool> UpdateTradeAsync(Trade trade, long userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteTradeAsync(string tradeId, long userId, CancellationToken cancellationToken = default);
    Task<List<string>> GetSuggestedOptionsAsync(string property, long userId, Trade? trade = null, int topN = 10, CancellationToken cancellationToken = default);
    Task<List<string>> GetSelectOptionsAsync(string property, Trade? trade = null, CancellationToken cancellationToken = default);
}

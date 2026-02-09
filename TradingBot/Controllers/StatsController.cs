using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly ITradeStorage _storage;
    private readonly IConfiguration _config;

    public StatsController(ITradeStorage storage, IConfiguration config)
    {
        _storage = storage;
        _config = config;
    }

    private long GetCurrentUserId()
    {
        var id = _config["Developer:UserId"];
        return long.TryParse(id, out var v) ? v : 1;
    }

    [HttpGet]
    public async Task<ActionResult<StatsDto>> GetStats([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var userId = GetCurrentUserId();
        var trades = from.HasValue && to.HasValue
            ? await _storage.GetTradesInDateRangeAsync(userId, from.Value, to.Value)
            : await _storage.GetTradesAsync(userId);

        var stats = Statistics.FromTrades(trades);
        return Ok(new StatsDto
        {
            TradeCount = stats.TradeCount,
            Profit = stats.Profit,
            Loss = stats.Loss,
            WinRate = stats.WinRate,
            AveragePnL = stats.AveragePnL,
            BestResult = stats.BestResult,
            WorstResult = stats.WorstResult,
            TotalPnL = stats.Profit - stats.Loss
        });
    }
}

public class StatsDto
{
    public int TradeCount { get; set; }
    public double Profit { get; set; }
    public double Loss { get; set; }
    public double WinRate { get; set; }
    public double AveragePnL { get; set; }
    public double BestResult { get; set; }
    public double WorstResult { get; set; }
    public double TotalPnL { get; set; }
}

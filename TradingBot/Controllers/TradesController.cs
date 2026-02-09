using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TradesController : ControllerBase
{
    private readonly ITradeStorage _storage;
    private readonly TradeRepository _repo;
    private readonly IConfiguration _config;
    private readonly ILogger<TradesController> _logger;

    public TradesController(ITradeStorage storage, TradeRepository repo, IConfiguration config, ILogger<TradesController> logger)
    {
        _storage = storage;
        _repo = repo;
        _config = config;
        _logger = logger;
    }

    private long GetCurrentUserId()
    {
        var id = _config["Developer:UserId"];
        return long.TryParse(id, out var v) ? v : 1;
    }

    [HttpGet]
    public async Task<ActionResult<List<TradeDto>>> GetTrades([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var userId = GetCurrentUserId();
        List<Trade> trades;
        if (from.HasValue && to.HasValue)
            trades = await _storage.GetTradesInDateRangeAsync(userId, from.Value, to.Value);
        else
            trades = await _storage.GetTradesAsync(userId);

        trades = trades.OrderByDescending(t => t.Date).ToList();
        return Ok(trades.Select(ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TradeDto>> GetTrade(int id)
    {
        var userId = GetCurrentUserId();
        var trade = await _repo.GetTradeByIdAsync(userId, id);
        if (trade == null)
            return NotFound();
        return Ok(ToDto(trade));
    }

    [HttpPost]
    public async Task<ActionResult<TradeDto>> CreateTrade([FromBody] CreateTradeRequest req)
    {
        var userId = GetCurrentUserId();
        var trade = new Trade
        {
            UserId = userId,
            Date = req.Date ?? DateTime.UtcNow,
            Ticker = req.Ticker?.Trim() ?? "",
            Account = req.Account?.Trim(),
            Session = req.Session?.Trim(),
            Position = req.Position?.Trim(),
            Direction = req.Direction?.Trim(),
            Context = req.Context ?? new List<string>(),
            Setup = req.Setup ?? new List<string>(),
            Result = req.Result?.Trim(),
            RR = req.RR?.Trim(),
            Risk = req.Risk,
            EntryDetails = req.EntryDetails?.Trim(),
            Comment = req.Comment?.Trim(),
            Note = req.Note?.Trim(),
            Emotions = req.Emotions ?? new List<string>(),
            PnL = req.PnL,
            ScreenshotPath = req.Screenshots != null && req.Screenshots.Count > 0
                ? (req.Screenshots.Count == 1 ? req.Screenshots[0] : System.Text.Json.JsonSerializer.Serialize(req.Screenshots))
                : null
        };

        await _storage.AddTradeAsync(trade);
        _logger.LogInformation("Web API: добавлена сделка {Id} для UserId={UserId}", trade.Id, userId);
        return CreatedAtAction(nameof(GetTrade), new { id = trade.Id }, ToDto(trade));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TradeDto>> UpdateTrade(int id, [FromBody] UpdateTradeRequest req)
    {
        var userId = GetCurrentUserId();
        var trade = await _repo.GetTradeByIdAsync(userId, id);
        if (trade == null)
            return NotFound();

        if (req.Date.HasValue) trade.Date = req.Date.Value;
        if (req.Ticker != null) trade.Ticker = req.Ticker.Trim();
        if (req.Account != null) trade.Account = req.Account.Trim();
        if (req.Session != null) trade.Session = req.Session.Trim();
        if (req.Position != null) trade.Position = req.Position.Trim();
        if (req.Direction != null) trade.Direction = req.Direction.Trim();
        if (req.Context != null) trade.Context = req.Context;
        if (req.Setup != null) trade.Setup = req.Setup;
        if (req.Result != null) trade.Result = req.Result.Trim();
        if (req.RR != null) trade.RR = req.RR.Trim();
        if (req.Risk.HasValue) trade.Risk = req.Risk;
        if (req.EntryDetails != null) trade.EntryDetails = req.EntryDetails.Trim();
        if (req.Comment != null) trade.Comment = req.Comment.Trim();
        if (req.Note != null) trade.Note = req.Note.Trim();
        if (req.Emotions != null) trade.Emotions = req.Emotions;
        if (req.PnL.HasValue) trade.PnL = req.PnL.Value;

        await _storage.UpdateTradeAsync(trade);
        _logger.LogInformation("Web API: обновлена сделка {Id}", id);
        return Ok(ToDto(trade));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTrade(int id)
    {
        var userId = GetCurrentUserId();
        var trade = await _repo.GetTradeByIdAsync(userId, id);
        if (trade == null)
            return NotFound();

        await _storage.DeleteTradeAsync(trade);
        _logger.LogInformation("Web API: удалена сделка {Id}", id);
        return NoContent();
    }

    private static TradeDto ToDto(Trade t) => new TradeDto
    {
        Id = t.Id,
        UserId = t.UserId,
        Date = t.Date,
        Ticker = t.Ticker ?? "",
        Account = t.Account,
        Session = t.Session,
        Position = t.Position,
        Direction = t.Direction,
        Context = t.Context ?? new List<string>(),
        Setup = t.Setup ?? new List<string>(),
        Result = t.Result,
        RR = t.RR,
        Risk = t.Risk,
        EntryDetails = t.EntryDetails,
        Comment = t.Comment,
        Note = t.Note,
        Emotions = t.Emotions ?? new List<string>(),
        PnL = t.PnL,
        NotionPageId = t.NotionPageId,
        Screenshots = ParseScreenshots(t.ScreenshotPath)
    };

    private static List<string>? ParseScreenshots(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (path.StartsWith("["))
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(path);
            }
            catch { return new List<string> { path }; }
        }
        return new List<string> { path };
    }
}

public class TradeDto
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public DateTime Date { get; set; }
    public string Ticker { get; set; } = "";
    public string? Account { get; set; }
    public string? Session { get; set; }
    public string? Position { get; set; }
    public string? Direction { get; set; }
    public List<string> Context { get; set; } = new();
    public List<string> Setup { get; set; } = new();
    public string? Result { get; set; }
    public string? RR { get; set; }
    public decimal? Risk { get; set; }
    public string? EntryDetails { get; set; }
    public string? Comment { get; set; }
    public string? Note { get; set; }
    public List<string> Emotions { get; set; } = new();
    public decimal PnL { get; set; }
    public string? NotionPageId { get; set; }
    public List<string>? Screenshots { get; set; }
}

public class CreateTradeRequest
{
    public DateTime? Date { get; set; }
    public string? Ticker { get; set; }
    public List<string>? Screenshots { get; set; }
    public string? Account { get; set; }
    public string? Session { get; set; }
    public string? Position { get; set; }
    public string? Direction { get; set; }
    public List<string>? Context { get; set; }
    public List<string>? Setup { get; set; }
    public string? Result { get; set; }
    public string? RR { get; set; }
    public decimal? Risk { get; set; }
    public string? EntryDetails { get; set; }
    public string? Comment { get; set; }
    public string? Note { get; set; }
    public List<string>? Emotions { get; set; }
    public decimal PnL { get; set; }
}

public class UpdateTradeRequest
{
    public DateTime? Date { get; set; }
    public string? Ticker { get; set; }
    public string? Account { get; set; }
    public string? Session { get; set; }
    public string? Position { get; set; }
    public string? Direction { get; set; }
    public List<string>? Context { get; set; }
    public List<string>? Setup { get; set; }
    public string? Result { get; set; }
    public string? RR { get; set; }
    public decimal? Risk { get; set; }
    public string? EntryDetails { get; set; }
    public string? Comment { get; set; }
    public string? Note { get; set; }
    public List<string>? Emotions { get; set; }
    public decimal? PnL { get; set; }
}

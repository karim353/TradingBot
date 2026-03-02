using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingBot.Models;

namespace TradingBot.Services;

/// <summary>
/// Seeds fake trades for the dashboard when the database has no trades for the API user.
/// Uses raw SQL so legacy NOT NULL columns (Comment, Emotion) are set and inserts succeed.
/// </summary>
public static class SeedData
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private static readonly (string Ticker, string Position, decimal PnL, string Setup, string Emotion, string Note)[] FakeTrades =
    {
        ("BTC", "LONG", 2.45m, "Breakout", "Confident", "Clean breakout above 64k. Volume confirmation."),
        ("ETH", "SHORT", -1.12m, "Mean Reversion", "Neutral", "Faded resistance, stopped out."),
        ("SOL", "LONG", 4.80m, "Trend Following", "Confident", "Strong trend from support."),
        ("BTC", "SHORT", 0.95m, "Breakout", "Cautious", "Short from failed breakout."),
        ("ETH", "LONG", -2.30m, "Breakout", "FOMO", "Entered too late on pump."),
        ("SOL", "SHORT", 1.55m, "Mean Reversion", "Neutral", "Scalp off daily resistance."),
        ("BTC", "LONG", 3.20m, "Trend Following", "Confident", "Held through pullback."),
        ("ETH", "LONG", 1.88m, "Breakout", "Confident", "Breakout with volume."),
        ("SOL", "LONG", -0.75m, "Mean Reversion", "Neutral", "Choppy session, small loss."),
        ("BTC", "SHORT", -1.50m, "Trend Following", "Stressed", "Trend was stronger than expected."),
        ("ETH", "SHORT", 2.10m, "Breakout", "Confident", "Failed breakout short."),
        ("SOL", "LONG", 5.40m, "Breakout", "Confident", "Momentum play, full target."),
        ("BTC", "LONG", 0.60m, "Mean Reversion", "Cautious", "Bounce from support."),
        ("ETH", "LONG", -0.90m, "Trend Following", "Neutral", "Whipsaw, closed BE."),
        ("SOL", "SHORT", 2.25m, "Breakout", "Confident", "Rejection at ATH."),
        ("BTC", "LONG", 1.35m, "Breakout", "Confident", "Consolidation breakout."),
        ("ETH", "SHORT", -2.00m, "Mean Reversion", "Stressed", "Short squeeze."),
        ("SOL", "LONG", 3.70m, "Trend Following", "Confident", "Trend continuation."),
        ("BTC", "SHORT", 0.45m, "Mean Reversion", "Neutral", "Small scalp."),
        ("ETH", "LONG", 2.80m, "Breakout", "Confident", "Ethereum upgrade hype."),
    };

    public static async Task SeedFakeTradesIfEmptyAsync(TradeContext context, long userId, ILogger logger)
    {
        var count = await context.Trades.CountAsync(t => t.UserId == userId);
        if (count > 0)
        {
            logger.LogInformation("SeedData: User {UserId} already has {Count} trades, skipping seed.", userId, count);
            return;
        }

        var now = DateTime.UtcNow;
        for (int i = 0; i < FakeTrades.Length; i++)
        {
            var f = FakeTrades[i];
            var date = now.AddDays(-FakeTrades.Length + i).AddHours(-i * 2);
            var session = i % 3 == 0 ? "NY" : i % 3 == 1 ? "LONDON" : "ASIA";
            var result = f.PnL > 0 ? "TP" : f.PnL < 0 ? "SL" : "BE";
            var direction = f.Position == "LONG" ? "Reversal" : "Continuation";
            var entryDetails = $"Entry — Exit (PnL {f.PnL}%)";
            var setupJson = JsonSerializer.Serialize(new List<string> { f.Setup }, JsonOptions);
            var emotionsJson = JsonSerializer.Serialize(new List<string> { f.Emotion }, JsonOptions);
            var contextJson = JsonSerializer.Serialize(new List<string>(), JsonOptions);

            // Raw SQL so legacy NOT NULL columns (Comment, Emotion, Strategy, Session) are set
            await context.Database.ExecuteSqlRawAsync(
                @"INSERT INTO Trades (UserId, Date, Ticker, Account, Session, Position, Direction, Context, Setup, Result, RR, Risk, EntryDetails, Comment, Note, Emotions, PnL, NotionPageId, ScreenshotPath, Emotion, Strategy)
                  VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20})",
                userId,
                date,
                f.Ticker ?? "",
                "Bybit",
                session,
                f.Position,
                direction,
                contextJson,
                setupJson,
                result,
                "2",
                1m,
                entryDetails,
                "",
                f.Note ?? "",
                emotionsJson,
                f.PnL,
                (string?)null,
                (string?)null,
                f.Emotion,
                f.Setup);
        }

        logger.LogInformation("SeedData: Added {Count} fake trades for UserId={UserId}.", FakeTrades.Length, userId);
    }
}

// UpdateHandler.cs

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using TradingBot.Models;
using TradingBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using ScottPlot;
using System.Text;
using SkiaSharp;

namespace TradingBot.Services
{
    public class UpdateHandler
    {
        private readonly TradeRepository _repo;
        private readonly PnLService _pnlService;
        private readonly NotionService _notionService;
        private readonly UIManager _uiManager;
        private readonly ILogger<UpdateHandler> _logger;
        private readonly IMemoryCache _cache;
        private readonly string _sqliteConnectionString;
        private readonly string _botId;

        private class UserState
        {
            public int Step { get; set; }
            public Trade Trade { get; set; }
            public string Action { get; set; }
            public int MessageId { get; set; }
            public string Language { get; set; } = "ru";
            public string TradeId { get; set; }
            public DateTime LastInputTime { get; set; } = DateTime.UtcNow;
            public int ErrorCount { get; set; } = 0;
        }

        private static readonly TimeSpan PendingTradeTimeout = TimeSpan.FromHours(24);
        private static readonly TimeSpan AutoReturnDelay = TimeSpan.FromMinutes(5);
        private const int MaxRequestsPerMinute = 20;

        public UpdateHandler(TradeRepository repo, PnLService pnlService, NotionService notionService,
            UIManager uiManager, ILogger<UpdateHandler> logger, IMemoryCache cache, string sqliteConnectionString,
            string botId)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _pnlService = pnlService ?? throw new ArgumentNullException(nameof(pnlService));
            _notionService = notionService ?? throw new ArgumentNullException(nameof(notionService));
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _botId = botId ?? throw new ArgumentNullException(nameof(botId));
            if (string.IsNullOrWhiteSpace(sqliteConnectionString))
            {
                throw new ArgumentNullException(nameof(sqliteConnectionString),
                    "SQLite connection string cannot be null or empty.");
            }

            try
            {
                var builder = new SqliteConnectionStringBuilder(sqliteConnectionString);
                _sqliteConnectionString = builder.ConnectionString;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid SQLite connection string format: {sqliteConnectionString}",
                    nameof(sqliteConnectionString), ex);
            }

            _logger.LogInformation(
                $"📈 UpdateHandler initialized (BotId={_botId}, ConnectionString={_sqliteConnectionString})");
            InitializeDatabaseAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_sqliteConnectionString);
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS PendingTrades (
                        BotId TEXT NOT NULL,
                        UserId INTEGER NOT NULL,
                        TradeId TEXT NOT NULL,
                        MessageId INTEGER NOT NULL,
                        TradeJson TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        PRIMARY KEY (BotId, UserId, TradeId)
                    );
                    CREATE TABLE IF NOT EXISTS UserStates (
                        BotId TEXT NOT NULL,
                        UserId INTEGER NOT NULL,
                        StateJson TEXT NOT NULL,
                        PRIMARY KEY (BotId, UserId)
                    );
                    CREATE TABLE IF NOT EXISTS UserSettings (
                        BotId TEXT NOT NULL,
                        UserId INTEGER NOT NULL,
                        SettingsJson TEXT NOT NULL,
                        PRIMARY KEY (BotId, UserId)
                    );
                ";
                await command.ExecuteNonQueryAsync();
                _logger.LogInformation("📊 Database initialized (PendingTrades, UserStates, UserSettings)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database initialization failed");
            }
        }

        private async Task SaveUserStateAsync(long userId, UserState state)
        {
            _cache.Set($"state_{userId}", state, TimeSpan.FromMinutes(30));
            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO UserStates (BotId, UserId, StateJson)
                VALUES ($botId, $userId, $stateJson)";
            command.Parameters.AddWithValue("$botId", _botId);
            command.Parameters.AddWithValue("$userId", userId);
            command.Parameters.AddWithValue("$stateJson", JsonSerializer.Serialize(state));
            await command.ExecuteNonQueryAsync();
        }

        private async Task<UserState?> GetUserStateAsync(long userId)
        {
            if (_cache.TryGetValue($"state_{userId}", out UserState cachedState))
            {
                return cachedState;
            }

            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT StateJson FROM UserStates
                WHERE BotId = $botId AND UserId = $userId";
            command.Parameters.AddWithValue("$botId", _botId);
            command.Parameters.AddWithValue("$userId", userId);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                string stateJson = reader.GetString(0);
                try
                {
                    var state = JsonSerializer.Deserialize<UserState>(stateJson);
                    if (state != null)
                    {
                        _cache.Set($"state_{userId}", state, TimeSpan.FromMinutes(30));
                        return state;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to deserialize UserState for UserId={userId}");
                }
            }

            return null;
        }

        private async Task DeleteUserStateAsync(long userId)
        {
            _cache.Remove($"state_{userId}");
            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM UserStates
                WHERE BotId = $botId AND UserId = $userId";
            command.Parameters.AddWithValue("$botId", _botId);
            command.Parameters.AddWithValue("$userId", userId);
            await command.ExecuteNonQueryAsync();
            _logger.LogInformation($"🗑️ Deleted user state (UserId={userId})");
        }

        private async Task<UserSettings> GetUserSettingsAsync(long userId)
        {
            if (_cache.TryGetValue($"settings_{userId}", out UserSettings cachedSettings))
            {
                return cachedSettings;
            }

            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT SettingsJson FROM UserSettings
                WHERE BotId = $botId AND UserId = $userId";
            command.Parameters.AddWithValue("$botId", _botId);
            command.Parameters.AddWithValue("$userId", userId);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                string settingsJson = reader.GetString(0);
                try
                {
                    var settings = JsonSerializer.Deserialize<UserSettings>(settingsJson);
                    if (settings != null)
                    {
                        _cache.Set($"settings_{userId}", settings, TimeSpan.FromMinutes(30));
                        return settings;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to deserialize UserSettings for UserId={userId}");
                }
            }

            var newSettings = new UserSettings();
            _cache.Set($"settings_{userId}", newSettings, TimeSpan.FromMinutes(30));
            using var insertConn = new SqliteConnection(_sqliteConnectionString);
            await insertConn.OpenAsync();
            var insertCmd = insertConn.CreateCommand();
            insertCmd.CommandText = @"
                INSERT OR REPLACE INTO UserSettings (BotId, UserId, SettingsJson)
                VALUES ($botId, $userId, $json)";
            insertCmd.Parameters.AddWithValue("$botId", _botId);
            insertCmd.Parameters.AddWithValue("$userId", userId);
            insertCmd.Parameters.AddWithValue("$json", JsonSerializer.Serialize(newSettings));
            await insertCmd.ExecuteNonQueryAsync();
            return newSettings;
        }

        private async Task SaveUserSettingsAsync(long userId, UserSettings settings)
        {
            _cache.Set($"settings_{userId}", settings, TimeSpan.FromMinutes(30));
            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO UserSettings (BotId, UserId, SettingsJson)
                VALUES ($botId, $userId, $json)";
            command.Parameters.AddWithValue("$botId", _botId);
            command.Parameters.AddWithValue("$userId", userId);
            command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(settings));
            await command.ExecuteNonQueryAsync();
        }

        private async Task SavePendingTradeAsync(long userId, string tradeId, int messageId, Trade trade)
        {
            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO PendingTrades (BotId, UserId, TradeId, MessageId, TradeJson, CreatedAt)
                VALUES ($botId, $userId, $tradeId, $messageId, $tradeJson, $createdAt)";
            command.Parameters.AddWithValue("$botId", _botId);
            command.Parameters.AddWithValue("$userId", userId);
            command.Parameters.AddWithValue("$tradeId", tradeId);
            command.Parameters.AddWithValue("$messageId", messageId);
            command.Parameters.AddWithValue("$tradeJson", JsonSerializer.Serialize(trade));
            command.Parameters.AddWithValue("$createdAt", DateTime.UtcNow.ToString("o"));
            _logger.LogInformation(
                $"💰 Trade saved in PendingTrades (UserId={userId}, TradeId={tradeId}, Ticker={trade.Ticker}, PnL={trade.PnL})");
            await command.ExecuteNonQueryAsync();
        }

        private async Task<(Trade Trade, int MessageId, DateTime CreatedAt)?> GetPendingTradeByTradeIdAsync(long userId,
            string tradeId)
        {
            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT TradeJson, MessageId, CreatedAt FROM PendingTrades
                WHERE BotId = $botId AND UserId = $userId AND TradeId = $tradeId";
            command.Parameters.AddWithValue("$botId", _botId);
            command.Parameters.AddWithValue("$userId", userId);
            command.Parameters.AddWithValue("$tradeId", tradeId);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                string tradeJson = reader.GetString(0);
                int msgId = reader.GetInt32(1);
                DateTime createdAt = DateTime.Parse(reader.GetString(2));
                try
                {
                    var trade = JsonSerializer.Deserialize<Trade>(tradeJson);
                    if (trade != null)
                    {
                        return (trade, msgId, createdAt);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to deserialize pending Trade for TradeId={tradeId}");
                }
            }

            return null;
        }

        private async Task DeletePendingTradeByTradeIdAsync(long userId, string tradeId)
        {
            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM PendingTrades
                WHERE BotId = $botId AND UserId = $userId AND TradeId = $tradeId";
            command.Parameters.AddWithValue("$botId", _botId);
            command.Parameters.AddWithValue("$userId", userId);
            command.Parameters.AddWithValue("$tradeId", tradeId);
            await command.ExecuteNonQueryAsync();
            _logger.LogInformation($"🗑️ Deleted pending trade (UserId={userId}, TradeId={tradeId})");
        }

        private async Task<List<(string TradeId, Trade Trade, int MessageId, DateTime CreatedAt)>>
            GetPendingTradesForUserAsync(long userId, int page, int pageSize = 5)
        {
            var result = new List<(string, Trade, int, DateTime)>();
            int offset = (page - 1) * pageSize;
            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT TradeId, TradeJson, MessageId, CreatedAt FROM PendingTrades
                WHERE BotId = $botId AND UserId = $userId
                ORDER BY CreatedAt DESC
                LIMIT $limit OFFSET $offset";
            command.Parameters.AddWithValue("$botId", _botId);
            command.Parameters.AddWithValue("$userId", userId);
            command.Parameters.AddWithValue("$limit", pageSize);
            command.Parameters.AddWithValue("$offset", offset);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string tradeId = reader.GetString(0);
                string tradeJson = reader.GetString(1);
                int messageId = reader.GetInt32(2);
                DateTime createdAt = DateTime.Parse(reader.GetString(3));
                try
                {
                    var trade = JsonSerializer.Deserialize<Trade>(tradeJson);
                    if (trade != null)
                    {
                        result.Add((tradeId, trade, messageId, createdAt));
                    }
                }
                catch
                {
                    /* ignore deserialization errors */
                }
            }

            return result;
        }

        private async Task<int> GetPendingTradesCountAsync(long userId)
        {
            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) FROM PendingTrades
                WHERE BotId = $botId AND UserId = $userId";
            command.Parameters.AddWithValue("$botId", _botId);
            command.Parameters.AddWithValue("$userId", userId);
            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        private async Task CleanExpiredPendingTradesAsync()
        {
            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM PendingTrades
                WHERE BotId = $botId AND CreatedAt < $expireBefore";
            command.Parameters.AddWithValue("$botId", _botId);
            command.Parameters.AddWithValue("$expireBefore", DateTime.UtcNow.Add(-PendingTradeTimeout).ToString("o"));
            int rows = await command.ExecuteNonQueryAsync();
            if (rows > 0)
            {
                _logger.LogInformation($"🕒 Cleaned up {rows} expired pending trades");
            }
        }

        /*private async Task SendMainMenuAsync(long chatId, long userId, ITelegramBotClient bot, CancellationToken ct)
        {
            var settings = await GetUserSettingsAsync(userId);
            var allTrades = await _repo.GetTradesAsync(userId);
            int totalTrades = allTrades.Count;
            decimal totalPnL = allTrades.Any() ? allTrades.Sum(t => t.PnL) : 0;
            int profitableCount = allTrades.Count(t => t.PnL > 0);
            int winRate = totalTrades > 0 ? (int)((double)profitableCount / totalTrades * 100) : 0;
            int tradesToday = (await _repo.GetTradesInDateRangeAsync(userId, DateTime.Today, DateTime.Now)).Count;
            string mainText = _uiManager.GetText("main_menu", settings.Language, tradesToday, totalPnL.ToString("F2"), winRate);

            using var stream = new FileStream("banner.png", FileMode.Open, FileAccess.Read); // Укажите путь к изображению
            await bot.SendPhoto(chatId, InputFile.FromStream(stream), caption: mainText, replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: ct);
            _logger.LogInformation($"🏠 Sent main menu with banner to UserId={userId}");
        }*/
        private async Task SendMainMenuAsync(long chatId, long userId, ITelegramBotClient bot, CancellationToken ct)
        {
            var settings = await GetUserSettingsAsync(userId);
            var allTrades = await _repo.GetTradesAsync(userId);
            int totalTrades = allTrades.Count;
            decimal totalPnL = allTrades.Any() ? allTrades.Sum(t => t.PnL) : 0;
            int profitableCount = allTrades.Count(t => t.PnL > 0);
            int winRate = totalTrades > 0 ? (int)((double)profitableCount / totalTrades * 100) : 0;
            int tradesToday = (await _repo.GetTradesInDateRangeAsync(userId, DateTime.Today, DateTime.Now)).Count;

            string mainText = _uiManager.GetText("main_menu", settings.Language, tradesToday, totalPnL.ToString("F2"),
                winRate);

            using var stream = new FileStream("banner.gif", FileMode.Open, FileAccess.Read); // Путь к GIF-анимации
            await bot.SendAnimation(
                chatId,
                InputFile.FromStream(stream, "banner.gif"),
                caption: mainText,
                replyMarkup: _uiManager.GetMainMenu(settings),
                cancellationToken: ct
            );

            _logger.LogInformation($"🏠 Sent main menu with GIF banner to UserId={userId}");
        }

        private async Task SendStatisticsAsync(long chatId, long userId, ITelegramBotClient bot, UserSettings settings, CancellationToken ct)
{
    var trades = await _repo.GetTradesAsync(userId);
    if (!trades.Any())
    {
        await bot.SendMessage(chatId, "📉 Нет сделок для статистики.",
            replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: ct);
        return;
    }

    int totalTrades = trades.Count;
    decimal totalPnL = trades.Sum(t => t.PnL);
    decimal avgPnL = totalTrades > 0 ? totalPnL / totalTrades : 0;
    int profitable = trades.Count(t => t.PnL > 0);
    int winRate = totalTrades > 0 ? (int)((double)profitable / totalTrades * 100) : 0;
    string statsText = _uiManager.GetText("stats_result", settings.Language, "за всё время", totalTrades,
        totalPnL.ToString("F2"), profitable, totalTrades - profitable, winRate);

    // ==== Стиль TradingView black & white ====
    int width = 950, height = 540;
    float marginLeft = 70, marginRight = 40, marginTop = 40, marginBottom = 50;

    string tmpPng = Path.Combine(Path.GetTempPath(), $"equity_{userId}_{Guid.NewGuid():N}.png");

    var ys = new List<float>();
    trades.Sort((a, b) => a.Date.CompareTo(b.Date));
    for (int i = 0; i < trades.Count; i++)
    {
        ys.Add((float)trades[i].PnL);
    }
    int N = ys.Count;
    var xs = Enumerable.Range(0, N).Select(i => marginLeft + (width - marginLeft - marginRight) * i / Math.Max(N - 1, 1)).ToArray();

    // min/max, шаг сетки
    float minY = ys.Min(), maxY = ys.Max();
    if (Math.Abs(maxY - minY) < 1e-3f) { minY -= 1; maxY += 1; }
    float gridStep = (float)Math.Pow(10, Math.Floor(Math.Log10((maxY - minY) / 5.0)));
    float gridMin = (float)Math.Floor(minY / gridStep) * gridStep;
    float gridMax = (float)Math.Ceiling(maxY / gridStep) * gridStep;

    using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
    {
        var canvas = surface.Canvas;

        // Тёмный фон
        canvas.Clear(SKColor.Parse("#101113"));

        // Сетка белая, но полупрозрачная (как на скрине)
        using (var gridPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            Color = new SKColor(255, 255, 255, 45),
            IsAntialias = true
        })
        {
            // Вертикальные (по X, нечасто)
            int xDivs = Math.Min(N, 8);
            for (int i = 0; i <= xDivs; i++)
            {
                float x = marginLeft + (width - marginLeft - marginRight) * i / xDivs;
                canvas.DrawLine(x, marginTop, x, height - marginBottom, gridPaint);
            }
            // Горизонтальные (по Y)
            for (float yVal = gridMin; yVal <= gridMax + 0.001f; yVal += gridStep)
            {
                float y = height - marginBottom - (yVal - minY) / (maxY - minY) * (height - marginTop - marginBottom);
                canvas.DrawLine(marginLeft, y, width - marginRight, y, gridPaint);
            }
        }

        // Оси (ярко-белые)
        using (var axisPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            Color = SKColors.White,
            IsAntialias = true
        })
        {
            canvas.DrawLine(marginLeft, marginTop, marginLeft, height - marginBottom, axisPaint);
            canvas.DrawLine(marginLeft, height - marginBottom, width - marginRight, height - marginBottom, axisPaint);
        }

        // Деления и подписи по Y (серый/белый)
        using (var yLabelPaint = new SKPaint
        {
            Color = SKColor.FromHsv(0, 0, 90), // светло-серый
            TextSize = 19,
            Typeface = SKTypeface.FromFamilyName("Arial"),
            IsAntialias = true
        })
        {
            for (float yVal = gridMin; yVal <= gridMax + 0.001f; yVal += gridStep)
            {
                float y = height - marginBottom - (yVal - minY) / (maxY - minY) * (height - marginTop - marginBottom);
                canvas.DrawText($"{yVal:F2}", 19, y + 7, yLabelPaint);
            }
        }

        // Деления по X (каждая 10я сделка)
        using (var xLabelPaint = new SKPaint
        {
            Color = SKColor.FromHsv(0, 0, 85),
            TextSize = 16,
            Typeface = SKTypeface.FromFamilyName("Arial"),
            IsAntialias = true
        })
        {
            int every = Math.Max(N / 10, 1);
            for (int i = 0; i < N; i += every)
            {
                float x = xs[i];
                canvas.DrawText((i + 1).ToString(), x - 10, height - marginBottom + 23, xLabelPaint);
            }
        }

        // Основная линия equity (ярко-белая)
        using (var linePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2.7f,
            Color = SKColors.White,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round
        })
        using (var path = new SKPath())
        {
            path.MoveTo(xs[0], height - marginBottom - (ys[0] - minY) / (maxY - minY) * (height - marginTop - marginBottom));
            for (int i = 1; i < N; i++)
                path.LineTo(xs[i], height - marginBottom - (ys[i] - minY) / (maxY - minY) * (height - marginTop - marginBottom));
            canvas.DrawPath(path, linePaint);
        }

        // Толстая подложка (имитация свечения)
        using (var glowPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 10,
            Color = new SKColor(255, 255, 255, 24),
            IsAntialias = true
        })
        using (var path = new SKPath())
        {
            path.MoveTo(xs[0], height - marginBottom - (ys[0] - minY) / (maxY - minY) * (height - marginTop - marginBottom));
            for (int i = 1; i < N; i++)
                path.LineTo(xs[i], height - marginBottom - (ys[i] - minY) / (maxY - minY) * (height - marginTop - marginBottom));
            canvas.DrawPath(path, glowPaint);
        }

        // Маркеры (начала и конца) — круги
        using (var dotPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.White,
            IsAntialias = true
        })
        {
            float y1 = height - marginBottom - (ys[0] - minY) / (maxY - minY) * (height - marginTop - marginBottom);
            float yN = height - marginBottom - (ys[N - 1] - minY) / (maxY - minY) * (height - marginTop - marginBottom);
            canvas.DrawCircle(xs[0], y1, 5, dotPaint);
            canvas.DrawCircle(xs[N - 1], yN, 7, dotPaint);
        }

        // (по желанию) High/Low — мелкие горизонтальные полоски
        int idxMax = ys.IndexOf(ys.Max());
        int idxMin = ys.IndexOf(ys.Min());
        float yMax = height - marginBottom - (ys[idxMax] - minY) / (maxY - minY) * (height - marginTop - marginBottom);
        float yMin = height - marginBottom - (ys[idxMin] - minY) / (maxY - minY) * (height - marginTop - marginBottom);
        using (var hiPaint = new SKPaint { Color = SKColors.White, StrokeWidth = 4, Style = SKPaintStyle.Stroke, IsAntialias = true })
        {
            canvas.DrawLine(xs[idxMax] - 12, yMax, xs[idxMax] + 12, yMax, hiPaint);
            canvas.DrawLine(xs[idxMin] - 12, yMin, xs[idxMin] + 12, yMin, hiPaint);
        }

        // Минималистичный заголовок (маленьким белым)
        using (var titlePaint = new SKPaint
        {
            Color = SKColor.FromHsv(0, 0, 90),
            TextSize = 21,
            Typeface = SKTypeface.FromFamilyName("Arial"),
            IsAntialias = true
        })
            //canvas.DrawText("TradingView Equity Curve", marginLeft + 8, marginTop + 3, titlePaint);

        // Подписи осей
        using (var axisPaint = new SKPaint
        {
            Color = SKColor.FromHsv(0, 0, 90),
            TextSize = 17,
            IsAntialias = true
        })
        {
            canvas.DrawText("Trades", width / 2 - 27, height - 12, axisPaint);
            canvas.Save();
            canvas.RotateDegrees(-90, 22, height / 2 + 48);
            canvas.DrawText("PnL %", 22, height / 2 + 48, axisPaint);
            canvas.Restore();
        }

        // Сохраняем в PNG
        using var img = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(tmpPng);
        data.SaveTo(stream);
    }

    using var fs = new FileStream(tmpPng, FileMode.Open, FileAccess.Read);
    await bot.SendPhoto(chatId, InputFile.FromStream(fs), caption: statsText,
        replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: ct);
    File.Delete(tmpPng);
}

        private async Task SendHistoryAsync(long chatId, long userId, ITelegramBotClient bot, UserSettings settings,
            CancellationToken ct, int page)
        {
            var trades = await _repo.GetTradesAsync(userId);
            trades.Sort((a, b) => b.Date.CompareTo(a.Date));
            int pageSize = 5;
            var pageTrades = trades.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var (historyText, historyKeyboard) = _uiManager.GetHistoryScreen(pageTrades, page, "all", "none", settings);
            await bot.SendMessage(chatId, historyText, replyMarkup: historyKeyboard, cancellationToken: ct);
        }

        private async Task SendUserProfileAsync(long chatId, long userId, ITelegramBotClient bot, UserSettings settings,
            CancellationToken ct)
        {
            var user = await bot.GetChat(chatId, ct);
            var trades = await _repo.GetTradesAsync(userId);
            int totalTrades = trades.Count;
            decimal totalPnL = trades.Any() ? trades.Sum(t => t.PnL) : 0;
            decimal avgPnL = totalTrades > 0 ? totalPnL / totalTrades : 0;
            string profileText = $"👤 Профиль:\n" +
                                 $"Имя: {user.FirstName} {user.LastName}\n" +
                                 $"Telegram ID: {userId}\n" +
                                 $"Сделок: {totalTrades}\n" +
                                 $"Средний PnL: {avgPnL:F2}%\n" +
                                 $"Язык: {(settings.Language == "ru" ? "Русский" : "English")}\n" +
                                 $"Уведомления: {(settings.NotificationsEnabled ? "Вкл" : "Выкл")}\n" +
                                 $"Избранные тикеры: {(settings.FavoriteTickers.Any() ? string.Join(", ", settings.FavoriteTickers) : "Нет")}";
            await bot.SendMessage(chatId, profileText, replyMarkup: _uiManager.GetMainMenu(settings),
                cancellationToken: ct);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            long userId = 0;
            long chatId = 0;
            try
            {
                if (update.Type == UpdateType.Message && update.Message != null)
                {
                    var message = update.Message;
                    chatId = message.Chat.Id;
                    userId = message.From?.Id ?? chatId;
                    string text = message.Text?.Trim() ?? "";

                    _logger.LogInformation(
                        $"📩 Message from UserId={userId}, ChatId={chatId}: {(string.IsNullOrEmpty(text) ? "[non‑text]" : text)}");

                    var settings = await GetUserSettingsAsync(userId);
                    var state = await GetUserStateAsync(userId) ?? new UserState { Language = settings.Language };

                    // Обработка изображения (скриншота PnL)
                    if ((message.Photo?.Any() == true) || (message.Document != null &&
                                                           (message.Document.MimeType?.StartsWith("image/") ?? false)))
                    {
                        _logger.LogInformation($"📸 Processing image from UserId={userId}");
                        try
                        {
                            var fileInfo = await bot.GetFile(message.Photo?.Last().FileId ?? message.Document!.FileId,
                                cancellationToken);
                            if (string.IsNullOrEmpty(fileInfo.FilePath))
                            {
                                await bot.SendMessage(chatId, "❌ Не удалось получить файл изображения.",
                                    replyMarkup: _uiManager.GetErrorKeyboard(settings),
                                    cancellationToken: cancellationToken);
                                return;
                            }

                            await using var stream = new MemoryStream();
                            await bot.DownloadFile(fileInfo.FilePath, stream, cancellationToken);
                            stream.Position = 0;

                            var data = _pnlService.ExtractFromImage(stream);
                            _logger.LogInformation(
                                $"📊 OCR result: Ticker={data.Ticker}, Direction={data.Direction}, PnL={data.PnLPercent}");

                            string tradeId = Guid.NewGuid().ToString();
                            var trade = new Trade
                            {
                                UserId = userId,
                                Date = data.TradeDate ?? DateTime.Now,
                                Ticker = data.Ticker ?? "",
                                Direction = data.Direction ?? "",
                                PnL = data.PnLPercent ?? 0,
                                OpenPrice = data.Open,
                                Entry = data.Close
                            };

                            if (string.IsNullOrEmpty(trade.Ticker) || string.IsNullOrEmpty(trade.Direction) ||
                                data.PnLPercent == null)
                            {
                                var newState = new UserState
                                {
                                    Action = "new_trade",
                                    Step = string.IsNullOrEmpty(trade.Ticker) ? 1 :
                                        string.IsNullOrEmpty(trade.Direction) ? 2 : 3,
                                    Trade = trade,
                                    Language = settings.Language,
                                    TradeId = tradeId
                                };
                                var (nextTxt, nextKb) =
                                    _uiManager.GetTradeInputScreen(trade, newState.Step, settings, tradeId);
                                var msg = await bot.SendMessage(chatId, nextTxt, replyMarkup: nextKb,
                                    cancellationToken: cancellationToken);
                                newState.MessageId = msg.MessageId;
                                await SaveUserStateAsync(userId, newState);
                            }
                            else
                            {
                                var (confTxt, confKb) = _uiManager.GetTradeConfirmationScreen(trade, tradeId, settings);
                                var confMsg = await bot.SendMessage(chatId, confTxt, replyMarkup: confKb,
                                    cancellationToken: cancellationToken);
                                await SavePendingTradeAsync(userId, tradeId, confMsg.MessageId, trade);
                                await UpdateRecentSettingsAsync(userId, trade, settings);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error processing image for UserId={userId}");
                            await bot.SendMessage(chatId, "❌ Ошибка при обработке изображения.",
                                replyMarkup: _uiManager.GetErrorKeyboard(settings),
                                cancellationToken: cancellationToken);
                        }

                        return;
                    }

                    // Лимит частоты запросов
                    string rlKey = $"rate_limit_{userId}";
                    if (_cache.TryGetValue(rlKey, out int req) && req >= MaxRequestsPerMinute)
                    {
                        await bot.SendMessage(chatId, "⏳ Слишком много запросов. Попробуйте позже.",
                            cancellationToken: cancellationToken);
                        return;
                    }

                    _cache.Set(rlKey, (req > 0 ? req : 0) + 1, TimeSpan.FromMinutes(1));

                    if (text == "/me")
                    {
                        await SendUserProfileAsync(chatId, userId, bot, settings, cancellationToken);
                        return;
                    }

                    if (text == "/menu")
                    {
                        await DeleteUserStateAsync(userId); // Очищаем состояние, если оно есть
                        await SendMainMenuAsync(chatId, userId, bot, cancellationToken);
                        return;
                    }


                    if (text == "/start")
                    {
                        if (_cache.TryGetValue($"seen_tutorial_{userId}", out bool _))
                        {
                            await DeleteUserStateAsync(userId);
                            var allTrades = await _repo.GetTradesAsync(userId);
                            int totalTrades = allTrades.Count;
                            decimal totalPnL = 0;
                            if (allTrades.Count > 0) totalPnL = allTrades.Sum(t => t.PnL);
                            int profitableCount = allTrades.Count(t => t.PnL > 0);
                            int winRate = totalTrades > 0 ? (int)((double)profitableCount / totalTrades * 100) : 0;
                            int tradesToday =
                                (await _repo.GetTradesInDateRangeAsync(userId, DateTime.Today, DateTime.Now)).Count;
                            string mainText = _uiManager.GetText("main_menu", settings.Language, tradesToday,
                                totalPnL.ToString("F2"), winRate);
                            await bot.SendMessage(chatId, mainText, replyMarkup: _uiManager.GetMainMenu(settings),
                                cancellationToken: cancellationToken);
                            return;
                        }

                        await DeleteUserStateAsync(userId);
                        state = new UserState
                        {
                            Language = settings.Language,
                            Action = "onboarding",
                            Step = 1
                        };
                        var (welcomeText, onboardingKeyboard) =
                            _uiManager.GetOnboardingScreen(state.Step, state.Language);
                        var sentMessage = await bot.SendMessage(chatId, welcomeText, replyMarkup: onboardingKeyboard,
                            cancellationToken: cancellationToken);
                        state.MessageId = sentMessage.MessageId;
                        await SaveUserStateAsync(userId, state);
                        _cache.Set($"seen_tutorial_{userId}", true, TimeSpan.FromDays(30));
                        return;
                    }

                    if (state.Action?.StartsWith("new_trade") == true ||
                        state.Action?.StartsWith("edit_trade") == true || state.Action?.StartsWith("input_") == true)
                    {
                        await HandleTradeInputAsync(bot, chatId, userId, state, settings, text, message.MessageId,
                            cancellationToken);
                        return;
                    }

                    if (state.Action?.StartsWith("settings_") == true && state.Action != "settings_menu")
                    {
                        await HandleSettingsInputAsync(bot, chatId, userId, state, settings, text, cancellationToken);
                        return;
                    }

                    // если сообщение не распознано контекстно, просим использовать кнопки
                    await bot.SendMessage(chatId, "👇 Пожалуйста, используйте кнопки ниже:",
                        replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                    return;
                }
                else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
                {
                    var callback = update.CallbackQuery;
                    chatId = callback.Message?.Chat.Id ?? 0;
                    userId = callback.From.Id;
                    int msgId = callback.Message?.MessageId ?? 0;
                    string data = callback.Data ?? string.Empty;

                    _logger.LogInformation($"📲 Callback from UserId={userId}, ChatId={chatId}, MsgId={msgId}: {data}");
                    await bot.AnswerCallbackQuery(callback.Id, cancellationToken: cancellationToken);

                    // получаем актуальные состояние и настройки
                    var state = await GetUserStateAsync(userId) ?? new UserState
                        { Language = (await GetUserSettingsAsync(userId)).Language };
                    var settings = await GetUserSettingsAsync(userId);

                    // пытаемся удалить старое сообщение с кнопками, если есть
                    try
                    {
                        await bot.DeleteMessage(chatId, msgId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Delete msg {msgId} failed");
                    }

                    // 🔁 Унифицированный парсинг callback-данных
                    string[] parts = data.Split('_', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0)
                        return;

                    string action = parts[0].ToLowerInvariant();
                    string tradeId = null;

                    // 🔄 Нормализация действий
                    if (action == "history" && parts.Length > 1 && parts[1] == "page")
                        action = "history_page";
                    else if (action == "stats" && parts.Length > 1 && parts[1] == "period")
                        action = "statsperiod";
                    else if (action == "advstats" && parts.Length > 1 && parts[1] == "period")
                        action = "advstatsperiod";

                    // 🧠 Извлечение TradeId
                    // 🧠 Извлечение TradeId
                    // 🧠 Извлечение TradeId
                    if (action == "confirm" || action == "confirm_trade" && parts.Length >= 2)
                        tradeId = parts[^1];
                    else if (action == "edit" && parts.Length >= 2)
                        tradeId = parts[1];
                    else if (action.StartsWith("skip") && parts.Length >= 3)
                        tradeId = parts.FirstOrDefault(p => Guid.TryParse(p, out _));
                    else if (action == "allcorrect" && parts.Length >= 2)
                        tradeId = parts[1];
                    else
                    {
                        int idx = Array.IndexOf(parts, "trade");
                        if (idx >= 0 && idx < parts.Length - 1)
                            tradeId = parts[idx + 1];
                    }

                    // [DEBUG]
                    _logger.LogInformation("[DEBUG] Parsed callback: action={Action}, tradeId={TradeId}, raw={Data}",
                        action, tradeId, data);


                    switch (action.ToLowerInvariant())
                    {
                        case "onboarding":
                            state.Step++;
                            if (state.Step <= 3)
                            {
                                var (onboardText, onboardKeyboard) =
                                    _uiManager.GetOnboardingScreen(state.Step, settings.Language);
                                var sendMessage = await bot.SendMessage(chatId, onboardText,
                                    replyMarkup: onboardKeyboard, cancellationToken: cancellationToken);
                                state.MessageId = sendMessage.MessageId;
                                await SaveUserStateAsync(userId, state);
                            }
                            else
                            {
                                await DeleteUserStateAsync(userId);

                                // Reuse variables from outer scope or declare without conflicts
                                var trades = await _repo.GetTradesAsync(userId); // Renamed to avoid conflict
                                int tradeCount = trades.Count; // Renamed to avoid conflict
                                decimal cumulativePnL = trades.Count > 0 ? trades.Sum(t => t.PnL) : 0; // Renamed
                                int positiveTrades = trades.Count(t => t.PnL > 0); // Renamed
                                int successRate =
                                    tradeCount > 0 ? (int)((double)positiveTrades / tradeCount * 100) : 0; // Renamed
                                int dailyTrades =
                                    (await _repo.GetTradesInDateRangeAsync(userId, DateTime.Today, DateTime.Now))
                                    .Count; // Renamed
                                string menuText = _uiManager.GetText("main_menu", settings.Language, dailyTrades,
                                    cumulativePnL.ToString("F2"), successRate); // Renamed

                                await bot.SendMessage(chatId, menuText,
                                    replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                            }

                            break;

                        case "start":
                        case "start_trade":
                            var lastTrade = await _repo.GetLastTradeAsync(userId);
                            tradeId = Guid.NewGuid().ToString();
                            state = new UserState
                            {
                                Action = "new_trade",
                                Step = 1,
                                Trade = new Trade { UserId = userId, Date = DateTime.Now },
                                Language = settings.Language,
                                TradeId = tradeId
                            };
                            var (tradeText, tradeKeyboard) = _uiManager.GetTradeInputScreen(state.Trade, state.Step,
                                settings, tradeId, lastTrade);
                            var tradeMessage = await bot.SendMessage(chatId, tradeText, replyMarkup: tradeKeyboard,
                                cancellationToken: cancellationToken);
                            state.MessageId = tradeMessage.MessageId;
                            await SaveUserStateAsync(userId, state);
                            break;

                        case "edit":
                            _logger.LogInformation($"[DEBUG] edit callback, tradeId={tradeId}");
                            if (string.IsNullOrEmpty(tradeId))
                            {
                                _logger.LogWarning($"[DEBUG] edit callback: tradeId is null or empty, raw data={data}");
                                await bot.SendMessage(chatId, "⏰ Сделка устарела.",
                                    replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                                break;
                            }

                            var pendingTradeDataEdit = await GetPendingTradeByTradeIdAsync(userId, tradeId);
                            if (pendingTradeDataEdit.HasValue)
                            {
                                var (trade, originalMessageId, createdAt) = pendingTradeDataEdit.Value;
                                try
                                {
                                    await bot.DeleteMessage(chatId, originalMessageId, cancellationToken);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex,
                                        $"Failed to delete message {originalMessageId} for UserId={userId}");
                                }

                                state = new UserState
                                {
                                    Action = $"edit_trade_{tradeId}",
                                    Step = 0,
                                    Trade = trade,
                                    Language = settings.Language,
                                    TradeId = tradeId
                                };
                                var (editMenuText, editMenuKeyboard) =
                                    _uiManager.GetEditFieldMenu(trade, tradeId, settings);
                                var editMenuMessage = await bot.SendMessage(chatId, editMenuText,
                                    replyMarkup: editMenuKeyboard, cancellationToken: cancellationToken);
                                state.MessageId = editMenuMessage.MessageId;
                                await SaveUserStateAsync(userId, state);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    $"[DEBUG] edit callback: tradeId {tradeId} not found in PendingTrades");
                                await bot.SendMessage(chatId, "⏰ Сделка устарела.",
                                    replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                            }

                            break;


                        case "confirm":
                        {
                            // формат callback: confirm_trade_<guid>
                            if (parts.Length >= 3 && parts[0] == "confirm" && parts[1] == "trade")
                            {
                                tradeId = parts[2];

                                var pending = await GetPendingTradeByTradeIdAsync(userId, tradeId);
                                if (pending.HasValue)
                                {
                                    var (trade, originalMessageId, _) = pending.Value;

                                    // удаляем старый экран подтверждения
                                    try
                                    {
                                        await bot.DeleteMessage(chatId, originalMessageId, cancellationToken);
                                    }
                                    catch
                                    {
                                        /* не критично */
                                    }

                                    // сохраняем сделку (БД → Notion → уведомления)
                                    await SaveTradeAsync(trade, chatId, userId, bot, settings, cancellationToken);

                                    // удаляем из PendingTrades
                                    await DeletePendingTradeByTradeIdAsync(userId, tradeId);
                                }
                                else
                                {
                                    await bot.SendMessage(chatId,
                                        _uiManager.GetText("trade_expired", settings.Language),
                                        replyMarkup: _uiManager.GetMainMenu(settings),
                                        cancellationToken: cancellationToken);
                                }
                            }

                            break;
                        }

                        case "skip":
                            if (parts.Length >= 5 && parts[1] == "trade" &&
                                int.TryParse(parts[^1], out int CurrentStep))
                            {
                                tradeId = parts[2];
                                var pendingTrade = await GetPendingTradeByTradeIdAsync(userId, tradeId);
                                if (pendingTrade.HasValue)
                                {
                                    state = new UserState
                                    {
                                        Action = $"edit_trade_{tradeId}",
                                        Step = CurrentStep,
                                        Trade = pendingTrade.Value.Trade,
                                        Language = settings.Language,
                                        TradeId = tradeId
                                    };
                                }

                                if (state?.Trade == null || state.TradeId != tradeId)
                                {
                                    _logger.LogWarning(
                                        $"Invalid state or TradeId mismatch for UserId={userId}, TradeId={tradeId}");
                                    await bot.SendMessage(chatId, "⏰ Сделка устарела.",
                                        replyMarkup: _uiManager.GetMainMenu(settings),
                                        cancellationToken: cancellationToken);
                                    await DeleteUserStateAsync(userId);
                                    break;
                                }

                                state.Step++;
                                if (state.Step <= 9)
                                {
                                    var (nextText, nextKb) =
                                        _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);
                                    var nextMsg = await bot.SendMessage(chatId, nextText, replyMarkup: nextKb,
                                        cancellationToken: cancellationToken);
                                    state.MessageId = nextMsg.MessageId;
                                    await SaveUserStateAsync(userId, state);
                                }
                                else
                                {
                                    var trade = state.Trade;
                                    await DeleteUserStateAsync(userId);
                                    var (confTxt, confKb) =
                                        _uiManager.GetTradeConfirmationScreen(trade, tradeId, settings);
                                    var confMsg = await bot.SendMessage(chatId, confTxt, replyMarkup: confKb,
                                        cancellationToken: cancellationToken);
                                    await SavePendingTradeAsync(userId, tradeId, confMsg.MessageId, trade);
                                    await UpdateRecentSettingsAsync(userId, trade, settings);
                                }
                            }

                            break;

                        case "editfield":
                            if (state.Trade != null && tradeId != null && parts.Length >= 3)
                            {
                                string fieldKey = parts[1];
                                int newStep = fieldKey switch
                                {
                                    "ticker" => 1,
                                    "direction" => 2,
                                    "pnl" => 3,
                                    "open" => 4,
                                    "close" => 5,
                                    "sl" => 6,
                                    "tp" => 7,
                                    "volume" => 8,
                                    "comment" => 9,
                                    _ => 1
                                };
                                state = new UserState
                                {
                                    Action = $"edit_trade_{tradeId}",
                                    Step = newStep,
                                    Trade = state.Trade,
                                    Language = settings.Language,
                                    TradeId = tradeId
                                };
                                var (editFieldText, editFieldKeyboard) =
                                    _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);
                                var editFieldMessage = await bot.SendMessage(chatId, editFieldText,
                                    replyMarkup: editFieldKeyboard, cancellationToken: cancellationToken);
                                state.MessageId = editFieldMessage.MessageId;
                                await SaveUserStateAsync(userId, state);
                            }

                            break;

                        case "delete":
                            if (string.IsNullOrEmpty(tradeId))
                            {
                                await bot.SendMessage(chatId, "⏰ Сделка устарела.",
                                    replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                                break;
                            }

                            var pendienteTradeDataDel = await GetPendingTradeByTradeIdAsync(userId, tradeId);
                            if (pendienteTradeDataDel.HasValue)
                            {
                                await DeletePendingTradeByTradeIdAsync(userId, tradeId);
                                try
                                {
                                    await bot.DeleteMessage(chatId, pendienteTradeDataDel.Value.MessageId,
                                        cancellationToken);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex,
                                        $"Failed to delete message {pendienteTradeDataDel.Value.MessageId} for UserId={userId}");
                                }

                                await bot.SendMessage(chatId, "✅ Сделка удалена.",
                                    replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                            }
                            else
                            {
                                await bot.SendMessage(chatId, "⏰ Сделка устарела.",
                                    replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                            }

                            break;

                        case "pending":
                            int page = 1;
                            if (parts.Length > 2 && parts[1] == "page")
                            {
                                int.TryParse(parts[2], out page);
                            }

                            var pendingTrades = await GetPendingTradesForUserAsync(userId, page);
                            int totalPending = await GetPendingTradesCountAsync(userId);
                            var (pendingText, pendingKeyboard) =
                                _uiManager.GetPendingTradesScreen(pendingTrades, page, totalPending, settings);
                            await bot.SendMessage(chatId, pendingText, replyMarkup: pendingKeyboard,
                                cancellationToken: cancellationToken);
                            break;

                        case "clearpending":
                            using (var connection = new SqliteConnection(_sqliteConnectionString))
                            {
                                await connection.OpenAsync();
                                var cmd = connection.CreateCommand();
                                cmd.CommandText = @"
                                    DELETE FROM PendingTrades
                                    WHERE BotId = $botId AND UserId = $userId";
                                cmd.Parameters.AddWithValue("$botId", _botId);
                                cmd.Parameters.AddWithValue("$userId", userId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            _logger.LogInformation($"🧹 Cleared all pending trades for UserId={userId}");
                            await bot.SendMessage(chatId, "✅ Все активные сделки очищены.",
                                replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                            break;

                        case "stats":
                            /*var (statsMenuText, statsMenuKeyboard) = _uiManager.GetStatsMenu(settings);
                            await bot.SendMessage(chatId, statsMenuText, replyMarkup: statsMenuKeyboard,
                            cancellationToken: cancellationToken);
                            break;*/
                            await SendStatisticsAsync(chatId, userId, bot, settings, cancellationToken);
                            break;

                        case "statsperiod":
                            if (parts.Length > 1)
                            {
                                string periodKey = parts[1];
                                DateTime now = DateTime.Now;
                                List<Trade> tradesForPeriod = periodKey switch
                                {
                                    "week" => await _repo.GetTradesInDateRangeAsync(userId, now.AddDays(-7), now),
                                    "month" => await _repo.GetTradesInDateRangeAsync(userId, now.AddDays(-30), now),
                                    _ => await _repo.GetTradesAsync(userId)
                                };
                                var (statsText, statsKeyboard) =
                                    _uiManager.GetStatsResult(tradesForPeriod, periodKey, settings);
                                await bot.SendMessage(chatId, statsText, replyMarkup: statsKeyboard,
                                    cancellationToken: cancellationToken);
                            }

                            break;

                        case "history":
                            await SendHistoryAsync(chatId, userId, bot, settings, cancellationToken, 1);
                            break;

                        case "advstats":
                            var (advStatsText, advStatsKeyboard) = _uiManager.GetAdvancedStatsMenu(settings);
                            await bot.SendMessage(chatId, advStatsText, replyMarkup: advStatsKeyboard,
                                cancellationToken: cancellationToken);
                            break;

                        case "advstatsperiod":
                            if (parts.Length > 1)
                            {
                                string advPeriod = parts[1];
                                await GenerateEquityCurveAsync(chatId, userId, bot, cancellationToken, advPeriod,
                                    settings, msgId);
                            }

                            break;

                        case "historyfilter":
                            if (parts.Length >= 3)
                            {
                                string filterType = parts[1];
                                string filterValue = parts[2];
                                string period = "all";
                                string filterParam = filterType == "pnl" && parts.Length >= 4
                                    ? $"{filterValue}:{parts[3]}"
                                    : $"{filterType}:{filterValue}";
                                var filteredTrades = await GetFilteredTradesAsync(userId, period, filterParam);
                                var (historyText, historyKeyboard) =
                                    _uiManager.GetHistoryScreen(filteredTrades, 1, period, filterParam, settings);
                                await bot.SendMessage(chatId, historyText, replyMarkup: historyKeyboard,
                                    cancellationToken: cancellationToken);
                            }

                            break;

                        case "history_page":
                            if (parts.Length >= 7)
                            {
                                int pageNumber = 1;
                                int.TryParse(parts[2], out pageNumber);
                                string period = parts[4];
                                string filterParam = parts[6];
                                var tradesPage = await GetFilteredTradesAsync(userId, period, filterParam);
                                var (historyPageText, historyPageKeyboard) = _uiManager.GetHistoryScreen(tradesPage,
                                    pageNumber, period, filterParam, settings);
                                await bot.SendMessage(chatId, historyPageText, replyMarkup: historyPageKeyboard,
                                    cancellationToken: cancellationToken);
                            }

                            break;

                        case "historydetail":
                            if (parts.Length > 1 && int.TryParse(parts[1], out int histId))
                            {
                                Trade trade = await _repo.GetTradeByIdAsync(userId, histId);
                                if (trade != null)
                                {
                                    var (detailText, detailKeyboard) = _uiManager.GetTradeDetailScreen(trade, settings);
                                    await bot.SendMessage(chatId, detailText, replyMarkup: detailKeyboard,
                                        cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    await bot.SendMessage(chatId, "⏰ Сделка устарела.",
                                        replyMarkup: _uiManager.GetMainMenu(settings),
                                        cancellationToken: cancellationToken);
                                }
                            }

                            break;

                        case "export":
                            var allTradesExport = await _repo.GetTradesAsync(userId);
                            var csvContent = GenerateCsv(allTradesExport);
                            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent)))
                            {
                                await bot.SendDocument(chatId, InputFile.FromStream(stream, "trades.csv"),
                                    caption: "💾 Экспорт успешен!", replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                            }

                            break;

                        case "settings":
                            var (settingsText, settingsKeyboard) = _uiManager.GetSettingsMenu(settings);
                            var settingsMsg = await bot.SendMessage(chatId, settingsText, replyMarkup: settingsKeyboard,
                                cancellationToken: cancellationToken);
                            state = new UserState
                            {
                                Action = "settings_menu", Step = 1, Language = settings.Language,
                                MessageId = settingsMsg.MessageId
                            };
                            await SaveUserStateAsync(userId, state);
                            break;

                        case "settings_language":
                            settings.Language = settings.Language == "ru" ? "en" : "ru";
                            await SaveUserSettingsAsync(userId, settings);
                            _logger.LogInformation($"🌐 Language switched to {settings.Language} for UserId={userId}");
                            var (langText, langKeyboard) = _uiManager.GetSettingsMenu(settings);
                            await bot.SendMessage(chatId, langText, replyMarkup: langKeyboard,
                                cancellationToken: cancellationToken);
                            break;

                        case "settings_notifications":
                            settings.NotificationsEnabled = !settings.NotificationsEnabled;
                            await SaveUserSettingsAsync(userId, settings);
                            _logger.LogInformation(
                                $"🔔 Notifications {(settings.NotificationsEnabled ? "enabled" : "disabled")} for UserId={userId}");
                            var (notifText, notifKeyboard) = _uiManager.GetSettingsMenu(settings);
                            await bot.SendMessage(chatId, notifText, replyMarkup: notifKeyboard,
                                cancellationToken: cancellationToken);
                            break;

                        case "settings_tickers":
                            var (tickersText, tickersKeyboard) = _uiManager.GetFavoriteTickersMenu(settings);
                            await bot.SendMessage(chatId, tickersText, replyMarkup: tickersKeyboard,
                                cancellationToken: cancellationToken);
                            break;

                        case "add_favorite_ticker":
                            state.Action = "input_favorite_ticker";
                            var (promptText, promptKeyboard) = _uiManager.GetInputPrompt("ticker", settings, "");
                            var promptMsg1 = await bot.SendMessage(chatId, promptText, replyMarkup: promptKeyboard,
                                cancellationToken: cancellationToken);
                            state.MessageId = promptMsg1.MessageId;
                            await SaveUserStateAsync(userId, state);
                            break;

                        case "remove_favorite_ticker":
                            var (removeText, removeKeyboard) = _uiManager.GetRemoveFavoriteTickerMenu(settings);
                            await bot.SendMessage(chatId, removeText, replyMarkup: removeKeyboard,
                                cancellationToken: cancellationToken);
                            break;

                        case "remove_ticker":
                            if (parts.Length > 1)
                            {
                                string ticker = parts[1];
                                if (settings.FavoriteTickers.Remove(ticker))
                                {
                                    await SaveUserSettingsAsync(userId, settings);
                                    _logger.LogInformation(
                                        $"⭐ Removed ticker {ticker} from favorites for UserId={userId}");
                                    await bot.SendMessage(chatId, $"✅ Тикер {ticker} удалён из избранного.",
                                        replyMarkup: _uiManager.GetMainMenu(settings),
                                        cancellationToken: cancellationToken);
                                }
                            }

                            break;

                        case "resetsettings":
                            string currentLang = settings.Language;
                            var newSettings = new UserSettings { Language = currentLang };
                            await SaveUserSettingsAsync(userId, newSettings);
                            settings = newSettings;
                            state.Language = settings.Language;
                            var (resetMenuText, resetMenuKeyboard) = _uiManager.GetSettingsMenu(settings);
                            await bot.SendMessage(chatId, "🔄 Настройки сброшены.", replyMarkup: resetMenuKeyboard,
                                cancellationToken: cancellationToken);
                            break;

                        case "help":
                            var (helpText, helpKeyboard) = _uiManager.GetHelpMenu(settings);
                            await bot.SendMessage(chatId, helpText, replyMarkup: helpKeyboard,
                                cancellationToken: cancellationToken);
                            break;

                        case "support":
                            await bot.SendMessage(chatId, "📧 По всем вопросам: support@example.com",
                                replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                            break;

                        case "whatsnew":
                            await bot.SendMessage(chatId,
                                "📰 Последние обновления:\n - Добавлены смайлики\n - Кнопка 'Всё верно'",
                                replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                            break;

                        case "main":
                            await DeleteUserStateAsync(userId);
                            await SendMainMenuAsync(chatId, userId, bot, cancellationToken);
                            break;

                        case "reset":
                            await DeleteUserStateAsync(userId);
                            var allTrades2 = await _repo.GetTradesAsync(userId);
                            int totalTrades2 = allTrades2.Count;
                            decimal totalPnL2 = 0;
                            if (allTrades2.Count > 0) totalPnL2 = allTrades2.Sum(t => t.PnL);
                            int profitableCount2 = allTrades2.Count(t => t.PnL > 0);
                            int winRate2 = totalTrades2 > 0 ? (int)((double)profitableCount2 / totalTrades2 * 100) : 0;
                            int tradesToday2 =
                                (await _repo.GetTradesInDateRangeAsync(userId, DateTime.Today, DateTime.Now)).Count;
                            string mainText2 = _uiManager.GetText("main_menu", settings.Language, tradesToday2,
                                totalPnL2.ToString("F2"), winRate2);
                            await bot.SendMessage(chatId, mainText2,
                                replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                            break;

                        case "more":
                        {
                            if (parts.Length > 1 && parts[1] == "tickers" && tradeId != null)
                            {
                                var allTickers = settings.FavoriteTickers.Concat(settings.RecentTickers)
                                    .Concat(_uiManager.PopularTickers).Distinct().ToList();
                                if (allTickers.Count <= 5) break;
                                var extraTickers = allTickers.Skip(5).Take(20).ToList();
                                var buttons = new List<InlineKeyboardButton[]>();
                                foreach (var t in extraTickers)
                                {
                                    buttons.Add(new[]
                                    {
                                        InlineKeyboardButton.WithCallbackData(t, $"set_ticker_{t}_trade_{tradeId}")
                                    });
                                }

                                buttons.Add(new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("✏️ Другое", $"input_ticker_trade_{tradeId}")
                                });
                                var last = await _repo.GetLastTradeAsync(userId);
                                if (last != null && !string.IsNullOrEmpty(last.Ticker))
                                {
                                    buttons.Add(new[]
                                    {
                                        InlineKeyboardButton.WithCallbackData("🔄 Последний тикер",
                                            $"set_ticker_{last.Ticker}_trade_{tradeId}")
                                    });
                                }

                                buttons.Add(new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("⬅️ Назад",
                                        $"back_trade_{tradeId}_step_{state.Step}")
                                });
                                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("❌ Отмена", "cancel") });
                                try
                                {
                                    await bot.EditMessageReplyMarkup(chatId, state.MessageId,
                                        new InlineKeyboardMarkup(buttons), cancellationToken: cancellationToken);
                                }
                                catch
                                {
                                    string step1Text = _uiManager.GetText("step_1", settings.Language);
                                    await bot.SendMessage(chatId, step1Text,
                                        replyMarkup: new InlineKeyboardMarkup(buttons),
                                        cancellationToken: cancellationToken);
                                }
                            }

                            break;
                        }

                        case "input":
                        {
                            _logger.LogInformation(
                                $"Processing input callback for UserId={userId}, Data={callback.Data}");
                            // valid: input_<field> (2 parts)
                            // valid: input_<field>_trade_<id> (4 parts)
                            if (parts.Length < 2)
                                break;

                            string field = parts[1]; // ticker|pnl|open|close|sl|tp|volume|comment
                            if (!new[] { "ticker", "pnl", "open", "close", "sl", "tp", "volume", "comment" }
                                    .Contains(field))
                                break;

                            // берем tradeId из callback, если есть; иначе из state
                            if (parts.Length >= 4 && parts[2] == "trade")
                                tradeId = parts[3];
                            else
                                tradeId = state?.TradeId;

                            // если трейд еще не создан, создаем новый
                            if (string.IsNullOrWhiteSpace(tradeId))
                            {
                                tradeId = Guid.NewGuid().ToString();
                                state.TradeId = tradeId;
                                state.Trade ??= new Trade { UserId = userId, Date = DateTime.UtcNow };
                            }

                            state.Action = $"input_{field}";
                            var (promptTxt, promptKb) = _uiManager.GetInputPrompt(field, settings, tradeId);
                            var promptMsg = await bot.SendMessage(chatId, promptTxt, replyMarkup: promptKb,
                                cancellationToken: cancellationToken);
                            state.MessageId = promptMsg.MessageId;
                            await SaveUserStateAsync(userId, state);
                            break;
                        }

                        case "back":
                            // Проверка актуальности состояния и TradeId
                            if (state?.Trade == null || state.TradeId != tradeId)
                            {
                                _logger.LogWarning(
                                    $"Invalid state or TradeId mismatch for UserId={userId}, TradeId={tradeId}");
                                await bot.SendMessage(chatId,
                                    _uiManager.GetText("trade_expired", settings.Language),
                                    replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                                await DeleteUserStateAsync(userId);
                                break;
                            }

                            state.Step--;
                            if (state.Step < 1)
                            {
                                await DeleteUserStateAsync(userId);
                                await bot.SendMessage(chatId,
                                    _uiManager.GetText("main_menu", settings.Language),
                                    replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                            }
                            else
                            {
                                // Гарантируем, что TradeId не потерян
                                tradeId = state.TradeId;
                                var (prevText, prevKeyboard) =
                                    _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);
                                var prevMsg = await bot.SendMessage(chatId, prevText,
                                    replyMarkup: prevKeyboard, cancellationToken: cancellationToken);
                                state.MessageId = prevMsg.MessageId;
                                state.TradeId = tradeId; // на всякий случай сохраняем TradeId
                                await SaveUserStateAsync(userId, state);
                            }

                            break;


                        case "set":
                            if (state?.Trade == null || state.TradeId != tradeId)
                            {
                                _logger.LogWarning(
                                    $"Invalid state or TradeId mismatch for UserId={userId}, TradeId={tradeId} in set callback");
                                await bot.SendMessage(chatId,
                                    _uiManager.GetText("trade_expired", settings.Language),
                                    replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                                await DeleteUserStateAsync(userId);
                                break;
                            }

                            if (parts.Length > 1 && parts[1] == "ticker" && tradeId != null)
                            {
                                state.Trade.Ticker = parts[2];
                                state.Step++;
                                await UpdateRecentSettingsAsync(userId, state.Trade, settings);
                                var (nextTextTicker, nextKeyboardTicker) =
                                    _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);
                                var sentMessageTicker = await bot.SendMessage(chatId, nextTextTicker,
                                    replyMarkup: nextKeyboardTicker, cancellationToken: cancellationToken);
                                state.MessageId = sentMessageTicker.MessageId;
                                await SaveUserStateAsync(userId, state);
                            }
                            else if (parts.Length > 1 && parts[1] == "direction" && tradeId != null)
                            {
                                state.Trade.Direction = parts[2];
                                state.Step++;
                                await UpdateRecentSettingsAsync(userId, state.Trade, settings);
                                var (nextTextDir, nextKeyboardDir) =
                                    _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);
                                var sentMessageDir = await bot.SendMessage(chatId, nextTextDir,
                                    replyMarkup: nextKeyboardDir, cancellationToken: cancellationToken);
                                state.MessageId = sentMessageDir.MessageId;
                                await SaveUserStateAsync(userId, state);
                            }
                            else if (parts.Length > 1 && parts[1] == "pnl" && tradeId != null)
                            {
                                decimal adjustment1 = decimal.TryParse(parts[2].Replace(",", "."), NumberStyles.Any,
                                    CultureInfo.InvariantCulture, out decimal value)
                                    ? value
                                    : 0;
                                state.Trade.PnL += adjustment1;
                                var (pnlText, pnlKeyboard) =
                                    _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);
                                var pnlMessage = await bot.SendMessage(chatId, pnlText, replyMarkup: pnlKeyboard,
                                    cancellationToken: cancellationToken);
                                state.MessageId = pnlMessage.MessageId;
                                await SaveUserStateAsync(userId, state);
                            }

                            break;
                        case "allcorrect":
                            if (parts.Length < 2 || string.IsNullOrEmpty(tradeId))
                            {
                                _logger.LogWarning(
                                    $"Missing TradeId in allcorrect for UserId={userId}: {callback.Data}");
                                await bot.SendMessage(chatId, "⏰ Сделка устарела.",
                                    replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                                break;
                            }

                            var pendingTradeAllCorrect = await GetPendingTradeByTradeIdAsync(userId, tradeId);
                            if (pendingTradeAllCorrect.HasValue)
                            {
                                state = state ?? new UserState
                                {
                                    Trade = pendingTradeAllCorrect.Value.Trade, TradeId = tradeId,
                                    Language = settings.Language
                                };
                                state.Step = state.Step > 0 ? state.Step + 1 : 9; // Переход к подтверждению
                                if (state.Step > 9)
                                {
                                    var trade = state.Trade;
                                    await DeleteUserStateAsync(userId);
                                    var (confirmText, confirmKb) =
                                        _uiManager.GetTradeConfirmationScreen(trade, tradeId, settings);
                                    var confirmMsg = await bot.SendMessage(chatId, confirmText, replyMarkup: confirmKb,
                                        cancellationToken: cancellationToken);
                                    await SavePendingTradeAsync(userId, tradeId, confirmMsg.MessageId, trade);
                                    await UpdateRecentSettingsAsync(userId, trade, settings);
                                }
                                else
                                {
                                    var (nextTxt, nextKb) =
                                        _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);
                                    var nextMsg = await bot.SendMessage(chatId, nextTxt, replyMarkup: nextKb,
                                        cancellationToken: cancellationToken);
                                    state.MessageId = nextMsg.MessageId;
                                    await SaveUserStateAsync(userId, state);
                                }
                            }
                            else
                            {
                                await bot.SendMessage(chatId, "⏰ Сделка устарела.",
                                    replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                            }

                            break;

                        case "adjust":
                            // Проверка состояния и TradeId
                            if (state?.Trade == null || state.TradeId != tradeId)
                            {
                                _logger.LogWarning(
                                    $"Invalid state or TradeId mismatch for UserId={userId}, TradeId={tradeId} in adjust callback");
                                await bot.SendMessage(chatId,
                                    _uiManager.GetText("trade_expired", settings.Language),
                                    replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                                await DeleteUserStateAsync(userId);
                                break;
                            }

                            if (parts.Length >= 3 && parts[1] == "pnl")
                            {
                                // Парсим изменение PnL с учетом точки/запятой
                                decimal adjValue;
                                if (!decimal.TryParse(parts[2].Replace(",", "."), NumberStyles.Any,
                                        CultureInfo.InvariantCulture, out adjValue))
                                {
                                    adjValue = 0;
                                }

                                state.Trade.PnL += adjValue;
                                var (adjustText, adjustKeyboard) =
                                    _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);
                                var adjustMsg = await bot.SendMessage(chatId, adjustText,
                                    replyMarkup: adjustKeyboard, cancellationToken: cancellationToken);
                                state.MessageId = adjustMsg.MessageId;
                                await SaveUserStateAsync(userId, state);
                            }

                            break;


                        case "retry":
                            await DeleteUserStateAsync(userId);
                            await bot.SendMessage(chatId, $"📈 Главное меню:",
                                replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unhandled exception in HandleUpdateAsync for UserId={userId}, ChatId={chatId}");
                if (chatId != 0)
                {
                    var settings = await GetUserSettingsAsync(userId);
                    await bot.SendMessage(chatId, "❌ Произошла ошибка.",
                        replyMarkup: _uiManager.GetErrorKeyboard(settings), cancellationToken: cancellationToken);
                }
            }
        }

        private async Task HandleTradeInputAsync(ITelegramBotClient bot, long chatId, long userId, UserState state,
            UserSettings settings, string text, int messageId, CancellationToken cancellationToken)
        {
            if (!state.Action.StartsWith("input_"))
            {
                await bot.SendMessage(chatId, "👇 Пожалуйста, используйте кнопки.",
                    replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                return;
            }

            if (state == null || state.Trade == null)
            {
                await bot.SendMessage(chatId, "❌ Ошибка: состояние утеряно.",
                    replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                return;
            }

            string field = state.Action.Substring("input_".Length);
            text = text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                state.ErrorCount++;
                var (retryText, retryKeyboard) = state.ErrorCount >= 3
                    ? (_uiManager.GetText("error_occurred", settings.Language), _uiManager.GetErrorKeyboard(settings))
                    : _uiManager.GetInputPrompt(field, settings, state.TradeId);
                var errorMessage = await bot.SendMessage(chatId, retryText, replyMarkup: retryKeyboard,
                    cancellationToken: cancellationToken);
                state.MessageId = errorMessage.MessageId;
                await SaveUserStateAsync(userId, state);
                return;
            }

            try
            {
                switch (field.ToLowerInvariant())
                {
                    case "ticker":
                        state.Trade.Ticker = text.ToUpper();
                        // обновляем частый список тикеров
                        settings.RecentTickers.Remove(state.Trade.Ticker);
                        settings.RecentTickers.Insert(0, state.Trade.Ticker);
                        settings.RecentTickers = settings.RecentTickers.Take(5).ToList();
                        await SaveUserSettingsAsync(userId, settings);
                        break;
                    case "pnl":
                        decimal parsedPnL = TryParseDecimal(text);
                        state.Trade.PnL = parsedPnL;
                        break;
                    case "open":
                        state.Trade.OpenPrice = TryParseNullableDecimal(text);
                        break;
                    case "close":
                        state.Trade.Entry = TryParseNullableDecimal(text);
                        break;
                    case "sl":
                        state.Trade.SL = TryParseNullableDecimal(text);
                        break;
                    case "tp":
                        state.Trade.TP = TryParseNullableDecimal(text);
                        break;
                    case "volume":
                        state.Trade.Volume = TryParseNullableDecimal(text);
                        break;
                    case "comment":
                        state.Trade.Comment = text;
                        break;
                }

                // переходим к следующему шагу
                state.Step++;
                _logger.LogInformation($"Advanced to step {state.Step} for TradeId={state.TradeId}");
                if (state.Step <= 9)
                {
                    var (nextText, nextKeyboard) =
                        _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, state.TradeId);
                    var nextMessage = await bot.SendMessage(chatId, nextText, replyMarkup: nextKeyboard,
                        cancellationToken: cancellationToken);
                    state.MessageId = nextMessage.MessageId;
                    await SaveUserStateAsync(userId, state);
                }
                else
                {
                    // все шаги пройдены: показываем подтверждение сделки
                    var trade = state.Trade;
                    await DeleteUserStateAsync(userId);
                    var (confText, confKeyboard) =
                        _uiManager.GetTradeConfirmationScreen(trade, state.TradeId, settings);
                    var confMessage = await bot.SendMessage(chatId, confText, replyMarkup: confKeyboard,
                        cancellationToken: cancellationToken);
                    await SavePendingTradeAsync(userId, state.TradeId, confMessage.MessageId, trade);
                    await UpdateRecentSettingsAsync(userId, trade, settings);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in HandleTradeInputAsync for UserId={userId}, Field={field}");
                await bot.SendMessage(chatId, "❌ Ошибка при обработке ввода.",
                    replyMarkup: _uiManager.GetErrorKeyboard(settings), cancellationToken: cancellationToken);
            }
        }

        private async Task HandleSettingsInputAsync(ITelegramBotClient bot, long chatId, long userId, UserState state,
            UserSettings settings, string text, CancellationToken cancellationToken)
        {
            string action = state.Action;
            text = text.Trim();
            if (action == "input_favorite_ticker")
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    settings.FavoriteTickers.Add(text.ToUpper());
                    await SaveUserSettingsAsync(userId, settings);
                    await bot.SendMessage(chatId, $"✅ Тикер {text.ToUpper()} добавлен в избранное!",
                        replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                    await DeleteUserStateAsync(userId);
                }
                else
                {
                    state.ErrorCount++;
                    var (retryText, retryKeyboard) = state.ErrorCount >= 3
                        ? ("⚠️ Слишком много ошибок. Выберите действие:", new InlineKeyboardMarkup(new[]
                        {
                            new[] { InlineKeyboardButton.WithCallbackData("🔄 Начать заново", "reset") },
                            new[] { InlineKeyboardButton.WithCallbackData("❌ Отмена", "cancel") }
                        }))
                        : _uiManager.GetInputPrompt("ticker", settings, "");
                    var errorMessage = await bot.SendMessage(chatId, retryText, replyMarkup: retryKeyboard,
                        cancellationToken: cancellationToken);
                    state.MessageId = errorMessage.MessageId;
                    await SaveUserStateAsync(userId, state);
                }
            }
            // другие настройки (например, язык или уведомления) обрабатываются напрямую в callback
        }

        private async Task UpdateRecentSettingsAsync(long userId, Trade trade, UserSettings settings)
        {
            if (!string.IsNullOrEmpty(trade.Ticker))
            {
                settings.RecentTickers.Remove(trade.Ticker);
                settings.RecentTickers.Insert(0, trade.Ticker);
                settings.RecentTickers = settings.RecentTickers.Take(5).ToList();
            }

            if (!string.IsNullOrEmpty(trade.Direction))
            {
                settings.RecentDirections.Remove(trade.Direction);
                settings.RecentDirections.Insert(0, trade.Direction);
                settings.RecentDirections = settings.RecentDirections.Take(5).ToList();
            }

            if (!string.IsNullOrEmpty(trade.Comment))
            {
                settings.RecentComments.Remove(trade.Comment);
                settings.RecentComments.Insert(0, trade.Comment);
                settings.RecentComments = settings.RecentComments.Take(5).ToList();
            }

            await SaveUserSettingsAsync(userId, settings);
        }

        private async Task SaveTradeAsync(
            Trade trade,
            long chatId,
            long userId,
            ITelegramBotClient bot,
            UserSettings settings,
            CancellationToken ct)
        {
            _logger.LogInformation($"💾 Saving trade for UserId={userId}: {trade.Ticker}, PnL={trade.PnL}");
            await _repo.AddTradeAsync(trade);
            await UpdateRecentSettingsAsync(userId, trade, settings);

            var baseText = _uiManager.GetText("trade_saved", settings.Language, trade.Ticker, trade.PnL);
            var mainMenu = _uiManager.GetMainMenu(settings);
            var sentMsg = await bot.SendMessage(chatId, baseText, replyMarkup: mainMenu, cancellationToken: ct);

            try
            {
                string pageId = await _notionService.CreatePageForTradeAsync(trade);
                if (!string.IsNullOrEmpty(pageId))
                {
                    trade.NotionPageId = pageId;
                    await _repo.UpdateTradeAsync(trade);
                    await bot.EditMessageText(chatId, sentMsg.MessageId,
                        baseText + "\n\n📝 Сделка отправлена в Notion!", replyMarkup: mainMenu, cancellationToken: ct);
                }
                else
                {
                    await bot.EditMessageText(chatId, sentMsg.MessageId,
                        baseText + "\n\n⚠️ Не удалось отправить в Notion.", replyMarkup: mainMenu,
                        cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Ошибка Notion для UserId={userId}");
                await bot.EditMessageText(chatId, sentMsg.MessageId,
                    baseText + "\n\n💾 Сделка сохранена локально.", replyMarkup: mainMenu, cancellationToken: ct);
            }

            if (settings.NotificationsEnabled)
            {
                int streak = await CalculateStreakAsync(userId);
                if (streak >= 3)
                {
                    string streakMsg = trade.PnL > 0
                        ? _uiManager.GetText("win_streak", settings.Language, streak)
                        : _uiManager.GetText("loss_streak", settings.Language, streak);
                    await bot.SendMessage(chatId, streakMsg, replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: ct);
                }
            }
        }

        private async Task<int> CalculateStreakAsync(long userId)
        {
            var trades = await _repo.GetTradesAsync(userId);
            trades.Sort((a, b) => b.Date.CompareTo(a.Date));
            if (trades.Count == 0) return 0;
            bool isWinStreak = trades[0].PnL > 0;
            int streak = 0;
            foreach (var t in trades)
            {
                if ((isWinStreak && t.PnL <= 0) || (!isWinStreak && t.PnL > 0))
                    break;
                streak++;
            }

            return streak;
        }

        private async Task<List<Trade>> GetFilteredTradesAsync(long userId, string period, string filter)
        {
            DateTime now = DateTime.Now;
            List<Trade> trades = period switch
            {
                "week" => await _repo.GetTradesInDateRangeAsync(userId, now.AddDays(-7), now),
                "month" => await _repo.GetTradesInDateRangeAsync(userId, now.AddDays(-30), now),
                _ => await _repo.GetTradesAsync(userId)
            };
            if (!string.IsNullOrEmpty(filter) && filter != "none")
            {
                string[] filterParts = filter.Split(':');
                if (filterParts.Length >= 2)
                {
                    string filterType = filterParts[0];
                    string filterValue = filterParts[1];
                    if (filterType == "ticker")
                    {
                        trades = trades.Where(t => t.Ticker.Equals(filterValue, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }
                    else if (filterType == "direction")
                    {
                        trades = trades.Where(t => t.Direction.Equals(filterValue, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }
                    else if (filterType == "pnl" && filterParts.Length == 3)
                    {
                        if (decimal.TryParse(filterParts[2].Replace(",", "."), NumberStyles.Any,
                                CultureInfo.InvariantCulture, out decimal value))
                        {
                            if (filterValue == "gt")
                                trades = trades.Where(t => t.PnL > value).ToList();
                            else if (filterValue == "lt")
                                trades = trades.Where(t => t.PnL < value).ToList();
                        }
                    }
                }
            }

            return trades;
        }

        private string GenerateCsv(List<Trade> trades)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Id,Ticker,Direction,PnL,OpenPrice,ClosePrice,SL,TP,Volume,Comment,Date");
            foreach (var trade in trades)
            {
                string safeComment = trade.Comment?.Replace(",", ";") ?? "";
                sb.AppendLine(
                    $"{trade.Id},{trade.Ticker},{trade.Direction},{trade.PnL},{trade.OpenPrice},{trade.Entry},{trade.SL},{trade.TP},{trade.Volume},{safeComment},{trade.Date:yyyy-MM-dd HH:mm:ss}");
            }

            return sb.ToString();
        }

        private static decimal TryParseDecimal(string text)
        {
            var match = Regex.Match(text, @"([+\-]?\d{1,10}(?:[.,]\d{1,4})?)\s*%?");
            if (match.Success)
            {
                string numStr = match.Groups[1].Value.Replace(",", ".");
                if (decimal.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }

            return 0;
        }

        private static decimal? TryParseNullableDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text == "-") return null;
            var match = Regex.Match(text, @"([+\-]?\d{1,10}(?:[.,]\d{1,4})?)\s*%?");
            if (match.Success)
            {
                string numStr = match.Groups[1].Value.Replace(",", ".");
                if (decimal.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                {
                    return val;
                }
            }

            return null;
        }

        /*private async Task GenerateEquityCurveAsync(long chatId, long userId, ITelegramBotClient bot,
            CancellationToken ct, string period, UserSettings settings, int triggerMessageId)
        {
            DateTime now = DateTime.UtcNow;
            List<Trade> trades = period switch
            {
                "week" => await _repo.GetTradesInDateRangeAsync(userId, now.AddDays(-7), now),
                "month" => await _repo.GetTradesInDateRangeAsync(userId, now.AddDays(-30), now),
                _ => await _repo.GetTradesAsync(userId)
            };
            if (trades.Count == 0)
            {
                await bot.SendMessage(chatId, "📉 Нет сделок для построения графика.",
                    replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: ct);
                return;
            }

            trades.Sort((a, b) => a.Date.CompareTo(b.Date));
            double cumulative = 0;
            var xs = new List<double>();
            var ys = new List<double>();
            for (int i = 0; i < trades.Count; i++)
            {
                cumulative += (double)trades[i].PnL;
                xs.Add(i);
                ys.Add(cumulative);
            }

            var plt = new ScottPlot.Plot();
            plt.Add.Scatter(xs, ys);
            plt.Title("📊 Кривая эквити");
            plt.XLabel("Сделки");
            plt.YLabel("PnL (%)");
            string tmpPng = Path.Combine(Path.GetTempPath(), $"equity_{userId}_{Guid.NewGuid():N}.png");
            plt.SavePng(tmpPng, 600, 400);
            await using var fs = new FileStream(tmpPng, FileMode.Open, FileAccess.Read);
            await bot.SendPhoto(chatId, InputFile.FromStream(fs, "equity.png"), caption: "📊 Ваша кривая эквити",
                replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: ct);
            fs.Close();
            File.Delete(tmpPng);
            try
            {
                await bot.DeleteMessage(chatId, triggerMessageId, ct);
            }
            catch
            {
                // ignore if already deleted
            }
        }*/

        private async Task GenerateEquityCurveAsync(long chatId, long userId, ITelegramBotClient bot,
            CancellationToken ct, string period, UserSettings settings, int triggerMessageId)
        {
            DateTime now = DateTime.UtcNow;
            List<Trade> trades = period switch
            {
                "week" => await _repo.GetTradesInDateRangeAsync(userId, now.AddDays(-7), now),
                "month" => await _repo.GetTradesInDateRangeAsync(userId, now.AddDays(-30), now),
                _ => await _repo.GetTradesAsync(userId)
            };
            if (trades.Count == 0)
            {
                await bot.SendMessage(chatId, "📉 Нет сделок для построения графика.",
                    replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: ct);
                return;
            }

            trades.Sort((a, b) => a.Date.CompareTo(b.Date));
            double cumulative = 0;
            var ys = new List<float>();
            for (int i = 0; i < trades.Count; i++)
            {
                cumulative += (double)trades[i].PnL;
                ys.Add((float)cumulative);
            }

            // Настройки размеров
            int width = 900, height = 600, margin = 60;
            string tmpPng = Path.Combine(Path.GetTempPath(), $"equity_{userId}_{Guid.NewGuid():N}.png");

            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);

            // Подготовим координаты
            float dx = (width - 2 * margin) / (float)Math.Max(ys.Count - 1, 1);
            float minY = ys.Min(), maxY = ys.Max();
            if (Math.Abs(maxY - minY) < 1e-6) maxY += 1; // чтобы не делить на 0
            float scale = (height - 2 * margin) / (maxY - minY);

            // Glow-подложка (толстая полупрозрачная неоновая линия)
            using (var glowPaint = new SKPaint
                   {
                       Style = SKPaintStyle.Stroke,
                       StrokeWidth = 18,
                       Color = new SKColor(0, 255, 255, 100), // Cyan, прозрачность
                       IsAntialias = true,
                       StrokeCap = SKStrokeCap.Round
                   })
            using (var path = new SKPath())
            {
                path.MoveTo(margin, height - margin - (ys[0] - minY) * scale);
                for (int i = 1; i < ys.Count; i++)
                    path.LineTo(margin + i * dx, height - margin - (ys[i] - minY) * scale);
                canvas.DrawPath(path, glowPaint);
            }

            // Основная неоновая линия
            using (var linePaint = new SKPaint
                   {
                       Style = SKPaintStyle.Stroke,
                       StrokeWidth = 7,
                       Color = SKColors.Cyan,
                       IsAntialias = true,
                       StrokeCap = SKStrokeCap.Round
                   })
            using (var path = new SKPath())
            {
                path.MoveTo(margin, height - margin - (ys[0] - minY) * scale);
                for (int i = 1; i < ys.Count; i++)
                    path.LineTo(margin + i * dx, height - margin - (ys[i] - minY) * scale);
                canvas.DrawPath(path, linePaint);
            }

            // Точки (магентовые)
            using (var dotPaint = new SKPaint
                   {
                       Style = SKPaintStyle.Fill,
                       Color = SKColors.Magenta,
                       IsAntialias = true
                   })
            {
                for (int i = 0; i < ys.Count; i++)
                {
                    float x = margin + i * dx;
                    float y = height - margin - (ys[i] - minY) * scale;
                    canvas.DrawCircle(x, y, 9, dotPaint);
                }
            }

            // Неоновая сетка
            using (var gridPaint = new SKPaint
                   {
                       Style = SKPaintStyle.Stroke,
                       Color = new SKColor(0, 255, 255, 60),
                       StrokeWidth = 2
                   })
            {
                // Горизонтальные линии
                for (int i = 0; i <= 5; i++)
                {
                    float y = margin + i * (height - 2 * margin) / 5;
                    canvas.DrawLine(margin, y, width - margin, y, gridPaint);
                }

                // Вертикальные линии
                for (int i = 0; i <= 5; i++)
                {
                    float x = margin + i * (width - 2 * margin) / 5;
                    canvas.DrawLine(x, margin, x, height - margin, gridPaint);
                }
            }

            // Заголовок
            using (var titlePaint = new SKPaint
                   {
                       Color = SKColors.Cyan,
                       TextSize = 40,
                       IsAntialias = true,
                       FakeBoldText = true
                   })
            {
                canvas.DrawText("💎 Crypto Equity Curve", margin, 48, titlePaint);
            }

            // Подписи осей
            using (var labelPaint = new SKPaint
                   {
                       Color = SKColors.Lime,
                       TextSize = 28,
                       IsAntialias = true
                   })
            {
                canvas.DrawText("Trades", width / 2 - 50, height - 15, labelPaint);
            }

            using (var labelPaint = new SKPaint
                   {
                       Color = SKColors.Magenta,
                       TextSize = 28,
                       IsAntialias = true
                   })
            {
                canvas.DrawText("PnL (%)", 5, height / 2, labelPaint);
            }

            // Сохраняем png
            using (var img = surface.Snapshot())
            using (var data = img.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = File.OpenWrite(tmpPng))
            {
                data.SaveTo(stream);
            }

            // Отправка пользователю
            await using var fs = new FileStream(tmpPng, FileMode.Open, FileAccess.Read);
            await bot.SendPhoto(chatId, InputFile.FromStream(fs, "equity.png"),
                caption: "💎 Ваша неоновая кривая эквити",
                replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: ct);

            fs.Close();
            File.Delete(tmpPng);

            try
            {
                await bot.DeleteMessage(chatId, triggerMessageId, ct);
            }
            catch
            {
                // ignore if already deleted
            }
        }
    }
}
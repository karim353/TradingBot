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
using System.Text;
using SkiaSharp;

namespace TradingBot.Services
{
    public class UpdateHandler
    {
        // Декодирование callback_data обратно в человекочитаемый вид
        private static string DecodeCallbackData(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return encoded;
            
            return encoded
                .Replace("_", " ")
                .Replace("PCT", "%")
                .Replace("NEWYORK", "NEW YORK")
                .Replace("REVR", "Reversal")
                .Replace("CONT", "Continuation")
                .Replace("AND", "&")
                .Replace("NUM", "#")
                .Replace("AT", "@")
                .Replace("USD", "$")
                .Replace("EUR", "€")
                .Replace("GBP", "£")
                .Replace("PLUS", "+")
                .Replace("MINUS", "-")
                .Replace("EQ", "=")
                .Replace("Q", "?");
        }

        private readonly ITradeStorage _tradeStorage;
        private readonly PnLService _pnlService;
        private readonly UIManager _uiManager;
        private readonly ILogger<UpdateHandler> _logger;
        private readonly IMemoryCache _cache;
        private readonly string _sqliteConnectionString;
        private readonly string _botId;

        private class UserState
        {
            public int Step { get; set; }
            public Trade? Trade { get; set; }           // nullable: создаём по мере ввода
            public string? Action { get; set; }         // nullable: может отсутствовать
            public int MessageId { get; set; }
            public string Language { get; set; } = "ru";
            public string? TradeId { get; set; }        // nullable: создаём по мере ввода
            public DateTime LastInputTime { get; set; } = DateTime.UtcNow;
            public int ErrorCount { get; set; } = 0;
        }

        private static readonly TimeSpan PendingTradeTimeout = TimeSpan.FromHours(24);
        private static readonly TimeSpan AutoReturnDelay = TimeSpan.FromMinutes(5);
        private const int MaxRequestsPerMinute = 20;

        public UpdateHandler(
            ITradeStorage tradeStorage,
            PnLService pnlService,
            UIManager uiManager,
            ILogger<UpdateHandler> logger,
            IMemoryCache cache,
            string sqliteConnectionString,
            string botId)
        {
            _tradeStorage = tradeStorage ?? throw new ArgumentNullException(nameof(tradeStorage));
            _pnlService = pnlService ?? throw new ArgumentNullException(nameof(pnlService));
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _botId = string.IsNullOrWhiteSpace(botId) ? "bot" : botId;

            if (string.IsNullOrWhiteSpace(sqliteConnectionString))
                throw new ArgumentNullException(nameof(sqliteConnectionString), "SQLite connection string cannot be null or empty.");

            try
            {
                _sqliteConnectionString = new SqliteConnectionStringBuilder(sqliteConnectionString).ConnectionString;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid SQLite connection string format: {sqliteConnectionString}",
                    nameof(sqliteConnectionString), ex);
            }

            _logger.LogInformation($"📈 UpdateHandler initialized (BotId={_botId}, ConnectionString={_sqliteConnectionString})");
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
            if (_cache.TryGetValue($"state_{userId}", out UserState? cachedState) && cachedState != null)
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

        private async Task<UserSettings> GetUserSettingsAsync(long userId)
        {
            if (_cache.TryGetValue($"settings_{userId}", out UserSettings? cachedSettings) && cachedSettings != null)
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
        
        private async Task SendUserProfileAsync(long chatId, long userId, ITelegramBotClient bot, UserSettings settings, CancellationToken ct)
        {
            var user = await bot.GetChat(chatId, ct);
            var trades = await _tradeStorage.GetTradesAsync(userId);
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
            await bot.SendMessage(chatId, profileText, replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: ct);
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
            _logger.LogInformation($"💰 Trade saved in PendingTrades (UserId={userId}, TradeId={tradeId}, Ticker={trade.Ticker}, PnL={trade.PnL})");
            await command.ExecuteNonQueryAsync();
        }
        
        

        private async Task UpdatePendingTradeAsync(long userId, string tradeId, Trade trade)
        {
            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE PendingTrades
                SET TradeJson = $tradeJson
                WHERE BotId = $botId AND UserId = $userId AND TradeId = $tradeId";
            command.Parameters.AddWithValue("$botId", _botId);
            command.Parameters.AddWithValue("$userId", userId);
            command.Parameters.AddWithValue("$tradeId", tradeId);
            command.Parameters.AddWithValue("$tradeJson", JsonSerializer.Serialize(trade));
            await command.ExecuteNonQueryAsync();
            _logger.LogInformation($"💾 Updated pending trade (UserId={userId}, TradeId={tradeId}, Ticker={trade.Ticker}, PnL={trade.PnL})");
        }

        private async Task<(Trade Trade, int MessageId, DateTime CreatedAt)?> GetPendingTradeByTradeIdAsync(long userId, string tradeId)
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
                        return (trade, msgId, createdAt);
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

        private async Task<List<(string TradeId, Trade Trade, int MessageId, DateTime CreatedAt)>> GetPendingTradesForUserAsync(long userId, int page, int pageSize = 5)
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
                        result.Add((tradeId, trade, messageId, createdAt));
                }
                catch { /* ignore deserialization errors */ }
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

        private async Task SendMainMenuAsync(long chatId, long userId, ITelegramBotClient bot, CancellationToken ct)
        {
            var settings = await GetUserSettingsAsync(userId);
            var allTrades = await _tradeStorage.GetTradesAsync(userId);
            int totalTrades = allTrades.Count;
            decimal totalPnL = allTrades.Any() ? allTrades.Sum(t => t.PnL) : 0;
            int profitableCount = allTrades.Count(t => t.PnL > 0);
            int winRate = totalTrades > 0 ? (int)((double)profitableCount / totalTrades * 100) : 0;
            int tradesToday = (await _tradeStorage.GetTradesInDateRangeAsync(userId, DateTime.Today, DateTime.Now)).Count;

            string mainText = _uiManager.GetText("main_menu", settings.Language, tradesToday, totalPnL.ToString("F2"), winRate);

            using var stream = new FileStream("banner.gif", FileMode.Open, FileAccess.Read);
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
            var trades = await _tradeStorage.GetTradesAsync(userId);
            if (!trades.Any())
            {
                await bot.SendMessage(chatId, _uiManager.GetText("no_trades", settings.Language),
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

            // ==== Рендер equity на SkiaSharp ====
            int width = 950, height = 540;
            float marginLeft = 70, marginRight = 40, marginTop = 40, marginBottom = 50;

            string tmpPng = Path.Combine(Path.GetTempPath(), $"equity_{userId}_{Guid.NewGuid():N}.png");

            var ys = new List<float>();
            trades.Sort((a, b) => a.Date.CompareTo(b.Date));
            for (int i = 0; i < trades.Count; i++)
                ys.Add((float)trades[i].PnL);

            int N = ys.Count;
            var xs = Enumerable.Range(0, N).Select(i => marginLeft + (width - marginLeft - marginRight) * i / Math.Max(N - 1, 1)).ToArray();

            float minY = ys.Min(), maxY = ys.Max();
            if (Math.Abs(maxY - minY) < 1e-3f) { minY -= 1; maxY += 1; }
            float gridStep = (float)Math.Pow(10, Math.Floor(Math.Log10((maxY - minY) / 5.0)));
            float gridMin = (float)Math.Floor(minY / gridStep) * gridStep;
            float gridMax = (float)Math.Ceiling(maxY / gridStep) * gridStep;

            using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColor.Parse("#101113"));

                using (var gridPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1,
                    Color = new SKColor(255, 255, 255, 45),
                    IsAntialias = true
                })
                {
                    int xDivs = Math.Min(N, 8);
                    for (int i = 0; i <= xDivs; i++)
                    {
                        float x = marginLeft + (width - marginLeft - marginRight) * i / xDivs;
                        canvas.DrawLine(x, marginTop, x, height - marginBottom, gridPaint);
                    }
                    for (float yVal = gridMin; yVal <= gridMax + 0.001f; yVal += gridStep)
                    {
                        float y = height - marginBottom - (yVal - minY) / (maxY - minY) * (height - marginTop - marginBottom);
                        canvas.DrawLine(marginLeft, y, width - marginRight, y, gridPaint);
                    }
                }

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

                using (var yLabelPaint = new SKPaint
                {
                    Color = SKColor.FromHsv(0, 0, 90),
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

                using (var dotPaint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = SKColors.Magenta,
                    IsAntialias = true
                })
                {
                    for (int i = 0; i < ys.Count; i++)
                    {
                        float x = marginLeft + i * (width - marginLeft - marginRight) / Math.Max(ys.Count - 1, 1);
                        float y = height - marginBottom - (ys[i] - minY) * (height - marginTop - marginBottom) / (maxY - minY);
                        canvas.DrawCircle(x, y, 6, dotPaint);
                    }
                }

                using (var img = surface.Snapshot())
                using (var data = img.Encode(SKEncodedImageFormat.Png, 100))
                using (var fs = File.OpenWrite(tmpPng))
                {
                    data.SaveTo(fs);
                }
            }

            await using var fileStream = new FileStream(tmpPng, FileMode.Open, FileAccess.Read);
            await bot.SendPhoto(chatId, InputFile.FromStream(fileStream, "equity.png"),
                caption: _uiManager.GetText("equity_curve", settings.Language),
                replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: ct);

            fileStream.Close();
            File.Delete(tmpPng);
        }

        private async Task SendHistoryAsync(long chatId, long userId, ITelegramBotClient bot, UserSettings settings, CancellationToken ct, int page = 1)
        {
            var trades = await _tradeStorage.GetTradesAsync(userId);
            var (historyText, historyKeyboard) = _uiManager.GetHistoryScreen(trades, page, "all", "none", settings);
            await bot.SendMessage(chatId, historyText, replyMarkup: historyKeyboard, cancellationToken: ct);
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

                    _logger.LogInformation($"📩 Message from UserId={userId}, ChatId={chatId}: {(string.IsNullOrEmpty(text) ? "[non-text]" : text)}");
                    var settings = await GetUserSettingsAsync(userId);
                    var state = await GetUserStateAsync(userId) ?? new UserState { Language = settings.Language };

                    // Обработка изображения (если пользователь отправил фото)
                    if ((message.Photo?.Any() == true) || (message.Document != null && (message.Document.MimeType?.StartsWith("image/") ?? false)))
                    {
                        _logger.LogInformation($"📸 Processing image from UserId={userId}");
                        try
                        {
                            var fileInfo = await bot.GetFile(message.Photo?.Last().FileId ?? message.Document!.FileId, cancellationToken);
                            if (string.IsNullOrEmpty(fileInfo.FilePath))
                            {
                                await bot.SendMessage(chatId, _uiManager.GetText("error_getting_image", settings.Language),
                                    replyMarkup: _uiManager.GetErrorKeyboard(settings),
                                    cancellationToken: cancellationToken);
                                return;
                            }

                            await using var stream = new MemoryStream();
                            await bot.DownloadFile(fileInfo.FilePath, stream, cancellationToken);
                            stream.Position = 0;

                            var data = _pnlService.ExtractFromImage(stream);
                            _logger.LogInformation($"📊 OCR result: Ticker={data.Ticker}, Direction={data.Direction}, PnL={data.PnLPercent}");

                            string tradeId = Guid.NewGuid().ToString();
                            var trade = new Trade
                            {
                                UserId = userId,
                                Date = data.TradeDate ?? DateTime.Now,
                                Ticker = data.Ticker ?? string.Empty,
                                Direction = data.Direction ?? string.Empty,
                                PnL = data.PnLPercent ?? 0m
                            };

                            var (confText, confKeyboard) = _uiManager.GetTradeConfirmationScreen(trade, tradeId, settings);
                            var confMsg = await bot.SendMessage(chatId, confText, replyMarkup: confKeyboard, cancellationToken: cancellationToken);
                            await SavePendingTradeAsync(userId, tradeId, confMsg.MessageId, trade);
                            await UpdateRecentSettingsAsync(userId, trade, settings);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error processing image for UserId={userId}");
                            await bot.SendMessage(chatId, _uiManager.GetText("error_processing_image", settings.Language),
                                replyMarkup: _uiManager.GetErrorKeyboard(settings),
                                cancellationToken: cancellationToken);
                        }
                        return;
                    }

                    // Лимит частоты запросов
                    string rlKey = $"rate_limit_{userId}";
                    if (_cache.TryGetValue(rlKey, out int req) && req >= MaxRequestsPerMinute)
                    {
                        await bot.SendMessage(chatId, _uiManager.GetText("rate_limit", settings.Language),
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
                        await DeleteUserStateAsync(userId);
                        // Подгружаем актуальные опции из хранилища (Notion или SQLite)
                        List<string> emotionOptions, sessionOptions, accountOptions,
                                    contextOptions, setupOptions, resultOptions,
                                    positionOptions, directionOptions;
                        try
                        {
                            emotionOptions   = await _tradeStorage.GetSelectOptionsAsync("Emotions", state.Trade);
                        }
                        catch (Exception ex) { _logger.LogWarning(ex, "Failed to get Emotions options"); emotionOptions = new List<string>(); }
                        try
                        {
                            sessionOptions   = await _tradeStorage.GetSelectOptionsAsync("Session", state.Trade);
                        }
                        catch (Exception ex) { _logger.LogWarning(ex, "Failed to get Session options"); sessionOptions = new List<string>(); }
                        try
                        {
                            accountOptions   = await _tradeStorage.GetSelectOptionsAsync("Account", state.Trade);
                        }
                        catch (Exception ex) { _logger.LogWarning(ex, "Failed to get Account options"); accountOptions = new List<string>(); }
                        try
                        {
                            contextOptions   = await _tradeStorage.GetSelectOptionsAsync("Context", state.Trade);
                        }
                        catch (Exception ex) { _logger.LogWarning(ex, "Failed to get Context options"); contextOptions = new List<string>(); }
                        try
                        {
                            setupOptions     = await _tradeStorage.GetSelectOptionsAsync("Setup", state.Trade);
                        }
                        catch (Exception ex) { _logger.LogWarning(ex, "Failed to get Setup options"); setupOptions = new List<string>(); }
                        try
                        {
                            resultOptions    = await _tradeStorage.GetSelectOptionsAsync("Result", state.Trade);
                        }
                        catch (Exception ex) { _logger.LogWarning(ex, "Failed to get Result options"); resultOptions = new List<string>(); }
                        try
                        {
                            positionOptions  = await _tradeStorage.GetSelectOptionsAsync("Position", state.Trade);
                        }
                        catch (Exception ex) { _logger.LogWarning(ex, "Failed to get Position options"); positionOptions = new List<string>(); }
                        try
                        {
                            directionOptions = await _tradeStorage.GetSelectOptionsAsync("Direction", state.Trade);
                        }
                        catch (Exception ex) { _logger.LogWarning(ex, "Failed to get Direction options"); directionOptions = new List<string>(); }

                        // Передаем пустой список для исторических стратегий (не используются в новой схеме)
                        _uiManager.SetSelectOptions(
                            new List<string>(),
                            emotionOptions,
                            sessionOptions,
                            accountOptions,
                            contextOptions,
                            setupOptions,
                            resultOptions,
                            positionOptions,
                            directionOptions
                        );
                        await SendMainMenuAsync(chatId, userId, bot, CancellationToken.None);
                        return;
                    }

                    if (text == "/start")
                    {
                        if (_cache.TryGetValue($"seen_tutorial_{userId}", out bool _))
                        {
                            await DeleteUserStateAsync(userId);

                            // Инициализируем опции селектов аналогично /menu
                            var emotOpts = await _tradeStorage.GetSelectOptionsAsync("Emotions", state.Trade);
                            var sessOpts = await _tradeStorage.GetSelectOptionsAsync("Session", state.Trade);
                            var accOpts  = await _tradeStorage.GetSelectOptionsAsync("Account", state.Trade);
                            var ctxOpts  = await _tradeStorage.GetSelectOptionsAsync("Context", state.Trade);
                            var setupOpts= await _tradeStorage.GetSelectOptionsAsync("Setup", state.Trade);
                            var resOpts  = await _tradeStorage.GetSelectOptionsAsync("Result", state.Trade);
                            var posOpts  = await _tradeStorage.GetSelectOptionsAsync("Position", state.Trade);
                            var dirOpts  = await _tradeStorage.GetSelectOptionsAsync("Direction", state.Trade);

                            _uiManager.SetSelectOptions(
                                new List<string>(), // strategies
                                emotOpts, sessOpts, accOpts, ctxOpts, setupOpts, resOpts, posOpts, dirOpts
                            );

                            var allTradesX = await _tradeStorage.GetTradesAsync(userId);
                            int totalTrades = allTradesX.Count;
                            decimal totalPnL = totalTrades > 0 ? allTradesX.Sum(t => t.PnL) : 0;
                            int profitableCount = allTradesX.Count(t => t.PnL > 0);
                            int winRate = totalTrades > 0 ? (int)((double)profitableCount / totalTrades * 100) : 0;
                            int tradesToday = (await _tradeStorage.GetTradesInDateRangeAsync(userId, DateTime.Today, DateTime.Now)).Count;

                            string mainText = _uiManager.GetText("main_menu", settings.Language, tradesToday, totalPnL.ToString("F2"), winRate);
                            await bot.SendMessage(chatId, mainText, replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: CancellationToken.None);
                            return;
                        }

                        // Начало обучения (tutorial)
                        await DeleteUserStateAsync(userId);
                        var st = new UserState { Language = settings.Language, Action = "onboarding", Step = 1 };
                        var (welcomeText, onboardingKeyboard) = _uiManager.GetOnboardingScreen(st.Step, st.Language);
                        var sentMessage = await bot.SendMessage(chatId, welcomeText, replyMarkup: onboardingKeyboard, cancellationToken: CancellationToken.None);
                        st.MessageId = sentMessage.MessageId;
                        await SaveUserStateAsync(userId, st);
                        _cache.Set($"seen_tutorial_{userId}", true, TimeSpan.FromDays(30));
                        return;
                    }

                    // Если пользователь находится в процессе ввода новой сделки или редактирования, обрабатываем текст как часть ввода сделки
                    if (state.Action?.StartsWith("new_trade") == true ||
                        state.Action?.StartsWith("edit_trade") == true ||
                        state.Action?.StartsWith("input_") == true)
                    {
                        await HandleTradeInputAsync(bot, chatId, userId, state, settings, text, message.MessageId, CancellationToken.None);
                        return;
                    }

                    if (state.Action?.StartsWith("settings_") == true && state.Action != "settings_menu")
                    {
                        await HandleSettingsInputAsync(bot, chatId, userId, state, settings, text, cancellationToken);
                        return;
                    }

                    await bot.SendMessage(chatId, _uiManager.GetText("please_use_buttons", settings.Language),
                        replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                    return;
                }
                else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
                {
                    var callback = update.CallbackQuery;
                    long cbChatId = callback.Message?.Chat.Id ?? 0;
                    long cbUserId = callback.From.Id;
                    string data = callback.Data ?? string.Empty;
                    _logger.LogInformation($"📲 Callback from UserId={cbUserId}: {data}");
                    await bot.AnswerCallbackQuery(callback.Id);

                    var state = await GetUserStateAsync(cbUserId) ?? new UserState { Language = (await GetUserSettingsAsync(cbUserId)).Language };
                    var settings = await GetUserSettingsAsync(cbUserId);

                    if (callback.Message != null)
                    {
                        bool suppressDelete = data.StartsWith("more_") || data.StartsWith("settings_") || data.StartsWith("noop");
                        if (!suppressDelete)
                        {
                            try { await bot.DeleteMessage(cbChatId, callback.Message.MessageId); } catch { }
                        }
                    }

                    string[] parts = data.Split('_', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0) return;

                    string action = parts[0].ToLowerInvariant();
                    string? tradeId = null;

                    // Универсальный способ извлечь tradeId из callback (если присутствует слово "trade")
                    int tradeIdx = Array.IndexOf(parts, "trade");
                    if (tradeIdx >= 0 && tradeIdx < parts.Length - 1)
                        tradeId = parts[tradeIdx + 1];

                    switch (action)
                    {
                        case "start":
                        case "start_trade":
                        {
                            var lastTrade = await _tradeStorage.GetLastTradeAsync(cbUserId);
                            tradeId = Guid.NewGuid().ToString();
                            state = new UserState
                            {
                                Action = "new_trade",
                                Step = 1,
                                Trade = new Trade { UserId = cbUserId, Date = DateTime.Now },
                                Language = settings.Language,
                                TradeId = tradeId
                            };
                            var (tradeText, tradeKeyboard) = _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId!, lastTrade);
                            var tradeMessage = await bot.SendMessage(cbChatId, tradeText, replyMarkup: tradeKeyboard);
                            state.MessageId = tradeMessage.MessageId;
                            await SaveUserStateAsync(cbUserId, state);
                            break;
                        }

                        case "edit":
                        {
                            // Переход в режим редактирования сохранённой сделки
                            string pendingTradeId = parts.Length > 1 ? parts[1] : string.Empty;
                            if (!string.IsNullOrEmpty(pendingTradeId))
                            {
                                var pending = await GetPendingTradeByTradeIdAsync(cbUserId, pendingTradeId);
                                if (pending.HasValue)
                                {
                                    state = new UserState
                                    {
                                        Action = $"edit_trade_{pendingTradeId}",
                                        Step = 0,
                                        Trade = pending.Value.Trade,
                                        Language = settings.Language,
                                        TradeId = pendingTradeId
                                    };
                                    var (editMenuText, editMenuKeyboard) = _uiManager.GetEditFieldMenu(state.Trade, pendingTradeId, settings);
                                    await bot.SendMessage(cbChatId, editMenuText, replyMarkup: editMenuKeyboard, cancellationToken: cancellationToken);
                                    await SaveUserStateAsync(cbUserId, state);
                                }
                                else
                                {
                                    await bot.SendMessage(cbChatId, _uiManager.GetText("trade_expired", settings.Language),
                                        replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                                }
                            }
                            break;
                        }

                        case "skip":
                        {
                            // ожидаем шаблон: skip_trade_<id>_<step>
                            if (parts.Length >= 5 && parts[1] == "trade" && int.TryParse(parts[^1], out int currentStep))
                            {
                                tradeId = parts[2];
                                var pendingTrade = await GetPendingTradeByTradeIdAsync(cbUserId, tradeId!);
                                if (pendingTrade.HasValue)
                                {
                                    state = new UserState
                                    {
                                        Action = $"edit_trade_{tradeId}",
                                        Step = currentStep,
                                        Trade = pendingTrade.Value.Trade,
                                        Language = settings.Language,
                                        TradeId = tradeId
                                    };
                                }
                                if (state.Trade == null || state.TradeId != tradeId)
                                {
                                    await bot.SendMessage(cbChatId, _uiManager.GetText("trade_expired", settings.Language),
                                        replyMarkup: _uiManager.GetMainMenu(settings));
                                    await DeleteUserStateAsync(cbUserId);
                                    break;
                                }

                                state.Step++;
                                await UpdatePendingTradeAsync(cbUserId, tradeId!, state.Trade!);
                                if (state.Step <= 14)
                                {
                                    var (nextText, nextKb) = _uiManager.GetTradeInputScreen(state.Trade!, state.Step, settings, tradeId!);
                                    var nextMsg = await bot.SendMessage(cbChatId, nextText, replyMarkup: nextKb);
                                    state.MessageId = nextMsg.MessageId;
                                    await SaveUserStateAsync(cbUserId, state);
                                }
                                else
                                {
                                    var trade = state.Trade!;
                                    await DeleteUserStateAsync(cbUserId);
                                    var (confTxt, confKb) = _uiManager.GetTradeConfirmationScreen(trade, tradeId!, settings);
                                    var confMsg = await bot.SendMessage(cbChatId, confTxt, replyMarkup: confKb);
                                    await SavePendingTradeAsync(cbUserId, tradeId!, confMsg.MessageId, trade);
                                    await UpdateRecentSettingsAsync(cbUserId, trade, settings);
                                }
                            }
                            break;
                        }

                        case "set":
                        {
                            if (state?.Trade == null || state.TradeId != tradeId || tradeId == null)
                            {
                                await bot.SendMessage(cbChatId, _uiManager.GetText("trade_expired", settings.Language),
                                      replyMarkup: _uiManager.GetMainMenu(settings));
                                await DeleteUserStateAsync(cbUserId);
                                break;
                            }

                            // ожидаем: set_<field>_<value>_trade_<id>
                            string field = parts.Length > 1 ? parts[1].ToLowerInvariant() : string.Empty;
                            string value = parts.Length > 2 ? parts[2] : string.Empty;

                            switch (field)
                            {
                                case "ticker":
                                    state.Trade.Ticker = value.ToUpperInvariant();
                                    settings.RecentTickers.Remove(state.Trade.Ticker);
                                    settings.RecentTickers.Insert(0, state.Trade.Ticker);
                                    settings.RecentTickers = settings.RecentTickers.Take(5).ToList();
                                    await SaveUserSettingsAsync(cbUserId, settings);
                                    state.Step++;
                                    break;

                                case "account":
                                    value = DecodeCallbackData(value);
                                    state.Trade.Account = value;
                                    state.Step++;
                                    break;

                                case "session":
                                    // декодируем безопасные значения в человекочитаемые
                                    value = DecodeCallbackData(value);
                                    state.Trade.Session = value;
                                    state.Step++;
                                    break;

                                case "position":
                                    value = DecodeCallbackData(value);
                                    state.Trade.Position = value;
                                    state.Step++;
                                    break;

                                case "direction":
                                    value = DecodeCallbackData(value);
                                    state.Trade.Direction = value;
                                    settings.RecentDirections.Remove(state.Trade.Direction);
                                    settings.RecentDirections.Insert(0, state.Trade.Direction);
                                    settings.RecentDirections = settings.RecentDirections.Take(5).ToList();
                                    await SaveUserSettingsAsync(cbUserId, settings);
                                    state.Step++;
                                    break;

                                case "context":
                                    value = DecodeCallbackData(value);
                                    state.Trade.Context = new List<string> { value };
                                    state.Step++;
                                    break;

                                case "setup":
                                    // декодируем callback_data обратно
                                    value = DecodeCallbackData(value);
                                    state.Trade.Setup = new List<string> { value };
                                    state.Step++;
                                    break;

                                case "result":
                                    value = DecodeCallbackData(value);
                                    state.Trade.Result = value;
                                    state.Step++;
                                    break;

                                case "emotions":
                                    value = DecodeCallbackData(value);
                                    state.Trade.Emotions = new List<string> { value };
                                    state.Step++;
                                    break;

                                case "risk":
                                {
                                    // 0_5 -> 0.5
                                    var decoded = value.Replace('_', '.');
                                    state.Trade.Risk = TryParseNullableDecimal(decoded);
                                    state.Step++;
                                    break;
                                }
                                case "rr":
                                {
                                    // 1_2 -> 2.0 (1:2), 1_3 -> 3.0 и т.д.
                                    decimal? rrVal = null;
                                    var partsRR = value.Split('_');
                                    if (partsRR.Length == 2 && decimal.TryParse(partsRR[1].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var denom))
                                    {
                                        rrVal = denom;
                                    }
                                    else
                                    {
                                        rrVal = TryParseNullableDecimal(value);
                                    }
                                    state.Trade.RR = rrVal;
                                    state.Step++;
                                    break;
                                }
                                case "profit":
                                case "pnl":
                                {
                                    decimal adj = TryParseDecimal(value);
                                    state.Trade.PnL = adj;
                                    state.Step++;
                                    break;
                                }

                                default:
                                    // неизвестное поле — игнорируем
                                    break;
                            }

                            // Обновляем pending trade
                            await UpdatePendingTradeAsync(cbUserId, state.TradeId!, state.Trade);

                            // Проверяем, не завершён ли ввод
                            if (state.Step > 14)
                            {
                                // Переходим к экрану подтверждения
                                var trade = state.Trade;
                                await DeleteUserStateAsync(cbUserId);
                                var (confText, confKeyboard) = _uiManager.GetTradeConfirmationScreen(trade, state.TradeId!, settings);
                                var confMessage = await bot.SendMessage(cbChatId, confText, replyMarkup: confKeyboard, cancellationToken: cancellationToken);
                                await SavePendingTradeAsync(cbUserId, state.TradeId!, confMessage.MessageId, trade);
                                await UpdateRecentSettingsAsync(cbUserId, trade, settings);
                            }
                            else
                            {
                                // Продолжаем ввод
                                var (nextText, nextKeyboard) = _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, state.TradeId!);
                                var nextMessage = await bot.SendMessage(cbChatId, nextText, replyMarkup: nextKeyboard);
                                state.MessageId = nextMessage.MessageId;
                                await SaveUserStateAsync(cbUserId, state);
                            }
                            break;
                        }

                        case "confirm":
                        {
                            // confirm_trade_<id>
                            if (parts.Length >= 3 && parts[1] == "trade")
                            {
                                tradeId = parts[2];
                                var pending = await GetPendingTradeByTradeIdAsync(cbUserId, tradeId);
                                if (pending.HasValue)
                                {
                                    var (trade, originalMsgId, _) = pending.Value;
                                    try { await bot.DeleteMessage(cbChatId, originalMsgId); } catch { }
                                    await SaveTradeAsync(trade, cbChatId, cbUserId, bot, settings, CancellationToken.None);
                                    await DeletePendingTradeByTradeIdAsync(cbUserId, tradeId);
                                }
                                else
                                {
                                    await bot.SendMessage(cbChatId, _uiManager.GetText("trade_expired", settings.Language),
                                                          replyMarkup: _uiManager.GetMainMenu(settings));
                                }
                            }
                            break;
                        }

                        case "save":
                        {
                            if (state?.Trade != null)
                            {
                                var trade = state.Trade;
                                try { if (state.MessageId != 0) await bot.DeleteMessage(cbChatId, state.MessageId); } catch { }
                                await DeleteUserStateAsync(cbUserId);
                                await SaveTradeAsync(trade, cbChatId, cbUserId, bot, settings, CancellationToken.None);
                            }
                            break;
                        }

                        case "editfield":
                        {
                            if (state.Trade != null && tradeId != null && parts.Length >= 3)
                            {
                                string fieldKey = parts[1];
                                int newStep = fieldKey switch
                                {
                                    "ticker"   => 1,
                                    "account"  => 2,
                                    "session"  => 3,
                                    "position" => 4,
                                    "direction"=> 5,
                                    "context"  => 6,
                                    "setup"    => 7,
                                    "risk"     => 8,
                                    "rr"       => 9,
                                    "result"   => 10,
                                    "profit"   => 11,
                                    "emotions" => 12,
                                    "entry"    => 13,
                                    "note"     => 14,
                                    _          => 1
                                };
                                state = new UserState
                                {
                                    Action = $"edit_trade_{tradeId}",
                                    Step = newStep,
                                    Trade = state.Trade,
                                    Language = settings.Language,
                                    TradeId = tradeId
                                };
                                var (editText, editKb) = _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);
                                var editMsg = await bot.SendMessage(cbChatId, editText, replyMarkup: editKb);
                                state.MessageId = editMsg.MessageId;
                                await SaveUserStateAsync(cbUserId, state);
                            }
                            break;
                        }

                        case "delete":
                        {
                            if (string.IsNullOrEmpty(tradeId))
                            {
                                await bot.SendMessage(cbChatId, _uiManager.GetText("trade_expired", settings.Language),
                                    replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                                break;
                            }

                            var pend = await GetPendingTradeByTradeIdAsync(cbUserId, tradeId);
                            if (pend.HasValue)
                            {
                                await DeletePendingTradeByTradeIdAsync(cbUserId, tradeId);
                                try { await bot.DeleteMessage(cbChatId, pend.Value.MessageId, cancellationToken); }
                                catch (Exception ex) { _logger.LogWarning(ex, $"Failed to delete message {pend.Value.MessageId} for UserId={cbUserId}"); }

                                await bot.SendMessage(cbChatId, _uiManager.GetText("trade_deleted", settings.Language),
                                    replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                            }
                            else
                            {
                                await bot.SendMessage(cbChatId, _uiManager.GetText("trade_expired", settings.Language),
                                    replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                            }
                            break;
                        }

                        case "pending":
                        {
                            int page = 1;
                            if (parts.Length > 2 && parts[1] == "page")
                                int.TryParse(parts[2], out page);

                            var pendingTrades = await GetPendingTradesForUserAsync(cbUserId, page);
                            int totalPending = await GetPendingTradesCountAsync(cbUserId);
                            var (pendingText, pendingKeyboard) = _uiManager.GetPendingTradesScreen(pendingTrades, page, totalPending, settings);
                            await bot.SendMessage(cbChatId, pendingText, replyMarkup: pendingKeyboard, cancellationToken: cancellationToken);
                            break;
                        }

                        case "clearpending":
                        {
                            using (var connection = new SqliteConnection(_sqliteConnectionString))
                            {
                                await connection.OpenAsync();
                                var cmd = connection.CreateCommand();
                                cmd.CommandText = @"
                                    DELETE FROM PendingTrades
                                    WHERE BotId = $botId AND UserId = $userId";
                                cmd.Parameters.AddWithValue("$botId", _botId);
                                cmd.Parameters.AddWithValue("$userId", cbUserId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            _logger.LogInformation($"🧹 Cleared all pending trades for UserId={cbUserId}");
                            await bot.SendMessage(cbChatId, _uiManager.GetText("all_pending_cleared", settings.Language),
                                replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                            break;
                        }

                        case "stats":
                        {
                            await SendStatisticsAsync(cbChatId, cbUserId, bot, settings, cancellationToken);
                            break;
                        }

                        case "statsperiod":
                        {
                            if (parts.Length > 1)
                            {
                                string periodKey = parts[1];
                                DateTime now = DateTime.Now;
                                List<Trade> tradesForPeriod = periodKey switch
                                {
                                    "week"  => await _tradeStorage.GetTradesInDateRangeAsync(cbUserId, now.AddDays(-7), now),
                                    "month" => await _tradeStorage.GetTradesInDateRangeAsync(cbUserId, now.AddDays(-30), now),
                                    _       => await _tradeStorage.GetTradesAsync(cbUserId)
                                };
                                var (statsText, statsKeyboard) = _uiManager.GetStatsResult(tradesForPeriod, periodKey, settings);
                                await bot.SendMessage(cbChatId, statsText, replyMarkup: statsKeyboard, cancellationToken: cancellationToken);
                            }
                            break;
                        }

                        case "history":
                        {
                            await SendHistoryAsync(cbChatId, cbUserId, bot, settings, cancellationToken, 1);
                            break;
                        }

                        case "advstats":
                        {
                            var (advStatsText, advStatsKeyboard) = _uiManager.GetAdvancedStatsMenu(settings);
                            await bot.SendMessage(cbChatId, advStatsText, replyMarkup: advStatsKeyboard, cancellationToken: cancellationToken);
                            break;
                        }

                        case "advstatsperiod":
                        {
                            if (parts.Length > 1)
                            {
                                string advPeriod = parts[1];
                                int triggerMsgId = callback.Message?.MessageId ?? 0;
                                await GenerateEquityCurveAsync(cbChatId, cbUserId, bot, cancellationToken, advPeriod, settings, triggerMsgId);
                            }
                            break;
                        }

                        case "historyfilter":
                        {
                            if (parts.Length >= 3)
                            {
                                string filterType = parts[1];
                                string filterValue = parts[2];
                                string period = "all";
                                string filterParam = filterType == "pnl" && parts.Length >= 4
                                    ? $"{filterValue}:{parts[3]}"
                                    : $"{filterType}:{filterValue}";
                                var filteredTrades = await GetFilteredTradesAsync(cbUserId, period, filterParam);
                                var (historyText, historyKeyboard) = _uiManager.GetHistoryScreen(filteredTrades, 1, period, filterParam, settings);
                                await bot.SendMessage(cbChatId, historyText, replyMarkup: historyKeyboard, cancellationToken: cancellationToken);
                            }
                            break;
                        }

                        case "history_page":
                        {
                            if (parts.Length >= 7)
                            {
                                int pageNumber = 1;
                                int.TryParse(parts[2], out pageNumber);
                                string period = parts[4];
                                string filterParam = parts[6];
                                var tradesPage = await GetFilteredTradesAsync(cbUserId, period, filterParam);
                                var (historyPageText, historyPageKeyboard) = _uiManager.GetHistoryScreen(tradesPage, pageNumber, period, filterParam, settings);
                                await bot.SendMessage(cbChatId, historyPageText, replyMarkup: historyPageKeyboard, cancellationToken: cancellationToken);
                            }
                            break;
                        }

                        case "historydetail":
                        {
                            if (parts.Length > 1 && int.TryParse(parts[1], out int histId))
                            {
                                var trade = await _tradeStorage.GetTradeByIdAsync(cbUserId, histId);
                                if (trade != null)
                                {
                                    var (detailText, detailKeyboard) = _uiManager.GetTradeDetailScreen(trade, settings);
                                    await bot.SendMessage(cbChatId, detailText, replyMarkup: detailKeyboard, cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    await bot.SendMessage(cbChatId, _uiManager.GetText("trade_expired", settings.Language),
                                        replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                                }
                            }
                            break;
                        }

                        case "export":
                        {
                            var allTradesExport = await _tradeStorage.GetTradesAsync(cbUserId);
                            var csvContent = GenerateCsv(allTradesExport);
                            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
                            await bot.SendDocument(cbChatId, InputFile.FromStream(stream, "trades.csv"),
                                caption: _uiManager.GetText("export_success", settings.Language), replyMarkup: _uiManager.GetMainMenu(settings),
                                cancellationToken: cancellationToken);
                            break;
                        }

                        case "settings":
                        {
                            var (settingsText, settingsKeyboard) = _uiManager.GetSettingsMenu(settings);
                            var settingsMsg = await bot.SendMessage(cbChatId, settingsText, replyMarkup: settingsKeyboard, cancellationToken: cancellationToken);
                            state = new UserState { Action = "settings_menu", Step = 1, Language = settings.Language, MessageId = settingsMsg.MessageId };
                            await SaveUserStateAsync(cbUserId, state);
                            break;
                        }

                        case "settings_language":
                        {
                            settings.Language = settings.Language == "ru" ? "en" : "ru";
                            await SaveUserSettingsAsync(cbUserId, settings);
                            if (callback.Message != null)
                            {
                                var (langText, langKeyboard) = _uiManager.GetSettingsMenu(settings);
                                await bot.EditMessageText(cbChatId, callback.Message.MessageId, langText, replyMarkup: langKeyboard, cancellationToken: cancellationToken);
                            }
                            break;
                        }

                        case "settings_notifications":
                        {
                            settings.NotificationsEnabled = !settings.NotificationsEnabled;
                            await SaveUserSettingsAsync(cbUserId, settings);
                            if (callback.Message != null)
                            {
                                var (notifText, notifKeyboard) = _uiManager.GetSettingsMenu(settings);
                                await bot.EditMessageText(cbChatId, callback.Message.MessageId, notifText, replyMarkup: notifKeyboard, cancellationToken: cancellationToken);
                            }
                            break;
                        }

                        case "settings_tickers":
                        {
                            var (tickersText, tickersKeyboard) = _uiManager.GetFavoriteTickersMenu(settings);
                            await bot.SendMessage(cbChatId, tickersText, replyMarkup: tickersKeyboard, cancellationToken: cancellationToken);
                            break;
                        }

                        case "add_favorite_ticker":
                        {
                            state.Action = "input_favorite_ticker";
                            var (promptText, promptKeyboard) = _uiManager.GetInputPrompt("ticker", settings, "");
                            var promptMsg1 = await bot.SendMessage(cbChatId, promptText, replyMarkup: promptKeyboard, cancellationToken: cancellationToken);
                            state.MessageId = promptMsg1.MessageId;
                            await SaveUserStateAsync(cbUserId, state);
                            break;
                        }

                        case "remove_favorite_ticker":
                        {
                            var (removeText, removeKeyboard) = _uiManager.GetRemoveFavoriteTickerMenu(settings);
                            await bot.SendMessage(cbChatId, removeText, replyMarkup: removeKeyboard, cancellationToken: cancellationToken);
                            break;
                        }

                        case "remove_ticker":
                        {
                            if (parts.Length > 1)
                            {
                                string ticker = DecodeCallbackData(parts[1]);
                                if (settings.FavoriteTickers.Remove(ticker))
                                {
                                    await SaveUserSettingsAsync(cbUserId, settings);
                                    await bot.SendMessage(cbChatId, _uiManager.GetText("ticker_removed", settings.Language, ticker),
                                        replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                                }
                            }
                            break;
                        }

                        case "resetsettings":
                        {
                            string currentLang = settings.Language;
                            var newSettings = new UserSettings { Language = currentLang };
                            await SaveUserSettingsAsync(cbUserId, newSettings);
                            settings = newSettings;
                            state.Language = settings.Language;
                            var (resetMenuText, resetMenuKeyboard) = _uiManager.GetSettingsMenu(settings);
                            await bot.SendMessage(cbUserId, _uiManager.GetText("settings_reset", settings.Language),
                                replyMarkup: resetMenuKeyboard, cancellationToken: cancellationToken);
                            break;
                        }

                        case "help":
                        {
                            var (helpText, helpKeyboard) = _uiManager.GetHelpMenu(settings);
                            await bot.SendMessage(cbChatId, helpText, replyMarkup: helpKeyboard, cancellationToken: cancellationToken);
                            break;
                        }

                        case "support":
                        {
                            await bot.SendMessage(cbChatId, _uiManager.GetText("support_contact", settings.Language),
                                replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                            break;
                        }

                        case "whatsnew":
                        {
                            await bot.SendMessage(cbChatId, "📰 Последние обновления:\n - Обновлена модель сделки под новую структуру Notion\n - Кнопка 'Сохранить' на каждом шаге",
                                replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                            break;
                        }

                        case "main":
                        {
                            await DeleteUserStateAsync(cbUserId);
                            await SendMainMenuAsync(cbChatId, cbUserId, bot, cancellationToken);
                            break;
                        }

                        case "reset":
                        {
                            await DeleteUserStateAsync(cbUserId);
                            var allTrades2 = await _tradeStorage.GetTradesAsync(cbUserId);
                            int totalTrades2 = allTrades2.Count;
                            decimal totalPnL2 = allTrades2.Count > 0 ? allTrades2.Sum(t => t.PnL) : 0;
                            int profitableCount2 = allTrades2.Count(t => t.PnL > 0);
                            int winRate2 = totalTrades2 > 0 ? (int)((double)profitableCount2 / totalTrades2 * 100) : 0;
                            int tradesToday2 = (await _tradeStorage.GetTradesInDateRangeAsync(cbUserId, DateTime.Today, DateTime.Now)).Count;

                            string mainText2 = _uiManager.GetText("main_menu", settings.Language, tradesToday2, totalPnL2.ToString("F2"), winRate2);
                            await bot.SendMessage(cbChatId, mainText2, replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                            break;
                        }

                        case "more":
                        {
                            // more_<field>_page_<n>_trade_<id>
                            if (parts.Length >= 6)
                            {
                                string field = parts[1];
                                int page = int.TryParse(parts[3], out var p) ? p : 1;
                                string tId = parts[5];
                                var st = await GetUserStateAsync(cbUserId) ?? state;
                                var trade = st.Trade ?? new Trade { UserId = cbUserId, Date = DateTime.Now };
                                                                var dynKb = await GetDynamicOptionsKeyboard(cbUserId, trade, st.Step, settings, tId, field, page);
                                string prompt = _uiManager.GetText($"step_{st.Step}", settings.Language);
                                if (callback.Message != null)
                                    await bot.EditMessageText(cbChatId, callback.Message.MessageId, prompt, replyMarkup: dynKb, cancellationToken: cancellationToken);
                                else
                                    await bot.SendMessage(cbChatId, prompt, replyMarkup: dynKb, cancellationToken: cancellationToken);
                            }
                            break;
                        }

                        case "input":
                        {
                            if (parts.Length < 2) break;
                            string field = parts[1]; // ticker|risk|rr|profit|context|setup|result|emotions|entry|note|account|session|position|direction

                            if (parts.Length >= 4 && parts[2] == "trade")
                                tradeId = parts[3];
                            else
                                tradeId = state?.TradeId;

                            if (string.IsNullOrWhiteSpace(tradeId))
                            {
                                tradeId = Guid.NewGuid().ToString();
                                if (state != null)
                                {
                                    state.TradeId ??= tradeId;
                                    state.Trade ??= new Trade { UserId = cbUserId, Date = DateTime.UtcNow };
                                }
                            }

                            if (state != null)
                                state.Action = $"input_{field}";

                            var (promptTxt, promptKb) = _uiManager.GetInputPrompt(field, settings, tradeId ?? "");
                            var promptMsg = await bot.SendMessage(cbChatId, promptTxt, replyMarkup: promptKb, cancellationToken: cancellationToken);
                            if (state != null)
                            {
                                state.MessageId = promptMsg.MessageId;
                                await SaveUserStateAsync(cbUserId, state);
                            }
                            break;
                        }

                        case "back":
                        {
                            if (state?.Trade == null || state.TradeId != tradeId || tradeId == null)
                            {
                                await bot.SendMessage(cbChatId, _uiManager.GetText("trade_expired", settings.Language),
                                    replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                                await DeleteUserStateAsync(cbUserId);
                                break;
                            }

                            state.Step--;
                            if (state.Step < 1)
                            {
                                await DeleteUserStateAsync(cbUserId);
                                await bot.SendMessage(cbChatId, _uiManager.GetText("main_menu", settings.Language),
                                    replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                            }
                            else
                            {
                                var (prevText, prevKeyboard) = _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);
                                var prevMsg = await bot.SendMessage(cbChatId, prevText, replyMarkup: prevKeyboard, cancellationToken: cancellationToken);
                                state.MessageId = prevMsg.MessageId;
                                await SaveUserStateAsync(cbUserId, state);
                            }
                            break;
                        }

                        case "adjust":
                        {
                            if (state?.Trade == null || state.TradeId != tradeId || tradeId == null)
                            {
                                await bot.SendMessage(cbChatId, _uiManager.GetText("trade_expired", settings.Language),
                                    replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                                await DeleteUserStateAsync(cbUserId);
                                break;
                            }

                            if (parts.Length >= 3 && parts[1] == "pnl")
                            {
                                decimal adjValue = TryParseDecimal(parts[2]);
                                state.Trade.PnL += adjValue;
                                var (adjustText, adjustKeyboard) = _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);
                                var adjustMsg = await bot.SendMessage(cbChatId, adjustText, replyMarkup: adjustKeyboard, cancellationToken: cancellationToken);
                                state.MessageId = adjustMsg.MessageId;
                                await SaveUserStateAsync(cbUserId, state);
                            }
                            break;
                        }

                        case "cancel":
                        {
                            await DeleteUserStateAsync(cbUserId);
                            await bot.SendMessage(cbChatId, _uiManager.GetText("trade_cancelled", settings.Language),
                                replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                            break;
                        }

                        case "allcorrect":
                        {
                            if (string.IsNullOrEmpty(tradeId))
                            {
                                await bot.SendMessage(cbChatId, _uiManager.GetText("trade_expired", settings.Language),
                                    replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                                break;
                            }
                            var pendingTradeAllCorrect = await GetPendingTradeByTradeIdAsync(cbUserId, tradeId);
                            if (pendingTradeAllCorrect.HasValue)
                            {
                                state ??= new UserState
                                {
                                    Trade = pendingTradeAllCorrect.Value.Trade,
                                    TradeId = tradeId,
                                    Language = settings.Language
                                };
                                state.Step = state.Step > 0 ? state.Step + 1 : 9;
                                if (state.Step > 9)
                                {
                                    var trade = state.Trade!;
                                    await DeleteUserStateAsync(cbUserId);
                                    var (confirmText, confirmKb) = _uiManager.GetTradeConfirmationScreen(trade, tradeId, settings);
                                    var confirmMsg = await bot.SendMessage(cbChatId, confirmText, replyMarkup: confirmKb, cancellationToken: cancellationToken);
                                    await SavePendingTradeAsync(cbUserId, tradeId, confirmMsg.MessageId, trade);
                                    await UpdateRecentSettingsAsync(cbUserId, trade, settings);
                                }
                                else
                                {
                                    var (nextTxt, nextKb) = _uiManager.GetTradeInputScreen(state.Trade!, state.Step, settings, tradeId);
                                    var nextMsg = await bot.SendMessage(cbChatId, nextTxt, replyMarkup: nextKb, cancellationToken: cancellationToken);
                                    state.MessageId = nextMsg.MessageId;
                                    await SaveUserStateAsync(cbUserId, state);
                                }
                            }
                            else
                            {
                                await bot.SendMessage(cbChatId, _uiManager.GetText("trade_expired", settings.Language),
                                    replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                            }
                            break;
                        }

                        case "retry":
                        {
                            await DeleteUserStateAsync(cbUserId);
                            await bot.SendMessage(cbChatId, _uiManager.GetText("main_menu_button", settings.Language) + ":",
                                replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                            break;
                        }

                        case "onboarding":
                        {
                            // onboarding_1, onboarding_2, onboarding_3
                            if (parts.Length > 1 && int.TryParse(parts[1], out int onboardingStep))
                            {
                                var (onboardingText, onboardingKeyboard) = _uiManager.GetOnboardingScreen(onboardingStep, settings.Language);
                                await bot.SendMessage(cbChatId, onboardingText, replyMarkup: onboardingKeyboard, cancellationToken: cancellationToken);
                            }
                            break;
                        }

                        case "noop":
                        {
                            // ничего не делаем
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unhandled exception in HandleUpdateAsync for UserId={userId}, ChatId={chatId}");
                if (chatId != 0)
                {
                    var settings = await GetUserSettingsAsync(userId);
                    await bot.SendMessage(chatId, _uiManager.GetText("error_occurred", settings.Language),
                        replyMarkup: _uiManager.GetErrorKeyboard(settings), cancellationToken: cancellationToken);
                }
            }
        }

        private async Task HandleTradeInputAsync(ITelegramBotClient bot, long chatId, long userId, UserState state,
            UserSettings settings, string text, int messageId, CancellationToken cancellationToken)
        {
            if (state.Action != null && state.Action.StartsWith("input_"))
            {
                string field = state.Action.Substring("input_".Length).ToLowerInvariant();
                text = text.Trim();

                if (string.IsNullOrEmpty(text))
                {
                    state.ErrorCount++;
                    var (retryText, retryKeyboard) = state.ErrorCount >= 3
                        ? (_uiManager.GetText("error_occurred", settings.Language), _uiManager.GetErrorKeyboard(settings))
                        : _uiManager.GetInputPrompt(field, settings, state.TradeId ?? "");
                    var errorMessage = await bot.SendMessage(chatId, retryText, replyMarkup: retryKeyboard, cancellationToken: cancellationToken);
                    state.MessageId = errorMessage.MessageId;
                    await SaveUserStateAsync(userId, state);
                    return;
                }

                try
                {
                    state.Trade ??= new Trade { UserId = userId, Date = DateTime.UtcNow };

                    switch (field)
                    {
                        case "ticker":
                            state.Trade.Ticker = text.ToUpperInvariant();
                            settings.RecentTickers.Remove(state.Trade.Ticker);
                            settings.RecentTickers.Insert(0, state.Trade.Ticker);
                            settings.RecentTickers = settings.RecentTickers.Take(5).ToList();
                            await SaveUserSettingsAsync(userId, settings);
                            break;

                        case "account":
                            state.Trade.Account = text;
                            break;

                        case "session":
                            state.Trade.Session = text;
                            break;

                        case "position":
                            state.Trade.Position = text;
                            break;

                        case "direction":
                            state.Trade.Direction = text;
                            settings.RecentDirections.Remove(state.Trade.Direction);
                            settings.RecentDirections.Insert(0, state.Trade.Direction);
                            settings.RecentDirections = settings.RecentDirections.Take(5).ToList();
                            await SaveUserSettingsAsync(userId, settings);
                            break;

                        case "risk":
                            state.Trade.Risk = TryParseNullableDecimal(text);
                            break;

                        case "rr":
                            state.Trade.RR = TryParseNullableDecimal(text);
                            break;

                        case "profit":
                        case "pnl":
                            state.Trade.PnL = TryParseDecimal(text);
                            break;

                        case "context":
                            state.Trade.Context = text.Split(',')
                                .Select(t => t.Trim())
                                .Where(t => !string.IsNullOrWhiteSpace(t))
                                .ToList();
                            break;

                        case "setup":
                            state.Trade.Setup = text.Split(',')
                                .Select(t => t.Trim())
                                .Where(t => !string.IsNullOrWhiteSpace(t))
                                .ToList();
                            break;

                        case "result":
                            state.Trade.Result = text;
                            break;

                        case "emotions":
                            state.Trade.Emotions = text.Split(',')
                                .Select(t => t.Trim())
                                .Where(t => !string.IsNullOrWhiteSpace(t))
                                .ToList();
                            break;

                        case "entry":
                            state.Trade.EntryDetails = text;
                            break;

                        case "note":
                            state.Trade.Note = text;
                            break;
                    }

                    state.Step++;
                    if (state.Step <= 14)
                    {
                        var (nextText, nextKeyboard) = _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, state.TradeId ?? Guid.NewGuid().ToString());
                        var nextMessage = await bot.SendMessage(chatId, nextText, replyMarkup: nextKeyboard, cancellationToken: cancellationToken);
                        state.MessageId = nextMessage.MessageId;
                        await SaveUserStateAsync(userId, state);
                    }
                    else
                    {
                        var trade = state.Trade;
                        await DeleteUserStateAsync(userId);
                        string tid = state.TradeId ?? Guid.NewGuid().ToString();
                        var (confText, confKeyboard) = _uiManager.GetTradeConfirmationScreen(trade, tid, settings);
                        var confMessage = await bot.SendMessage(chatId, confText, replyMarkup: confKeyboard, cancellationToken: cancellationToken);
                        await SavePendingTradeAsync(userId, tid, confMessage.MessageId, trade);
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
        }

        private async Task HandleSettingsInputAsync(ITelegramBotClient bot, long chatId, long userId, UserState state,
            UserSettings settings, string text, CancellationToken cancellationToken)
        {
            string? action = state.Action;
            text = text.Trim();
            if (action == "input_favorite_ticker")
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    settings.FavoriteTickers.Add(text.ToUpperInvariant());
                    await SaveUserSettingsAsync(userId, settings);
                    await bot.SendMessage(chatId, _uiManager.GetText("ticker_added", settings.Language, text.ToUpperInvariant()),
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

            if (!string.IsNullOrEmpty(trade.Account))
            {
                settings.RecentAccounts.Remove(trade.Account);
                settings.RecentAccounts.Insert(0, trade.Account);
                settings.RecentAccounts = settings.RecentAccounts.Take(5).ToList();
            }

            if (!string.IsNullOrEmpty(trade.Session))
            {
                settings.RecentSessions.Remove(trade.Session);
                settings.RecentSessions.Insert(0, trade.Session);
                settings.RecentSessions = settings.RecentSessions.Take(5).ToList();
            }

            if (!string.IsNullOrEmpty(trade.Position))
            {
                settings.RecentPositions.Remove(trade.Position);
                settings.RecentPositions.Insert(0, trade.Position);
                settings.RecentPositions = settings.RecentPositions.Take(5).ToList();
            }

            if (!string.IsNullOrEmpty(trade.Result))
            {
                settings.RecentResults.Remove(trade.Result);
                settings.RecentResults.Insert(0, trade.Result);
                settings.RecentResults = settings.RecentResults.Take(5).ToList();
            }

            if (trade.Setup != null && trade.Setup.Any())
            {
                foreach (var s in trade.Setup)
                {
                    settings.RecentSetups.Remove(s);
                    settings.RecentSetups.Insert(0, s);
                }
                settings.RecentSetups = settings.RecentSetups.Distinct().Take(5).ToList();
            }

            if (trade.Context != null && trade.Context.Any())
            {
                foreach (var s in trade.Context)
                {
                    settings.RecentContexts.Remove(s);
                    settings.RecentContexts.Insert(0, s);
                }
                settings.RecentContexts = settings.RecentContexts.Distinct().Take(5).ToList();
            }

            if (trade.Emotions != null && trade.Emotions.Any())
            {
                foreach (var s in trade.Emotions)
                {
                    settings.RecentEmotions.Remove(s);
                    settings.RecentEmotions.Insert(0, s);
                }
                settings.RecentEmotions = settings.RecentEmotions.Distinct().Take(5).ToList();
            }

            if (!string.IsNullOrEmpty(trade.Note))
            {
                settings.RecentComments.Remove(trade.Note);
                settings.RecentComments.Insert(0, trade.Note);
                settings.RecentComments = settings.RecentComments.Take(5).ToList();
            }

            await SaveUserSettingsAsync(userId, settings);
            // Шэрим последние настройки в IMemoryCache для приоритезации опций в NotionTradeStorage
            try
            {
                _cache.Set("last_user_settings", new NotionTradeStorage.ModelUserSettingsProxy(settings), TimeSpan.FromMinutes(10));
            }
            catch { /* ignore cross-type visibility in case of DI differences */ }
        }

        private async Task SaveTradeAsync(Trade trade, long chatId, long userId, ITelegramBotClient bot, UserSettings settings, CancellationToken ct)
        {
            _logger.LogInformation($"💾 Saving trade for UserId={userId}: {trade.Ticker}, PnL={trade.PnL}");
            try
            {
                await _tradeStorage.AddTradeAsync(trade);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Ошибка при сохранении сделки UserId={userId}");
                // Выдаем разные сообщения об ошибке для Notion и локального хранилища
                string notSavedText = _uiManager.GetText("trade_not_saved", settings.Language);
                string errorDetail = _tradeStorage is NotionTradeStorage
                    ? _uiManager.GetText("notion_save_error", settings.Language)
                    : _uiManager.GetText("local_save_error", settings.Language);
                await bot.SendMessage(chatId, $"{notSavedText} {errorDetail}",
                    replyMarkup: _uiManager.GetMainMenu(settings),
                    cancellationToken: ct);
                return;
            }

            await UpdateRecentSettingsAsync(userId, trade, settings);

            var baseText = _uiManager.GetText("trade_saved", settings.Language, trade.Ticker, trade.PnL);
            var mainMenu = _uiManager.GetMainMenu(settings);
            var sentMsg = await bot.SendMessage(chatId, baseText, replyMarkup: mainMenu, cancellationToken: ct);

            if (_tradeStorage is NotionTradeStorage && !string.IsNullOrEmpty(trade.NotionPageId))
            {
                await bot.EditMessageText(chatId, sentMsg.MessageId,
                    baseText + "\n\n" + _uiManager.GetText("trade_sent_notion", settings.Language),
                    replyMarkup: mainMenu, cancellationToken: ct);
            }

            if (settings.NotificationsEnabled)
            {
                int streak = await CalculateStreakAsync(userId);
                if (streak >= 3)
                {
                    string streakMsg = trade.PnL > 0
                        ? _uiManager.GetText("win_streak", settings.Language, streak)
                        : _uiManager.GetText("loss_streak", settings.Language, streak);
                    await bot.SendMessage(chatId, streakMsg, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
                }
            }
        }

        private async Task<int> CalculateStreakAsync(long userId)
        {
            var trades = await _tradeStorage.GetTradesAsync(userId);
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
                "week"  => await _tradeStorage.GetTradesInDateRangeAsync(userId, now.AddDays(-7), now),
                "month" => await _tradeStorage.GetTradesInDateRangeAsync(userId, now.AddDays(-30), now),
                _       => await _tradeStorage.GetTradesAsync(userId)
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
                        trades = trades.Where(t => t.Ticker.Equals(filterValue, StringComparison.OrdinalIgnoreCase)).ToList();
                    }
                    else if (filterType == "direction")
                    {
                        trades = trades.Where(t => t.Direction.Equals(filterValue, StringComparison.OrdinalIgnoreCase)).ToList();
                    }
                    else if (filterType == "pnl" && filterParts.Length == 3)
                    {
                        if (decimal.TryParse(filterParts[2].Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value))
                        {
                            trades = filterValue == "gt" ? trades.Where(t => t.PnL > value).ToList()
                                                         : trades.Where(t => t.PnL < value).ToList();
                        }
                    }
                }
            }

            return trades;
        }

        private string GenerateCsv(List<Trade> trades)
        {
            // Экспорт под новую модель
            var sb = new StringBuilder();
            sb.AppendLine("Id,Date,Pair,Account,Session,Position,Direction,Context,Setup,Result,RR,Risk,EntryDetails,Note,Emotions,PercentProfit");
            foreach (var t in trades)
            {
                string ctx  = t.Context != null  ? string.Join("|", t.Context)    : "";
                string setup= t.Setup != null    ? string.Join("|", t.Setup)      : "";
                string emos = t.Emotions != null ? string.Join("|", t.Emotions)  : "";
                string entry= t.EntryDetails?.Replace(",", ";") ?? "";
                string note = t.Note?.Replace(",", ";") ?? "";

                sb.AppendLine(string.Join(",", new[]
                {
                    t.Id.ToString(),
                    t.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                    t.Ticker ?? "",
                    t.Account ?? "",
                    t.Session ?? "",
                    t.Position ?? "",
                    t.Direction ?? "",
                    ctx,
                    setup,
                    t.Result ?? "",
                    (t.RR?.ToString(CultureInfo.InvariantCulture) ?? ""),
                    (t.Risk?.ToString(CultureInfo.InvariantCulture) ?? ""),
                    entry,
                    note,
                    emos,
                    t.PnL.ToString(CultureInfo.InvariantCulture)
                }));
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
                    return result;
            }
            return 0m;
        }

        private static decimal? TryParseNullableDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text == "-") return null;
            var match = Regex.Match(text, @"([+\-]?\d{1,10}(?:[.,]\d{1,4})?)\s*%?");
            if (match.Success)
            {
                string numStr = match.Groups[1].Value.Replace(",", ".");
                if (decimal.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                    return val;
            }
            return null;
        }

        private async Task GenerateEquityCurveAsync(long chatId, long userId, ITelegramBotClient bot,
                                                    CancellationToken ct, string period, UserSettings settings, int triggerMessageId)
        {
            DateTime now = DateTime.UtcNow;
            List<Trade> trades = period switch
            {
                "week"  => await _tradeStorage.GetTradesInDateRangeAsync(userId, now.AddDays(-7), now),
                "month" => await _tradeStorage.GetTradesInDateRangeAsync(userId, now.AddDays(-30), now),
                _       => await _tradeStorage.GetTradesAsync(userId)
            };
            if (trades.Count == 0)
            {
                await bot.SendMessage(chatId, _uiManager.GetText("no_trades", settings.Language),
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

            int width = 900, height = 600, margin = 60;
            string tmpPng = Path.Combine(Path.GetTempPath(), $"equity_{userId}_{Guid.NewGuid():N}.png");

            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);

            float dx = (width - 2 * margin) / (float)Math.Max(ys.Count - 1, 1);
            float minY = ys.Min(), maxY = ys.Max();
            if (Math.Abs(maxY - minY) < 1e-6) maxY += 1;
            float scale = (height - 2 * margin) / (maxY - minY);

            using (var glowPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 18,
                Color = new SKColor(0, 255, 255, 100),
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

            using (var gridPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = new SKColor(0, 255, 255, 60),
                StrokeWidth = 2
            })
            {
                for (int i = 0; i <= 5; i++)
                {
                    float y = margin + i * (height - 2 * margin) / 5;
                    canvas.DrawLine(margin, y, width - margin, y, gridPaint);
                }
                for (int i = 0; i <= 5; i++)
                {
                    float x = margin + i * (width - 2 * margin) / 5;
                    canvas.DrawLine(x, margin, x, height - margin, gridPaint);
                }
            }

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

            using (var img = surface.Snapshot())
            using (var data = img.Encode(SKEncodedImageFormat.Png, 100))
            using (var outStream = File.OpenWrite(tmpPng))
            {
                data.SaveTo(outStream);
            }

            await using var fs = new FileStream(tmpPng, FileMode.Open, FileAccess.Read);
            await bot.SendPhoto(chatId, InputFile.FromStream(fs, "equity.png"),
                caption: "💎 Ваша неоновая кривая эквити",
                replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: ct);

            fs.Close();
            File.Delete(tmpPng);

            try
            {
                if (triggerMessageId != 0)
                    await bot.DeleteMessage(chatId, triggerMessageId, ct);
            }
            catch { /* ignore */ }
        }

        private async Task<InlineKeyboardMarkup?> GetDynamicOptionsKeyboard(long userId, Trade trade, int step, UserSettings settings, string tradeId, string? explicitField = null, int page = 1)
        {
            string? field = explicitField;
            string? property = null;
            if (field == null)
            {
                switch (step)
                {
                    case 2: field = "account"; property = "Account"; break;
                    case 3: field = "session"; property = "Session"; break;
                    case 4: field = "position"; property = "Position"; break;
                    case 5: field = "direction"; property = "Direction"; break;
                    case 6: field = "context"; property = "Context"; break;
                    case 7: field = "setup"; property = "Setup"; break;
                    case 10: field = "result"; property = "Result"; break;
                    case 12: field = "emotions"; property = "Emotions"; break;
                    default: return null;
                }
            }
            property ??= field switch
            {
                "account" => "Account",
                "session" => "Session",
                "position" => "Position",
                "direction" => "Direction",
                "context" => "Context",
                "setup" => "Setup",
                "result" => "Result",
                "emotions" => "Emotions",
                _ => null
            };
            if (property == null) return null;
            List<string> options;
            try
            {
                // сначала попробуем умные подсказки (кэш 45с), fallback на сырые опции
                options = await _tradeStorage.GetSuggestedOptionsAsync(property, userId, trade, topN: 12);
                if (options == null || options.Count == 0)
                    options = await _tradeStorage.GetSelectOptionsAsync(property, trade);
            }
            catch
            {
                options = new List<string>();
            }
            return _uiManager.BuildOptionsKeyboard(field!, options, tradeId, settings, page: page, step: step);
        }
    }
}

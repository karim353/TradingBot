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
        private static readonly TimeSpan AutoReturnDelay = TimeSpan.FromSeconds(3);
        private const int MaxRequestsPerMinute = 60;

        public UpdateHandler(
            TradeRepository repo,
            PnLService pnlService,
            NotionService notionService,
            UIManager uiManager,
            ILogger<UpdateHandler> logger,
            IMemoryCache cache,
            string sqliteConnectionString,
            string botId)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _pnlService = pnlService ?? throw new ArgumentNullException(nameof(pnlService));
            _notionService = notionService ?? throw new ArgumentNullException(nameof(notionService));
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _sqliteConnectionString =
                sqliteConnectionString ?? throw new ArgumentNullException(nameof(sqliteConnectionString));
            _botId = botId ?? throw new ArgumentNullException(nameof(botId));
            _logger.LogInformation($"📈 UpdateHandler initialized (BotId={_botId})");
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
                    );";
                await command.ExecuteNonQueryAsync();
                _logger.LogInformation("📊 Database initialized (PendingTrades, UserStates, UserSettings)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
                throw;
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
            _logger.LogInformation($"💾 Saved user state (UserId={userId}, Action={state.Action}, Step={state.Step})");
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
                var state = JsonSerializer.Deserialize<UserState>(stateJson);
                if (state != null)
                {
                    _cache.Set($"state_{userId}", state, TimeSpan.FromMinutes(30));
                }

                return state;
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
                var settings = JsonSerializer.Deserialize<UserSettings>(settingsJson) ?? new UserSettings();
                _cache.Set($"settings_{userId}", settings, TimeSpan.FromMinutes(30));
                return settings;
            }

            var defaultSettings = new UserSettings();
            await SaveUserSettingsAsync(userId, defaultSettings);
            return defaultSettings;
        }

        private async Task SaveUserSettingsAsync(long userId, UserSettings settings)
        {
            _cache.Set($"settings_{userId}", settings, TimeSpan.FromMinutes(30));
            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO UserSettings (BotId, UserId, SettingsJson)
                VALUES ($botId, $userId, $json)";
            cmd.Parameters.AddWithValue("$botId", _botId);
            cmd.Parameters.AddWithValue("$userId", userId);
            cmd.Parameters.AddWithValue("$json", JsonSerializer.Serialize(settings));
            await cmd.ExecuteNonQueryAsync();
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
            await command.ExecuteNonQueryAsync();
            _logger.LogInformation(
                $"💰 Trade saved in PendingTrades (UserId={userId}, TradeId={tradeId}, Ticker={trade.Ticker}, PnL={trade.PnL})");
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
                int messageId = reader.GetInt32(1);
                DateTime createdAt = DateTime.Parse(reader.GetString(2), null, DateTimeStyles.RoundtripKind);
                var trade = JsonSerializer.Deserialize<Trade>(tradeJson);
                if (trade != null)
                {
                    return (trade, messageId, createdAt);
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
            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            int offset = (page - 1) * pageSize;
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
                DateTime createdAt = DateTime.Parse(reader.GetString(3), null, DateTimeStyles.RoundtripKind);
                var trade = JsonSerializer.Deserialize<Trade>(tradeJson);
                if (trade != null)
                {
                    result.Add((tradeId, trade, messageId, createdAt));
                }
            }

            return result;
        }

        private async Task HandleImageAsync(
            ITelegramBotClient bot,
            Message message,
            long chatId,
            long userId,
            UserSettings settings,
            CancellationToken ct)
        {
            string fileId = message.Photo?.Last().FileId
                            ?? message.Document!.FileId;

            _logger.LogInformation($"📸 OCR start for UserId={userId}, fileId={fileId}");
            try
            {
                var fileInfo = await bot.GetFile(fileId, ct);
                if (string.IsNullOrEmpty(fileInfo.FilePath))
                {
                    await bot.SendMessage(chatId, "❌ Не удалось получить изображение.",
                        replyMarkup: _uiManager.GetErrorKeyboard(settings), cancellationToken: ct);
                    return;
                }

                await using var ms = new MemoryStream();
                await bot.DownloadFile(fileInfo.FilePath, ms, ct);
                ms.Position = 0;

                var ocr = _pnlService.ExtractFromImage(ms);
                _logger.LogInformation($"📊 OCR: {JsonSerializer.Serialize(ocr)}");

                string tradeId = Guid.NewGuid().ToString();
                var trade = new Trade
                {
                    UserId = userId,
                    Date = ocr.TradeDate ?? DateTime.Now,
                    Ticker = ocr.Ticker ?? "",
                    Direction = ocr.Direction ?? "",
                    PnL = ocr.PnLPercent ?? 0,
                    OpenPrice = ocr.Open,
                    Entry = ocr.Close
                };

                // если распознано не всё → продолжаем руками
                if (string.IsNullOrEmpty(trade.Ticker) ||
                    string.IsNullOrEmpty(trade.Direction) ||
                    ocr.PnLPercent == null)
                {
                    var st = new UserState
                    {
                        Action = "new_trade",
                        Step = string.IsNullOrEmpty(trade.Ticker) ? 1 :
                            string.IsNullOrEmpty(trade.Direction) ? 2 : 3,
                        Trade = trade,
                        Language = settings.Language,
                        TradeId = tradeId
                    };

                    var (txt, kb) =
                        _uiManager.GetTradeInputScreen(trade, st.Step, settings, tradeId);

                    var sent = await bot.SendMessage(chatId, txt, replyMarkup: kb, cancellationToken: ct);
                    st.MessageId = sent.MessageId;
                    await SaveUserStateAsync(userId, st);
                }
                else
                {
                    var (txt, kb) = _uiManager.GetTradeConfirmationScreen(trade, tradeId, settings);
                    var sent = await bot.SendMessage(chatId, txt, replyMarkup: kb, cancellationToken: ct);
                    await SavePendingTradeAsync(userId, tradeId, sent.MessageId, trade);
                    await UpdateRecentSettingsAsync(userId, trade, settings);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"OCR error for UserId={userId}");
                await bot.SendMessage(chatId, "❌ Ошибка при обработке изображения.",
                    replyMarkup: _uiManager.GetErrorKeyboard(settings), cancellationToken: ct);
            }
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
                _logger.LogInformation($"🧹 Cleaned {rows} expired pending trades");
            }
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

                    // ⚙ настройки / состояние
                    var settings = await GetUserSettingsAsync(userId);
                    var state = await GetUserStateAsync(userId) ?? new UserState { Language = settings.Language };

                    /* ─────────────────────────────  ОБРАБОТКА ИЗОБРАЖЕНИЯ  ───────────────────────────── */
                    if ((message.Photo?.Any() == true) ||
                        (message.Document != null && (message.Document.MimeType?.StartsWith("image/") ?? false)))
                    {
                        await HandleImageAsync(bot, message, chatId, userId, settings, cancellationToken);
                        return; // ⬅ ни одного меню после фото
                    }
                    /* ─────────────────────────  КОНЕЦ ОБРАБОТКИ ИЗОБРАЖЕНИЯ  ────────────────────────── */

                    /* --- rate‑limit & прочее, как было --- */
                    string rlKey = $"rate_limit_{userId}";
                    if (_cache.TryGetValue(rlKey, out int req))
                    {
                        if (req >= MaxRequestsPerMinute)
                        {
                            await bot.SendMessage(chatId, "⏳ Слишком много запросов. Попробуйте позже.",
                                cancellationToken: cancellationToken);
                            return;
                        }

                        _cache.Set(rlKey, req + 1, TimeSpan.FromMinutes(1));
                    }
                    else _cache.Set(rlKey, 1, TimeSpan.FromMinutes(1));


                    if (text == "/start")
                    {
                        if (_cache.TryGetValue($"seen_tutorial_{userId}", out bool _))
                        {
                            await DeleteUserStateAsync(userId);
                            await bot.SendMessage(chatId, $"📈 Добро пожаловать в главное меню!",
                                replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
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

                    // ─── ОБРАБОТКА ИЗОБРАЖЕНИЯ (photo ИЛИ document‑image) ───────────────────────
                    if ((message.Photo?.Any() == true) ||
                        (message.Document != null && (message.Document.MimeType?.StartsWith("image/") ?? false)))
                    {
                        string fileId = message.Photo?.Last().FileId
                                        ?? message.Document!.FileId; // document‑image
                        _logger.LogInformation($"📸 Processing image from UserId={userId}, fileId={fileId}");

                        try
                        {
                            var fileInfo = await bot.GetFile(fileId, cancellationToken);
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
                                $"📊 OCR‑result: Ticker={data.Ticker}, Dir={data.Direction}, PnL={data.PnLPercent}");

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

                            // если не всё распозналось — переключаемся на диалог ввода
                            if (string.IsNullOrEmpty(trade.Ticker) ||
                                string.IsNullOrEmpty(trade.Direction) ||
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
                                var (confTxt, confKb) =
                                    _uiManager.GetTradeConfirmationScreen(trade, tradeId, settings);

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

                        return; // ← НЕ допускаем падения в «главное меню»
                    }
// ─── КОНЕЦ ОБРАБОТКИ ИЗОБРАЖЕНИЯ ─────────────────────────────────────────────


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

                    // читаем состояние и настройки
                    var state = await GetUserStateAsync(userId)
                                ?? new UserState { Language = (await GetUserSettingsAsync(userId)).Language };
                    var settings = await GetUserSettingsAsync(userId);

                    // пробуем убрать сообщение‑клавиатуру
                    try
                    {
                        await bot.DeleteMessage(chatId, msgId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Delete msg {msgId} failed");
                    }

                    // ─── ПАРСИНГ CALLBACK‑ДАННЫХ ───────────────────────────────────────────
                    string[] parts = data.Split('_', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0) return;

                    string action = parts[0].ToLowerInvariant();
                    string tradeId = null;

                    /* Нормализация составных callback’ов */
                    if (action == "history" && parts.Length > 1 && parts[1] == "page") action = "history_page";
                    if (action == "stats" && parts.Length > 1 && parts[1] == "period") action = "statsperiod";
                    if (action == "advstats" && parts.Length > 1 && parts[1] == "period") action = "advstatsperiod";

                    /* Выдёргиваем tradeId, где бы он ни стоял */
                    if (action == "confirm_trade" || (action == "confirm" && parts.Length >= 3))
                        tradeId = parts[^1];
                    else if (action == "skip" && parts.Length >= 5 && parts[1] == "trade")
                        tradeId = parts[2];
                    else if (action == "allcorrect" && parts.Length >= 2)
                        tradeId = parts[1];
                    else
                    {
                        int idx = Array.IndexOf(parts, "trade");
                        if (idx >= 0 && idx < parts.Length - 1) tradeId = parts[idx + 1];
                    }


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
                                await bot.SendMessage(chatId, $"📈 Главное меню:",
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
                                TradeId = tradeId // Устанавливаем TradeId
                            };
                            var (tradeText, tradeKeyboard) = _uiManager.GetTradeInputScreen(state.Trade, state.Step,
                                settings, tradeId, lastTrade);
                            var tradeMessage = await bot.SendMessage(chatId, tradeText, replyMarkup: tradeKeyboard,
                                cancellationToken: cancellationToken);
                            state.MessageId = tradeMessage.MessageId;
                            await SaveUserStateAsync(userId, state);
                            break;

                        case "edit":
                            if (string.IsNullOrEmpty(tradeId))
                            {
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
                                    TradeId = tradeId // Устанавливаем TradeId
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
                                await bot.SendMessage(chatId, "⏰ Сделка устарела.",
                                    replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                            }

                            break;

                        case "confirm_pnl":
                            _logger.LogInformation(
                                $"Processing confirm_pnl for UserId={userId}, TradeId={tradeId}, State.Trade={(state.Trade != null ? "exists" : "null")}");
                            if (string.IsNullOrEmpty(tradeId) || state.Trade == null)
                            {
                                _logger.LogWarning(
                                    $"Invalid tradeId or state.Trade is null for UserId={userId}, TradeId={tradeId}");
                                await bot.SendMessage(chatId, "⏰ Сделка устарела.",
                                    replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                                break;
                            }

                            try
                            {
                                state.Step++;
                                _logger.LogInformation($"Advancing to step {state.Step} for TradeId={tradeId}");
                                var (nextText, nextKeyboard) =
                                    _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);
                                var nextMessage = await bot.SendMessage(chatId, nextText, replyMarkup: nextKeyboard,
                                    cancellationToken: cancellationToken);
                                state.MessageId = nextMessage.MessageId;
                                _logger.LogInformation(
                                    $"Sent message for step {state.Step}, MessageId={nextMessage.MessageId} for UserId={userId}");
                                await SaveUserStateAsync(userId, state);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error in confirm_pnl for UserId={userId}, TradeId={tradeId}");
                                await bot.SendMessage(chatId, "❌ Ошибка при обработке подтверждения PnL.",
                                    replyMarkup: _uiManager.GetErrorKeyboard(settings),
                                    cancellationToken: cancellationToken);
                            }

                            break;

                        // ───────── UpdateHandler.cs  ➜  внутри switch(action) ─────────
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
                                    try { await bot.DeleteMessage(chatId, originalMessageId, cancellationToken); }
                                    catch { /* не критично */ }

                                    // сохраняем сделку единым методом (БД → Notion → уведомления)
                                    await SaveTradeAsync(trade, chatId, userId, bot, settings, cancellationToken);

                                    // чистим PendingTrades
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
                            // ожидаем: skip_trade_<guid>_step_<n>
                            if (parts.Length >= 5 && parts[0] == "skip" && parts[1] == "trade")
                            {
                                tradeId = parts[2];

                                // валидация состояния
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

                                if (!int.TryParse(parts[^1], out int step)) // parts[^1] — последний элемент («n»)
                                {
                                    _logger.LogWarning(
                                        $"Invalid step format in skip callback for UserId={userId}: {callback.Data}");
                                    await bot.SendMessage(chatId,
                                        _uiManager.GetText("invalid_input", settings.Language),
                                        replyMarkup: _uiManager.GetMainMenu(settings),
                                        cancellationToken: cancellationToken);
                                    break;
                                }

                                state.Step = step + 1; // переходим к следующему шагу
                                state.ErrorCount = 0;

                                if (state.Step <= 8)
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
                                int step = fieldKey switch
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
                                    Step = step,
                                    Trade = state.Trade,
                                    Language = settings.Language
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
                            await bot.SendMessage(chatId, "✅ Все ожидающие сделки очищены.",
                                replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                            break;

                        case "stats":
                            var (statsMenuText, statsMenuKeyboard) = _uiManager.GetStatsMenu(settings);
                            await bot.SendMessage(chatId, statsMenuText, replyMarkup: statsMenuKeyboard,
                                cancellationToken: cancellationToken);
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
                            var allTrades = await _repo.GetTradesAsync(userId);
                            var csvContent = GenerateCsv(allTrades);
                            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent)))
                            {
                                await bot.SendDocument(chatId, InputFile.FromStream(stream, "trades.csv"),
                                    caption: "💾 Экспорт успешен!", replyMarkup: _uiManager.GetMainMenu(settings),
                                    cancellationToken: cancellationToken);
                            }

                            break;

                        case "settings":
                            state = new UserState { Action = "settings_menu", Step = 1, Language = settings.Language };
                            var (settingsMenuText, settingsMenuKeyboard) = _uiManager.GetSettingsMenu(settings);
                            var settingsMenuMessage = await bot.SendMessage(chatId, settingsMenuText,
                                replyMarkup: settingsMenuKeyboard, cancellationToken: cancellationToken);
                            state.MessageId = settingsMenuMessage.MessageId;
                            await SaveUserStateAsync(userId, state);
                            break;

                        case "settings_language":
                            settings.Language = settings.Language == "ru" ? "en" : "ru";
                            await SaveUserSettingsAsync(userId, settings);
                            var (langMenuText, langMenuKeyboard) = _uiManager.GetSettingsMenu(settings);
                            try
                            {
                                await bot.EditMessageText(chatId, msgId, langMenuText, replyMarkup: langMenuKeyboard,
                                    cancellationToken: cancellationToken);
                            }
                            catch
                            {
                                await bot.SendMessage(chatId, langMenuText, replyMarkup: langMenuKeyboard,
                                    cancellationToken: cancellationToken);
                            }

                            state.Language = settings.Language;
                            await SaveUserStateAsync(userId, state);
                            break;

                        case "settings_notifications":
                            settings.NotificationsEnabled = !settings.NotificationsEnabled;
                            await SaveUserSettingsAsync(userId, settings);
                            var (notifMenuText, notifMenuKeyboard) = _uiManager.GetSettingsMenu(settings);
                            try
                            {
                                await bot.EditMessageText(chatId, msgId, notifMenuText, replyMarkup: notifMenuKeyboard,
                                    cancellationToken: cancellationToken);
                            }
                            catch
                            {
                                await bot.SendMessage(chatId, notifMenuText, replyMarkup: notifMenuKeyboard,
                                    cancellationToken: cancellationToken);
                            }

                            break;

                        case "settings_tickers":
                        {
                            // переходим в режим ручного ввода избранного тикера
                            state.Action = "input_ticker_settings";

                            var (promptText, promptKeyboard) = _uiManager.GetSettingsInputPrompt("ticker", settings);
                            var sent = await bot.SendMessage(chatId, promptText, replyMarkup: promptKeyboard,
                                cancellationToken: cancellationToken);

                            state.MessageId = sent.MessageId;
                            await SaveUserStateAsync(userId, state);
                            break;
                        }


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
                            await bot.SendMessage(chatId, $"📈 Главное меню:",
                                replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                            break;

                        case "reset":
                            await DeleteUserStateAsync(userId);
                            await bot.SendMessage(chatId, $"📈 Главное меню:",
                                replyMarkup: _uiManager.GetMainMenu(settings), cancellationToken: cancellationToken);
                            break;

                        case "more":
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

                        case "input":
                        {
                            _logger.LogInformation(
                                $"Processing input callback for UserId={userId}, Data={callback.Data}");

                            // valid: input_<field>            (2 parts)
                            // valid: input_<field>_trade_<id> (4+ parts)
                            if (parts.Length < 2)
                                break;

                            string field = parts[1]; // ticker | pnl | open | … | comment
                            if (!new[] { "ticker", "pnl", "open", "close", "sl", "tp", "volume", "comment" }
                                    .Contains(field))
                                break;

                            // Если guid пришёл в callback, берём его; иначе — из state
                            if (parts.Length >= 4 && parts[2] == "trade")
                                tradeId = parts[3];
                            else
                                tradeId = state?.TradeId;

                            // Если трейд ещё не создан — создаём заготовку
                            if (string.IsNullOrEmpty(tradeId))
                            {
                                tradeId = Guid.NewGuid().ToString();
                                state.TradeId = tradeId;
                                state.Trade ??= new Trade { UserId = userId, Date = DateTime.UtcNow };
                            }

                            state.Action = $"input_{field}"; // в state храним только название поля
                            var (promptTxt, promptKb) = _uiManager.GetInputPrompt(field, settings, tradeId);

                            var promptMsg = await bot.SendMessage(chatId, promptTxt, replyMarkup: promptKb,
                                cancellationToken: cancellationToken);

                            state.MessageId = promptMsg.MessageId;
                            await SaveUserStateAsync(userId, state);
                            break;
                        }


                        case "back":
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
                                // FIX: гарантируем, что tradeId не потерялся
                                tradeId = state.TradeId;

                                var (prevText, prevKeyboard) =
                                    _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);

                                var prevMsg = await bot.SendMessage(chatId, prevText,
                                    replyMarkup: prevKeyboard, cancellationToken: cancellationToken);

                                state.MessageId = prevMsg.MessageId;
                                state.TradeId = tradeId; // на всякий случай
                                await SaveUserStateAsync(userId, state);
                            }

                            break;


                        case "set":
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
                                state.Trade.PnL += adjustment1; // Add to OCR-detected PnL
                                var (pnlText, pnlKeyboard) =
                                    _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);
                                var pnlMessage = await bot.SendMessage(chatId, pnlText, replyMarkup: pnlKeyboard,
                                    cancellationToken: cancellationToken);
                                state.MessageId = pnlMessage.MessageId;
                                await SaveUserStateAsync(userId, state);
                            }

                            break;
                        // UpdateHandler.cs  ──────────────────────────────────────────────────────
                        case "allcorrect":
                        {
                            // callback: allcorrect_<guid>
                            if (string.IsNullOrEmpty(tradeId))
                            {
                                _logger.LogWarning(
                                    $"Missing TradeId in allcorrect for UserId={userId}: {callback.Data}");
                                break;
                            }

                            // current step -> next
                            state.Step++;

                            if (state.Step > 9) // все поля заполнены — финальное подтверждение
                            {
                                var ready = state.Trade;
                                await DeleteUserStateAsync(userId);

                                var (confTxt, confKb) = _uiManager.GetTradeConfirmationScreen(ready, tradeId, settings);
                                var confMsg = await bot.SendMessage(chatId, confTxt, replyMarkup: confKb,
                                    cancellationToken: cancellationToken);
                                await SavePendingTradeAsync(userId, tradeId, confMsg.MessageId, ready);
                                await UpdateRecentSettingsAsync(userId, ready, settings);
                                break;
                            }

                            // иначе показываем следующий шаг
                            var (nextTxt, nextKb) =
                                _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);
                            var nextMsg = await bot.SendMessage(chatId, nextTxt, replyMarkup: nextKb,
                                cancellationToken: cancellationToken);
                            state.MessageId = nextMsg.MessageId;
                            await SaveUserStateAsync(userId, state);
                            break;
                        }


                        case "adjust":
                            if (parts.Length >= 3 && parts[1] == "pnl" &&
                                decimal.TryParse(parts[2], out decimal adjustment))
                            {
                                state.Trade.PnL += adjustment;
                                var (adjustText, adjustKeyboard) =
                                    _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);
                                await bot.SendMessage(chatId, adjustText, replyMarkup: adjustKeyboard,
                                    cancellationToken: cancellationToken);
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
                await DeleteUserStateAsync(userId);
                return;
            }

            var parts = state.Action.Split('_');
            string field = parts.Length > 1 ? parts[1] : "";
            string tradeId = state.TradeId;

            if (string.IsNullOrEmpty(tradeId))
            {
                _logger.LogWarning($"TradeId is missing in state for UserId={userId}. Generating new TradeId.");
                tradeId = Guid.NewGuid().ToString();
                state.TradeId = tradeId;
                await SaveUserStateAsync(userId, state);
            }

            try
            {
                await bot.DeleteMessage(chatId, messageId, cancellationToken);
                await bot.DeleteMessage(chatId, state.MessageId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to delete message for UserId={userId}, MessageId={messageId}");
            }

            switch (field.ToLowerInvariant())
            {
                case "ticker":
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        state.Trade.Ticker = text.ToUpper();
                        state.Action = "trade_in_progress";
                        state.Step++;
                        state.ErrorCount = 0;
                        await UpdateRecentSettingsAsync(userId, state.Trade, settings);
                        var (textNext, keyboardNext) =
                            _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);
                        var sentMessage = await bot.SendMessage(chatId, textNext, replyMarkup: keyboardNext,
                            cancellationToken: cancellationToken);
                        state.MessageId = sentMessage.MessageId;
                        await SaveUserStateAsync(userId, state);
                    }
                    else
                    {
                        state.ErrorCount++;
                        (string retryText, InlineKeyboardMarkup retryKeyboard) = state.ErrorCount >= 3
                            ? ("⚠️ Слишком много ошибок ввода.\nВыберите действие:",
                                new InlineKeyboardMarkup(new[]
                                {
                                    new[] { InlineKeyboardButton.WithCallbackData("🔄 Начать сначала", "reset") },
                                    new[] { InlineKeyboardButton.WithCallbackData("❌ Отмена", "cancel") }
                                }))
                            : _uiManager.GetInputPrompt(field, settings, tradeId);
                        var errorMessage = await bot.SendMessage(chatId, retryText, replyMarkup: retryKeyboard,
                            cancellationToken: cancellationToken);
                        state.MessageId = errorMessage.MessageId;
                        await SaveUserStateAsync(userId, state);
                    }

                    break;

                case "pnl":
                    state.Trade.PnL = TryParseDecimal(text);
                    if (state.Trade.PnL == 0 && !text.Equals("0") && !text.Equals("0.0") && !text.Equals("0%"))
                    {
                        state.ErrorCount++;
                        (string retryText, InlineKeyboardMarkup retryKeyboard) = state.ErrorCount >= 3
                            ? ("⚠️ Неверный формат PnL.\nСлишком много ошибок:",
                                new InlineKeyboardMarkup(new[]
                                {
                                    new[] { InlineKeyboardButton.WithCallbackData("🔄 Начать сначала", "reset") },
                                    new[] { InlineKeyboardButton.WithCallbackData("❌ Отмена", "cancel") }
                                }))
                            : _uiManager.GetInputPrompt(field, settings, tradeId);
                        var errorMessage = await bot.SendMessage(chatId, retryText, replyMarkup: retryKeyboard,
                            cancellationToken: cancellationToken);
                        state.MessageId = errorMessage.MessageId;
                        await SaveUserStateAsync(userId, state);
                    }
                    else
                    {
                        state.Action = "trade_in_progress";
                        state.Step++;
                        state.ErrorCount = 0;
                        var (textNext, keyboardNext) =
                            _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);
                        var sentMessage = await bot.SendMessage(chatId, textNext, replyMarkup: keyboardNext,
                            cancellationToken: cancellationToken);
                        state.MessageId = sentMessage.MessageId;
                        await SaveUserStateAsync(userId, state);
                    }

                    break;

                case "open":
                case "close":
                case "sl":
                case "tp":
                case "volume":
                    decimal? v = TryParseNullableDecimal(text);
                    switch (field)
                    {
                        case "open": state.Trade.OpenPrice = v; break;
                        case "close": state.Trade.Entry = v; break;
                        case "sl": state.Trade.SL = v; break;
                        case "tp": state.Trade.TP = v; break;
                        case "volume": state.Trade.Volume = v; break;
                    }

                    state.Action = "trade_in_progress";
                    state.Step++;
                    state.ErrorCount = 0;
                    await UpdateRecentSettingsAsync(userId, state.Trade, settings);
                    if (state.Step <= 8)
                    {
                        var (nTxt, nKb) = _uiManager.GetTradeInputScreen(state.Trade, state.Step, settings, tradeId);
                        var nMsg = await bot.SendMessage(chatId, nTxt, replyMarkup: nKb,
                            cancellationToken: cancellationToken);
                        state.MessageId = nMsg.MessageId;
                        await SaveUserStateAsync(userId, state);
                    }
                    else
                    {
                        Trade ready = state.Trade;
                        await DeleteUserStateAsync(userId);
                        var (confText, confKb) = _uiManager.GetTradeConfirmationScreen(ready, tradeId, settings);
                        var confMsg = await bot.SendMessage(chatId, confText, replyMarkup: confKb,
                            cancellationToken: cancellationToken);
                        await SavePendingTradeAsync(userId, tradeId, confMsg.MessageId, ready);
                        await UpdateRecentSettingsAsync(userId, ready, settings);
                    }

                    break;

                case "comment":
                    state.Trade.Comment = string.IsNullOrWhiteSpace(text) ? "" : text;
                    state.Action = parts[0] + "_trade_" + tradeId;
                    state.Step++;
                    state.ErrorCount = 0;
                    await UpdateRecentSettingsAsync(userId, state.Trade, settings);
                    var trade = state.Trade;
                    await DeleteUserStateAsync(userId);
                    var (confirmText, confirmKeyboard) =
                        _uiManager.GetTradeConfirmationScreen(trade, tradeId, settings);
                    var confirmMessage = await bot.SendMessage(chatId, confirmText, replyMarkup: confirmKeyboard,
                        cancellationToken: cancellationToken);
                    await SavePendingTradeAsync(userId, tradeId, confirmMessage.MessageId, trade);
                    await UpdateRecentSettingsAsync(userId, trade, settings);
                    break;

                default:
                    await bot.SendMessage(chatId, "⚠️ Неверный ввод.", replyMarkup: _uiManager.GetMainMenu(settings),
                        cancellationToken: cancellationToken);
                    await DeleteUserStateAsync(userId);
                    break;
            }
        }

        private async Task HandleSettingsInputAsync(ITelegramBotClient bot, long chatId, long userId, UserState state,
            UserSettings settings, string text, CancellationToken cancellationToken)
        {
            var parts = state.Action.Split('_');
            string field = parts.Length > 1 ? parts[1] : "";

            try
            {
                await bot.DeleteMessage(chatId, state.MessageId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to delete settings prompt for UserId={userId}");
            }

            if (field == "ticker")
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    settings.FavoriteTickers.Add(text.ToUpper());
                    await SaveUserSettingsAsync(userId, settings);
                    var (menuText, menuKeyboard) = _uiManager.GetSettingsMenu(settings);
                    await bot.SendMessage(chatId, "✅ Настройки обновлены!", replyMarkup: menuKeyboard,
                        cancellationToken: cancellationToken);
                    await DeleteUserStateAsync(userId);
                }
                else
                {
                    var (promptText, promptKeyboard) = _uiManager.GetSettingsInputPrompt("ticker", settings);
                    var errorMessage = await bot.SendMessage(chatId, "⚠️ Введите корректный тикер.",
                        replyMarkup: promptKeyboard, cancellationToken: cancellationToken);
                    state.MessageId = errorMessage.MessageId;
                    await SaveUserStateAsync(userId, state);
                }
            }
        }

        private async Task UpdateRecentSettingsAsync(long userId, Trade trade, UserSettings settings)
        {
            if (!string.IsNullOrEmpty(trade.Ticker))
            {
                if (settings.RecentTickers.Contains(trade.Ticker))
                    settings.RecentTickers.Remove(trade.Ticker);
                settings.RecentTickers.Insert(0, trade.Ticker);
                if (settings.RecentTickers.Count > 5)
                    settings.RecentTickers.RemoveAt(settings.RecentTickers.Count - 1);
            }

            if (!string.IsNullOrEmpty(trade.Direction))
            {
                if (settings.RecentDirections.Contains(trade.Direction))
                    settings.RecentDirections.Remove(trade.Direction);
                settings.RecentDirections.Insert(0, trade.Direction);
                if (settings.RecentDirections.Count > 5)
                    settings.RecentDirections.RemoveAt(settings.RecentDirections.Count - 1);
            }

            if (!string.IsNullOrEmpty(trade.Comment))
            {
                if (settings.RecentComments.Contains(trade.Comment))
                    settings.RecentComments.Remove(trade.Comment);
                settings.RecentComments.Insert(0, trade.Comment);
                if (settings.RecentComments.Count > 5)
                    settings.RecentComments.RemoveAt(settings.RecentComments.Count - 1);
            }

            await SaveUserSettingsAsync(userId, settings);
        }

        // ───────── UpdateHandler.cs  (замените ВЕСЬ метод SaveTradeAsync) ───────────
        private async Task SaveTradeAsync(
            Trade trade,
            long chatId,
            long userId,
            ITelegramBotClient bot,
            UserSettings settings,
            CancellationToken ct)
        {
            _logger.LogInformation($"💾 Saving trade for UserId={userId}: {trade.Ticker}, PnL={trade.PnL}");

            /* 1. сохраняем в БД */
            await _repo.AddTradeAsync(trade);
            await UpdateRecentSettingsAsync(userId, trade, settings);

            /* 2. готовим единое сообщение */
            string baseText = _uiManager.GetText("trade_saved", settings.Language,
                trade.Ticker, trade.PnL);
            var mainKb = _uiManager.GetMainMenu(settings);
            var sentMsg = await bot.SendMessage(chatId, baseText,
                replyMarkup: mainKb, cancellationToken: ct);

            /* 3. пробуем отправить в Notion и редактируем то же сообщение */
            try
            {
                string pageId = await _notionService.CreatePageForTradeAsync(trade);
                if (!string.IsNullOrEmpty(pageId))
                {
                    trade.NotionPageId = pageId;
                    await _repo.UpdateTradeAsync(trade);
                    await bot.EditMessageText(chatId, sentMsg.MessageId,
                        baseText + "\n\n📝 Отправлено в Notion ✅", replyMarkup: mainKb, cancellationToken: ct);
                }
                else
                {
                    await bot.EditMessageText(chatId, sentMsg.MessageId,
                        baseText + "\n\n⚠️ Не удалось отправить в Notion (пустой PageId)",
                        replyMarkup: mainKb, cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Notion error for UserId={userId}");
                await bot.EditMessageText(chatId, sentMsg.MessageId,
                    baseText + "\n\n💾 " + _uiManager.GetText("trade_saved_local", settings.Language),
                    replyMarkup: mainKb, cancellationToken: ct);
            }

            /* 4. streak‑уведомление отдельным сообщением БЕЗ клавиатуры */
            if (settings.NotificationsEnabled)
            {
                int streak = await CalculateStreakAsync(userId);
                if (streak >= 3)
                {
                    string streakMsg = trade.PnL > 0
                        ? _uiManager.GetText("win_streak", settings.Language, streak)
                        : _uiManager.GetText("loss_streak", settings.Language, streak);

                    await bot.SendMessage(chatId, streakMsg,
                        replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
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
                /* Ignore if message is already deleted */
            }
        }
    }
}
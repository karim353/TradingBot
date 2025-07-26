using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using TradingBot.Models;
using TradingBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Logging;
using ScottPlot;

namespace TradingBot.Services
{
    public class UpdateHandler
    {
        private readonly TradeRepository _repo;
        private readonly PnLService _pnlService;
        private readonly NotionService _notionService;
        private readonly ILogger<UpdateHandler> _logger;

        private class PendingTradeState { public Trade Trade { get; set; } }
        private readonly Dictionary<long, PendingTradeState> _pendingTrades = new();

        private class LogState { public Trade Trade { get; set; } public int Step { get; set; } }
        private readonly Dictionary<long, LogState> _logStates = new();

        private static readonly string[] StepsQuestions = new[]
        {
            "Тикер:",
            "Направление (Long/Short):",
            "PnL (прибыль или убыток, например, +552.15):",
            "Open Price (Avg. Open Price):",
            "Close Price (Entry):",
            "Stop Loss (SL):",
            "Take Profit (TP):",
            "Объём (Volume):",
            "Комментарий (необязательно):"
        };

        public UpdateHandler(TradeRepository repo, PnLService pnlService, NotionService notionService, ILogger<UpdateHandler> logger)
        {
            _repo = repo;
            _pnlService = pnlService;
            _notionService = notionService;
            _logger = logger;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            long chatId = 0;
            try
            {
                
                string text = "";

                if (update.Type == UpdateType.Message && update.Message != null)
                {
                    var message = update.Message;
                    chatId = message.Chat.Id;
                    text = message.Text?.Trim() ?? "";

                    if (_logStates.ContainsKey(chatId))
                    {
                        var state = _logStates[chatId];
                        var trade = state.Trade;

                        switch (state.Step)
                        {
                            case 1: if (!string.IsNullOrWhiteSpace(text)) trade.Ticker = text; break;
                            case 2: if (!string.IsNullOrWhiteSpace(text)) trade.Direction = text; break;
                            case 3: trade.PnL = TryParseDecimal(text); break;
                            case 4: trade.OpenPrice = TryParseNullableDecimal(text); break;
                            case 5: trade.Entry = TryParseNullableDecimal(text); break;
                            case 6: trade.SL = TryParseNullableDecimal(text); break;
                            case 7: trade.TP = TryParseNullableDecimal(text); break;
                            case 8: trade.Volume = TryParseNullableDecimal(text); break;
                            case 9: trade.Comment = string.IsNullOrWhiteSpace(text) ? "" : text; break;
                        }

                        state.Step++;
                        if (state.Step <= StepsQuestions.Length)
                        {
                            string current = GetTradePreview(trade, state.Step);
                            await bot.SendMessage(chatId, StepsQuestions[state.Step - 1] + "\n" + current, cancellationToken: cancellationToken);
                        }
                        else
                        {
                            _logStates.Remove(chatId);
                            await SaveTradeAsync(trade, chatId, bot, cancellationToken);
                        }
                        return;
                    }

                    if (text.StartsWith("/"))
                    {
                        if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
                        {
                            string welcome = "Привет! Этот бот поможет логировать сделки. Отправьте скриншот PnL или используйте /log для ручного ввода.";
                            await bot.SendMessage(chatId, welcome, cancellationToken: cancellationToken);
                            return;
                        }
                        if (text.Equals("/log", StringComparison.OrdinalIgnoreCase))
                        {
                            var newTrade = new Trade { ChatId = chatId, Date = DateTime.Now };
                            _logStates[chatId] = new LogState { Trade = newTrade, Step = 1 };
                            await bot.SendMessage(chatId, StepsQuestions[0], cancellationToken: cancellationToken);
                            return;
                        }
                        if (text.Equals("/cancel", StringComparison.OrdinalIgnoreCase))
                        {
                            if (_logStates.ContainsKey(chatId))
                            {
                                _logStates.Remove(chatId);
                                await bot.SendMessage(chatId, "Ввод сделки отменен.", cancellationToken: cancellationToken);
                            }
                            else
                            {
                                await bot.SendMessage(chatId, "Нет активного ввода сделки.", cancellationToken: cancellationToken);
                            }
                            return;
                        }
                        if (text.Equals("/menu", StringComparison.OrdinalIgnoreCase))
                        {
                            var keyboard = new InlineKeyboardMarkup(new[]
                            {
                                new []
                                {
                                    InlineKeyboardButton.WithCallbackData("📊 Stats", "stats"),
                                    InlineKeyboardButton.WithCallbackData("📈 Advanced Stats", "advstats")
                                },
                                new []
                                {
                                    InlineKeyboardButton.WithCallbackData("🗓 Summary Week", "summary_week"),
                                    InlineKeyboardButton.WithCallbackData("📅 Summary Month", "summary_month")
                                },
                                new []
                                {
                                    InlineKeyboardButton.WithCallbackData("❌ Delete Last Trade", "delete_last")
                                }
                            });
                            await bot.SendMessage(chatId, "Меню:", replyMarkup: keyboard, cancellationToken: cancellationToken);
                            return;
                        }
                        if (text.Equals("/stats", StringComparison.OrdinalIgnoreCase))
                        {
                            var trades = await _repo.GetTradesAsync(chatId);
                            int totalTrades = trades.Count;
                            int wins = trades.Count(t => t.PnL > 0);
                            int losses = trades.Count(t => t.PnL < 0);
                            decimal totalPnl = trades.Sum(t => t.PnL);
                            string stats = $"Всего сделок: {totalTrades}\n" +
                                           $"Общий PnL: {totalPnl}\n" +
                                           $"Прибыльных: {wins}, Убыточных: {losses}\n" +
                                           $"Win Rate: {(totalTrades > 0 ? (wins * 100 / totalTrades) : 0)}%";
                            await bot.SendMessage(chatId, stats, cancellationToken: cancellationToken);
                            return;
                        }
                        if (text.Equals("/advstats", StringComparison.OrdinalIgnoreCase))
                        {
                            await GenerateEquityCurveAsync(chatId, bot, cancellationToken);
                            return;
                        }
                        if (text.StartsWith("/summary", StringComparison.OrdinalIgnoreCase))
                        {
                            string[] parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            string period = parts.Length > 1 ? parts[1].ToLower() : "";
                            DateTime from;
                            DateTime to = DateTime.Now;
                            if (period == "week") from = DateTime.Now.AddDays(-7);
                            else if (period == "month") from = DateTime.Now.AddDays(-30);
                            else
                            {
                                await bot.SendMessage(chatId, "Укажите период: /summary week | month", cancellationToken: cancellationToken);
                                return;
                            }
                            var trades = await _repo.GetTradesInDateRangeAsync(chatId, from, to);
                            if (trades.Count == 0)
                            {
                                await bot.SendMessage(chatId, "Сделок за выбранный период нет.", cancellationToken: cancellationToken);
                            }
                            else
                            {
                                decimal total = trades.Sum(t => t.PnL);
                                string summary = $"Период: {period}\n" +
                                                 $"Сделок: {trades.Count}\n" +
                                                 $"Суммарный PnL: {total}";
                                await bot.SendMessage(chatId, summary, cancellationToken: cancellationToken);
                            }
                            return;
                        }
                        if (text.StartsWith("/pnl", StringComparison.OrdinalIgnoreCase))
                        {
                            string[] parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length < 3)
                            {
                                await bot.SendMessage(chatId, "Формат: /pnl YYYY-MM-DD YYYY-MM-DD", cancellationToken: cancellationToken);
                                return;
                            }
                            if (!DateTime.TryParse(parts[1], out DateTime fromDate) || !DateTime.TryParse(parts[2], out DateTime toDate))
                            {
                                await bot.SendMessage(chatId, "Неверный формат дат. Пример: /pnl 2025-01-01 2025-01-31", cancellationToken: cancellationToken);
                                return;
                            }
                            if (fromDate > toDate) (fromDate, toDate) = (toDate, fromDate);
                            var trades = await _repo.GetTradesInDateRangeAsync(chatId, fromDate, toDate);
                            if (trades.Count == 0)
                            {
                                await bot.SendMessage(chatId, "Сделок за указанный период не найдено.", cancellationToken: cancellationToken);
                            }
                            else
                            {
                                decimal total = trades.Sum(t => t.PnL);
                                string result = $"PnL с {fromDate:yyyy-MM-dd} по {toDate:yyyy-MM-dd}: {total}";
                                await bot.SendMessage(chatId, result, cancellationToken: cancellationToken);
                            }
                            return;
                        }
                        if (text.Equals("/last", StringComparison.OrdinalIgnoreCase))
                        {
                            var lastTrade = await _repo.GetLastTradeAsync(chatId);
                            if (lastTrade == null)
                            {
                                await bot.SendMessage(chatId, "Последняя сделка не найдена.", cancellationToken: cancellationToken);
                            }
                            else
                            {
                                string tradeInfo = GetTradePreview(lastTrade, 0, showAll: true);
                                await bot.SendMessage(chatId, tradeInfo, cancellationToken: cancellationToken);
                            }
                            return;
                        }
                        if (text.Equals("/undo", StringComparison.OrdinalIgnoreCase) ||
                            text.Equals("/deletelast", StringComparison.OrdinalIgnoreCase) ||
                            text.Equals("/delete_last", StringComparison.OrdinalIgnoreCase))
                        {
                            var lastTrade = await _repo.GetLastTradeAsync(chatId);
                            if (lastTrade == null)
                            {
                                await bot.SendMessage(chatId, "Удалять нечего (нет сохранённых сделок).", cancellationToken: cancellationToken);
                            }
                            else
                            {
                                await _repo.DeleteTradeAsync(lastTrade);
                                await bot.SendMessage(chatId, $"Последняя сделка ({lastTrade.Ticker}) удалена.", cancellationToken: cancellationToken);
                            }
                            return;
                        }
                    }

                    if (message.Type == MessageType.Photo)
                    {
                        var photos = message.Photo;
                        if (photos?.Length > 0)
                        {
                            var fileId = photos[^1].FileId;
                            try
                            {
                                var fileInfo = await bot.GetFile(fileId, cancellationToken);
                                if (fileInfo.FilePath == null)
                                {
                                    await bot.SendMessage(chatId, "Не удалось получить файл.", cancellationToken: cancellationToken);
                                    return;
                                }
                                using var stream = new MemoryStream();
                                await bot.DownloadFile(fileInfo.FilePath, stream, cancellationToken);
                                stream.Position = 0;

                                PnLData data = _pnlService.ExtractFromImage(stream);
                                if (string.IsNullOrEmpty(data.Ticker) || string.IsNullOrEmpty(data.Direction))
                                {
                                    await bot.SendMessage(chatId, "Не удалось распознать данные. Попробуйте ввести вручную (/log).", cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    var trade = new Trade
                                    {
                                        ChatId = chatId,
                                        Date = data.TradeDate ?? DateTime.Now,
                                        Ticker = data.Ticker,
                                        Direction = data.Direction,
                                        PnL = data.PnLPercent ?? 0,
                                        OpenPrice = data.Open,
                                        Entry = data.Close,
                                        SL = null,
                                        TP = null,
                                        Volume = null,
                                        Comment = null
                                    };
                                    _pendingTrades[chatId] = new PendingTradeState { Trade = trade };

                                    string preview = GetTradePreview(trade, 0, showAll: true);
                                    var keyboard = new InlineKeyboardMarkup(new[]
                                    {
                                        new []
                                        {
                                            InlineKeyboardButton.WithCallbackData("✅ Всё верно", "confirm_trade"),
                                            InlineKeyboardButton.WithCallbackData("✏️ Редактировать", "edit_trade")
                                        }
                                    });
                                    await bot.SendMessage(chatId, preview + "\nВсё правильно?", replyMarkup: keyboard, cancellationToken: cancellationToken);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Ошибка при обработке изображения.");
                                await bot.SendMessage(chatId, "Ошибка при обработке изображения.", cancellationToken: cancellationToken);
                            }
                        }
                        return;
                    }
                }
                else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
                {
                    var callback = update.CallbackQuery;
                    chatId = callback.Message.Chat.Id;
                    string data = callback.Data;
                    await bot.AnswerCallbackQuery(callback.Id, cancellationToken: cancellationToken);

                    if (_pendingTrades.ContainsKey(chatId))
                    {
                        if (data == "confirm_trade")
                        {
                            var pendingTrade = _pendingTrades[chatId].Trade;
                            _pendingTrades.Remove(chatId);
                            await SaveTradeAsync(pendingTrade, chatId, bot, cancellationToken);
                            return;
                        }
                        else if (data == "edit_trade")
                        {
                            var trade = _pendingTrades[chatId].Trade;
                            _pendingTrades.Remove(chatId);
                            _logStates[chatId] = new LogState { Trade = trade, Step = 1 };
                            string current = GetTradePreview(trade, 1);
                            await bot.SendMessage(chatId, StepsQuestions[0] + "\n" + current, cancellationToken: cancellationToken);
                            return;
                        }
                    }

                    if (data == "stats")
                    {
                        var trades = await _repo.GetTradesAsync(chatId);
                        int totalTrades = trades.Count;
                        int wins = trades.Count(t => t.PnL > 0);
                        int losses = trades.Count(t => t.PnL < 0);
                        decimal totalPnl = trades.Sum(t => t.PnL);
                        string stats = $"Всего сделок: {totalTrades}\n" +
                                       $"Общий PnL: {totalPnl}\n" +
                                       $"Прибыльных: {wins}, Убыточных: {losses}\n" +
                                       $"Win Rate: {(totalTrades > 0 ? (wins * 100 / totalTrades) : 0)}%";
                        await bot.SendMessage(chatId, stats, cancellationToken: cancellationToken);
                    }
                    else if (data == "advstats")
                    {
                        await GenerateEquityCurveAsync(chatId, bot, cancellationToken);
                    }
                    else if (data == "summary_week" || data == "summary_month")
                    {
                        DateTime from = data == "summary_week" ? DateTime.Now.AddDays(-7) : DateTime.Now.AddDays(-30);
                        var trades = await _repo.GetTradesInDateRangeAsync(chatId, from, DateTime.Now);
                        if (trades.Count == 0)
                        {
                            await bot.SendMessage(chatId, "Нет сделок за выбранный период.", cancellationToken: cancellationToken);
                        }
                        else
                        {
                            decimal total = trades.Sum(t => t.PnL);
                            string periodName = data == "summary_week" ? "неделю" : "месяц";
                            string summary = $"За последнюю {periodName}:\n" +
                                             $"Сделок: {trades.Count}\n" +
                                             $"Суммарный PnL: {total}";
                            await bot.SendMessage(chatId, summary, cancellationToken: cancellationToken);
                        }
                    }
                    else if (data == "delete_last")
                    {
                        var lastTrade = await _repo.GetLastTradeAsync(chatId);
                        if (lastTrade == null)
                        {
                            await bot.SendMessage(chatId, "Последняя сделка отсутствует.", cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await _repo.DeleteTradeAsync(lastTrade);
                            await bot.SendMessage(chatId, "Последняя сделка удалена.", cancellationToken: cancellationToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке обновления.");
                if (update.Type == UpdateType.Message && update.Message != null)
                {
                    chatId = update.Message.Chat.Id;
                    await bot.SendMessage(chatId, "Произошла ошибка при обработке запроса.", cancellationToken: cancellationToken);
                }
            }
        }

        private async Task SaveTradeAsync(Trade trade, long chatId, ITelegramBotClient bot, CancellationToken cancellationToken)
        {
            await _repo.AddTradeAsync(trade);
            try
            {
                string notionPageId = await _notionService.CreatePageForTradeAsync(trade);
                trade.NotionPageId = notionPageId;
                await _repo.UpdateTradeAsync(trade);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось отправить сделку в Notion.");
                await bot.SendMessage(chatId, "Сделка сохранена локально, но не отправлена в Notion.", cancellationToken: cancellationToken);
            }
            await bot.SendMessage(chatId, $"Сделка {trade.Ticker} ({trade.Direction}) PnL={trade.PnL} сохранена.", cancellationToken: cancellationToken);
        }

        private static decimal TryParseDecimal(string text)
        {
            var match = Regex.Match(text, @"([+-]?\d{1,10}(?:[.,]\d{1,4})?)");
            if (match.Success)
            {
                string numStr = match.Groups[1].Value.Replace(",", ".");
                if (decimal.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                    return result;
            }
            return 0;
        }

        private static decimal? TryParseNullableDecimal(string text)
        {
            if (string.IsNullOrEmpty(text) || text == "-") return null;
            var match = Regex.Match(text, @"([+-]?\d{1,10}(?:[.,]\d{1,4})?)");
            if (match.Success)
            {
                string numStr = match.Groups[1].Value.Replace(",", ".");
                if (decimal.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                    return val;
            }
            return null;
        }

        private static string GetTradePreview(Trade t, int step, bool showAll = false)
        {
            string[] lines = new[]
            {
                $"Тикер: {t.Ticker ?? ""}",
                $"Направление: {t.Direction ?? ""}",
                $"PnL: {t.PnL}%",
                $"Open Price: {t.OpenPrice?.ToString() ?? ""}",
                $"Close Price (Entry): {t.Entry?.ToString() ?? ""}",
                $"Stop Loss (SL): {t.SL?.ToString() ?? ""}",
                $"Take Profit (TP): {t.TP?.ToString() ?? ""}",
                $"Объём: {t.Volume?.ToString() ?? ""}",
                $"Комментарий: {t.Comment ?? ""}"
            };
            if (showAll) return string.Join("\n", lines);

            var preview = "";
            for (int i = 0; i < lines.Length; i++)
                preview += (i == step - 1 ? "➡️ " : "") + lines[i] + "\n";
            return preview;
        }

        private async Task GenerateEquityCurveAsync(long chatId, ITelegramBotClient bot, CancellationToken cancellationToken)
        {
            var trades = await _repo.GetTradesAsync(chatId);
            if (trades.Count == 0)
            {
                await bot.SendMessage(chatId, "Нет данных по сделкам.", cancellationToken: cancellationToken);
                return;
            }

            decimal total = trades.Sum(t => t.PnL);
            decimal best = trades.Max(t => t.PnL);
            decimal worst = trades.Min(t => t.PnL);
            decimal average = trades.Count > 0 ? total / trades.Count : 0;
            int wins = trades.Count(t => t.PnL > 0);
            string advStats = $"Сделок: {trades.Count}\n" +
                              $"Общий PnL: {total}\n" +
                              $"Средний PnL: {Math.Round(average, 2)}\n" +
                              $"Лучший результат: {best}\n" +
                              $"Худший результат: {worst}\n" +
                              $"Win Rate: {(trades.Count > 0 ? (wins * 100 / trades.Count) : 0)}%";

            trades.Sort((a, b) => a.Date.CompareTo(b.Date));
            double cumulative = 0;
            double[] xs = new double[trades.Count];
            double[] ys = new double[trades.Count];
            for (int i = 0; i < trades.Count; i++)
            {
                cumulative += (double)trades[i].PnL;
                xs[i] = trades[i].Date.ToOADate();
                ys[i] = cumulative;
            }

            var plt = new ScottPlot.Plot();
            var scatter = plt.Add.Scatter(xs, ys);
            scatter.MarkerSize = 0;
            plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.DateTimeAutomatic();
            plt.XLabel("Date");
            plt.YLabel("Cumulative PnL");
            plt.Title("Equity Curve");

            var image = plt.GetImage(600, 400);
            var bytes = image.GetImageBytes(ScottPlot.ImageFormat.Png);
            using var ms = new MemoryStream(bytes);
            await bot.SendMessage(chatId, advStats, cancellationToken: cancellationToken);
            await bot.SendPhoto(chatId, new InputFileStream(ms, "equity.png"), caption: "Equity curve", cancellationToken: cancellationToken);
        }
    }
}

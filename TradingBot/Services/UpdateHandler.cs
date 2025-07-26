using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
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
    /// <summary>
    /// Обработчик входящих обновлений (сообщений, команд, callback) из Telegram.
    /// </summary>
    public class UpdateHandler
    {
        private readonly TradeRepository _repo;
        private readonly PnLService _pnlService;
        private readonly NotionService _notionService;
        private readonly ILogger<UpdateHandler> _logger;

        // Состояние пошагового ввода для команды /log (по пользователям):
        private class LogState
        {
            public Trade Trade { get; set; }
            public int Step { get; set; }
        }
        private readonly Dictionary<long, LogState> _logStates = new Dictionary<long, LogState>();

        public UpdateHandler(TradeRepository repo, PnLService pnlService, NotionService notionService, ILogger<UpdateHandler> logger)
        {
            _repo = repo;
            _pnlService = pnlService;
            _notionService = notionService;
            _logger = logger;
        }

        /// <summary>
        /// Основной метод обработки обновления от Telegram.
        /// </summary>
        public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.Message && update.Message != null)
                {
                    var message = update.Message;
                    long chatId = message.Chat.Id;

                    // Обработка текстовых сообщений (включая команды)
                    if (message.Type == MessageType.Text)
                    {
                        string text = message.Text?.Trim() ?? string.Empty;

                        // Если это команда (начинается с '/')
                        if (text.StartsWith("/"))
                        {
                            // Если во время пошагового ввода пришла новая команда (не /log и не /cancel) – сбрасываем состояние ввода
                            if (_logStates.ContainsKey(chatId) && text != "/cancel" && !text.StartsWith("/log"))
                            {
                                _logStates.Remove(chatId);
                            }

                            if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
                            {
                                string welcome = "Привет! Этот бот поможет логировать сделки. Отправьте скриншот PnL или используйте /log для ручного ввода.";
                                await bot.SendMessage(chatId, welcome, cancellationToken: cancellationToken);
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

                            if (text.Equals("/log", StringComparison.OrdinalIgnoreCase))
                            {
                                var newTrade = new Trade
                                {
                                    ChatId = chatId,
                                    Date = DateTime.Now
                                };
                                _logStates[chatId] = new LogState { Trade = newTrade, Step = 1 };
                                await bot.SendMessage(chatId, "Логирование новой сделки. Введите тикер:", cancellationToken: cancellationToken);
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
                                scatter.MarkerSize = 0; // Скрываем маркеры
                                plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.DateTimeAutomatic();
                                plt.XLabel("Date");
                                plt.YLabel("Cumulative PnL");
                                plt.Title("Equity Curve");

                                var image = plt.GetImage(600, 400);
                                var bytes = image.GetImageBytes(ScottPlot.ImageFormat.Png);
                                using var ms = new MemoryStream(bytes);
                                await bot.SendMessage(chatId, advStats, cancellationToken: cancellationToken);
                                await bot.SendPhoto(chatId, new InputFileStream(ms, "equity.png"), caption: "Equity curve", cancellationToken: cancellationToken);
                                return;
                            }

                            if (text.StartsWith("/summary", StringComparison.OrdinalIgnoreCase))
                            {
                                string[] parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                string period = parts.Length > 1 ? parts[1].ToLower() : string.Empty;
                                DateTime from;
                                DateTime to = DateTime.Now;
                                if (period == "week")
                                {
                                    from = DateTime.Now.AddDays(-7);
                                }
                                else if (period == "month")
                                {
                                    from = DateTime.Now.AddDays(-30);
                                }
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
                                if (fromDate > toDate)
                                {
                                    (fromDate, toDate) = (toDate, fromDate);
                                }
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
                                    string tradeInfo =
                                        $"Дата: {lastTrade.Date}\n" +
                                        $"Тикер: {lastTrade.Ticker}\n" +
                                        $"Направление: {lastTrade.Direction}\n" +
                                        $"PnL: {lastTrade.PnL}\n" +
                                        $"Entry: {lastTrade.Entry}\n" +
                                        $"SL: {lastTrade.SL}\n" +
                                        $"TP: {lastTrade.TP}\n" +
                                        $"Объём: {lastTrade.Volume}\n" +
                                        $"Комментарий: {lastTrade.Comment}";
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
                                    await bot.SendMessage(chatId, $"Последняя сделка ({lastTrade.Ticker}) удалена из базы.", cancellationToken: cancellationToken);
                                }
                                return;
                            }
                        }

                        // Обработка пошагового ручного ввода сделки (/log)
                        if (_logStates.ContainsKey(chatId))
                        {
                            var state = _logStates[chatId];
                            switch (state.Step)
                            {
                                case 1:
                                    state.Trade.Ticker = text;
                                    state.Step++;
                                    await bot.SendMessage(chatId, "Направление (Long/Short):", cancellationToken: cancellationToken);
                                    break;
                                case 2:
                                    state.Trade.Direction = text;
                                    state.Step++;
                                    await bot.SendMessage(chatId, "PnL (прибыль или убыток):", cancellationToken: cancellationToken);
                                    break;
                                case 3:
                                    state.Trade.PnL = decimal.TryParse(text, out decimal pnl) ? pnl : 0;
                                    state.Step++;
                                    await bot.SendMessage(chatId, "Entry price (цена входа, опционально):", cancellationToken: cancellationToken);
                                    break;
                                case 4:
                                    state.Trade.Entry = string.IsNullOrEmpty(text) || text == "-" ? null : decimal.TryParse(text, out decimal entryVal) ? entryVal : null;
                                    state.Step++;
                                    await bot.SendMessage(chatId, "Stop Loss (опционально):", cancellationToken: cancellationToken);
                                    break;
                                case 5:
                                    state.Trade.SL = string.IsNullOrEmpty(text) || text == "-" ? null : decimal.TryParse(text, out decimal slVal) ? slVal : null;
                                    state.Step++;
                                    await bot.SendMessage(chatId, "Take Profit (опционально):", cancellationToken: cancellationToken);
                                    break;
                                case 6:
                                    state.Trade.TP = string.IsNullOrEmpty(text) || text == "-" ? null : decimal.TryParse(text, out decimal tpVal) ? tpVal : null;
                                    state.Step++;
                                    await bot.SendMessage(chatId, "Объём (количество, опционально):", cancellationToken: cancellationToken);
                                    break;
                                case 7:
                                    state.Trade.Volume = string.IsNullOrEmpty(text) || text == "-" ? null : decimal.TryParse(text, out decimal volVal) ? volVal : null;
                                    state.Step++;
                                    await bot.SendMessage(chatId, "Комментарий (опционально):", cancellationToken: cancellationToken);
                                    break;
                                case 8:
                                    state.Trade.Comment = text;
                                    var completedTrade = state.Trade;
                                    _logStates.Remove(chatId);

                                    await _repo.AddTradeAsync(completedTrade);
                                    try
                                    {
                                        string notionPageId = await _notionService.CreatePageForTradeAsync(completedTrade);
                                        completedTrade.NotionPageId = notionPageId;
                                        await _repo.UpdateTradeAsync(completedTrade);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Не удалось отправить сделку в Notion.");
                                        await bot.SendMessage(chatId, "Сделка сохранена локально, но не отправлена в Notion (ошибка API).", cancellationToken: cancellationToken);
                                    }
                                    await bot.SendMessage(chatId, $"Сделка {completedTrade.Ticker} ({completedTrade.Direction}) PnL={completedTrade.PnL} сохранена.", cancellationToken: cancellationToken);
                                    break;
                            }
                            return;
                        }
                    }
                    // Обработка изображений (скриншотов)
                    else if (message.Type == MessageType.Photo)
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
                                    await bot.SendMessage(chatId, "Не удалось распознать данные на скриншоте. Попробуйте ввести сделку вручную.", cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    var trade = new Trade
                                    {
                                        ChatId = chatId,
                                        Date = DateTime.Now,
                                        Ticker = data.Ticker,
                                        Direction = data.Direction,
                                        PnL = data.PnL
                                    };
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
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Ошибка при обработке изображения.");
                                await bot.SendMessage(chatId, "Произошла ошибка при обработке изображения.", cancellationToken: cancellationToken);
                            }
                        }
                    }
                }
                else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
                {
                    var callback = update.CallbackQuery;
                    long chatId = callback.Message.Chat.Id;
                    string data = callback.Data;
                    await bot.AnswerCallbackQuery(callback.Id, cancellationToken: cancellationToken);

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
                    else if (data == "advstats") {
                    var trades = await _repo.GetTradesAsync(chatId);
                    if (trades.Count == 0)
                    {
                        await bot.SendMessage(chatId, "Нет данных по сделкам.", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        decimal total = trades.Sum(t => t.PnL);
                        decimal best = trades.Max(t => t.PnL);
                        decimal worst = trades.Min(t => t.PnL);
                        decimal average = trades.Count > 0 ? total / trades.Count : 0;
                        int wins = trades.Count(t => t.PnL > 0);
                        string advStats = $"Сделок: {trades.Count}\n" +
                                          $"Общий PnL: {total}\n" +
                                          $"Средний PnL: {Math.Round(average, 2)}\n" +
                                          $"Лучший: {best}\n" +
                                          $"Худший: {worst}\n" +
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
                        scatter.MarkerSize = 0; // Скрываем маркеры
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
                    long chatId = update.Message.Chat.Id;
                    await bot.SendMessage(chatId, "Произошла ошибка при обработке вашего запроса.", cancellationToken: cancellationToken);
                }
            }
        }
    }
}
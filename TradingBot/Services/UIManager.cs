using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types.ReplyMarkups;
using TradingBot.Models;

namespace TradingBot.Services
{
    public class UIManager
    {
        // Локализованные ресурсы интерфейса (русский и английский)
        private readonly Dictionary<string, Dictionary<string, string>> _resources = new()
        {
            ["ru"] = new Dictionary<string, string>
            {
                ["welcome"] = "Добро пожаловать в TradingBot 2.0!\nЯ помогу вам вести учёт сделок.\nНажмите 'Далее' для обучения.",
                ["onboarding_1"] = "📥 Вы можете добавлять сделки через скриншоты или вручную.",
                ["onboarding_2"] = "📊 Просматривайте статистику и графики эквити.",
                ["onboarding_3"] = "⚙ Настраивайте бота под себя (язык, тикеры, уведомления).",
                ["main_menu"] = "Выберите действие:",
                ["please_use_buttons"] = "Пожалуйста, используйте кнопки для управления ботом.",
                ["error_occurred"] = "Произошла ошибка. Попробуйте снова или вернитесь в меню.",
                ["trade_cancelled"] = "Ввод сделки отменён.",
                ["trade_saved"] = "Сделка {0} (PnL={1}%) сохранена.",
                ["trade_saved_local"] = "Сделка сохранена локально, но не отправлена в Notion.",
                ["error_saving_trade"] = "Ошибка при сохранении сделки.",
                ["trade_expired"] = "Сделка устарела или не найдена. Попробуйте снова.",
                ["trade_deleted"] = "Сделка удалена.",
                ["all_pending_cleared"] = "Все активные сделки очищены.",
                ["no_trades"] = "Нет данных по сделкам за выбранный период.",
                ["invalid_input"] = "Некорректный ввод. Попробуйте снова.",
                ["invalid_pnl"] = "Введите корректное число для PnL (например, +5.25).",
                ["step_1"] = "🟩⬜⬜⬜⬜⬜⬜⬜ Шаг 1/9: Выберите тикер",
                ["step_2"] = "🟩🟩⬜⬜⬜⬜⬜⬜ Шаг 2/9: Выберите направление",
                ["step_3"] = "🟩🟩🟩⬜⬜⬜⬜⬜ Шаг 3/9: Введите PnL",
                ["step_4"] = "🟩🟩🟩🟩⬜⬜⬜⬜ Шаг 4/9: Введите Open Price",
                ["step_5"] = "🟩🟩🟩🟩🟩⬜⬜⬜ Шаг 5/9: Введите Close Price",
                ["step_6"] = "🟩🟩🟩🟩🟩🟩⬜⬜ Шаг 6/9: Введите Stop Loss",
                ["step_7"] = "🟩🟩🟩🟩🟩🟩🟩⬜ Шаг 7/9: Введите Take Profit",
                ["step_8"] = "🟩🟩🟩🟩🟩🟩🟩🟩 Шаг 8/9: Введите Volume",
                ["step_9"] = "🟩🟩🟩🟩🟩🟩🟩🟩🟩 Шаг 9/9: Введите комментарий",
                ["trade_preview"] = "Текущая сделка:\nТикер: {0}\nНаправление: {1}\nPnL: {2}%\nOpen Price: {3}\nClose Price: {4}\nSL: {5}\nTP: {6}\nVolume: {7}\nКомментарий: {8}",
                ["confirm_trade"] = "Всё верно?",
                ["edit_field"] = "Выберите поле для редактирования:",
                ["pending_trades"] = "Активные сделки:\n{0}",
                ["no_pending_trades"] = "Нет активных сделок.",
                ["stats_menu"] = "Выберите период для статистики:",
                ["stats_result"] = "Статистика {0}:\nВсего сделок: {1}\nОбщий PnL: {2}%\nПрибыльных: {3}\nУбыточных: {4}\nWin Rate: {5}%",
                ["advanced_stats"] = "Сделок: {0}\nОбщий PnL: {1}%\nСредний PnL: {2}%\nЛучший: {3}%\nХудший: {4}%\nWin Rate: {5}%",
                ["date_label"] = "Дата",
                ["pnl_label"] = "Накопленный PnL",
                ["equity_curve"] = "Кривая эквити",
                ["error_graph"] = "Ошибка при создании графика.",
                ["settings_menu"] = "Настройки:",
                ["settings_updated"] = "Настройки обновлены.",
                ["settings_reset"] = "Настройки сброшены.",
                ["help_menu"] = "Помощь:\nВыберите раздел",
                ["support"] = "Связаться с поддержкой",
                ["whats_new"] = "Что нового?",
                ["export_success"] = "Сделки экспортированы в CSV.",
                ["win_streak"] = "🔥 {0} прибыльных сделок подряд!",
                ["loss_streak"] = "⚠️ {0} убыточных сделок подряд!",
                ["back"] = "◀ Назад",
                ["cancel"] = "❌ Отменить",
                ["main_menu_button"] = "🏠 В меню",
                ["next"] = "▶ Далее",
                ["skip"] = "➡ Пропустить",
                ["confirm"] = "✅ Подтвердить",
                ["edit"] = "✏️ Редактировать",
                ["delete"] = "🗑 Удалить",
                ["retry"] = "🔄 Повторить",
                ["other"] = "Другое",
                ["input_manually"] = "⌨ Ввести вручную",
                ["prefill_last"] = "↺ Предзаполнить из последней",
                ["period_week"] = "за неделю",
                ["period_month"] = "за месяц",
                ["period_all"] = "за всё время",
                ["input_ticker"] = "Введите тикер (например, BTCUSDT):",
                ["input_pnl"] = "Введите PnL (например, +5.25 или -3.1):",
                ["input_open"] = "Введите Open Price (или пропустите):",
                ["input_close"] = "Введите Close Price (или пропустите):",
                ["input_sl"] = "Введите Stop Loss (или пропустите):",
                ["input_tp"] = "Введите Take Profit (или пропустите):",
                ["input_volume"] = "Введите Volume (или пропустите):",
                ["input_comment"] = "Введите комментарий (или пропустите):",
                ["history_title"] = "История сделок",
                ["history_filter"] = "Фильтры:",
                ["trade_detail"] = "Детали сделки:\nID: {0}\nТикер: {1}\nНаправление: {2}\nPnL: {3}%\nOpen Price: {4}\nClose Price: {5}\nSL: {6}\nTP: {7}\nVolume: {8}\nКомментарий: {9}\nДата: {10}",
                ["show_more"] = "Показать ещё"
            },
            ["en"] = new Dictionary<string, string>
            {
                ["welcome"] = "Welcome to TradingBot 2.0!\nI'll help you track your trades.\nClick 'Next' to start the tutorial.",
                ["onboarding_1"] = "📥 You can add trades via screenshots or manually.",
                ["onboarding_2"] = "📊 View statistics and equity curves.",
                ["onboarding_3"] = "⚙ Customize the bot (language, tickers, notifications).",
                ["main_menu"] = "Choose an action:",
                ["please_use_buttons"] = "Please use the buttons to control the bot.",
                ["error_occurred"] = "An error occurred. Try again or return to the menu.",
                ["trade_cancelled"] = "Trade input cancelled.",
                ["trade_saved"] = "Trade {0} (PnL={1}%) saved.",
                ["trade_saved_local"] = "Trade saved locally but not sent to Notion.",
                ["error_saving_trade"] = "Error saving trade.",
                ["trade_expired"] = "Trade expired or not found. Try again.",
                ["trade_deleted"] = "Trade deleted.",
                ["all_pending_cleared"] = "All pending trades cleared.",
                ["no_trades"] = "No trade data for the selected period.",
                ["invalid_input"] = "Invalid input. Try again.",
                ["invalid_pnl"] = "Enter a valid number for PnL (e.g., +5.25).",
                ["step_1"] = "🟩⬜⬜⬜⬜⬜⬜⬜ Step 1/9: Select ticker",
                ["step_2"] = "🟩🟩⬜⬜⬜⬜⬜⬜ Step 2/9: Select direction",
                ["step_3"] = "🟩🟩🟩⬜⬜⬜⬜⬜ Step 3/9: Enter PnL",
                ["step_4"] = "🟩🟩🟩🟩⬜⬜⬜⬜ Step 4/9: Enter Open Price",
                ["step_5"] = "🟩🟩🟩🟩🟩⬜⬜⬜ Step 5/9: Enter Close Price",
                ["step_6"] = "🟩🟩🟩🟩🟩🟩⬜⬜ Step 6/9: Enter Stop Loss",
                ["step_7"] = "🟩🟩🟩🟩🟩🟩🟩⬜ Step 7/9: Enter Take Profit",
                ["step_8"] = "🟩🟩🟩🟩🟩🟩🟩🟩 Step 8/9: Enter Volume",
                ["step_9"] = "🟩🟩🟩🟩🟩🟩🟩🟩🟩 Step 9/9: Enter comment",
                ["trade_preview"] = "Current trade:\nTicker: {0}\nDirection: {1}\nPnL: {2}%\nOpen Price: {3}\nClose Price: {4}\nSL: {5}\nTP: {6}\nVolume: {7}\nComment: {8}",
                ["confirm_trade"] = "Everything correct?",
                ["edit_field"] = "Select field to edit:",
                ["pending_trades"] = "Pending trades:\n{0}",
                ["no_pending_trades"] = "No pending trades.",
                ["stats_menu"] = "Select period for statistics:",
                ["stats_result"] = "Statistics {0}:\nTotal trades: {1}\nTotal PnL: {2}%\nProfitable: {3}\nLoss-making: {4}\nWin Rate: {5}%",
                ["advanced_stats"] = "Trades: {0}\nTotal PnL: {1}%\nAverage PnL: {2}%\nBest: {3}%\nWorst: {4}%\nWin Rate: {5}%",
                ["date_label"] = "Date",
                ["pnl_label"] = "Cumulative PnL",
                ["equity_curve"] = "Equity Curve",
                ["error_graph"] = "Error generating graph.",
                ["settings_menu"] = "Settings:",
                ["settings_updated"] = "Settings updated.",
                ["settings_reset"] = "Settings reset.",
                ["help_menu"] = "Help:\nSelect a section",
                ["support"] = "Contact support",
                ["whats_new"] = "What's new?",
                ["export_success"] = "Trades exported to CSV.",
                ["win_streak"] = "🔥 {0} winning trades in a row!",
                ["loss_streak"] = "⚠️ {0} losing trades in a row!",
                ["back"] = "◀ Back",
                ["cancel"] = "❌ Cancel",
                ["main_menu_button"] = "🏠 Menu",
                ["next"] = "▶ Next",
                ["skip"] = "➡ Skip",
                ["confirm"] = "✅ Confirm",
                ["edit"] = "✏️ Edit",
                ["delete"] = "🗑 Delete",
                ["retry"] = "🔄 Retry",
                ["other"] = "Other",
                ["input_manually"] = "⌨ Enter manually",
                ["prefill_last"] = "↺ Prefill from last",
                ["period_week"] = "for the week",
                ["period_month"] = "for the month",
                ["period_all"] = "for all time",
                ["input_ticker"] = "Enter ticker (e.g., BTCUSDT):",
                ["input_pnl"] = "Enter PnL (e.g., +5.25 or -3.1):",
                ["input_open"] = "Enter Open Price (or skip):",
                ["input_close"] = "Enter Close Price (or skip):",
                ["input_sl"] = "Enter Stop Loss (or skip):",
                ["input_tp"] = "Enter Take Profit (or skip):",
                ["input_volume"] = "Enter Volume (or skip):",
                ["input_comment"] = "Enter comment (or skip):",
                ["history_title"] = "Trade History",
                ["history_filter"] = "Filters:",
                ["trade_detail"] = "Trade Details:\nID: {0}\nTicker: {1}\nDirection: {2}\nPnL: {3}%\nOpen Price: {4}\nClose Price: {5}\nSL: {6}\nTP: {7}\nVolume: {8}\nComment: {9}\nDate: {10}",
                ["show_more"] = "Show more"
            }
        };

        // Популярные тикеры и PnL для быстрых кнопок
        private readonly List<string> _popularTickers = new() { "BTCUSDT", "ETHUSDT", "SOLUSDT", "BNBUSDT", "XRPUSDT" };
        private readonly List<string> _popularPnL = new() { "+0.5", "-0.5", "+1", "-1" };

        public IReadOnlyList<string> PopularTickers => _popularTickers;

        // Получение текста из ресурсов по ключу (с форматированием при необходимости)
        public string GetText(string key, string language, params object[] args)
        {
            if (_resources.TryGetValue(language, out var dict) && dict.TryGetValue(key, out string value))
            {
                return (args != null && args.Length > 0) ? string.Format(value, args) : value;
            }
            // Если перевод не найден, возвращаем ключ
            return key;
        }

        // Экран обучения (3 шага onboarding)
        public (string Text, InlineKeyboardMarkup Keyboard) GetOnboardingScreen(int step, string language)
        {
            string text = step switch
            {
                1 => GetText("welcome", language) + "\n\n" + GetText("onboarding_1", language),
                2 => GetText("onboarding_2", language),
                3 => GetText("onboarding_3", language),
                _ => ""
            };
            // Кнопка "Далее" или переход к меню после последнего шага
            InlineKeyboardMarkup keyboard;
            if (step < 3)
            {
                keyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData(GetText("next", language), "onboarding")
                });
            }
            else
            {
                keyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", language), "main")
                });
            }
            return (text, keyboard);
        }

        // Главное меню
        public InlineKeyboardMarkup GetMainMenu(UserSettings settings)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("➕ Новая сделка", "start_trade"),
                    InlineKeyboardButton.WithCallbackData("📊 Статистика", "stats")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📜 История", "history_page_1_period_all_filter_none"),
                    InlineKeyboardButton.WithCallbackData("⚙️ Настройки", "settings")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("❓ Помощь", "help"),
                    InlineKeyboardButton.WithCallbackData("ℹ️ О боте", "whatsnew")
                }
            });
        }

        // Клавиатура при ошибке (повторить или в меню)
        public InlineKeyboardMarkup GetErrorKeyboard(UserSettings settings)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("retry", settings.Language), "retry") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") }
            });
        }

        // Экран ввода сделки (конкретный шаг)
        public (string Text, InlineKeyboardMarkup Keyboard) GetTradeInputScreen(Trade trade, int step, UserSettings settings, string tradeId, Trade lastTrade = null)
        {
            // Формируем превью заполненных полей на данный момент
            string preview = GetText("trade_preview", settings.Language,
                string.IsNullOrEmpty(trade.Ticker) ? "-" : trade.Ticker,
                string.IsNullOrEmpty(trade.Direction) ? "-" : trade.Direction,
                trade.PnL,
                trade.OpenPrice?.ToString() ?? "-",
                trade.Entry?.ToString() ?? "-",
                trade.SL?.ToString() ?? "-",
                trade.TP?.ToString() ?? "-",
                trade.Volume?.ToString() ?? "-",
                string.IsNullOrEmpty(trade.Comment) ? "-" : trade.Comment);
            // Текст подсказки для текущего шага
            string prompt = GetText($"step_{step}", settings.Language);
            var buttons = new List<InlineKeyboardButton[]>();
            if (step == 1)
            {
                // Предлагаем популярные и избранные тикеры
                var initialTickers = settings.FavoriteTickers.Concat(settings.RecentTickers).Concat(_popularTickers)
                                        .Distinct().Take(5).ToList();
                buttons.AddRange(initialTickers.Select(ticker => new[] {
                    InlineKeyboardButton.WithCallbackData(ticker, $"set_ticker_{ticker}_trade_{tradeId}")
                }));
                if (settings.FavoriteTickers.Count + settings.RecentTickers.Count + _popularTickers.Count > 5)
                {
                    // Кнопка "Показать ещё"
                    buttons.Add(new[] {
                        InlineKeyboardButton.WithCallbackData(GetText("show_more", settings.Language), $"more_tickers_trade_{tradeId}")
                    });
                }
                // Кнопка ручного ввода тикера
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("other", settings.Language), $"input_ticker_trade_{tradeId}")
                });
                // Кнопка "Предзаполнить из последней"
                if (lastTrade != null && !string.IsNullOrEmpty(lastTrade.Ticker))
                {
                    buttons.Add(new[] {
                        InlineKeyboardButton.WithCallbackData(GetText("prefill_last", settings.Language), $"set_ticker_{lastTrade.Ticker}_trade_{tradeId}")
                    });
                }
            }
            else if (step == 2)
            {
                // Выбор направления сделки
                buttons.AddRange(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("Long", $"set_direction_Long_trade_{tradeId}") },
                    new[] { InlineKeyboardButton.WithCallbackData("Short", $"set_direction_Short_trade_{tradeId}") }
                });
                // Предлагаем последние использованные направления (если есть)
                foreach (var direction in settings.RecentDirections.Take(2))
                {
                    buttons.Add(new[] {
                        InlineKeyboardButton.WithCallbackData(direction, $"set_direction_{direction}_trade_{tradeId}")
                    });
                }
            }
            else if (step == 3)                   // ввод PnL
            {
                buttons.Clear();

                // быстрые варианты ±0.5/±1
                buttons.AddRange(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("-0.5%", $"adjust_pnl_-0.5_trade_{tradeId}") },
                    new[] { InlineKeyboardButton.WithCallbackData("+0.5%", $"adjust_pnl_0.5_trade_{tradeId}") },
                    new[] { InlineKeyboardButton.WithCallbackData("-1%",   $"adjust_pnl_-1_trade_{tradeId}") },
                    new[] { InlineKeyboardButton.WithCallbackData("+1%",   $"adjust_pnl_1_trade_{tradeId}") }
                });

                // «Ввести вручную»
                buttons.Add(new[] {
                    InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language),
                        $"input_pnl_trade_{tradeId}")
                });

                // «Пропустить»
                buttons.Add(new[] {
                    InlineKeyboardButton.WithCallbackData(GetText("skip", settings.Language),
                        $"skip_trade_{tradeId}_step_{step}")
                });

                // «✅ Всё верно» (подтверждение текущего значения)
                buttons.Add(new[] {
                    InlineKeyboardButton.WithCallbackData("✅ Всё верно", $"allcorrect_{tradeId}")
                });
            }


            else if (step >= 4 && step <= 8)
            {
                string field = step switch
                {
                    4 => "open",
                    5 => "close",
                    6 => "sl",
                    7 => "tp",
                    8 => "volume",
                    _ => ""
                };
                buttons.Add(new[] {
                    InlineKeyboardButton.WithCallbackData(GetText("skip", settings.Language), $"skip_trade_{tradeId}_step_{step}")
                });
                buttons.Add(new[] {
                    InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language), $"input_{field}") // Убрано _trade_{tradeId}
                });
            }
            else if (step == 9)
            {
                // Шаг 9: ввод комментария
                foreach (var comment in settings.RecentComments.Take(3))
                {
                    string label = comment.Length > 20 ? comment.Substring(0, 17) + "..." : comment;
                    buttons.Add(new[] {
                        InlineKeyboardButton.WithCallbackData(label, $"input_comment_trade_{tradeId}")
                    });
                }
                buttons.Add(new[] {
                    InlineKeyboardButton.WithCallbackData(GetText("skip", settings.Language), $"skip_trade_{tradeId}_step_{step}")
                });
                buttons.Add(new[] {
                    InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language), $"input_comment_trade_{tradeId}")
                });
            }
            // Добавляем кнопки "Назад" и "Отменить" на каждом шаге
            buttons.Add(new[] {
                InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), $"back_trade_{tradeId}_step_{step}")
            });
            buttons.Add(new[] {
                InlineKeyboardButton.WithCallbackData(GetText("cancel", settings.Language), "cancel")
            });
            return ($"{preview}\n\n{prompt}", new InlineKeyboardMarkup(buttons));
        }

        // Экран подтверждения сделки (предпросмотр + кнопки подтверждения/редактирования/удаления)
        public (string Text, InlineKeyboardMarkup Keyboard) GetTradeConfirmationScreen(Trade trade, string tradeId, UserSettings settings)
        {
            string text = GetText("trade_preview", settings.Language,
                trade.Ticker ?? "-", trade.Direction ?? "-", trade.PnL,
                trade.OpenPrice?.ToString() ?? "-", trade.Entry?.ToString() ?? "-",
                trade.SL?.ToString() ?? "-", trade.TP?.ToString() ?? "-",
                trade.Volume?.ToString() ?? "-", trade.Comment ?? "-");
            text += "\n\n" + GetText("confirm_trade", settings.Language);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("confirm", settings.Language), $"confirm_trade_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("edit", settings.Language), $"edit_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("delete", settings.Language), $"delete_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") }
            });
            return (text, keyboard);
        }

        // Меню выбора поля для редактирования сделки
        public (string Text, InlineKeyboardMarkup Keyboard) GetEditFieldMenu(Trade trade, string tradeId, UserSettings settings)
        {
            string text = GetText("edit_field", settings.Language);
            var buttons = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData("Тикер", $"editfield_ticker_trade_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData("Направление", $"editfield_direction_trade_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData("PnL", $"editfield_pnl_trade_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData("Open Price", $"editfield_open_trade_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData("Close Price", $"editfield_close_trade_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData("Stop Loss", $"editfield_sl_trade_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData("Take Profit", $"editfield_tp_trade_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData("Volume", $"editfield_volume_trade_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData("Комментарий", $"editfield_comment_trade_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") }
            };
            return (text, new InlineKeyboardMarkup(buttons));
        }

        // Подсказка ввода для поля сделки (текст + только кнопка Отмена)
        public (string Text, InlineKeyboardMarkup Keyboard) GetInputPrompt(string field, UserSettings settings, string tradeId)
        {
            string text = field switch
            {
                "ticker" => GetText("input_ticker", settings.Language),
                "pnl" => GetText("input_pnl", settings.Language),
                "open" => GetText("input_open", settings.Language),
                "close" => GetText("input_close", settings.Language),
                "sl" => GetText("input_sl", settings.Language),
                "tp" => GetText("input_tp", settings.Language),
                "volume" => GetText("input_volume", settings.Language),
                "comment" => GetText("input_comment", settings.Language),
                _ => GetText("invalid_input", settings.Language)
            };
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("cancel", settings.Language), "cancel") }
            });
            return (text, keyboard);
        }

        // Экран активных сделок (PendingTrades) с пагинацией
        public (string Text, InlineKeyboardMarkup Keyboard) GetPendingTradesScreen(List<(string TradeId, Trade Trade, int MessageId, DateTime CreatedAt)> pendingTrades, int page, int total, UserSettings settings)
        {
            string text;
            if (pendingTrades.Count == 0)
            {
                text = GetText("no_pending_trades", settings.Language);
            }
            else
            {
                // Формируем список активных сделок
                var lines = pendingTrades.Select(t =>
                    $"Тикер: {t.Trade.Ticker}, PnL: {t.Trade.PnL}% ({t.CreatedAt:yyyy-MM-dd HH:mm})");
                text = GetText("pending_trades", settings.Language, string.Join("\n", lines));
            }
            int pageSize = 5;
            int totalPages = (int)Math.Ceiling(total / (double)pageSize);
            var buttons = new List<InlineKeyboardButton[]>();
            if (pendingTrades.Count > 0)
            {
                // Каждая активная сделка – кнопка "Редактировать"
                foreach (var (tradeId, trade, msgId, createdAt) in pendingTrades)
                {
                    string label = $"{trade.Ticker} ({trade.PnL}%)";
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(label, $"edit_{tradeId}") });
                }
                // Кнопка очистки всех
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("all_pending_cleared", settings.Language), "clearpending") });
            }
            // Пагинация (стрелки и номера страниц)
            var paginationButtons = new List<InlineKeyboardButton>();
            if (page > 1)
                paginationButtons.Add(InlineKeyboardButton.WithCallbackData("◀", $"pending_page_{page - 1}"));
            for (int i = Math.Max(1, page - 2); i <= Math.Min(totalPages, page + 2); i++)
            {
                paginationButtons.Add(InlineKeyboardButton.WithCallbackData(i == page ? $"[{i}]" : i.ToString(), $"pending_page_{i}"));
            }
            if (page < totalPages)
                paginationButtons.Add(InlineKeyboardButton.WithCallbackData("▶", $"pending_page_{page + 1}"));
            if (paginationButtons.Any())
                buttons.Add(paginationButtons.ToArray());
            // Кнопка назад в меню
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") });
            return (text, new InlineKeyboardMarkup(buttons));
        }

        // Меню статистики
        public (string Text, InlineKeyboardMarkup Keyboard) GetStatsMenu(UserSettings settings)
        {
            string text = GetText("stats_menu", settings.Language);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("period_week", settings.Language), "statsperiod_week") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("period_month", settings.Language), "statsperiod_month") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("period_all", settings.Language), "statsperiod_all") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") }
            });
            return (text, keyboard);
        }

        // Результат статистики за период
        public (string Text, InlineKeyboardMarkup Keyboard) GetStatsResult(List<Trade> trades, string period, UserSettings settings)
        {
            int totalTrades = trades.Count;
            decimal totalPnL = trades.Sum(t => t.PnL);
            int profitable = trades.Count(t => t.PnL > 0);
            int losing = trades.Count(t => t.PnL < 0);
            int winRate = totalTrades > 0 ? (int)((double)profitable / totalTrades * 100) : 0;
            string periodText = GetText($"period_{period}", settings.Language);
            string text = GetText("stats_result", settings.Language, periodText, totalTrades, totalPnL.ToString("F2"), profitable, losing, winRate);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") }
            });
            return (text, keyboard);
        }

        // Меню продвинутой статистики (график эквити)
        public (string Text, InlineKeyboardMarkup Keyboard) GetAdvancedStatsMenu(UserSettings settings)
        {
            string text = GetText("equity_curve", settings.Language);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("period_week", settings.Language), "advstatsperiod_week") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("period_month", settings.Language), "advstatsperiod_month") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("period_all", settings.Language), "advstatsperiod_all") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") }
            });
            return (text, keyboard);
        }

        // Экран истории сделок (список с фильтрами и пагинацией)
        public (string Text, InlineKeyboardMarkup Keyboard) GetHistoryScreen(List<Trade> trades, int page, string period, string filter, UserSettings settings)
        {
            string text = GetText("history_title", settings.Language);
            if (trades.Count == 0)
            {
                text += "\n" + GetText("no_trades", settings.Language);
            }
            else
            {
                int pageSize = 5;
                var pageTrades = trades.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                var lines = pageTrades.Select(t => $"Тикер: {t.Ticker}, Направление: {t.Direction}, PnL: {t.PnL}% ({t.Date:yyyy-MM-dd})");
                text += "\n" + string.Join("\n", lines);
            }
            var buttons = new List<InlineKeyboardButton[]>();
            if (trades.Count > 0)
            {
                // Фильтры: популярные тикеры, направления и PnL >1%, < -1%
                var uniqueTickers = trades.Select(t => t.Ticker).Distinct().Take(3);
                var uniqueDirections = trades.Select(t => t.Direction).Distinct().Take(2);
                foreach (var ticker in uniqueTickers)
                {
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData($"Только {ticker}", $"historyfilter_ticker_{ticker}") });
                }
                foreach (var direction in uniqueDirections)
                {
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(direction, $"historyfilter_direction_{direction}") });
                }
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(">1%", $"historyfilter_pnl_gt_1") });
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("<-1%", $"historyfilter_pnl_lt_-1") });
                // Список сделок (максимум 5) для подробного просмотра
                foreach (var t in trades.Take(5))
                {
                    string label = $"{t.Ticker} ({t.PnL}%)";
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(label, $"historydetail_{t.Id}") });
                }
            }
            // Кнопка экспорта
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("export_success", settings.Language), "export") });
            // Пагинация
            int totalPages = (int)Math.Ceiling(trades.Count / 5.0);
            var paginationButtons = new List<InlineKeyboardButton>();
            if (page > 1)
                paginationButtons.Add(InlineKeyboardButton.WithCallbackData("◀", $"history_page_{page - 1}_period_{period}_filter_{filter ?? "none"}"));
            for (int i = Math.Max(1, page - 2); i <= Math.Min(totalPages, page + 2); i++)
            {
                paginationButtons.Add(InlineKeyboardButton.WithCallbackData(i == page ? $"[{i}]" : i.ToString(), $"history_page_{i}_period_{period}_filter_{filter ?? "none"}"));
            }
            if (page < totalPages)
                paginationButtons.Add(InlineKeyboardButton.WithCallbackData("▶", $"history_page_{page + 1}_period_{period}_filter_{filter ?? "none"}"));
            if (paginationButtons.Any())
                buttons.Add(paginationButtons.ToArray());
            // Кнопка назад в меню
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") });
            return (text, new InlineKeyboardMarkup(buttons));
        }

        // Экран подробностей сделки (при нажатии из истории)
        public (string Text, InlineKeyboardMarkup Keyboard) GetTradeDetailScreen(Trade trade, UserSettings settings)
        {
            string text = GetText("trade_detail", settings.Language,
                trade.Id, trade.Ticker ?? "-", trade.Direction ?? "-", trade.PnL,
                trade.OpenPrice?.ToString() ?? "-", trade.Entry?.ToString() ?? "-",
                trade.SL?.ToString() ?? "-", trade.TP?.ToString() ?? "-",
                trade.Volume?.ToString() ?? "-", trade.Comment ?? "-", trade.Date);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") }
            });
            return (text, keyboard);
        }

        // Меню настроек
        public (string Text, InlineKeyboardMarkup Keyboard) GetSettingsMenu(UserSettings settings)
        {
            string text = GetText("settings_menu", settings.Language);
            var buttons = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData($"Язык: {(settings.Language == "ru" ? "Русский" : "English")}", "settings_language") },
                new[] { InlineKeyboardButton.WithCallbackData($"Уведомления: {(settings.NotificationsEnabled ? "Вкл" : "Выкл")}", "settings_notifications") },
                new[] { InlineKeyboardButton.WithCallbackData("Избранные тикеры", "settings_tickers") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("settings_reset", settings.Language), "resetsettings") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") }
            };
            return (text, new InlineKeyboardMarkup(buttons));
        }

        // Промпт ввода настройки (например, добавление избранного тикера)
        public (string Text, InlineKeyboardMarkup Keyboard) GetSettingsInputPrompt(string field, UserSettings settings)
        {
            string text = field switch
            {
                "ticker" => "Введите тикер для добавления в избранное:",
                _ => GetText("invalid_input", settings.Language)
            };
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("cancel", settings.Language), "cancel") }
            });
            return (text, keyboard);
        }

        // Меню помощи
        public (string Text, InlineKeyboardMarkup Keyboard) GetHelpMenu(UserSettings settings)
        {
            string text = GetText("help_menu", settings.Language);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("support", settings.Language), "support") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("whats_new", settings.Language), "whatsnew") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") }
            });
            return (text, keyboard);
        }
    }
}
// UIManager.cs

using Telegram.Bot.Types.ReplyMarkups;
using TradingBot.Models;

namespace TradingBot.Services
{
    // Локализованные ресурсы интерфейса (русский и английский)
    public class UIManager
    {
        private readonly Dictionary<string, Dictionary<string, string>> _resources = new()
        {
            ["ru"] = new Dictionary<string, string>
            {
                ["welcome"] = "🚀 Добро пожаловать в TradingBot!\nЯ помогу вам вести учёт сделок.\nНажмите 'Далее' для обучения.",
                ["onboarding_1"] = "📥 Вы можете добавлять сделки через скриншоты или вручную.",
                ["onboarding_2"] = "📊 Просматривайте статистику и графики эквити.",
                ["onboarding_3"] = "⚙ Настраивайте бота под себя (язык, уведомления).",
                ["main_menu"] = "🚀 Добро пожаловать! Что будем делать?\n\n📊 Мои сделки:\n- ➕ Добавить сделку\n- 📈 Моя статистика\n- 📜 История сделок\n\n⚙️ Настройки:\n- 🔔 Уведомления (вкл/выкл)\n- 🌐 Язык (RU/EN)\n\n💡 Помощь и поддержка:\n- 🆘 Связаться с поддержкой\n\n📅 Сделок сегодня: {0} | 📈 Общий PnL: {1}% | ✅ Winrate: {2}%",
                ["please_use_buttons"] = "👇 Пожалуйста, используйте кнопки ниже.",
                ["error_occurred"] = "⚠️ Произошла ошибка. Попробуйте снова.",
                ["trade_cancelled"] = "❌ Ввод сделки отменён.",
                ["trade_saved"] = "✅ Сделка {0} (PnL={1}%) сохранена!",
                ["trade_saved_local"] = "💾 Сделка сохранена локально.",
                ["error_saving_trade"] = "⚠️ Ошибка при сохранении сделки.",
                ["trade_expired"] = "⏰ Сделка устарела. Начните заново.",
                ["trade_deleted"] = "🗑️ Сделка удалена.",
                ["all_pending_cleared"] = "🧹 Все активные сделки очищены.",
                ["no_trades"] = "📉 Нет сделок за выбранный период.",
                ["invalid_input"] = "⚠️ Некорректный ввод. Попробуйте снова.",
                ["invalid_pnl"] = "⚠️ Введите корректное число для PnL (например, +5.25).",
                ["step_1"] = "🟩⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜ Шаг 1/12: Выберите тикер",
                ["step_2"] = "🟩🟩⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜ Шаг 2/12: Выберите направление",
                ["step_3"] = "🟩🟩🟩⬜⬜⬜⬜⬜⬜⬜⬜⬜ Шаг 3/12: Введите PnL",
                ["step_4"] = "🟩🟩🟩🟩⬜⬜⬜⬜⬜⬜⬜⬜ Шаг 4/12: Введите Open Price",
                ["step_5"] = "🟩🟩🟩🟩🟩⬜⬜⬜⬜⬜⬜⬜ Шаг 5/12: Введите Close Price",
                ["step_6"] = "🟩🟩🟩🟩🟩🟩⬜⬜⬜⬜⬜⬜ Шаг 6/12: Введите Stop Loss",
                ["step_7"] = "🟩🟩🟩🟩🟩🟩🟩⬜⬜⬜⬜⬜ Шаг 7/12: Введите Take Profit",
                ["step_8"] = "🟩🟩🟩🟩🟩🟩🟩🟩⬜⬜⬜⬜ Шаг 8/12: Введите Volume",
                ["step_9"] = "🟩🟩🟩🟩🟩🟩🟩🟩🟩⬜⬜⬜ Шаг 9/12: Введите комментарий",
                ["step_10"] = "🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩⬜⬜ Шаг 10/12: Выберите стратегию",
                ["step_11"] = "🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩⬜ Шаг 11/12: Выберите эмоцию",
                ["step_12"] = "🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩 Шаг 12/12: Выберите сессию",
                ["trade_preview"] = "✅ Всё верно?\n\n📌 Тикер: {0}\n📌 Направление: {1}\n📌 PnL: {2}%\n📌 Open Price: {3}\n📌 Close Price: {4}\n📌 SL: {5}\n📌 TP: {6}\n📌 Volume: {7}\n📌 Стратегия: {8}\n📌 Эмоция: {9}\n📌 Сессия: {10}\n📝 Комментарий: {11}",
                ["confirm_trade"] = "Всё верно?",
                ["edit_field"] = "✏️ Выберите поле для редактирования:",
                ["pending_trades"] = "⏳ Активные сделки:\n{0}",
                ["no_pending_trades"] = "⏳ Нет активных сделок.",
                ["stats_menu"] = "📊 Выберите период для статистики:",
                ["stats_result"] = "📊 Статистика {0}:\nВсего сделок: {1}\nОбщий PnL: {2}%\nПрибыльных: {3}\nУбыточных: {4}\nWin Rate: {5}%",
                ["advanced_stats"] = "📈 Сделок: {0}\nОбщий PnL: {1}%\nСредний PnL: {2}%\nЛучший: {3}%\nХудший: {4}%\nWin Rate: {5}%",
                ["date_label"] = "📅 Дата",
                ["pnl_label"] = "📈 Накопленный PnL",
                ["equity_curve"] = "📈 Кривая эквити",
                ["error_graph"] = "⚠️ Ошибка при создании графика.",
                ["settings_menu"] = "⚙️ Настройки:",
                ["settings_updated"] = "✅ Настройки обновлены!",
                ["settings_reset"] = "🔄 Настройки сброшены.",
                ["help_menu"] = "💡 Помощь:\nВыберите раздел",
                ["support"] = "🆘 Связаться с поддержкой",
                ["whats_new"] = "📰 Что нового?",
                ["export_success"] = "💾 Сделки экспортированы в CSV.",
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
                ["other"] = "✨ Другое",
                ["input_manually"] = "⌨ Ввести вручную",
                ["prefill_last"] = "↺ Из последней",
                ["period_week"] = "за неделю",
                ["period_month"] = "за месяц",
                ["period_all"] = "за всё время",
                ["input_ticker"] = "💡 Введите тикер (например, BTCUSDT):",
                ["input_pnl"] = "💡 Введите PnL (например, +5.25 или -3.1):",
                ["input_open"] = "💡 Введите Open Price (или пропустите):",
                ["input_close"] = "💡 Введите Close Price (или пропустите):",
                ["input_sl"] = "💡 Введите Stop Loss (или пропустите):",
                ["input_tp"] = "💡 Введите Take Profit (или пропустите):",
                ["input_volume"] = "💡 Введите Volume (или пропустите):",
                ["input_comment"] = "💡 Введите комментарий (или пропустите):",
                ["input_strategy"] = "💡 Введите стратегию (или пропустите):",
                ["input_emotion"] = "💡 Введите эмоцию (или пропустите):",
                ["input_session"] = "💡 Введите сессию (или пропустите):"
            },
            ["en"] = new Dictionary<string, string>
            {
                // Аналогичные ключи на английском с эмодзи
            }
        };
        
        private List<string> _strategyOptions = new();
        private List<string> _emotionOptions = new();
        private List<string> _sessionOptions = new();

        // Список популярных тикеров и PnL для быстрых кнопок
        private readonly List<string> _popularTickers = new() { "BTCUSDT", "ETHUSDT", "SOLUSDT", "BNBUSDT", "XRPUSDT" };
        private readonly List<string> _popularPnL = new() { "+0.5", "-0.5", "+1", "-1" };

        public IReadOnlyList<string> PopularTickers => _popularTickers;

        // Получение текста из ресурсов по ключу (с подстановкой аргументов при необходимости)
        public string GetText(string key, string language, params object[] args)
        {
            if (_resources.TryGetValue(language, out var dict) && dict.TryGetValue(key, out string value))
            {
                return (args != null && args.Length > 0) ? string.Format(value, args) : value;
            }
            // Если перевод не найден, возвращаем ключ
            return key;
        }
        
        public void SetSelectOptions(List<string> strategies, List<string> emotions, List<string> sessions)
        {
            _strategyOptions = strategies ?? new List<string>();
            _emotionOptions = emotions ?? new List<string>();
            _sessionOptions = sessions ?? new List<string>();
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
            InlineKeyboardMarkup keyboard = step < 3
                ? new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData(GetText("next", language), "onboarding"))
                : new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", language), "main"));
            return (text, keyboard);
        }

        // Главное меню
        public InlineKeyboardMarkup GetMainMenu(UserSettings settings)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("➕ Добавить сделку", "start_trade"),
                    InlineKeyboardButton.WithCallbackData("📊 Статистика", "stats")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🕓 История", "history"),
                    InlineKeyboardButton.WithCallbackData("⚙️ Настройки", "settings")
                },
                new[] { InlineKeyboardButton.WithCallbackData("💡 Помощь", "help") }
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

        // Экран ввода сделки (определенный шаг)
        public (string Text, InlineKeyboardMarkup Keyboard) GetTradeInputScreen(Trade trade, int step, UserSettings settings, string tradeId, Trade lastTrade = null)
        {
            string preview = GetText("trade_preview", settings.Language,
                trade.Ticker ?? "-", trade.Direction ?? "-", trade.PnL,
                trade.OpenPrice?.ToString() ?? "-", trade.Entry?.ToString() ?? "-",
                trade.SL?.ToString() ?? "-", trade.TP?.ToString() ?? "-", trade.Volume?.ToString() ?? "-",
                trade.Strategy ?? "-", trade.Emotion ?? "-", trade.Session ?? "-", trade.Comment ?? "-");
            string prompt = GetText($"step_{step}", settings.Language);
            var buttons = new List<InlineKeyboardButton[]>();

            switch (step)
            {
                case 1:
                    var tickers = settings.FavoriteTickers.Concat(settings.RecentTickers).Concat(PopularTickers)
                                   .Distinct().Take(5).ToList();
                    foreach (var t in tickers)
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(t, $"set_ticker_{t}_trade_{tradeId}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("other", settings.Language), $"input_ticker_trade_{tradeId}") });
                    if (lastTrade?.Ticker != null)
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("prefill_last", settings.Language), $"set_ticker_{lastTrade.Ticker}_trade_{tradeId}") });
                    break;
                case 2:
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Long", $"set_direction_Long_trade_{tradeId}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Short", $"set_direction_Short_trade_{tradeId}") });
                    break;
                case 3:
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("-0.5%", $"adjust_pnl_-0.5_trade_{tradeId}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("+0.5%", $"adjust_pnl_0.5_trade_{tradeId}") });
                    // Добавляем кнопки ручного ввода и пропуска
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language), $"input_pnl_trade_{tradeId}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("skip", settings.Language), $"skip_trade_{tradeId}_step_{step}") });
                    break;
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                    string field = step switch
                    {
                        4 => "open",
                        5 => "close",
                        6 => "sl",
                        7 => "tp",
                        8 => "volume",
                        _ => ""
                    };
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("skip", settings.Language), $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language), $"input_{field}_trade_{tradeId}") });
                    break;
                case 9:
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("skip", settings.Language), $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language), $"input_comment_trade_{tradeId}") });
                    break;
                case 10:
                    foreach (var option in _strategyOptions)
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(option, $"set_strategy_{option}_trade_{tradeId}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("skip", settings.Language), $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language), $"input_strategy_trade_{tradeId}") });
                    break;
                case 11:
                    foreach (var option in _emotionOptions)
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(option, $"set_emotion_{option}_trade_{tradeId}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("skip", settings.Language), $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language), $"input_emotion_trade_{tradeId}") });
                    break;
                case 12:
                    foreach (var option in _sessionOptions)
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(option, $"set_session_{option}_trade_{tradeId}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("skip", settings.Language), $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language), $"input_session_trade_{tradeId}") });
                    break;
            }

            // Кнопки "Назад" и "Отменить" внизу
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), $"back_trade_{tradeId}_step_{step}") });
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("cancel", settings.Language), "cancel") });

            return ($"{preview}\n\n{prompt}", new InlineKeyboardMarkup(buttons));
        }

        // Экран подтверждения сделки (предпросмотр + кнопки подтверждения/редактирования/удаления)
        public (string Text, InlineKeyboardMarkup Keyboard) GetTradeConfirmationScreen(Trade trade, string tradeId, UserSettings settings)
        {
            string text = GetText("trade_preview", settings.Language,
                trade.Ticker ?? "-", trade.Direction ?? "-", trade.PnL,
                trade.OpenPrice?.ToString() ?? "-", trade.Entry?.ToString() ?? "-",
                trade.SL?.ToString() ?? "-", trade.TP?.ToString() ?? "-", trade.Volume?.ToString() ?? "-",
                trade.Strategy ?? "-", trade.Emotion ?? "-", trade.Session ?? "-", trade.Comment ?? "-");
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
            string text = GetText("trade_preview", settings.Language,
                trade.Ticker ?? "-", trade.Direction ?? "-", trade.PnL,
                trade.OpenPrice?.ToString() ?? "-", trade.Entry?.ToString() ?? "-",
                trade.SL?.ToString() ?? "-", trade.TP?.ToString() ?? "-", trade.Volume?.ToString() ?? "-",
                trade.Strategy ?? "-", trade.Emotion ?? "-", trade.Session ?? "-", trade.Comment ?? "-");
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
                new[] { InlineKeyboardButton.WithCallbackData("Стратегия", $"editfield_strategy_trade_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData("Эмоция", $"editfield_emotion_trade_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData("Сессия", $"editfield_session_trade_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") }
            };
            return (text, new InlineKeyboardMarkup(buttons));
        }

        // Подсказка ввода для поля сделки (текст + кнопка "Отмена")
        public (string Text, InlineKeyboardMarkup Keyboard) GetInputPrompt(string field, UserSettings settings,
            string tradeId)
        {
            string text = GetText($"input_{field}", settings.Language);
            var keyboard = new InlineKeyboardMarkup(new[]
                { new[] { InlineKeyboardButton.WithCallbackData(GetText("cancel", settings.Language), "cancel") } });
            return (text, keyboard);
        }
        
        public (string Text, InlineKeyboardMarkup Keyboard) GetHistoryFilterMenu(UserSettings settings, string period, string filter)
        {
            string text = "🔍 Выберите фильтр для истории:\nТекущий период: " + GetText($"period_{period}", settings.Language);
            var buttons = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData("Все тикеры", "historyfilter_ticker_all") },
                new[] { InlineKeyboardButton.WithCallbackData(">1%", "historyfilter_pnl_gt_1") },
                new[] { InlineKeyboardButton.WithCallbackData("<-1%", "historyfilter_pnl_lt_-1") },
                new[] { InlineKeyboardButton.WithCallbackData("Long", "historyfilter_direction_Long") },
                new[] { InlineKeyboardButton.WithCallbackData("Short", "historyfilter_direction_Short") },
                new[] { InlineKeyboardButton.WithCallbackData("◀ Назад", "history") }
            };

            return (text, new InlineKeyboardMarkup(buttons));
        }

        // Экран активных сделок (PendingTrades) с пагинацией
        public (string Text, InlineKeyboardMarkup Keyboard) GetPendingTradesScreen(
            List<(string TradeId, Trade Trade, int MessageId, DateTime CreatedAt)> pendingTrades, int page, int total,
            UserSettings settings)
        {
            string text = pendingTrades.Count == 0
                ? GetText("no_pending_trades", settings.Language)
                : GetText("pending_trades", settings.Language,
                    string.Join("\n",
                        pendingTrades.Select(t =>
                            $"Тикер: {t.Trade.Ticker}, PnL: {t.Trade.PnL}% ({t.CreatedAt:yyyy-MM-dd HH:mm})")));
            int pageSize = 5;
            int totalPages = (int)Math.Ceiling(total / (double)pageSize);
            var buttons = new List<InlineKeyboardButton[]>();

            if (pendingTrades.Count > 0)
            {
                foreach (var (tradeId, trade, _, _) in pendingTrades)
                    buttons.Add(new[]
                        { InlineKeyboardButton.WithCallbackData($"{trade.Ticker} ({trade.PnL}%)", $"edit_{tradeId}") });
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(GetText("all_pending_cleared", settings.Language),
                        "clearpending")
                });
            }

            var pagination = new List<InlineKeyboardButton>();
            if (page > 1) pagination.Add(InlineKeyboardButton.WithCallbackData("◀", $"pending_page_{page - 1}"));
            for (int i = Math.Max(1, page - 2); i <= Math.Min(totalPages, page + 2); i++)
                pagination.Add(InlineKeyboardButton.WithCallbackData(i == page ? $"[{i}]" : i.ToString(),
                    $"pending_page_{i}"));
            if (page < totalPages)
                pagination.Add(InlineKeyboardButton.WithCallbackData("▶", $"pending_page_{page + 1}"));
            if (pagination.Any()) buttons.Add(pagination.ToArray());

            buttons.Add(new[]
                { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") });
            return (text, new InlineKeyboardMarkup(buttons));
        }

        // Меню статистики
        public (string Text, InlineKeyboardMarkup Keyboard) GetStatsMenu(UserSettings settings)
        {
            string text = GetText("stats_menu", settings.Language);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(GetText("period_week", settings.Language), "statsperiod_week")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(GetText("period_month", settings.Language),
                        "statsperiod_month")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(GetText("period_all", settings.Language), "statsperiod_all")
                },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") }
            });
            return (text, keyboard);
        }

        // Результат статистики за период
        public (string Text, InlineKeyboardMarkup Keyboard) GetStatsResult(List<Trade> trades, string period,
            UserSettings settings)
        {
            int totalTrades = trades.Count;
            decimal totalPnL = trades.Sum(t => t.PnL);
            int profitable = trades.Count(t => t.PnL > 0);
            int losing = trades.Count(t => t.PnL < 0);
            int winRate = totalTrades > 0 ? (int)((double)profitable / totalTrades * 100) : 0;
            string periodText = GetText($"period_{period}", settings.Language);
            string text = GetText("stats_result", settings.Language, periodText, totalTrades, totalPnL.ToString("F2"),
                profitable, losing, winRate);
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
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(GetText("period_week", settings.Language),
                        "advstatsperiod_week")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(GetText("period_month", settings.Language),
                        "advstatsperiod_month")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(GetText("period_all", settings.Language),
                        "advstatsperiod_all")
                },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") }
            });
            return (text, keyboard);
        }

        // Экран истории сделок (список с фильтрами и пагинацией)
        public (string Text, InlineKeyboardMarkup Keyboard) GetHistoryScreen(List<Trade> trades, int page,
            string period, string filter, UserSettings settings)
        {
            string text = GetText("history_title", settings.Language);
            if (trades.Count == 0)
            {
                text += "\n" + GetText("no_trades", settings.Language);
            }
            else
            {
                // Структурированный вывод с эмодзи и таблицей
                text += "📋 Список сделок:\n";
                text += "```plaintext\n";
                text += $"{"ID",-8} {"Тикер",-10} {"Напр.",-6} {"PnL",-8} {"Дата",-12}\n";
                text += new string('-', 48) + "\n";
                var pageTrades = trades.Skip((page - 1) * 5).Take(5).ToList();
                foreach (var t in pageTrades)
                {
                    text += $"{t.Id,-8} {t.Ticker,-10} {t.Direction,-6} {t.PnL:F2}%, {t.Date:yyyy-MM-dd}\n";
                }

                text += "```";
                text += $"\nСтраница {page} из {(int)Math.Ceiling(trades.Count / 5.0)}";
            }

            var buttons = new List<InlineKeyboardButton[]>();
            if (trades.Count > 0)
            {
                // Простая пагинация
                int totalPages = (int)Math.Ceiling(trades.Count / 5.0);
                var paginationButtons = new List<InlineKeyboardButton>();
                if (page > 1)
                    paginationButtons.Add(InlineKeyboardButton.WithCallbackData("◀ Пред.",
                        $"history_page_{page - 1}_period_{period}_filter_{filter ?? "none"}"));
                paginationButtons.Add(InlineKeyboardButton.WithCallbackData($"[{page}]",
                    $"history_page_{page}_period_{period}_filter_{filter ?? "none"}"));
                if (page < totalPages)
                    paginationButtons.Add(InlineKeyboardButton.WithCallbackData("След. ▶",
                        $"history_page_{page + 1}_period_{period}_filter_{filter ?? "none"}"));
                if (paginationButtons.Any()) buttons.Add(paginationButtons.ToArray());

                // Кнопка экспорта
                buttons.Add(new[]
                    { InlineKeyboardButton.WithCallbackData(GetText("export_success", settings.Language), "export") });

                // Кнопка фильтров
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🔍 Фильтры", $"history_filter_menu") });
            }

            // Кнопка возврата в меню
            buttons.Add(new[]
                { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") });

            return (text, new InlineKeyboardMarkup(buttons));
        }

        // Экран подробностей сделки (по нажатию из истории)
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
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"Язык: {(settings.Language == "ru" ? "Русский" : "English")}", "settings_language")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"Уведомления: {(settings.NotificationsEnabled ? "Вкл" : "Выкл")}", "settings_notifications")
                },
                new[] { InlineKeyboardButton.WithCallbackData("Избранные тикеры", "settings_tickers") },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(GetText("settings_reset", settings.Language), "resetsettings")
                },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") }
            };
            return (text, new InlineKeyboardMarkup(buttons));
        }

        // Промпт ввода для настройки (например, добавление избранного тикера)
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

        public (string Text, InlineKeyboardMarkup Keyboard) GetFavoriteTickersMenu(UserSettings settings)
        {
            string text;
            if (settings.FavoriteTickers.Count == 0)
                text = "⭐ Избранные тикеры: (список пуст)";
            else
                text = "⭐ Избранные тикеры:\n" + string.Join(", ", settings.FavoriteTickers);
            var buttons = new List<InlineKeyboardButton[]>();
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("➕ Добавить тикер", "add_favorite_ticker"),
                InlineKeyboardButton.WithCallbackData("➖ Удалить тикер", "remove_favorite_ticker")
            });
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("◀ Назад", "settings") });
            return (text, new InlineKeyboardMarkup(buttons));
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetRemoveFavoriteTickerMenu(UserSettings settings)
        {
            string text = "❌ Выберите тикер для удаления:";
            var buttons = new List<InlineKeyboardButton[]>();
            if (settings.FavoriteTickers.Count == 0)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("◀ Назад", "settings_tickers") });
                return (text + "\n(список пуст)", new InlineKeyboardMarkup(buttons));
            }

            foreach (var ticker in settings.FavoriteTickers)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(ticker, $"remove_ticker_{ticker}") });
            }

            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("◀ Назад", "settings_tickers") });
            return (text, new InlineKeyboardMarkup(buttons));
        }
    }
}
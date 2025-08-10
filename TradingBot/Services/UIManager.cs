using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Telegram.Bot.Types.ReplyMarkups;
using TradingBot.Models;

namespace TradingBot.Services
{
    // Локализованные ресурсы интерфейса (русский и английский)
    public class UIManager
    {
        // Очистка callback_data от недопустимых символов для Telegram API
        public static string SanitizeCallbackData(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            // Убираем пробелы, проценты, двоеточия, и другие проблемные символы
            var result = input
                .Replace(" ", "")
                .Replace("%", "PCT")
                .Replace(":", "_")
                .Replace("/", "_")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("&", "AND")
                .Replace("#", "NUM")
                .Replace("@", "AT")
                .Replace("$", "USD")
                .Replace("€", "EUR")
                .Replace("£", "GBP")
                .Replace("+", "PLUS")
                .Replace("-", "MINUS")
                .Replace("=", "EQ")
                .Replace("?", "Q")
                .Replace("!", "")
                .Replace(",", "_")
                .Replace(".", "_");

            // Удаляем все не-ASCII символы (эмодзи, кириллица и пр.) чтобы гарантировать <=64 байт
            result = Regex.Replace(result, @"[^\x00-\x7F]", string.Empty);

            // Обрезаем до 20 символов чтобы весь callback_data не превышал 64 байта
            if (result.Length > 20)
                result = result.Substring(0, 20);

            return result;
        }

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
                ["trade_sent_notion"] = "🌐 Данные отправлены в Notion.",
                ["trade_not_saved"] = "❌ Не удалось сохранить сделку.",
                ["notion_save_error"] = "Проверьте настройки Notion API.",
                ["local_save_error"] = "Проблемы с локальной базой данных.",
                ["error_saving_trade"] = "⚠️ Ошибка при сохранении сделки.",
                ["trade_expired"] = "⏰ Сделка устарела. Начните заново.",
                ["trade_deleted"] = "🗑️ Сделка удалена.",
                ["all_pending_cleared"] = "🧹 Все активные сделки очищены.",
                ["no_trades"] = "📉 Нет сделок за выбранный период.",
                ["invalid_input"] = "⚠️ Некорректный ввод. Попробуйте снова.",
                ["invalid_pnl"] = "⚠️ Введите корректное число для PnL (например, +5.25).",
                ["error_getting_image"] = "❌ Ошибка при получении изображения.",
                ["error_processing_image"] = "❌ Ошибка при обработке изображения.",
                ["rate_limit"] = "⏳ Слишком много запросов. Подождите минуту.",
                ["support_contact"] = "📞 Свяжитесь с поддержкой: @support_username",
                ["win_streak"] = "🔥 Серия побед: {0} сделок подряд!",
                ["loss_streak"] = "💔 Серия убытков: {0} сделок подряд. Не сдавайтесь!",
                ["ticker_added"] = "✅ Тикер {0} добавлен в избранные.",
                ["ticker_removed"] = "✅ Тикер {0} удалён из избранных.",

                // Тексты шагов
                ["step_1"] = "🟩⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜ Шаг 1/14: Выберите тикер",
                ["step_2"] = "🟩🟩⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜ Шаг 2/14: Выберите аккаунт",
                ["step_3"] = "🟩🟩🟩⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜ Шаг 3/14: Выберите сессию",
                ["step_4"] = "🟩🟩🟩🟩⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜ Шаг 4/14: Выберите позицию (LONG/SHORT)",
                ["step_5"] = "🟩🟩🟩🟩🟩⬜⬜⬜⬜⬜⬜⬜⬜⬜ Шаг 5/14: Выберите направление (LONG/SHORT)",
                ["step_6"] = "🟩🟩🟩🟩🟩🟩⬜⬜⬜⬜⬜⬜⬜⬜ Шаг 6/14: Выберите контекст сделки",
                ["step_7"] = "🟩🟩🟩🟩🟩🟩🟩⬜⬜⬜⬜⬜⬜⬜ Шаг 7/14: Выберите сетап/стратегию",
                ["step_8"] = "🟩🟩🟩🟩🟩🟩🟩🟩⬜⬜⬜⬜⬜⬜ Шаг 8/14: Укажите риск (%)",
                ["step_9"] = "🟩🟩🟩🟩🟩🟩🟩🟩🟩⬜⬜⬜⬜⬜ Шаг 9/14: Укажите соотношение R:R",
                ["step_10"] = "🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩⬜⬜⬜⬜ Шаг 10/14: Выберите результат сделки",
                ["step_11"] = "🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩⬜⬜⬜ Шаг 11/14: Укажите прибыль (%)",
                ["step_12"] = "🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩⬜⬜ Шаг 12/14: Выберите эмоции",
                ["step_13"] = "🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩⬜ Шаг 13/14: Введите детали входа",
                ["step_14"] = "🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩 Шаг 14/14: Введите заметку",

                ["trade_preview"] =
                    "✅ Проверьте введённые данные:\n\n" +
                    "📌 Тикер: {0}\n📌 Аккаунт: {1}\n📌 Сессия: {2}\n📌 Позиция: {3}\n📌 Направление: {4}\n" +
                    "📌 Контекст: {5}\n📌 Сетап: {6}\n📌 Результат: {7}\n📌 R:R = {8}\n📌 Риск: {9}%\n📌 Прибыль: {10}%\n" +
                    "😃 Эмоции: {11}\n🔍 Детали входа: {12}\n📝 Заметка: {13}",

                ["confirm_trade"] = "Сохранить сделку?",
                ["edit_field"] = "✏️ Какое поле исправить?",

                ["pending_trades"] = "⏳ Активные сделки:\n{0}",
                ["no_pending_trades"] = "⏳ Нет активных сделок.",
                ["stats_menu"] = "📊 Выберите период для статистики:",
                ["stats_result"] = "📊 Статистика {0}:\nВсего сделок: {1}\nОбщий PnL: {2}%\nПрибыльных: {3}\nУбыточных: {4}\nWin Rate: {5}%",

                ["advanced_stats"] = "📈 Сделок: {0}\nОбщий PnL: {1}%\nСредний PnL: {2}%\nЛучший: {3}%\nХудший: {4}%\nWin Rate: {5}%",
                ["date_label"] = "📅 Дата",
                ["pnl_label"] = "📈 Накопленный PnL",
                ["equity_curve"] = "📈 Кривая эквити",
                ["error_graph"] = "⚠️ Ошибка при создании графика.",
                ["export_success"] = "📄 Экспорт завершён успешно!",

                ["settings_menu"] = "⚙️ Настройки:",
                ["settings_updated"] = "✅ Настройки обновлены!",
                ["settings_reset"] = "🔄 Настройки сброшены.",
                ["main_menu_button"] = "◀️ Главное меню",
                ["other"] = "Другое...",
                ["prefill_last"] = "Как в последней",
                ["cancel"] = "🚫 Отмена",
                ["skip"] = "➡ Пропустить",
                ["input_manually"] = "⌨️ Ввести вручную",
                ["confirm"] = "✅ Подтвердить",
                ["edit"] = "✏️ Редактировать",
                ["delete"] = "🗑 Удалить",
                ["retry"] = "🔄 Повторить",
                ["period_week"] = "Неделя",
                ["period_month"] = "Месяц",
                ["period_all"] = "Всё время",
                ["support"] = "🆘 Поддержка",
                ["help_menu"] = "💡 Выберите раздел помощи:",
                ["whats_new"] = "📣 Что нового",

                // Тексты для ввода
                ["input_ticker"] = "📝 Введите тикер (например: BTC/USDT):",
                ["input_account"] = "📝 Введите название аккаунта:",
                ["input_session"] = "📝 Введите торговую сессию:",
                ["input_position"] = "📝 Введите тип позиции:",
                ["input_direction"] = "📝 Введите направление:",
                ["input_risk"] = "📝 Введите размер риска в %:",
                ["input_rr"] = "📝 Введите соотношение R:R:",
                ["input_profit"] = "📝 Введите прибыль в %:",
                ["input_context"] = "📝 Введите контекст сделки:",
                ["input_setup"] = "📝 Введите сетап/стратегию:",
                ["input_result"] = "📝 Введите результат сделки:",
                ["input_emotions"] = "📝 Введите эмоции:",
                ["input_entry"] = "📝 Введите детали входа:",
                ["input_note"] = "📝 Введите заметку:"
            },
            // Английские тексты (можно аналогично заполнить или оставить пустыми для примера)
            ["en"] = new Dictionary<string, string>
            {
                ["main_menu_button"] = "◀️ Main Menu",
                ["skip"] = "➡ Skip",
                ["cancel"] = "🚫 Cancel",
                ["confirm"] = "✅ Confirm",
                ["edit"] = "✏️ Edit",
                ["delete"] = "🗑 Delete"
                // ... остальные ключи для EN инициализированы по аналогии ...
            }
        };

        // Внутренние списки опций (подставляются из Notion или SQLite)
        private List<string> _emotionOptions = new();
        private List<string> _sessionOptions = new();
        private List<string> _accountOptions = new();
        private List<string> _contextOptions = new();
        private List<string> _setupOptions = new();
        private List<string> _resultOptions = new();
        private List<string> _positionOptions = new();
        private List<string> _directionOptions = new();

        // Популярные тикеры можно задать статически (для автоподстановки на шаге 1)
        public static readonly List<string> PopularTickers = new() { "BTC/USDT", "ETH/USDT", "BNB/USDT", "SOL/USDT", "ADA/USDT" };

        public string GetText(string key, string language, params object[] args)
        {
            if (!_resources.TryGetValue(language, out var dict) || !dict.ContainsKey(key))
                return key;
            var text = dict[key];
            return args.Length > 0 ? string.Format(text, args) : text;
        }

        public InlineKeyboardMarkup GetMainMenu(UserSettings settings)
        {
            // Компактное расположение меню, без "Активных сделок"
            var buttons = new List<InlineKeyboardButton[]>
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("➕ Добавить сделку", "start_trade"),
                    InlineKeyboardButton.WithCallbackData("📈 Моя статистика", "stats")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📜 История сделок", "history"),
                    InlineKeyboardButton.WithCallbackData("⚙️ Настройки", "settings")
                },
                new[] { InlineKeyboardButton.WithCallbackData("🆘 Помощь", "help") }
            };
            return new InlineKeyboardMarkup(buttons);
        }

        public InlineKeyboardMarkup GetErrorKeyboard(UserSettings settings)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["retry"], "retry"),
                    InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["main_menu_button"], "main")
                }
            });
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetOnboardingScreen(int step, string language)
        {
            string text = step switch
            {
                1 => GetText("onboarding_1", language),
                2 => GetText("onboarding_2", language),
                3 => GetText("onboarding_3", language),
                _ => GetText("welcome", language)
            };

            var buttons = new List<InlineKeyboardButton[]>();
            
            if (step < 3)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Далее ▶", $"onboarding_{step + 1}") });
            }
            else
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🚀 Начать!", "main") });
            }

            if (step > 1)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("◀ Назад", $"onboarding_{step - 1}") });
            }

            return (text, new InlineKeyboardMarkup(buttons));
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetTradeInputScreen(Trade trade, int step, UserSettings settings, string tradeId, Trade? lastTrade = null)
        {
            string preview = _resources[settings.Language]["trade_preview"];
            string formattedPreview = string.Format(preview,
                trade.Ticker ?? "-",
                trade.Account ?? "-",
                trade.Session ?? "-",
                trade.Position ?? "-",
                trade.Direction ?? "-",
                (trade.Context != null && trade.Context.Any()) ? string.Join(", ", trade.Context) : "-",
                (trade.Setup != null && trade.Setup.Any()) ? string.Join(", ", trade.Setup) : "-",
                trade.Result ?? "-",
                trade.RR?.ToString("0.##") ?? "-",
                trade.Risk?.ToString("0.##") ?? "-",
                trade.PnL.ToString("0.##"),
                (trade.Emotions != null && trade.Emotions.Any()) ? string.Join(", ", trade.Emotions) : "-",
                trade.EntryDetails ?? "-",
                trade.Note ?? "-"
            );

            string prompt = _resources[settings.Language][$"step_{step}"];
            var buttons = new List<InlineKeyboardButton[]>();

            switch (step)
            {
                case 1: // Тикер
                    var fav = settings.FavoriteTickers ?? new List<string>();
                    var recent = settings.RecentTickers ?? new List<string>();
                    var tickers = fav.Concat(recent).Concat(PopularTickers).Distinct().Take(5).ToList();
                    foreach (var t in tickers)
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(t, $"set_ticker_{SanitizeCallbackData(t)}_trade_{tradeId}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["other"], $"input_ticker_trade_{tradeId}") });
                    if (!string.IsNullOrEmpty(lastTrade?.Ticker))
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["prefill_last"], $"set_ticker_{SanitizeCallbackData(lastTrade!.Ticker)}_trade_{tradeId}") });
                    break;

                case 2: // Аккаунт
                    foreach (var option in _accountOptions)
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(option, $"set_account_{SanitizeCallbackData(option)}_trade_{tradeId}") });
                    //buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["skip"], $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["input_manually"], $"input_account_trade_{tradeId}") });
                    break;

                case 3: // Сессия - добавляем предустановленные варианты
                    // Добавляем популярные сессии по две в ряд
                    var defaultSessions = new[] { "ASIA", "FRANKFURT", "LONDON", "NEW YORK" };
                    buttons.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🇯🇵 ASIA", $"set_session_ASIA_trade_{tradeId}"),
                        InlineKeyboardButton.WithCallbackData("🇩�� FRANKFURT", $"set_session_FRANKFURT_trade_{tradeId}")
                    });
                    buttons.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🇬🇧 LONDON", $"set_session_LONDON_trade_{tradeId}"),
                        InlineKeyboardButton.WithCallbackData("🇺🇸 NEW YORK", $"set_session_NEWYORK_trade_{tradeId}")
                    });

                    // Кастомные сессии из Notion/SQLite — тоже по две в ряд
                    var customSessions = _sessionOptions.Where(s => !defaultSessions.Contains(s)).ToList();
                    for (int i = 0; i < customSessions.Count; i += 2)
                    {
                        if (i + 1 < customSessions.Count)
                            buttons.Add(new[]
                            {
                                InlineKeyboardButton.WithCallbackData(customSessions[i], $"set_session_{SanitizeCallbackData(customSessions[i])}_trade_{tradeId}"),
                                InlineKeyboardButton.WithCallbackData(customSessions[i + 1], $"set_session_{SanitizeCallbackData(customSessions[i + 1])}_trade_{tradeId}")
                            });
                        else
                            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(customSessions[i], $"set_session_{SanitizeCallbackData(customSessions[i])}_trade_{tradeId}") });
                    }

                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["input_manually"], $"input_session_trade_{tradeId}") });
                    break;

                case 4: // Позиция - добавляем кнопку "Пропустить"
                    var defaultPositions = new[] { "Long", "Short" };
                    // Long/Short — в один ряд
                    buttons.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🟢 LONG", $"set_position_Long_trade_{tradeId}"),
                        InlineKeyboardButton.WithCallbackData("🔴 SHORT", $"set_position_Short_trade_{tradeId}")
                    });

                    // Остальные опции — по две в ряд
                    var extraPositions = _positionOptions.Where(p => !defaultPositions.Contains(p)).ToList();
                    for (int i = 0; i < extraPositions.Count; i += 2)
                    {
                        if (i + 1 < extraPositions.Count)
                            buttons.Add(new[]
                            {
                                                            InlineKeyboardButton.WithCallbackData(extraPositions[i], $"set_position_{SanitizeCallbackData(extraPositions[i])}_trade_{tradeId}"),
                            InlineKeyboardButton.WithCallbackData(extraPositions[i + 1], $"set_position_{SanitizeCallbackData(extraPositions[i + 1])}_trade_{tradeId}")
                            });
                        else
                            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(extraPositions[i], $"set_position_{SanitizeCallbackData(extraPositions[i])}_trade_{tradeId}") });
                    }
                    break;

                case 5: // Направление - добавляем кнопку "Пропустить"
                    var defaultDirections = new[] { "Long", "Short" };
                    // Long/Short — в один ряд
                    buttons.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🟢 LONG", $"set_direction_Long_trade_{tradeId}"),
                        InlineKeyboardButton.WithCallbackData("🔴 SHORT", $"set_direction_Short_trade_{tradeId}")
                    });
                    // Подтип сделки (Type): Reversal / Continuation в одном ряду
                    buttons.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔴 Reversal", $"set_setup_REVR_trade_{tradeId}"),
                        InlineKeyboardButton.WithCallbackData("🟢 Continuation", $"set_setup_CONT_trade_{tradeId}")
                    });

                    // Остальные опции — по две в ряд
                    var extraDirections = _directionOptions.Where(d => !defaultDirections.Contains(d)).ToList();
                    for (int i = 0; i < extraDirections.Count; i += 2)
                    {
                        if (i + 1 < extraDirections.Count)
                            buttons.Add(new[]
                            {
                                                            InlineKeyboardButton.WithCallbackData(extraDirections[i], $"set_direction_{SanitizeCallbackData(extraDirections[i])}_trade_{tradeId}"),
                            InlineKeyboardButton.WithCallbackData(extraDirections[i + 1], $"set_direction_{SanitizeCallbackData(extraDirections[i + 1])}_trade_{tradeId}")
                            });
                        else
                            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(extraDirections[i], $"set_direction_{SanitizeCallbackData(extraDirections[i])}_trade_{tradeId}") });
                    }
                    break;

                case 6: // Контекст
                    foreach (var option in _contextOptions)
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(option, $"set_context_{SanitizeCallbackData(option)}_trade_{tradeId}") });
                    //buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["skip"], $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["input_manually"], $"input_context_trade_{tradeId}") });
                    break;

                case 7: // Сетап
                    foreach (var option in _setupOptions)
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(option, $"set_setup_{SanitizeCallbackData(option)}_trade_{tradeId}") });
                   // buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["skip"], $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["input_manually"], $"input_setup_trade_{tradeId}") });
                    break;

                case 8: // Риск
                case 9: // R:R
                case 11: // Прибыль
                    string field = step switch { 8 => "risk", 9 => "rr", 11 => "profit", _ => "" };
                    if (step == 8)
                    {
                        // Risk быстрые кнопки в один ряд
                        buttons.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("0.5%", $"set_risk_0_5_trade_{tradeId}"),
                            InlineKeyboardButton.WithCallbackData("1%", $"set_risk_1_0_trade_{tradeId}"),
                            InlineKeyboardButton.WithCallbackData("2%", $"set_risk_2_0_trade_{tradeId}"),
                            InlineKeyboardButton.WithCallbackData("3%", $"set_risk_3_0_trade_{tradeId}")
                        });
                    }
                    if (step == 9)
                    {
                        // RR быстрые кнопки в один ряд
                        buttons.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("1:1", $"set_rr_1_1_trade_{tradeId}"),
                            InlineKeyboardButton.WithCallbackData("1:2", $"set_rr_1_2_trade_{tradeId}"),
                            InlineKeyboardButton.WithCallbackData("1:3", $"set_rr_1_3_trade_{tradeId}"),
                            InlineKeyboardButton.WithCallbackData("1:4", $"set_rr_1_4_trade_{tradeId}")
                        });
                    }
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["input_manually"], $"input_{field}_trade_{tradeId}") });
                    break;

                case 10: // Результат
                    // Быстрые кнопки в один ряд: TP / BE / SL / SK
                    buttons.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🟢 TP", $"set_result_TP_trade_{tradeId}"),
                        InlineKeyboardButton.WithCallbackData("🟠 BE", $"set_result_BE_trade_{tradeId}"),
                        InlineKeyboardButton.WithCallbackData("🔴 SL", $"set_result_SL_trade_{tradeId}"),
                        InlineKeyboardButton.WithCallbackData("🟣 SK", $"set_result_SK_trade_{tradeId}")
                    });
                    foreach (var option in _resultOptions)
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(option, $"set_result_{SanitizeCallbackData(option)}_trade_{tradeId}") });
                    //buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["skip"], $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["input_manually"], $"input_result_trade_{tradeId}") });
                    break;

                case 12: // Эмоции
                    var emo = _emotionOptions.Any() ? _emotionOptions : new List<string> { "😎 Уверенность", "😨 Страх", "🤑 Жадность", "🤔 Сомнения" };
                    // по две в ряд
                    for (int i = 0; i < emo.Count; i += 2)
                    {
                        if (i + 1 < emo.Count)
                            buttons.Add(new[]
                            {
                                                        InlineKeyboardButton.WithCallbackData(emo[i], $"set_emotions_{SanitizeCallbackData(emo[i])}_trade_{tradeId}"),
                        InlineKeyboardButton.WithCallbackData(emo[i + 1], $"set_emotions_{SanitizeCallbackData(emo[i + 1])}_trade_{tradeId}")
                            });
                        else
                            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(emo[i], $"set_emotions_{SanitizeCallbackData(emo[i])}_trade_{tradeId}") });
                    }
                    //buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["skip"], $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["input_manually"], $"input_emotions_trade_{tradeId}") });
                    break;

                case 13: // Детали входа
                   // buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["skip"], $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["input_manually"], $"input_entry_trade_{tradeId}") });
                    break;

                case 14: // Заметка
                   // buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["skip"], $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["input_manually"], $"input_note_trade_{tradeId}") });
                    break;
            }

            // Добавляем общие кнопки навигации
            if (step > 1 && step <= 14)
                buttons.Add(new[] {
                    InlineKeyboardButton.WithCallbackData("◀️ Назад", $"back_trade_{tradeId}_step_{step}"),
                    InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["skip"], $"skip_trade_{tradeId}_step_{step}")
                });

            buttons.Add(new[] {
                InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["cancel"], "cancel"),
                InlineKeyboardButton.WithCallbackData("✅ Сохранить", $"save_trade_{tradeId}")
            });

            return ($"{formattedPreview}\n\n{prompt}", new InlineKeyboardMarkup(buttons));
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetTradeConfirmationScreen(Trade trade, string tradeId, UserSettings settings)
        {
            string text = string.Format(_resources[settings.Language]["trade_preview"],
                trade.Ticker ?? "-", trade.Account ?? "-", trade.Session ?? "-", trade.Position ?? "-",
                trade.Direction ?? "-", (trade.Context != null && trade.Context.Any()) ? string.Join(", ", trade.Context) : "-",
                (trade.Setup != null && trade.Setup.Any()) ? string.Join(", ", trade.Setup) : "-",
                trade.Result ?? "-", trade.RR?.ToString("0.##") ?? "-", trade.Risk?.ToString("0.##") ?? "-",
                trade.PnL.ToString("0.##"),
                (trade.Emotions != null && trade.Emotions.Any()) ? string.Join(", ", trade.Emotions) : "-",
                trade.EntryDetails ?? "-", trade.Note ?? "-");

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["confirm"], $"confirm_trade_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["edit"], $"edit_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["delete"], $"delete_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["main_menu_button"], "main") }
            });
            return (text, keyboard);
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetEditFieldMenu(Trade trade, string tradeId, UserSettings settings)
        {
            string preview = string.Format(_resources[settings.Language]["trade_preview"],
                trade.Ticker ?? "-", trade.Account ?? "-", trade.Session ?? "-", trade.Position ?? "-",
                trade.Direction ?? "-", (trade.Context != null && trade.Context.Any()) ? string.Join(", ", trade.Context) : "-",
                (trade.Setup != null && trade.Setup.Any()) ? string.Join(", ", trade.Setup) : "-",
                trade.Result ?? "-", trade.RR?.ToString("0.##") ?? "-", trade.Risk?.ToString("0.##") ?? "-",
                trade.PnL.ToString("0.##"),
                (trade.Emotions != null && trade.Emotions.Any()) ? string.Join(", ", trade.Emotions) : "-",
                trade.EntryDetails ?? "-", trade.Note ?? "-");

            // Готовим плоский список кнопок с эмодзи
            var flat = new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("📌 Тикер", $"editfield_ticker_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData("🧾 Аккаунт", $"editfield_account_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData("🕒 Сессия", $"editfield_session_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData("📐 Позиция", $"editfield_position_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData("↕️ Направление", $"editfield_direction_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData("🧩 Контекст", $"editfield_context_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData("🧠 Сетап", $"editfield_setup_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData("🎯 Результат", $"editfield_result_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData("⚖️ R:R", $"editfield_rr_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData("⚠️ Риск %", $"editfield_risk_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData("📈 Profit %", $"editfield_profit_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData("🙂 Эмоции", $"editfield_emotions_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData("🔍 Детали входа", $"editfield_entry_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData("📝 Заметка", $"editfield_note_trade_{tradeId}")
            };

            // Складываем по 2 в ряд
            var rows = new List<InlineKeyboardButton[]>();
            for (int i = 0; i < flat.Count; i += 2)
            {
                if (i + 1 < flat.Count)
                    rows.Add(new[] { flat[i], flat[i + 1] });
                else
                    rows.Add(new[] { flat[i] });
            }

            // В конце — главное меню
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["main_menu_button"], "main") });

            return (preview, new InlineKeyboardMarkup(rows));
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetInputPrompt(string field, UserSettings settings, string tradeId)
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
                // Long/Short — по две в ряд
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Long", "historyfilter_direction_Long"),
                    InlineKeyboardButton.WithCallbackData("Short", "historyfilter_direction_Short")
                },
                new[] { InlineKeyboardButton.WithCallbackData("◀ Назад", "history") }
            };

            return (text, new InlineKeyboardMarkup(buttons));
        }

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
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData($"{trade.Ticker} ({trade.PnL}%)", $"edit_{tradeId}") });
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("🧹 Очистить активные", "clearpending")
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

        public (string Text, InlineKeyboardMarkup Keyboard) GetStatsResult(List<Trade> trades, string period, UserSettings settings)
        {
            int totalTrades = trades.Count;
            decimal totalPnL = trades.Sum(t => t.PnL);
            int profitable = trades.Count(t => t.PnL > 0);
            int losing = totalTrades - profitable;
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

        public (string Text, InlineKeyboardMarkup Keyboard) GetHistoryScreen(List<Trade> trades, int page, string period, string filter, UserSettings settings)
        {
            int pageSize = 5;
            var ordered = trades.OrderByDescending(t => t.Date).ToList();
            int totalPages = Math.Max(1, (int)Math.Ceiling(ordered.Count / (double)pageSize));
            page = Math.Min(Math.Max(page, 1), totalPages);
            var pageTrades = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var sb = new System.Text.StringBuilder();
            if (!ordered.Any())
            {
                sb.AppendLine(GetText("no_trades", settings.Language));
            }
            else
            {
                foreach (var t in pageTrades)
                {
                    string date = t.Date.ToString("dd.MM.yyyy HH:mm");
                    string ticker = t.Ticker ?? "-";
                    string direction = t.Direction ?? "-";
                    string ctx = (t.Context != null && t.Context.Any()) ? string.Join(", ", t.Context) : "-";
                    string sign = t.PnL >= 0 ? "+" : "-";
                    string absPnl = Math.Abs(t.PnL).ToString("F2");
                    sb.AppendLine($"📅 {date}");
                    sb.AppendLine($"📈 Ticker: {ticker}");
                    sb.AppendLine($"↕ Direction: {direction}");
                    sb.AppendLine($"💰 PnL: {sign}{absPnl}%");
                    sb.AppendLine($"📄 Context: {ctx}");
                    sb.AppendLine("");
                }
                sb.AppendLine($"Страница {page} из {totalPages}");
            }

            var buttons = new List<InlineKeyboardButton[]>();
            if (ordered.Any())
            {
                var pag = new List<InlineKeyboardButton>();
                if (page > 1) pag.Add(InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"history_page_{page - 1}_period_{period}_filter_{filter ?? "none"}"));
                if (page < totalPages) pag.Add(InlineKeyboardButton.WithCallbackData("…Ещё", $"history_page_{page + 1}_period_{period}_filter_{filter ?? "none"}"));
                if (pag.Any()) buttons.Add(pag.ToArray());

                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🔍 Фильтры", $"history_filter_menu") });
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("💾 Экспорт в CSV", "export") });
            }

            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") });
            return (sb.ToString(), new InlineKeyboardMarkup(buttons));
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetHistoryFiltersMenu(UserSettings settings)
        {
            string text = "🔍 Фильтры истории:\nВыберите категорию:";
            var rows = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData("📅 По дате", "historyfilter_date_menu") },
                new[] { InlineKeyboardButton.WithCallbackData("📈 По тикеру", "historyfilter_ticker_menu") },
                new[] { InlineKeyboardButton.WithCallbackData("↕ По направлению", "historyfilter_direction_menu") },
                new[] { InlineKeyboardButton.WithCallbackData("✅/❌ По результату", "historyfilter_result_menu") },
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "history") }
            };
            return (text, new InlineKeyboardMarkup(rows));
        }

        public InlineKeyboardMarkup GetHistoryFilterSubmenu(string type, UserSettings settings)
        {
            var rows = new List<InlineKeyboardButton[]>();
            switch (type)
            {
                case "date":
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData("7 дней", "historyfilter_date_7d") });
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData("30 дней", "historyfilter_date_30d") });
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData("Все время", "historyfilter_date_all") });
                    break;
                case "ticker":
                    if (settings.FavoriteTickers.Any())
                    {
                        foreach (var t in settings.FavoriteTickers.Take(12))
                        {
                            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(t, $"historyfilter_ticker_{SanitizeCallbackData(t)}") });
                        }
                    }
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData("Все", "historyfilter_ticker_all") });
                    break;
                case "direction":
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData("LONG", "historyfilter_direction_Long") });
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData("SHORT", "historyfilter_direction_Short") });
                    break;
                case "result":
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData("✅ Профит", "historyfilter_result_profit") });
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData("❌ Убыток", "historyfilter_result_loss") });
                    break;
            }
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "history_filter_menu") });
            return new InlineKeyboardMarkup(rows);
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetTradeDetailScreen(Trade trade, UserSettings settings)
        {
            string text =
                $"🧾 Сделка #{trade.Id}\n" +
                $"📅 Дата: {trade.Date:yyyy-MM-dd HH:mm}\n" +
                $"📌 Тикер: {trade.Ticker ?? "-"}\n" +
                $"🧾 Аккаунт: {trade.Account ?? "-"} | 🕒 Сессия: {trade.Session ?? "-"}\n" +
                $"📐 Позиция: {trade.Position ?? "-"} | ↕️ Направление: {trade.Direction ?? "-"}\n" +
                $"🎯 Результат: {trade.Result ?? "-"} | R:R: {trade.RR?.ToString("0.##") ?? "-"} | Риск: {trade.Risk?.ToString("0.##") ?? "-"}%\n" +
                $"📈 PnL: {trade.PnL:0.##}%\n" +
                $"🧩 Контекст: {(trade.Context != null && trade.Context.Any() ? string.Join(", ", trade.Context) : "-" )}\n" +
                $"🧠 Сетап: {(trade.Setup != null && trade.Setup.Any() ? string.Join(", ", trade.Setup) : "-" )}\n" +
                $"🙂 Эмоции: {(trade.Emotions != null && trade.Emotions.Any() ? string.Join(", ", trade.Emotions) : "-" )}\n" +
                $"🔍 Детали входа: {trade.EntryDetails ?? "-"}\n" +
                $"📝 Заметка: {trade.Note ?? "-"}";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") }
            });
            return (text, keyboard);
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetFavoriteTickersMenu(UserSettings settings)
        {
            string text = "📈 Избранные тикеры:\n\n";
            if (settings.FavoriteTickers.Any())
            {
                text += string.Join(", ", settings.FavoriteTickers);
            }
            else
            {
                text += "Пусто";
            }

            var buttons = new List<InlineKeyboardButton[]>();
            foreach (var ticker in settings.FavoriteTickers)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData($"❌ {ticker}", $"remove_ticker_{SanitizeCallbackData(ticker)}") });
            }
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить тикер", "add_favorite_ticker") });
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "settings") });

            return (text, new InlineKeyboardMarkup(buttons));
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetRemoveFavoriteTickerMenu(UserSettings settings)
        {
            string text = "❌ Выберите тикер для удаления:";
            var buttons = new List<InlineKeyboardButton[]>();

            foreach (var ticker in settings.FavoriteTickers)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData($"❌ {ticker}", $"remove_ticker_{SanitizeCallbackData(ticker)}") });
            }

            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "settings_tickers") });

            return (text, new InlineKeyboardMarkup(buttons));
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetHelpMenu(UserSettings settings)
        {
            string text = "💡 Помощь:\n\n" +
                         "📸 Отправьте скриншот сделки для автоматического заполнения\n" +
                         "⌨️ Или создайте сделку вручную через главное меню\n" +
                         "📊 Просматривайте статистику и анализируйте результаты\n" +
                         "⚙️ Настройте бота под себя в настройках";

            var buttons = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData("🆘 Техподдержка", "support") },
                new[] { InlineKeyboardButton.WithCallbackData("📣 Что нового", "whatsnew") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") }
            };

            return (text, new InlineKeyboardMarkup(buttons));
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetSettingsMenu(UserSettings settings)
        {
            string text =
                "⚙️ Настройки:\n\n" +
                $"🌐 Сменить язык: {(settings.Language == "ru" ? "Русский" : "English")}\n" +
                $"🔔 Уведомления: {(settings.NotificationsEnabled ? "Включены ✅" : "Выключены ❌")}\n" +
                $"📈 Избранные тикеры: {settings.FavoriteTickers.Count}";

            var rows = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData("🌐 Сменить язык", "settings_language") },
                new[] { InlineKeyboardButton.WithCallbackData(settings.NotificationsEnabled ? "🔔 Уведомления: ✅" : "🔔 Уведомления: ❌", "settings_notifications") },
                new[] { InlineKeyboardButton.WithCallbackData("📈 Избранные тикеры", "settings_tickers") },
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "main") }
            };

            return (text, new InlineKeyboardMarkup(rows));
        }

        // Метод установки списков опций, вызывается при каждом открытии /start или /menu
        public void SetSelectOptions(List<string> strategies,
                                     List<string> emotionOptions,
                                     List<string> sessionOptions,
                                     List<string> accountOptions,
                                     List<string> contextOptions,
                                     List<string> setupOptions,
                                     List<string> resultOptions,
                                     List<string> positionOptions,
                                     List<string> directionOptions)
        {
            _emotionOptions = emotionOptions;
            _sessionOptions = sessionOptions;
            _accountOptions = accountOptions;
            _contextOptions = contextOptions;
            _setupOptions = setupOptions;
            _resultOptions = resultOptions;
            _positionOptions = positionOptions;
            _directionOptions = directionOptions;
        }

        public InlineKeyboardMarkup BuildOptionsKeyboard(string field,
                                                         List<string> options,
                                                         string tradeId,
                                                         UserSettings settings,
                                                         int page = 1,
                                                         int pageSize = 24,
                                                         int step = 0,
                                                         HashSet<string>? selected = null)
        {
            selected ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var recents = field switch
            {
                "ticker" => settings.RecentTickers,
                "direction" => settings.RecentDirections,
                _ => new List<string>()
            };
            var preferred = new HashSet<string>(recents.Take(5), StringComparer.OrdinalIgnoreCase);
            var ordered = options
                .OrderByDescending(o => preferred.Contains(o))
                .ThenBy(o => o)
                .ToList();

            int total = ordered.Count;
            int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Min(Math.Max(page, 1), totalPages);
            var pageSlice = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var rows = new List<InlineKeyboardButton[]>();
            if (field.Equals("direction", StringComparison.OrdinalIgnoreCase) ||
                field.Equals("position", StringComparison.OrdinalIgnoreCase) ||
                field.Equals("session", StringComparison.OrdinalIgnoreCase))
            {
                // Жёстко по две в ряд
                for (int i = 0; i < pageSlice.Count && rows.Count < 8; i += 2)
                {
                    if (i + 1 < pageSlice.Count)
                        rows.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData((selected.Contains(pageSlice[i]) ? "✅ " : "") + pageSlice[i], $"set_{field}_{SanitizeCallbackData(pageSlice[i])}_trade_{tradeId}"),
                            InlineKeyboardButton.WithCallbackData((selected.Contains(pageSlice[i + 1]) ? "✅ " : "") + pageSlice[i + 1], $"set_{field}_{SanitizeCallbackData(pageSlice[i + 1])}_trade_{tradeId}")
                        });
                    else
                        rows.Add(new[] { InlineKeyboardButton.WithCallbackData((selected.Contains(pageSlice[i]) ? "✅ " : "") + pageSlice[i], $"set_{field}_{SanitizeCallbackData(pageSlice[i])}_trade_{tradeId}") });
                }
            }
            else
            {
                int i = 0;
                while (i < pageSlice.Count && rows.Count < 8)
                {
                    int len = pageSlice[i].Length;
                    int perRow = len <= 8 ? 4 : len <= 12 ? 3 : 2;
                    var row = new List<InlineKeyboardButton>();
                    for (int j = 0; j < perRow && i < pageSlice.Count; j++, i++)
                    {
                        string v = pageSlice[i];
                        string text = (selected.Contains(v) ? "✅ " : "") + v;
                        row.Add(InlineKeyboardButton.WithCallbackData(text, $"set_{field}_{SanitizeCallbackData(v)}_trade_{tradeId}"));
                    }
                    rows.Add(row.ToArray());
                }
            }

            if (totalPages > 1)
            {
                var pag = new List<InlineKeyboardButton>();
                if (page > 1) pag.Add(InlineKeyboardButton.WithCallbackData("◀", $"more_{field}_page_{page - 1}_trade_{tradeId}"));
                pag.Add(InlineKeyboardButton.WithCallbackData($"[{page}/{totalPages}]", $"noop"));
                if (page < totalPages) pag.Add(InlineKeyboardButton.WithCallbackData("▶", $"more_{field}_page_{page + 1}_trade_{tradeId}"));
                rows.Add(pag.ToArray());
            }

            if (step > 1 && step <= 14)
                rows.Add(new[] {
                    InlineKeyboardButton.WithCallbackData("◀️ Назад", $"back_trade_{tradeId}_step_{step}"),
                    InlineKeyboardButton.WithCallbackData(GetText("skip", settings.Language), $"skip_trade_{tradeId}_step_{step}")
                });

            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language), $"input_{field}_trade_{tradeId}") });
            rows.Add(new[] {
                InlineKeyboardButton.WithCallbackData("✅ Сохранить", $"save_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData(GetText("cancel", settings.Language), "cancel")
            });

            return new InlineKeyboardMarkup(rows);
        }

        // Разрешение исходной опции по её безопасному значению callback_data
        public string? TryResolveOriginalOption(string field, string sanitized)
        {
            List<string> list = field.ToLowerInvariant() switch
            {
                "account" => _accountOptions,
                "session" => _sessionOptions,
                "position" => _positionOptions,
                "direction" => _directionOptions,
                "context" => _contextOptions,
                "setup" => _setupOptions,
                "result" => _resultOptions,
                "emotions" => _emotionOptions,
                // Для тикера списков нет, вернём null чтобы применить запасную логику
                _ => new List<string>()
            };

            // Ищем строгое совпадение по правилу санитизации
            foreach (var option in list)
            {
                if (SanitizeCallbackData(option) == sanitized)
                    return option;
            }

            return null;
        }
    }
}
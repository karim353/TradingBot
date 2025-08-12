using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types.ReplyMarkups;
using TradingBot.Models;

namespace TradingBot.Services
{
    /// <summary>
    /// Сервис для создания клавиатур Telegram
    /// Выносит логику создания клавиатур из UIManager для лучшей масштабируемости
    /// </summary>
    public class KeyboardService
    {
        /// <summary>
        /// Создает клавиатуру с опциями для поля сделки
        /// </summary>
        public InlineKeyboardMarkup BuildOptionsKeyboard(
            string field,
            List<string> options,
            string tradeId,
            UserSettings settings,
            int page = 1,
            int pageSize = 24,
            int step = 0,
            HashSet<string>? selected = null)
        {
            selected ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Получаем недавние значения для приоритизации
            var recents = field.ToLowerInvariant() switch
            {
                "ticker" => settings.RecentTickers,
                "direction" => settings.RecentDirections,
                "account" => settings.RecentAccounts,
                "session" => settings.RecentSessions,
                "position" => settings.RecentPositions,
                "context" => settings.RecentContexts,
                "setup" => settings.RecentSetups,
                "result" => settings.RecentResults,
                "emotions" => settings.RecentEmotions,
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
            
            // Специальная логика для определенных полей
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
                            CreateOptionButton(pageSlice[i], field, tradeId, selected),
                            CreateOptionButton(pageSlice[i + 1], field, tradeId, selected)
                        });
                    else
                        rows.Add(new[] { CreateOptionButton(pageSlice[i], field, tradeId, selected) });
                }
            }
            else
            {
                // Адаптивное количество кнопок в ряду
                int i = 0;
                while (i < pageSlice.Count && rows.Count < 8)
                {
                    int len = pageSlice[i].Length;
                    int perRow = len <= 8 ? 4 : len <= 12 ? 3 : 2;
                    var row = new List<InlineKeyboardButton>();
                    
                    for (int j = 0; j < perRow && i < pageSlice.Count; j++, i++)
                    {
                        row.Add(CreateOptionButton(pageSlice[i], field, tradeId, selected));
                    }
                    rows.Add(row.ToArray());
                }
            }

            // Пагинация
            if (totalPages > 1)
            {
                var paginationRow = new List<InlineKeyboardButton>();
                if (page > 1) 
                    paginationRow.Add(InlineKeyboardButton.WithCallbackData("◀", $"more_{field}_page_{page - 1}_trade_{tradeId}"));
                
                paginationRow.Add(InlineKeyboardButton.WithCallbackData($"[{page}/{totalPages}]", "noop"));
                
                if (page < totalPages) 
                    paginationRow.Add(InlineKeyboardButton.WithCallbackData("▶", $"more_{field}_page_{page + 1}_trade_{tradeId}"));
                
                rows.Add(paginationRow.ToArray());
            }

            // Кнопки навигации
            if (step > 1 && step <= 14)
                rows.Add(new[] {
                    InlineKeyboardButton.WithCallbackData("◀️ Назад", $"back_trade_{tradeId}_step_{step}"),
                    InlineKeyboardButton.WithCallbackData("➡ Пропустить", $"skip_trade_{tradeId}_step_{step}")
                });

            // Дополнительные опции
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("⌨️ Ввести вручную", $"input_{field}_trade_{tradeId}") });
            rows.Add(new[] {
                InlineKeyboardButton.WithCallbackData("✅ Сохранить", $"save_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData("🚫 Отмена", "cancel")
            });

            return new InlineKeyboardMarkup(rows);
        }

        /// <summary>
        /// Создает кнопку для опции
        /// </summary>
        private InlineKeyboardButton CreateOptionButton(string value, string field, string tradeId, HashSet<string> selected)
        {
            string text = (selected.Contains(value) ? "✅ " : "") + value;
            string callbackData = $"set_{field}_{UIManager.SanitizeCallbackData(value)}_trade_{tradeId}";
            return InlineKeyboardButton.WithCallbackData(text, callbackData);
        }

        /// <summary>
        /// Создает главное меню
        /// </summary>
        public InlineKeyboardMarkup GetMainMenu(UserSettings settings)
        {
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

        /// <summary>
        /// Создает меню настроек
        /// </summary>
        public InlineKeyboardMarkup GetSettingsMenu(UserSettings settings)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData("🌐 Сменить язык", "settings_language") },
                new[] { InlineKeyboardButton.WithCallbackData(
                    settings.NotificationsEnabled ? "🔔 Уведомления: ✅" : "🔔 Уведомления: ❌", 
                    "settings_notifications") },
                new[] { InlineKeyboardButton.WithCallbackData("📈 Избранные тикеры", "settings_tickers") },
                new[] { InlineKeyboardButton.WithCallbackData("🌐 Настройки Notion", "settings_notion") },
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "main") }
            };

            return new InlineKeyboardMarkup(rows);
        }

        /// <summary>
        /// Создает меню настроек Notion
        /// </summary>
        public InlineKeyboardMarkup GetNotionSettingsMenu(UserSettings settings)
        {
            var rows = new List<InlineKeyboardButton[]>();
            
            if (settings.NotionEnabled)
            {
                // Если Notion подключен, показываем опции управления
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔑 Изменить токен", "notion_token_input") });
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🗄️ Изменить Database ID", "notion_database_input") });
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🧪 Проверить подключение", "notion_test_connection") });
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔌 Отключить Notion", "notion_disconnect") });
            }
            else
            {
                // Если Notion не подключен, показываем опцию подключения
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔗 Подключить Notion", "notion_connect") });
            }
            
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "settings") });
            
            return new InlineKeyboardMarkup(rows);
        }

        /// <summary>
        /// Создает клавиатуру для ввода токена Notion
        /// </summary>
        public InlineKeyboardMarkup GetNotionTokenInputMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "settings_notion") }
            });
        }

        /// <summary>
        /// Создает клавиатуру для ввода Database ID Notion
        /// </summary>
        public InlineKeyboardMarkup GetNotionDatabaseInputMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "settings_notion") }
            });
        }

        /// <summary>
        /// Создает клавиатуру для выбора языка
        /// </summary>
        public InlineKeyboardMarkup GetLanguageSelectionMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[] { 
                    InlineKeyboardButton.WithCallbackData("🇷🇺 Русский", "language_ru"),
                    InlineKeyboardButton.WithCallbackData("🇺🇸 English", "language_en")
                },
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "settings") }
            });
        }

        /// <summary>
        /// Создает клавиатуру для управления уведомлениями
        /// </summary>
        public InlineKeyboardMarkup GetNotificationsMenu(UserSettings settings)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[] { 
                    InlineKeyboardButton.WithCallbackData("🔔 Включить", "notifications_on"),
                    InlineKeyboardButton.WithCallbackData("🔕 Выключить", "notifications_off")
                },
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "settings") }
            });
        }

        /// <summary>
        /// Создает клавиатуру для управления избранными тикерами
        /// </summary>
        public InlineKeyboardMarkup GetFavoriteTickersMenu(List<string> favoriteTickers, List<string> popularTickers)
        {
            var rows = new List<InlineKeyboardButton[]>();
            
            // Показываем избранные тикеры
            if (favoriteTickers.Any())
            {
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData("⭐ Избранные тикеры:", "noop") });
                
                // Группируем по 3 в ряд
                for (int i = 0; i < favoriteTickers.Count; i += 3)
                {
                    var row = new List<InlineKeyboardButton>();
                    for (int j = 0; j < 3 && i + j < favoriteTickers.Count; j++)
                    {
                        var ticker = favoriteTickers[i + j];
                        row.Add(InlineKeyboardButton.WithCallbackData($"❌ {ticker}", $"remove_ticker_{ticker}"));
                    }
                    rows.Add(row.ToArray());
                }
            }

            // Показываем популярные тикеры для добавления
            if (popularTickers.Any())
            {
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData("📈 Популярные тикеры:", "noop") });
                
                // Группируем по 3 в ряд
                for (int i = 0; i < popularTickers.Count; i += 3)
                {
                    var row = new List<InlineKeyboardButton>();
                    for (int j = 0; j < 3 && i + j < popularTickers.Count; j++)
                    {
                        var ticker = popularTickers[i + j];
                        if (!favoriteTickers.Contains(ticker, StringComparer.OrdinalIgnoreCase))
                        {
                            row.Add(InlineKeyboardButton.WithCallbackData($"➕ {ticker}", $"add_ticker_{ticker}"));
                        }
                    }
                    if (row.Any())
                        rows.Add(row.ToArray());
                }
            }

            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "settings") });
            
            return new InlineKeyboardMarkup(rows);
        }

        /// <summary>
        /// Создает клавиатуру для подтверждения действия
        /// </summary>
        public InlineKeyboardMarkup GetConfirmationMenu(string action, string callbackData)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[] { 
                    InlineKeyboardButton.WithCallbackData("✅ Да", callbackData),
                    InlineKeyboardButton.WithCallbackData("❌ Нет", "settings")
                }
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
        /// Создает стабильный callback_data для предотвращения коллизий
        /// </summary>
        private static string CreateStableCallbackData(string action, string? value = null, string? tradeId = null)
        {
            var parts = new List<string> { action };
            
            if (!string.IsNullOrEmpty(value))
            {
                // Создаем короткий хэш для значения, чтобы избежать коллизий
                using var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
                var shortHash = Convert.ToBase64String(hashBytes).Substring(0, 8)
                    .Replace("+", "PLUS")
                    .Replace("/", "SLASH")
                    .Replace("=", "EQ");
                parts.Add(shortHash);
            }
            
            if (!string.IsNullOrEmpty(tradeId))
            {
                parts.Add(tradeId);
            }
            
            return string.Join("_", parts);
        }

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
                    paginationRow.Add(InlineKeyboardButton.WithCallbackData("◀", CreateStableCallbackData("more", field, $"{page - 1}_{tradeId}")));
                
                paginationRow.Add(InlineKeyboardButton.WithCallbackData($"{page}/{totalPages}", "pagination_info"));
                
                if (page < totalPages) 
                    paginationRow.Add(InlineKeyboardButton.WithCallbackData("▶", CreateStableCallbackData("more", field, $"{page + 1}_{tradeId}")));
                
                rows.Add(paginationRow.ToArray());
            }

            // Кнопка "Назад"
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", CreateStableCallbackData("back", field, tradeId)) });

            return new InlineKeyboardMarkup(rows);
        }

        /// <summary>
        /// Создает кнопку для выбора опции
        /// </summary>
        private InlineKeyboardButton CreateOptionButton(string option, string field, string tradeId, HashSet<string> selected)
        {
            var isSelected = selected.Contains(option, StringComparer.OrdinalIgnoreCase);
            var text = isSelected ? $"✅ {option}" : option;
            var callbackData = CreateStableCallbackData("set", $"{field}_{option}", tradeId);
            
            return InlineKeyboardButton.WithCallbackData(text, callbackData);
        }

        /// <summary>
        /// Создает клавиатуру настроек
        /// </summary>
        public InlineKeyboardMarkup CreateSettingsKeyboard(UserSettings settings)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData("🌐 Сменить язык", "settings_language") },
                new[] { InlineKeyboardButton.WithCallbackData("🔔 Уведомления", "settings_notifications") },
                new[] { InlineKeyboardButton.WithCallbackData("📈 Избранные тикеры", "settings_tickers") },
                new[] { InlineKeyboardButton.WithCallbackData("🌐 Настройки Notion", "settings_notion") }
            };

            // Добавляем кнопку "Назад"
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "main") });

            return new InlineKeyboardMarkup(rows);
        }

        /// <summary>
        /// Создает клавиатуру настроек Notion
        /// </summary>
        public InlineKeyboardMarkup CreateNotionSettingsKeyboard(UserSettings settings)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (settings.NotionEnabled && !string.IsNullOrEmpty(settings.NotionIntegrationToken))
            {
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔌 Отключить Notion", "notion_disconnect") });
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔑 Изменить токен", "notion_token") });
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🗄️ Изменить базу", "notion_database") });
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🧪 Проверить подключение", "notion_test") });
            }
            else
            {
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔗 Подключить Notion", "notion_connect") });
            }

            // Кнопка "Назад"
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "settings_notion") });

            return new InlineKeyboardMarkup(rows);
        }

        /// <summary>
        /// Создает клавиатуру для ввода токена Notion
        /// </summary>
        public InlineKeyboardMarkup CreateNotionTokenInputKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("❌ Отмена", "notion_cancel") }
            });
        }

        /// <summary>
        /// Создает клавиатуру для ввода Database ID Notion
        /// </summary>
        public InlineKeyboardMarkup CreateNotionDatabaseInputKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("❌ Отмена", "notion_cancel") }
            });
        }

        /// <summary>
        /// Создает клавиатуру для управления избранными тикерами
        /// </summary>
        public InlineKeyboardMarkup CreateFavoriteTickersKeyboard(UserSettings settings)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (settings.FavoriteTickers != null && settings.FavoriteTickers.Any())
            {
                foreach (var ticker in settings.FavoriteTickers.Take(20)) // Ограничиваем 20 тикерами
                {
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"❌ {ticker}", CreateStableCallbackData("remove_ticker", ticker)) });
                }
            }

            // Кнопка "Назад"
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "settings_tickers") });

            return new InlineKeyboardMarkup(rows);
        }

        /// <summary>
        /// Создает клавиатуру для выбора языка
        /// </summary>
        public InlineKeyboardMarkup CreateLanguageSelectionKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Русский", "language_ru") },
                new[] { InlineKeyboardButton.WithCallbackData("🇺🇸 English", "language_en") },
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "settings_language") }
            });
        }

        /// <summary>
        /// Создает клавиатуру для уведомлений
        /// </summary>
        public InlineKeyboardMarkup CreateNotificationsKeyboard(UserSettings settings)
        {
            var status = settings.NotificationsEnabled ? "🔔 Включены" : "🔕 Отключены";
            var toggleText = settings.NotificationsEnabled ? "🔕 Отключить" : "🔔 Включить";

            return new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(status, "notification_status") },
                new[] { InlineKeyboardButton.WithCallbackData(toggleText, "settings_notifications") },
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "settings_notifications") }
            });
        }
    }
}

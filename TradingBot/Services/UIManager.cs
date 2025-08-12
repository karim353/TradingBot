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
                .Replace(" ", "_")
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
                ["welcome"] = "🚀 Добро пожаловать в TradingBot!\n\n📊 Я помогу вам вести учёт торговых сделок и анализировать результаты.\n\n💡 Основные возможности:\n• 📸 Добавление сделок через скриншоты\n• ⌨️ Ручной ввод данных\n• 📈 Детальная статистика и аналитика\n• 🌐 Синхронизация с Notion\n• 💾 Локальное хранение в SQLite\n\nНажмите 'Далее' для краткого обучения.",
                ["onboarding_1"] = "📥 Добавление сделок\n\nУ вас есть два способа:\n\n📸 Скриншот: Отправьте фото с экрана терминала - я автоматически извлеку данные\n\n⌨️ Ручной ввод: Пошаговое заполнение всех полей сделки\n\nКаждый способ одинаково эффективен!",
                ["onboarding_2"] = "📊 Аналитика и статистика\n\nПосле добавления сделок вы получите:\n\n📈 Детальную статистику по периодам\n📊 Графики эквити и P&L\n🎯 Анализ win rate и серий\n📅 Отчёты по дням/неделям/месяцам\n\nВся информация в удобном формате!",
                ["onboarding_3"] = "⚙️ Настройки и интеграции\n\nНастройте бота под себя:\n\n🌐 Язык интерфейса (RU/EN)\n🔔 Уведомления о важных событиях\n📊 Интеграция с Notion для командной работы\n💾 Локальная база данных для приватности\n\n🌐 Персональная интеграция с Notion:\n• Подключите свою базу данных\n• Используйте собственные справочники\n• Синхронизируйте сделки\n\n🔧 Меню настроек:\n• Управление языком и уведомлениями\n• Подключение персонального Notion\n• Настройка избранных тикеров\n• Персональные справочники\n\nГотовы начать? Нажмите 'Главное меню'!",
                ["main_menu"] = "🎯 TradingBot - Ваш помощник в торговле\n\n📊 Статистика за сегодня:\n📅 Сделок: {0} | 📈 PnL: {1}% | ✅ Win Rate: {2}%\n\n🚀 Что хотите сделать?\n\n➕ Добавить новую сделку\n📈 Посмотреть статистику\n📜 История всех сделок\n⚙️ Настройки бота\n🆘 Помощь и поддержка",
                ["please_use_buttons"] = "👇 Пожалуйста, используйте кнопки ниже для навигации.",
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
                ["ticker_removed"] = "🗑️ Тикер {0} удален из избранного!",
                
                // Настройки Notion
                ["notion_settings"] = "🌐 Настройки Notion",
                ["notion_enabled"] = "✅ Notion подключен и активен",
                ["notion_disabled"] = "❌ Notion не подключен",
                ["notion_connect"] = "🔗 Подключить Notion",
                ["notion_disconnect"] = "🔌 Отключить Notion",
                ["notion_token"] = "🔑 Изменить токен интеграции",
                ["notion_database"] = "🗄️ Изменить Database ID",
                ["notion_test"] = "🧪 Проверить подключение",
                ["notion_token_input"] = "🔑 Введите токен интеграции Notion\n\nОтправьте сообщение с вашим токеном интеграции Notion API",
                ["notion_database_input"] = "🗄️ Введите Database ID\n\nОтправьте URL или ID вашей базы данных Notion",
            ["notion_database_input"] = "🗄️ Введите ID базы данных или URL вашей базы данных Notion:\n\n📋 Инструкция:\n1️⃣ Откройте созданную базу данных в Notion\n2️⃣ Нажмите 'Share' в правом верхнем углу\n3️⃣ Найдите созданную интеграцию и пригласите её\n4️⃣ Скопируйте ID из URL (часть после notion.so/ и перед ?v=)\n5️⃣ Отправьте ID в следующем сообщении",
                ["notion_connection_success"] = "✅ Подключение к Notion успешно установлено!",
                ["notion_connection_failed"] = "❌ Не удалось подключиться к Notion\n\nПроверьте:\n• Правильность токена\n• Доступ к базе данных\n• Права интеграции",
                ["notion_settings_saved"] = "✅ Настройки Notion сохранены",
                ["notion_disconnected"] = "🔌 Notion отключен",

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
                ["stats_menu"] = "📊 Статистика:",
                ["stats_result"] = "📊 Статистика за {0}:\n\n📈 Всего сделок: {1}\n💰 Общий PnL: {2}%\n✅ Прибыльных: {3}\n❌ Убыточных: {4}\n🎯 Винрейт: {5}%",

                ["advanced_stats"] = "📈 Сделок: {0}\nОбщий PnL: {1}%\nСредний PnL: {2}%\nЛучший: {3}%\nХудший: {4}%\nWin Rate: {5}%",
                ["date_label"] = "📅 Дата",
                ["pnl_label"] = "📈 Накопленный PnL",
                ["equity_curve"] = "📈 Кривая эквити:",
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
                ["support"] = "🆘 Техподдержка",
                ["help_menu"] = "💡 Выберите раздел помощи:",
                ["whats_new"] = "📣 Что нового",
                
                // Новые тексты для настроек Notion
                ["notion_settings"] = "🌐 Настройки Notion:",
                ["notion_enabled"] = "✅ Notion подключен",
                ["notion_disabled"] = "❌ Notion отключен",
                ["notion_connect"] = "🔗 Подключить Notion",
                ["notion_disconnect"] = "🔌 Отключить Notion",
                ["notion_token"] = "🔑 Ввести токен",
                ["notion_database"] = "🗄️ Ввести Database ID",
                ["notion_test"] = "🧪 Проверить подключение",
                ["notion_status"] = "📊 Статус подключения",
                ["notion_token_input"] = "🔑 Введите ваш Integration Token от Notion:\n\n📋 Инструкция:\n1️⃣ Перейдите на https://www.notion.so/my-integrations\n2️⃣ Нажмите '+ New integration'\n3️⃣ Укажите название и выберите рабочее пространство\n4️⃣ Скопируйте секретный токен\n5️⃣ Отправьте его в следующем сообщении",
                ["notion_database_input"] = "🗄️ Введите Database ID или URL вашей базы Notion:\n\n📋 Инструкция:\n1️⃣ Откройте вашу базу данных в Notion\n2️⃣ Нажмите 'Share' в правом верхнем углу\n3️⃣ Найдите созданную интеграцию и пригласите её\n4️⃣ Скопируйте ID из URL (часть после notion.so/ и до ?v=)\n5️⃣ Отправьте ID в следующем сообщении",
                ["notion_connection_success"] = "✅ Подключение к Notion успешно!\n\nТеперь ваши сделки будут синхронизироваться с вашей персональной базой данных.",
                ["notion_connection_failed"] = "❌ Не удалось подключиться к Notion.\n\nПроверьте:\n• Правильность токена\n• Правильность Database ID\n• Доступ интеграции к базе данных\n• Версию API (должна быть 2022-06-28)",
                ["notion_token_invalid"] = "⚠️ Токен недействителен. Проверьте правильность ввода.",
                ["notion_database_invalid"] = "⚠️ Database ID недействителен. Проверьте правильность ввода.",
                ["notion_already_connected"] = "ℹ️ Notion уже подключен. Используйте 'Отключить' для изменения настроек.",
                ["notion_not_connected"] = "ℹ️ Notion не подключен. Сначала подключите интеграцию.",
                ["notion_disconnected"] = "✅ Notion успешно отключен. Ваши сделки больше не будут синхронизироваться.",
                ["notion_help"] = "📚 Помощь по подключению Notion:\n\n🔑 Integration Token:\n• Создайте интеграцию на https://www.notion.so/my-integrations\n• Скопируйте секретный токен\n\n🗄️ Database ID:\n• Откройте базу данных в Notion\n• Скопируйте ID из URL\n• Предоставьте доступ интеграции\n\n❓ Нужна помощь? Обратитесь к документации Notion",
            ["notion_settings"] = "🌐 Настройки Notion",
            ["notion_enabled"] = "✅ Notion подключен",
            ["notion_disabled"] = "❌ Notion не подключен",
            ["notion_connect"] = "🔗 Подключить Notion",
            ["notion_disconnect"] = "🔌 Отключить Notion",
            ["notion_token"] = "🔑 Изменить токен",
            ["notion_database"] = "🗄️ Изменить базу данных",
            ["notion_test"] = "🧪 Тест подключения",
            ["notion_test"] = "🧪 Тест подключения",
            ["back_to_settings"] = "⬅️ Назад к настройкам",
            ["notion_settings"] = "🌐 Настройки Notion",
            ["notion_enabled"] = "✅ Notion подключен",
            ["notion_disabled"] = "❌ Notion не подключен",

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
                ["input_note"] = "📝 Введите заметку:",

                // Тексты для уведомлений
                ["notifications_enabled"] = "🔔 Уведомления включены!",
                ["notifications_disabled"] = "🔕 Уведомления отключены!",
                ["ticker_added"] = "✅ Тикер {0} добавлен в избранное!",
                ["ticker_removed"] = "🗑️ Тикер {0} удален из избранного!",
                ["back"] = "⬅️ Назад",
                ["add_ticker"] = "➕ Добавить тикер",
                ["remove_ticker"] = "🗑️ Удалить тикер",
                ["support"] = "🆘 Техподдержка",
                ["whatsnew"] = "📣 Что нового",
                ["back_to_settings"] = "⬅️ Назад к настройкам",
                ["no_trades"] = "📭 Нет сделок для отображения",
                ["history_filters"] = "🔍 Фильтры истории:",
                ["history_page"] = "Страница {0} из {1}",
                ["export"] = "💾 Экспорт в CSV"
            },
            // Английские тексты (можно аналогично заполнить или оставить пустыми для примера)
            ["en"] = new Dictionary<string, string>
            {
                ["welcome"] = "🚀 Welcome to TradingBot!\n\n📊 I'll help you track trading deals and analyze results.\n\n💡 Main features:\n• 📸 Add deals via screenshots\n• ⌨️ Manual data entry\n• 📈 Detailed statistics and analytics\n• 🌐 Notion synchronization\n• 💾 Local SQLite storage\n\nClick 'Next' for a quick tutorial.",
                ["onboarding_1"] = "📥 Adding Deals\n\nYou have two ways:\n\n📸 Screenshot: Send a photo of your terminal screen - I'll automatically extract data\n\n⌨️ Manual entry: Step-by-step filling of all deal fields\n\nBoth methods are equally effective!",
                ["onboarding_2"] = "📊 Analytics and Statistics\n\nAfter adding deals, you'll get:\n\n📈 Detailed statistics by periods\n📊 Equity and P&L charts\n🎯 Win rate and streak analysis\n📅 Reports by days/weeks/months\n\nAll information in a convenient format!",
                ["onboarding_3"] = "⚙️ Settings and Integrations\n\nConfigure the bot for yourself:\n\n🌐 Interface language (RU/EN)\n🔔 Notifications about important events\n📊 Notion integration for team work\n💾 Local database for privacy\n\n🌐 Personal Notion integration:\n• Connect your own database\n• Use custom dictionaries\n• Sync your trades\n\n🔧 Settings menu:\n• Language and notification management\n• Personal Notion connection\n• Favorite tickers setup\n• Personal dictionaries\n\nReady to start? Click 'Main Menu'!",
                ["main_menu"] = "🎯 TradingBot - Your Trading Assistant\n\n📊 Today's Statistics:\n📅 Deals: {0} | 📈 PnL: {1}% | ✅ Win Rate: {2}%\n\n🚀 What would you like to do?\n\n➕ Add new deal\n📈 View statistics\n📜 Deal history\n⚙️ Bot settings\n🆘 Help and support",
                ["main_menu_button"] = "◀️ Main Menu",
                ["skip"] = "➡ Skip",
                ["cancel"] = "🚫 Cancel",
                ["confirm"] = "✅ Confirm",
                ["edit"] = "✏️ Edit",
                ["delete"] = "🗑 Delete",
                ["please_use_buttons"] = "👇 Please use the buttons below for navigation.",
                ["other"] = "Other...",
                ["prefill_last"] = "As in last",
                ["input_manually"] = "⌨️ Enter manually",
                
                // English versions of Notion settings texts
                ["notion_settings"] = "🌐 Notion Settings:",
                ["notion_enabled"] = "✅ Notion connected",
                ["notion_disabled"] = "❌ Notion disconnected",
                ["notion_connect"] = "🔗 Connect Notion",
                ["notion_disconnect"] = "🔌 Disconnect Notion",
                ["notion_token"] = "🔑 Enter token",
                ["notion_database"] = "🗄️ Enter Database ID",
                ["notion_test"] = "🧪 Test connection",
                ["notion_status"] = "📊 Connection status",
                ["notion_token_input"] = "🔑 Enter your Notion Integration Token:\n\n📋 Instructions:\n1️⃣ Go to https://www.notion.so/my-integrations\n2️⃣ Click '+ New integration'\n3️⃣ Enter name and select workspace\n4️⃣ Copy the secret token\n5️⃣ Send it in the next message",
                ["notion_database_input"] = "🗄️ Enter Database ID or URL of your Notion database:\n\n📋 Instructions:\n1️⃣ Open your database in Notion\n2️⃣ Click 'Share' in the top right corner\n3️⃣ Find the created integration and invite it\n4️⃣ Copy ID from URL (part after notion.so/ and before ?v=)\n5️⃣ Send the ID in the next message",
                ["notion_connection_success"] = "✅ Successfully connected to Notion!\n\nNow your trades will be synchronized with your personal database.",
                ["notion_connection_failed"] = "❌ Failed to connect to Notion.\n\nCheck:\n• Token correctness\n• Database ID correctness\n• Integration access to database\n• API version (should be 2022-06-28)",
                ["notion_token_invalid"] = "⚠️ Token is invalid. Check the input.",
                ["notion_database_invalid"] = "⚠️ Database ID is invalid. Check the input.",
                ["notion_already_connected"] = "ℹ️ Notion is already connected. Use 'Disconnect' to change settings.",
                ["notion_not_connected"] = "ℹ️ Notion is not connected. First connect the integration.",
                ["notion_disconnected"] = "✅ Notion successfully disconnected. Your trades will no longer be synchronized.",
                ["notion_help"] = "📚 Notion Connection Help:\n\n🔑 Integration Token:\n• Create integration at https://www.notion.so/my-integrations\n• Copy the secret token\n\n🗄️ Database ID:\n• Open database in Notion\n• Copy ID from URL\n• Grant access to integration\n\n❓ Need help? Check Notion documentation",
                ["notion_disconnected"] = "✅ Notion successfully disconnected. Your trades will no longer be synchronized.",
                ["notion_help"] = "📚 Notion Connection Help:\n\n🔑 Integration Token:\n• Create integration at https://www.notion.so/my-integrations\n• Copy the secret token\n\n🗄️ Database ID:\n• Open database in Notion\n• Copy ID from URL\n• Grant access to integration\n\n❓ Need help? Check Notion documentation",
                ["notion_settings"] = "🌐 Notion Settings",
                ["notion_enabled"] = "✅ Notion connected",
                ["notion_disabled"] = "❌ Notion not connected",
                ["notion_connect"] = "🔗 Connect Notion",
                ["notion_disconnect"] = "🔌 Disconnect Notion",
                ["notion_token"] = "🔑 Change token",
                ["notion_database"] = "🗄️ Change database",
                ["notion_test"] = "🧪 Test connection",
                ["notion_database_input"] = "🗄️ Enter Database ID or URL of your Notion database:\n\n📋 Instructions:\n1️⃣ Open your database in Notion\n2️⃣ Click 'Share' in the top right corner\n3️⃣ Find the created integration and invite it\n4️⃣ Copy ID from URL (part after notion.so/ and before ?v=)\n5️⃣ Send the ID in the next message",

                // Тексты для уведомлений
                ["notifications_enabled"] = "🔔 Notifications enabled!",
                ["notifications_disabled"] = "🔕 Notifications disabled!",
                ["ticker_added"] = "✅ Ticker {0} added to favorites!",
                ["ticker_removed"] = "🗑️ Ticker {0} removed from favorites!",
                ["back"] = "⬅️ Back",
                ["add_ticker"] = "➕ Add Ticker",
                ["remove_ticker"] = "🗑️ Remove Ticker",
                ["support"] = "🆘 Support",
                ["whatsnew"] = "📣 What's new",
                ["back_to_settings"] = "⬅️ Back to settings",
                ["period_week"] = "Week",
                ["period_month"] = "Month",
                ["period_all"] = "All time",
                ["stats_menu"] = "📊 Statistics:",
                ["stats_result"] = "📊 Statistics for {0}:\n\n📈 Total trades: {1}\n💰 Total PnL: {2}%\n✅ Profitable: {3}\n❌ Losing: {4}\n🎯 Win rate: {5}%",
                ["equity_curve"] = "📈 Equity curve:",
                ["no_trades"] = "📭 No trades to display",
                ["history_filters"] = "🔍 History filters:",
                ["history_page"] = "Page {0} of {1}",
                ["export"] = "💾 Export to CSV"
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
        public static readonly List<string> PopularTickers = new() { "BTC/USDT", "ETH/USDT", "SOL/USDT", "BNB/USDT", "EUR/USD", "XAU/USD" };

        // Значения по умолчанию, если Notion/SQLite не предоставили готовые ответы
        private static readonly List<string> DefaultAccounts = new() { "🏦 BingX", "🏦 Binance", "🏦 MEXC", "🏦 Bybit", "🧪 Demo" };
        private static readonly List<string> DefaultSessions = new() { "ASIA", "LONDON", "NEW YORK", "FRANKFURT" };
        private static readonly List<string> DefaultPositionTypes = new() { "⚡ Scalp", "⏱ Intraday", "📅 Swing", "🏋️ Position" };
        private static readonly List<string> DefaultDirections = new() { "Long", "Short" };
        private static readonly List<string> DefaultContexts = new() { "📈 Uptrend", "📉 Downtrend", "➖ Range" };
        private static readonly List<string> DefaultSetups = new() { "↗️ Continuation (CONT)", "📈 Breakout", "🔄 Reversal (REVR)", "🔁 Double Top/Bottom", "👤 Head & Shoulders" };
        private static readonly List<string> DefaultResults = new() { "TP", "SL", "BE" };
        private static readonly List<string> DefaultEmotions = new() { "😌 Calm", "🎯 Focused", "😨 Fear", "😵‍💫 FOMO" };

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

            // Добавляем кнопку "Пропустить обучение" на всех экранах
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("⏭️ Пропустить обучение", "main") });

            return (text, new InlineKeyboardMarkup(buttons));
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetTradeInputScreen(Trade trade, int step, UserSettings settings, string tradeId, Trade? lastTrade = null)
        {
            // Переписанный превью с эмодзи в значениях
            var ctx = (trade.Context != null && trade.Context.Any()) ? string.Join(", ", trade.Context) : "-";
            var setup = (trade.Setup != null && trade.Setup.Any()) ? string.Join(", ", trade.Setup) : "-";
            var emos = (trade.Emotions != null && trade.Emotions.Any()) ? string.Join(", ", trade.Emotions) : "-";
            string formattedPreview =
                "📌 Тикер: " + (string.IsNullOrWhiteSpace(trade.Ticker) ? "-" : trade.Ticker) + "\n" +
                "🧾 Аккаунт: " + (string.IsNullOrWhiteSpace(trade.Account) ? "-" : trade.Account) + "\n" +
                "🕒 Сессия: " + (string.IsNullOrWhiteSpace(trade.Session) ? "-" : trade.Session) + "\n" +
                "📐 Позиция: " + (string.IsNullOrWhiteSpace(trade.Position) ? "-" : trade.Position) + "\n" +
                "↕️ Направление: " + (string.IsNullOrWhiteSpace(trade.Direction) ? "-" : trade.Direction) + "\n" +
                "🧩 Контекст: " + ctx + "\n" +
                "🧠 Сетап: " + setup + "\n" +
                "🎯 Результат: " + (string.IsNullOrWhiteSpace(trade.Result) ? "-" : trade.Result) + "\n" +
                "⚖️ R:R = " + (string.IsNullOrWhiteSpace(trade.RR) ? "-" : trade.RR) + "\n" +
                "⚠️ Риск: " + (trade.Risk?.ToString("0.##") ?? "-") + "%\n" +
                "📈 Прибыль: " + trade.PnL.ToString("0.##") + "%\n" +
                "😃 Эмоции: " + emos + "\n" +
                "🔍 Детали входа: " + (string.IsNullOrWhiteSpace(trade.EntryDetails) ? "-" : trade.EntryDetails) + "\n" +
                "📝 Заметка: " + (string.IsNullOrWhiteSpace(trade.Note) ? "-" : trade.Note);

            string prompt = _resources[settings.Language][$"step_{step}"];
            var buttons = new List<InlineKeyboardButton[]>();

            switch (step)
            {
                case 1: // Тикер
                    var fav = settings.FavoriteTickers ?? new List<string>();
                    var recent = settings.RecentTickers ?? new List<string>();
                    var tickers = fav.Concat(recent).Concat(PopularTickers).Distinct().Take(6).ToList();
                    foreach (var t in tickers)
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(t, $"set_ticker_{SanitizeCallbackData(t)}_trade_{tradeId}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["other"], $"input_ticker_trade_{tradeId}") });
                    if (!string.IsNullOrEmpty(lastTrade?.Ticker))
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["prefill_last"], $"set_ticker_{SanitizeCallbackData(lastTrade!.Ticker)}_trade_{tradeId}") });
                    // Добавляем кнопку Пропустить на первом шаге
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["skip"], $"skip_trade_{tradeId}_step_{step}") });
                    break;

                case 2: // Аккаунт
                    var accounts = _accountOptions.Any() ? _accountOptions : DefaultAccounts;
                    foreach (var option in accounts)
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
                        InlineKeyboardButton.WithCallbackData("🇩🇪 FRANKFURT", $"set_session_FRANKFURT_trade_{tradeId}")
                    });
                    buttons.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🇬🇧 LONDON", $"set_session_LONDON_trade_{tradeId}"),
                        InlineKeyboardButton.WithCallbackData("🇺🇸 NEW YORK", $"set_session_NEWYORK_trade_{tradeId}")
                    });

                    // Кастомные сессии из Notion/SQLite — тоже по две в ряд
                    var sessionBase = _sessionOptions.Any() ? _sessionOptions : DefaultSessions;
                    var customSessions = sessionBase.Where(s => !defaultSessions.Contains(s)).ToList();
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

                case 4: // Позиция (тип сделки)
                    var positionTypes = _positionOptions.Any() ? _positionOptions : DefaultPositionTypes;
                    for (int i = 0; i < positionTypes.Count; i += 2)
                    {
                        if (i + 1 < positionTypes.Count)
                            buttons.Add(new[]
                            {
                                InlineKeyboardButton.WithCallbackData(positionTypes[i], $"set_position_{SanitizeCallbackData(positionTypes[i])}_trade_{tradeId}"),
                                InlineKeyboardButton.WithCallbackData(positionTypes[i + 1], $"set_position_{SanitizeCallbackData(positionTypes[i + 1])}_trade_{tradeId}")
                            });
                        else
                            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(positionTypes[i], $"set_position_{SanitizeCallbackData(positionTypes[i])}_trade_{tradeId}") });
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
                    var baseDirections = _directionOptions.Any() ? _directionOptions : DefaultDirections;
                    var extraDirections = baseDirections.Where(d => !defaultDirections.Contains(d)).ToList();
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
                    var contexts = _contextOptions.Any() ? _contextOptions : DefaultContexts;
                    foreach (var option in contexts)
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(option, $"set_context_{SanitizeCallbackData(option)}_trade_{tradeId}") });
                    //buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["skip"], $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["input_manually"], $"input_context_trade_{tradeId}") });
                    break;

                case 7: // Сетап
                    var setups = _setupOptions.Any() ? _setupOptions : DefaultSetups;
                    foreach (var option in setups)
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
                            InlineKeyboardButton.WithCallbackData("1%",   $"set_risk_1_0_trade_{tradeId}"),
                            InlineKeyboardButton.WithCallbackData("1.5%", $"set_risk_1_5_trade_{tradeId}"),
                            InlineKeyboardButton.WithCallbackData("2%",   $"set_risk_2_0_trade_{tradeId}"),
                            InlineKeyboardButton.WithCallbackData("3%",   $"set_risk_3_0_trade_{tradeId}")
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
                    // Быстрые кнопки: TP / SL / BE
                    buttons.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🟢 TP", $"set_result_TP_trade_{tradeId}"),
                        InlineKeyboardButton.WithCallbackData("🔴 SL", $"set_result_SL_trade_{tradeId}"),
                        InlineKeyboardButton.WithCallbackData("🟠 BE", $"set_result_BE_trade_{tradeId}")
                    });
                    var results = _resultOptions.Any() ? _resultOptions : DefaultResults;
                    foreach (var option in results)
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(option, $"set_result_{SanitizeCallbackData(option)}_trade_{tradeId}") });
                    //buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["skip"], $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["input_manually"], $"input_result_trade_{tradeId}") });
                    break;

                case 12: // Эмоции
                    var emo = _emotionOptions.Any() ? _emotionOptions : DefaultEmotions;
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
                    // Быстрые варианты типа входа
                    buttons.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🛒 Market", $"set_entry_market_trade_{tradeId}"),
                        InlineKeyboardButton.WithCallbackData("🏷 Limit",  $"set_entry_limit_trade_{tradeId}"),
                        InlineKeyboardButton.WithCallbackData("⛔ Stop",    $"set_entry_stop_trade_{tradeId}")
                    });
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
            string text =
                "📌 Тикер: " + (string.IsNullOrWhiteSpace(trade.Ticker) ? "-" : trade.Ticker) + "\n" +
                "🧾 Аккаунт: " + (string.IsNullOrWhiteSpace(trade.Account) ? "-" : trade.Account) + "\n" +
                "🕒 Сессия: " + (string.IsNullOrWhiteSpace(trade.Session) ? "-" : trade.Session) + "\n" +
                "📐 Позиция: " + (string.IsNullOrWhiteSpace(trade.Position) ? "-" : trade.Position) + "\n" +
                "↕️ Направление: " + (string.IsNullOrWhiteSpace(trade.Direction) ? "-" : trade.Direction) + "\n" +
                "🧩 Контекст: " + ((trade.Context != null && trade.Context.Any()) ? string.Join(", ", trade.Context) : "-") + "\n" +
                "🧠 Сетап: " + ((trade.Setup != null && trade.Setup.Any()) ? string.Join(", ", trade.Setup) : "-") + "\n" +
                "🎯 Результат: " + (string.IsNullOrWhiteSpace(trade.Result) ? "-" : trade.Result) + "\n" +
                "⚖️ R:R = " + (string.IsNullOrWhiteSpace(trade.RR) ? "-" : trade.RR) + "\n" +
                "⚠️ Риск: " + (trade.Risk?.ToString("0.##") ?? "-") + "%\n" +
                "📈 Прибыль: " + trade.PnL.ToString("0.##") + "%\n" +
                "😃 Эмоции: " + ((trade.Emotions != null && trade.Emotions.Any()) ? string.Join(", ", trade.Emotions) : "-") + "\n" +
                "🔍 Детали входа: " + (string.IsNullOrWhiteSpace(trade.EntryDetails) ? "-" : trade.EntryDetails) + "\n" +
                "📝 Заметка: " + (string.IsNullOrWhiteSpace(trade.Note) ? "-" : trade.Note);

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
            string preview =
                "📌 Тикер: " + (string.IsNullOrWhiteSpace(trade.Ticker) ? "-" : trade.Ticker) + "\n" +
                "🧾 Аккаунт: " + (string.IsNullOrWhiteSpace(trade.Account) ? "-" : trade.Account) + "\n" +
                "🕒 Сессия: " + (string.IsNullOrWhiteSpace(trade.Session) ? "-" : trade.Session) + "\n" +
                "📐 Позиция: " + (string.IsNullOrWhiteSpace(trade.Position) ? "-" : trade.Position) + "\n" +
                "↕️ Направление: " + (string.IsNullOrWhiteSpace(trade.Direction) ? "-" : trade.Direction) + "\n" +
                "🧩 Контекст: " + ((trade.Context != null && trade.Context.Any()) ? string.Join(", ", trade.Context) : "-") + "\n" +
                "🧠 Сетап: " + ((trade.Setup != null && trade.Setup.Any()) ? string.Join(", ", trade.Setup) : "-") + "\n" +
                "🎯 Результат: " + (string.IsNullOrWhiteSpace(trade.Result) ? "-" : trade.Result) + "\n" +
                "⚖️ R:R = " + (string.IsNullOrWhiteSpace(trade.RR) ? "-" : trade.RR) + "\n" +
                "⚠️ Риск: " + (trade.Risk?.ToString("0.##") ?? "-") + "%\n" +
                "📈 Прибыль: " + trade.PnL.ToString("0.##") + "%\n" +
                "😃 Эмоции: " + ((trade.Emotions != null && trade.Emotions.Any()) ? string.Join(", ", trade.Emotions) : "-") + "\n" +
                "🔍 Детали входа: " + (string.IsNullOrWhiteSpace(trade.EntryDetails) ? "-" : trade.EntryDetails) + "\n" +
                "📝 Заметка: " + (string.IsNullOrWhiteSpace(trade.Note) ? "-" : trade.Note);

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
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "history") }
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
                new[] { InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), "main") }
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
                new[] { InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), "main") }
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
                new[] { InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), "main") }
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
                sb.AppendLine(GetText("history_page", settings.Language, page, totalPages));
            }

            var buttons = new List<InlineKeyboardButton[]>();
            if (ordered.Any())
            {
                var pag = new List<InlineKeyboardButton>();
                if (page > 1) pag.Add(InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), $"history_page_{page - 1}_period_{period}_filter_{filter ?? "none"}"));
                if (page < totalPages) pag.Add(InlineKeyboardButton.WithCallbackData(settings.Language == "ru" ? "…Ещё" : "…More", $"history_page_{page + 1}_period_{period}_filter_{filter ?? "none"}"));
                if (pag.Any()) buttons.Add(pag.ToArray());

                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("history_filters", settings.Language), $"history_filter_menu") });
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("export", settings.Language), "export") });
            }

            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), "main") });
            return (sb.ToString(), new InlineKeyboardMarkup(buttons));
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetHistoryFiltersMenu(UserSettings settings)
        {
            string text = settings.Language == "ru" ? "🔍 Фильтры истории:\nВыберите категорию:" : "🔍 History filters:\nSelect category:";
            var rows = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData(settings.Language == "ru" ? "📅 По дате" : "📅 By date", "historyfilter_date_menu") },
                new[] { InlineKeyboardButton.WithCallbackData(settings.Language == "ru" ? "📈 По тикеру" : "📈 By ticker", "historyfilter_ticker_menu") },
                new[] { InlineKeyboardButton.WithCallbackData(settings.Language == "ru" ? "↕ По направлению" : "↕ By direction", "historyfilter_direction_menu") },
                new[] { InlineKeyboardButton.WithCallbackData(settings.Language == "ru" ? "✅/❌ По результату" : "✅/❌ By result", "historyfilter_result_menu") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), "history") }
            };
            return (text, new InlineKeyboardMarkup(rows));
        }

        public InlineKeyboardMarkup GetHistoryFilterSubmenu(string type, UserSettings settings)
        {
            var rows = new List<InlineKeyboardButton[]>();
            switch (type)
            {
                case "date":
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData(settings.Language == "ru" ? "7 дней" : "7 days", "historyfilter_date_7d") });
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData(settings.Language == "ru" ? "30 дней" : "30 days", "historyfilter_date_30d") });
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData(settings.Language == "ru" ? "Все время" : "All time", "historyfilter_date_all") });
                    break;
                case "ticker":
                    if (settings.FavoriteTickers.Any())
                    {
                        foreach (var t in settings.FavoriteTickers.Take(12))
                        {
                            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(t, $"historyfilter_ticker_{SanitizeCallbackData(t)}") });
                        }
                    }
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData(settings.Language == "ru" ? "Все" : "All", "historyfilter_ticker_all") });
                    break;
                case "direction":
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData("LONG", "historyfilter_direction_Long") });
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData("SHORT", "historyfilter_direction_Short") });
                    break;
                case "result":
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData(settings.Language == "ru" ? "✅ Профит" : "✅ Profit", "historyfilter_result_profit") });
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData(settings.Language == "ru" ? "❌ Убыток" : "❌ Loss", "historyfilter_result_loss") });
                    break;
            }
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), "history_filter_menu") });
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
                $"🎯 Результат: {trade.Result ?? "-"} | R:R: {trade.RR ?? "-"} | Риск: {trade.Risk?.ToString("0.##") ?? "-"}%\n" +
                $"📈 PnL: {trade.PnL:0.##}%\n" +
                $"🧩 Контекст: {(trade.Context != null && trade.Context.Any() ? string.Join(", ", trade.Context) : "-" )}\n" +
                $"🧠 Сетап: {(trade.Setup != null && trade.Setup.Any() ? string.Join(", ", trade.Setup) : "-" )}\n" +
                $"🙂 Эмоции: {(trade.Emotions != null && trade.Emotions.Any() ? string.Join(", ", trade.Emotions) : "-" )}\n" +
                $"🔍 Детали входа: {trade.EntryDetails ?? "-"}\n" +
                $"📝 Заметка: {trade.Note ?? "-"}";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), "history") }
            });
            return (text, keyboard);
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetFavoriteTickersMenu(UserSettings settings)
        {
            string text;
            if (settings.Language == "ru")
            {
                text = "📈 Избранные тикеры:\n\n";
                if (settings.FavoriteTickers.Any())
                {
                    text += string.Join(", ", settings.FavoriteTickers);
                }
                else
                {
                    text += "Пусто";
                }
            }
            else
            {
                text = "📈 Favorite tickers:\n\n";
                if (settings.FavoriteTickers.Any())
                {
                    text += string.Join(", ", settings.FavoriteTickers);
                }
                else
                {
                    text += "Empty";
                }
            }

            var buttons = new List<InlineKeyboardButton[]>();
            foreach (var ticker in settings.FavoriteTickers)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData($"❌ {ticker}", $"remove_ticker_{SanitizeCallbackData(ticker)}") });
            }
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("add_ticker", settings.Language), "add_favorite_ticker") });
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("back_to_settings", settings.Language), "settings") });

            return (text, new InlineKeyboardMarkup(buttons));
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetRemoveFavoriteTickerMenu(UserSettings settings)
        {
            string text;
            if (settings.Language == "ru")
            {
                text = "❌ Выберите тикер для удаления:";
            }
            else
            {
                text = "❌ Select ticker to remove:";
            }
            
            var buttons = new List<InlineKeyboardButton[]>();

            foreach (var ticker in settings.FavoriteTickers)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData($"❌ {ticker}", $"remove_ticker_{SanitizeCallbackData(ticker)}") });
            }

            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("back_to_settings", settings.Language), "settings_tickers") });

            return (text, new InlineKeyboardMarkup(buttons));
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetHelpMenu(UserSettings settings)
        {
            string text;
            if (settings.Language == "ru")
            {
                text = "💡 Помощь:\n\n" +
                       "📸 Отправьте скриншот сделки для автоматического заполнения\n" +
                       "⌨️ Или создайте сделку вручную через главное меню\n" +
                       "📊 Просматривайте статистику и анализируйте результаты\n" +
                       "⚙️ Настройте бота под себя в настройках";
            }
            else
            {
                text = "💡 Help:\n\n" +
                       "📸 Send a screenshot of the deal for automatic filling\n" +
                       "⌨️ Or create a deal manually through the main menu\n" +
                       "📊 View statistics and analyze results\n" +
                       "⚙️ Configure the bot for yourself in settings";
            }

            var buttons = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData(settings.Language == "ru" ? "🆘 Техподдержка" : "🆘 Support", "support") },
                new[] { InlineKeyboardButton.WithCallbackData(settings.Language == "ru" ? "📣 Что нового" : "📣 What's new", "whatsnew") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), "main") }
            };

            return (text, new InlineKeyboardMarkup(buttons));
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetSettingsMenu(UserSettings settings)
        {
            string text;
            if (settings.Language == "ru")
            {
                text = "⚙️ Настройки:\n\n" +
                       $"🌐 Язык: {(settings.Language == "ru" ? "Русский 🇷🇺" : "English 🇺🇸")}\n" +
                       $"🔔 Уведомления: {(settings.NotificationsEnabled ? "Включены ✅" : "Выключены ❌")}\n" +
                       $"📈 Избранные тикеры: {settings.FavoriteTickers.Count} шт.\n" +
                       $"🌐 Notion: {(settings.NotionEnabled ? "Подключен ✅" : "Отключен ❌")}";
            }
            else
            {
                text = "⚙️ Settings:\n\n" +
                       $"🌐 Language: {(settings.Language == "ru" ? "Russian 🇷🇺" : "English 🇺🇸")}\n" +
                       $"🔔 Notifications: {(settings.NotificationsEnabled ? "Enabled ✅" : "Disabled ❌")}\n" +
                       $"📈 Favorite tickers: {settings.FavoriteTickers.Count} items\n" +
                       $"🌐 Notion: {(settings.NotionEnabled ? "Connected ✅" : "Disconnected ❌")}";
            }

            var rows = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData(settings.Language == "ru" ? "🌐 Сменить язык" : "🌐 Change language", "settings_language") },
                new[] { InlineKeyboardButton.WithCallbackData(
                    settings.Language == "ru" 
                        ? (settings.NotificationsEnabled ? "🔔 Уведомления: ✅" : "🔔 Уведомления: ❌")
                        : (settings.NotificationsEnabled ? "🔔 Notifications: ✅" : "🔔 Notifications: ❌"), 
                    "settings_notifications") },
                new[] { InlineKeyboardButton.WithCallbackData(settings.Language == "ru" ? "📈 Избранные тикеры" : "📈 Favorite tickers", "settings_tickers") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("notion_settings", settings.Language), "settings_notion") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), "main") }
            };

            return (text, new InlineKeyboardMarkup(rows));
        }

        /// <summary>
        /// Меню настроек Notion
        /// </summary>
        public (string Text, InlineKeyboardMarkup Keyboard) GetNotionSettingsMenu(UserSettings settings)
        {
            var status = settings.NotionEnabled ? GetText("notion_enabled", settings.Language) : GetText("notion_disabled", settings.Language);
            
            string text = $"{GetText("notion_settings", settings.Language)}\n\n{status}";
            
            if (settings.NotionEnabled)
            {
                if (settings.Language == "ru")
                {
                    text += $"\n\n🔑 Токен: {(string.IsNullOrEmpty(settings.NotionIntegrationToken) ? "❌ Не указан" : "✅ Указан")}";
                    text += $"\n🗄️ База данных: {(string.IsNullOrEmpty(settings.NotionDatabaseId) ? "❌ Не указана" : "✅ Указана")}";
                }
                else
                {
                    text += $"\n\n🔑 Token: {(string.IsNullOrEmpty(settings.NotionIntegrationToken) ? "❌ Not specified" : "✅ Specified")}";
                    text += $"\n🗄️ Database: {(string.IsNullOrEmpty(settings.NotionDatabaseId) ? "❌ Not specified" : "✅ Specified")}";
                }
            }
            
            var rows = new List<InlineKeyboardButton[]>();
            
            if (settings.NotionEnabled)
            {
                // Если Notion подключен, показываем опции управления
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("notion_token", settings.Language), "notion_token_input") });
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("notion_database", settings.Language), "notion_database_input") });
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("notion_test", settings.Language), "notion_test_connection") });
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("notion_disconnect", settings.Language), "notion_disconnect") });
            }
            else
            {
                // Если Notion не подключен, показываем опцию подключения
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("notion_connect", settings.Language), "notion_connect") });
            }
            
            // Добавляем кнопку помощи
            var helpText = settings.Language == "ru" ? "❓ Помощь" : "❓ Help";
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(helpText, "notion_help") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("back_to_settings", settings.Language), "settings") });
            
            return (text, new InlineKeyboardMarkup(rows));
        }

        /// <summary>
        /// Меню ввода токена Notion
        /// </summary>
        public (string Text, InlineKeyboardMarkup Keyboard) GetNotionTokenInputMenu(UserSettings settings)
        {
            string text = GetText("notion_token_input", settings.Language);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("back_to_settings", settings.Language), "settings_notion") }
            });
            
            return (text, keyboard);
        }

        /// <summary>
        /// Меню ввода Database ID Notion
        /// </summary>
        public (string Text, InlineKeyboardMarkup Keyboard) GetNotionDatabaseInputMenu(UserSettings settings)
        {
            string text = GetText("notion_database_input", settings.Language);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("back_to_settings", settings.Language), "settings_notion") }
            });
            
            return (text, keyboard);
        }

        /// <summary>
        /// Справка по подключению Notion
        /// </summary>
        public string GetNotionHelpText(string language)
        {
            if (language == "ru")
            {
                return @"🌐 Как подключить свою базу данных Notion

📋 Пошаговая инструкция:

1️⃣ **Создание интеграции**
   • Перейдите на https://www.notion.so/my-integrations
   • Нажмите 'New integration'
   • Введите название (например, 'TradingBot')
   • Выберите рабочее пространство
   • Скопируйте 'Internal Integration Token'

2️⃣ **Подготовка базы данных**
   • Создайте новую страницу в Notion
   • Добавьте базу данных (Database)
   • Настройте свойства для торговых сделок:
     - Ticker (Text)
     - Direction (Select: Long/Short)
     - PnL (Number)
     - Date (Date)
     - Account (Select)
     - Session (Select)
     - Position (Select)
     - Context (Select)
     - Setup (Select)
     - Result (Select)
     - Emotions (Multi-select)
     - RR (Text)
     - Risk (Number)

3️⃣ **Предоставление доступа**
   • Откройте созданную базу данных
   • Нажмите 'Share' в правом верхнем углу
   • Добавьте созданную интеграцию
   • Установите права 'Can edit'

4️⃣ **Получение Database ID**
   • Откройте базу данных в браузере
   • Скопируйте ID из URL:
     https://notion.so/workspace/DATABASE_ID?v=...
   • Или скопируйте весь URL

5️⃣ **Подключение в боте**
   • Нажмите 'Подключить Notion'
   • Введите токен интеграции
   • Введите Database ID или URL
   • Проверьте подключение

❓ Если что-то не работает:
   • Проверьте правильность токена
   • Убедитесь, что интеграция добавлена в базу
   • Проверьте права доступа
   • Убедитесь, что база данных содержит нужные свойства";
            }
            else
            {
                return @"🌐 How to connect your Notion database

📋 Step-by-step guide:

1️⃣ **Create Integration**
   • Go to https://www.notion.so/my-integrations
   • Click 'New integration'
   • Enter name (e.g., 'TradingBot')
   • Select workspace
   • Copy 'Internal Integration Token'

2️⃣ **Prepare Database**
   • Create new page in Notion
   • Add database
   • Configure properties for trades:
     - Ticker (Text)
     - Direction (Select: Long/Short)
     - PnL (Number)
     - Date (Date)
     - Account (Select)
     - Session (Select)
     - Position (Select)
     - Context (Select)
     - Setup (Select)
     - Result (Select)
     - Emotions (Multi-select)
     - RR (Text)
     - Risk (Number)

3️⃣ **Grant Access**
   • Open created database
   • Click 'Share' in top right
   • Add created integration
   • Set permissions to 'Can edit'

4️⃣ **Get Database ID**
   • Open database in browser
   • Copy ID from URL:
     https://notion.so/workspace/DATABASE_ID?v=...
   • Or copy entire URL

5️⃣ **Connect in Bot**
   • Click 'Connect Notion'
   • Enter integration token
   • Enter Database ID or URL
   • Test connection

❓ If something doesn't work:
   • Check token correctness
   • Ensure integration is added to database
   • Check access permissions
   • Verify database has required properties";
            }
        }

        /// <summary>
        /// Клавиатура для справки по Notion
        /// </summary>
        public InlineKeyboardMarkup GetNotionHelpKeyboard(string language)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("back_to_settings", language), "settings_notion") }
            };
            
            return new InlineKeyboardMarkup(rows);
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
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
                ["welcome"] = "🚀 Добро пожаловать в TradingBot Pro!\n\n📊 Ваш профессиональный помощник для анализа торговых сделок и улучшения результатов.\n\n💡 Ключевые возможности:\n• 📸 Автоматическое извлечение данных из скриншотов\n• ⌨️ Интуитивный ручной ввод сделок\n• 📈 Продвинутая аналитика и отчёты\n• 🌐 Интеграция с Notion для командной работы\n• 💾 Безопасное локальное хранение\n• 📊 Детальные графики и метрики\n\n🎯 Нажмите 'Далее' для быстрого обучения!",
                ["onboarding_1"] = "📥 Добавление сделок\n\nУ вас есть два способа:\n\n📸 Скриншот: Отправьте фото с экрана терминала - я автоматически извлеку данные\n\n⌨️ Ручной ввод: Пошаговое заполнение всех полей сделки\n\nКаждый способ одинаково эффективен!",
                ["onboarding_2"] = "📊 Аналитика и статистика\n\nПосле добавления сделок вы получите:\n\n📈 Детальную статистику по периодам\n📊 Графики эквити и P&L\n🎯 Анализ win rate и серий\n📅 Отчёты по дням/неделям/месяцам\n\nВся информация в удобном формате!",
                ["onboarding_3"] = "⚙️ Настройки и интеграции\n\nНастройте бота под себя:\n\n🌐 Язык интерфейса (RU/EN)\n🔔 Уведомления о важных событиях\n📊 Интеграция с Notion для командной работы\n💾 Локальная база данных для приватности\n\n🌐 Персональная интеграция с Notion:\n• Подключите свою базу данных\n• Используйте собственные справочники\n• Синхронизируйте сделки\n\n🔧 Меню настроек:\n• Управление языком и уведомлениями\n• Подключение персонального Notion\n• Настройка избранных тикеров\n• Персональные справочники\n\nГотовы начать? Нажмите 'Главное меню'!",
                ["main_menu"] = "🎯 TradingBot - Ваш помощник в торговле\n\n📊 Статистика за сегодня:\n📅 Сделок: {0} | 📈 PnL: {1} | ✅ Win Rate: {2}\n\n🚀 Что хотите сделать?\n\n➕ Добавить новую сделку\n📈 Посмотреть статистику\n📜 История всех сделок\n⚙️ Настройки бота\n🆘 Помощь и поддержка",
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
                
                // Новые ключи для улучшенных сообщений об ошибках
                ["error_what_happened"] = "Что произошло",
                ["error_what_to_do"] = "Что делать",
                ["error_next_step"] = "Следующий шаг",
                ["error_unknown"] = "Неизвестная ошибка",

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
                ["export"] = "💾 Экспорт в CSV",
                ["validation_error"] = "⚠️ Ошибка валидации сделки:",
                
                // Кнопки и элементы интерфейса
                ["main_menu_button"] = "🏠 Главное меню",
                ["add_trade"] = "➕ Добавить сделку",
                ["my_stats"] = "📈 Моя статистика",
                ["trade_history"] = "📜 История сделок",
                ["settings"] = "⚙️ Настройки",
                ["help"] = "🆘 Помощь",
                ["support"] = "💬 Поддержка",
                ["next"] = "Далее ▶",
                ["start"] = "🚀 Начать!",
                ["back"] = "◀ Назад",
                ["skip_tutorial"] = "⏭️ Пропустить обучение",
                ["other"] = "Другое",
                ["input_manually"] = "✏️ Ввести вручную",
                ["prefill_last"] = "🔄 Использовать последнюю",
                ["skip"] = "⏭️ Пропустить",
                ["cancel"] = "❌ Отмена",
                ["save"] = "✅ Сохранить",
                ["confirm"] = "✅ Подтвердить",
                ["edit"] = "✏️ Редактировать",
                ["delete"] = "🗑️ Удалить",
                ["edit_ticker"] = "📌 Тикер",
                ["edit_account"] = "🧾 Аккаунт",
                ["edit_session"] = "🕒 Сессия",
                ["edit_position"] = "📐 Позиция",
                ["edit_direction"] = "↕️ Направление",
                ["edit_context"] = "🧩 Контекст",
                ["edit_setup"] = "🧠 Сетап",
                ["edit_result"] = "🎯 Результат",
                ["edit_rr"] = "⚖️ R:R",
                ["edit_risk"] = "⚠️ Риск %",
                ["edit_profit"] = "📈 Прибыль %",
                ["edit_emotions"] = "😃 Эмоции",
                ["edit_entry"] = "🔍 Детали входа",
                ["edit_note"] = "📝 Заметка",
                ["language"] = "🌐 Язык",
                ["notifications"] = "🔔 Уведомления",
                ["favorite_tickers"] = "⭐ Избранные тикеры",
                ["notion_integration"] = "🌐 Интеграция с Notion",
                ["reset_settings"] = "🔄 Сбросить настройки",
                ["back_to_main"] = "🏠 Вернуться в главное меню",
                ["stats_today"] = "📊 Статистика за сегодня",
                ["stats_week"] = "📊 Статистика за неделю",
                ["stats_month"] = "📊 Статистика за месяц",
                ["stats_all_time"] = "📊 Общая статистика",
                ["export_data"] = "📄 Экспорт данных",
                ["clear_history"] = "🧹 Очистить историю",
                ["about_bot"] = "ℹ️ О боте",
                ["contact_support"] = "📞 Связаться с поддержкой",
                ["version_info"] = "ℹ️ Версия",
                ["changelog"] = "📝 История изменений",
                ["more"] = "…Ещё",
                ["filter_by_date"] = "📅 По дате",
                ["filter_by_ticker"] = "📈 По тикеру",
                ["filter_by_direction"] = "↕ По направлению",
                ["filter_by_result"] = "✅/❌ По результату",
                
                // Локализация для превью сделок
                ["preview_ticker"] = "📌 Тикер",
                ["preview_account"] = "🧾 Аккаунт",
                ["preview_session"] = "🕒 Сессия",
                ["preview_position"] = "📐 Позиция",
                ["preview_direction"] = "↕️ Направление",
                ["preview_context"] = "🧩 Контекст",
                ["preview_setup"] = "🧠 Сетап",
                ["preview_result"] = "🎯 Результат",
                ["preview_rr"] = "⚖️ R:R",
                ["preview_risk"] = "⚠️ Риск",
                ["preview_profit"] = "📈 Прибыль",
                ["preview_emotions"] = "😃 Эмоции",
                ["preview_entry_details"] = "🔍 Детали входа",
                ["preview_note"] = "📝 Заметка",
                ["preview_empty_value"] = "-",
                
                // Локализация для фильтров истории
                ["history_filter_title"] = "🔍 Выберите фильтр для истории:",
                ["history_filter_period"] = "Текущий период",
                ["history_filter_all_tickers"] = "Все тикеры",
                ["history_filter_pnl_gt_1"] = ">1%",
                ["history_filter_pnl_lt_minus_1"] = "<-1%",
                ["history_filter_direction_long"] = "Long",
                ["history_filter_direction_short"] = "Short",
                ["history_filter_clear_active"] = "🧹 Очистить активные",
                ["history_filter_ticker_label"] = "Тикер",
                ["history_filter_pnl_label"] = "PnL"
            },
            // Английские тексты (можно аналогично заполнить или оставить пустыми для примера)
            ["en"] = new Dictionary<string, string>
            {
                ["welcome"] = "🚀 Welcome to TradingBot Pro!\n\n📊 Your professional assistant for trading analysis and performance improvement.\n\n💡 Key capabilities:\n• 📸 Automatic data extraction from screenshots\n• ⌨️ Intuitive manual trade entry\n• 📈 Advanced analytics and reports\n• 🌐 Notion integration for team collaboration\n• 💾 Secure local storage\n• 📊 Detailed charts and metrics\n\n🎯 Click 'Next' for a quick tutorial!",
                ["onboarding_1"] = "📥 Adding Deals\n\nYou have two ways:\n\n📸 Screenshot: Send a photo of your terminal screen - I'll automatically extract data\n\n⌨️ Manual entry: Step-by-step filling of all deal fields\n\nBoth methods are equally effective!",
                ["onboarding_2"] = "📊 Analytics and Statistics\n\nAfter adding deals, you'll get:\n\n📈 Detailed statistics by periods\n📊 Equity and P&L charts\n🎯 Win rate and streak analysis\n📅 Reports by days/weeks/months\n\nAll information in a convenient format!",
                ["onboarding_3"] = "⚙️ Settings and Integrations\n\nConfigure the bot for yourself:\n\n🌐 Interface language (RU/EN)\n🔔 Notifications about important events\n📊 Notion integration for team work\n💾 Local database for privacy\n\n🌐 Personal Notion integration:\n• Connect your own database\n• Use custom dictionaries\n• Sync your trades\n\n🔧 Settings menu:\n• Language and notification management\n• Personal Notion connection\n• Favorite tickers setup\n• Personal dictionaries\n\nReady to start? Click 'Main Menu'!",
                ["main_menu"] = "🎯 TradingBot - Your Trading Assistant\n\n📊 Today's Statistics:\n📅 Deals: {0} | 📈 PnL: {1} | ✅ Win Rate: {2}\n\n🚀 What would you like to do?\n\n➕ Add new deal\n📈 View statistics\n📜 Deal history\n⚙️ Bot settings\n🆘 Help and support",
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
                ["validation_error"] = "⚠️ Trade validation error:",
                ["stats_menu"] = "📊 Statistics:",
                ["stats_result"] = "📊 Statistics for {0}:\n\n📈 Total trades: {1}\n💰 Total PnL: {2}%\n✅ Profitable: {3}\n❌ Losing: {4}\n🎯 Win rate: {5}%",
                ["equity_curve"] = "📈 Equity curve:",
                ["no_trades"] = "📭 No trades to display",
                ["history_filters"] = "🔍 History filters:",
                ["history_page"] = "Page {0} of {1}",
                ["export"] = "💾 Export to CSV",
                ["more"] = "…More",
                ["filter_by_date"] = "📅 By date",
                ["filter_by_ticker"] = "📈 By ticker",
                ["filter_by_direction"] = "↕ By direction",
                ["filter_by_result"] = "✅/❌ By result",
                
                // Localization for trade previews
                ["preview_ticker"] = "📌 Ticker",
                ["preview_account"] = "🧾 Account",
                ["preview_session"] = "🕒 Session",
                ["preview_position"] = "📐 Position",
                ["preview_direction"] = "↕️ Direction",
                ["preview_context"] = "🧩 Context",
                ["preview_setup"] = "🧠 Setup",
                ["preview_result"] = "🎯 Result",
                ["preview_rr"] = "⚖️ R:R",
                ["preview_risk"] = "⚠️ Risk",
                ["preview_profit"] = "📈 Profit",
                ["preview_emotions"] = "😃 Emotions",
                ["preview_entry_details"] = "🔍 Entry Details",
                ["preview_note"] = "📝 Note",
                ["preview_empty_value"] = "-",
                
                // Localization for history filters
                ["history_filter_title"] = "🔍 Select history filter:",
                ["history_filter_period"] = "Current period",
                ["history_filter_all_tickers"] = "All tickers",
                ["history_filter_pnl_gt_1"] = ">1%",
                ["history_filter_pnl_lt_minus_1"] = "<-1%",
                ["history_filter_direction_long"] = "Long",
                ["history_filter_direction_short"] = "Short",
                ["history_filter_clear_active"] = "🧹 Clear active",
                ["history_filter_ticker_label"] = "Ticker",
                ["history_filter_pnl_label"] = "PnL",
                
                // New keys for improved error messages
                ["error_what_happened"] = "What happened",
                ["error_what_to_do"] = "What to do",
                ["error_next_step"] = "Next step",
                ["error_unknown"] = "Unknown error",
                
                // Тексты шагов для английского языка
                ["step_1"] = "🟩⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜ Step 1/14: Select ticker",
                ["step_2"] = "🟩🟩⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜ Step 2/14: Select account",
                ["step_3"] = "🟩🟩🟩⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜ Step 3/14: Select session",
                ["step_4"] = "🟩🟩🟩🟩⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜ Step 4/14: Select position (LONG/SHORT)",
                ["step_5"] = "🟩🟩🟩🟩🟩⬜⬜⬜⬜⬜⬜⬜⬜⬜ Step 5/14: Select direction (LONG/SHORT)",
                ["step_6"] = "🟩🟩🟩🟩🟩🟩⬜⬜⬜⬜⬜⬜⬜⬜ Step 6/14: Select trade context",
                ["step_7"] = "🟩🟩🟩🟩🟩🟩🟩⬜⬜⬜⬜⬜⬜⬜ Step 7/14: Select setup/strategy",
                ["step_8"] = "🟩🟩🟩🟩🟩🟩🟩🟩⬜⬜⬜⬜⬜⬜ Step 8/14: Enter risk (%)",
                ["step_9"] = "🟩🟩🟩🟩🟩🟩🟩🟩🟩⬜⬜⬜⬜⬜ Step 9/14: Enter R:R ratio",
                ["step_10"] = "🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩⬜⬜⬜⬜ Step 10/14: Select trade result",
                ["step_11"] = "🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩⬜⬜⬜ Step 11/14: Enter profit (%)",
                ["step_12"] = "🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩⬜⬜ Step 12/14: Select emotions",
                ["step_13"] = "🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩⬜ Step 13/14: Enter entry details",
                ["step_14"] = "🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩🟩 Step 14/14: Enter note",
                
                // Дополнительные тексты для английского языка
                ["trade_preview"] = "✅ Check entered data:\n\n📌 Ticker: {0}\n📌 Account: {1}\n📌 Session: {2}\n📌 Position: {3}\n📌 Direction: {4}\n📌 Context: {5}\n📌 Setup: {6}\n📌 Result: {7}\n📌 R:R = {8}\n📌 Risk: {9}%\n📌 Profit: {10}%\n😃 Emotions: {11}\n🔍 Entry details: {12}\n📝 Note: {13}",
                ["confirm_trade"] = "Save trade?",
                ["edit_field"] = "✏️ Which field to edit?",
                ["pending_trades"] = "⏳ Active trades:\n{0}",
                ["no_pending_trades"] = "⏳ No active trades.",
                ["advanced_stats"] = "📈 Trades: {0}\nTotal PnL: {1}%\nAverage PnL: {2}%\nBest: {3}%\nWorst: {4}%\nWin Rate: {5}%",
                ["date_label"] = "📅 Date",
                ["pnl_label"] = "📈 Cumulative PnL",
                ["error_graph"] = "⚠️ Error creating graph.",
                ["export_success"] = "📄 Export completed successfully!",
                
                // Тексты для ввода
                ["input_ticker"] = "📝 Enter ticker (e.g.: BTC/USDT):",
                ["input_account"] = "📝 Enter account name:",
                ["input_session"] = "📝 Enter trading session:",
                ["input_position"] = "📝 Enter position type:",
                ["input_direction"] = "📝 Enter direction:",
                ["input_risk"] = "📝 Enter risk size in %:",
                ["input_rr"] = "📝 Enter R:R ratio:",
                ["input_profit"] = "📝 Enter profit in %:",
                ["input_context"] = "📝 Enter trade context:",
                ["input_setup"] = "📝 Enter setup/strategy:",
                ["input_result"] = "📝 Enter trade result:",
                ["input_emotions"] = "📝 Enter emotions:",
                ["input_entry"] = "📝 Enter entry details:",
                ["input_note"] = "📝 Enter note:",
                
                // Тексты для уведомлений
                ["ticker_added"] = "✅ Ticker {0} added to favorites!",
                ["ticker_removed"] = "🗑️ Ticker {0} removed from favorites!",
                ["add_ticker"] = "➕ Add ticker",
                ["remove_ticker"] = "🗑️ Remove ticker",
                ["retry"] = "🔄 Retry",
                ["help_menu"] = "💡 Select help section:",
                ["settings_menu"] = "⚙️ Settings:",
                ["settings_updated"] = "✅ Settings updated!",
                ["settings_reset"] = "🔄 Settings reset.",
                ["trade_expired"] = "⏰ Trade expired. Start over.",
                ["trade_deleted"] = "🗑️ Trade deleted.",
                ["trade_cancelled"] = "❌ Trade input cancelled.",
                ["trade_not_saved"] = "❌ Failed to save trade.",
                ["notion_save_error"] = "Check Notion API settings.",
                ["local_save_error"] = "Local database issues.",
                ["trade_saved"] = "✅ Trade {0} (PnL={1}%) saved!",
                ["trade_saved_local"] = "💾 Trade saved locally.",
                ["trade_sent_notion"] = "🌐 Data sent to Notion.",
                ["win_streak"] = "🔥 Win streak: {0} trades in a row!",
                ["loss_streak"] = "💔 Loss streak: {0} trades in a row. Don't give up!",
                ["error_occurred"] = "⚠️ An error occurred. Try again.",
                ["all_pending_cleared"] = "🧹 All active trades cleared.",
                ["support_contact"] = "📞 Contact support: @support_username",
                ["error_getting_image"] = "❌ Error getting image.",
                ["error_processing_image"] = "❌ Error processing image.",
                ["rate_limit"] = "⏳ Too many requests. Wait a minute.",
                ["notion_settings_saved"] = "✅ Notion settings saved",
                
                // Buttons and UI elements
                ["main_menu_button"] = "🏠 Main Menu",
                ["add_trade"] = "➕ Add Trade",
                ["my_stats"] = "📈 My Statistics",
                ["trade_history"] = "📜 Trade History",
                ["settings"] = "⚙️ Settings",
                ["help"] = "🆘 Help",
                ["support"] = "💬 Support",
                ["next"] = "Next ▶",
                ["start"] = "🚀 Start!",
                ["back"] = "◀ Back",
                ["skip_tutorial"] = "⏭️ Skip Tutorial",
                ["other"] = "Other",
                ["input_manually"] = "✏️ Enter Manually",
                ["prefill_last"] = "🔄 Use Last",
                ["skip"] = "⏭️ Skip",
                ["cancel"] = "❌ Cancel",
                ["save"] = "✅ Save",
                ["confirm"] = "✅ Confirm",
                ["edit"] = "✏️ Edit",
                ["delete"] = "🗑️ Delete",
                ["edit_ticker"] = "📌 Ticker",
                ["edit_account"] = "🧾 Account",
                ["edit_session"] = "🕒 Session",
                ["edit_position"] = "📐 Position",
                ["edit_direction"] = "↕️ Direction",
                ["edit_context"] = "🧩 Context",
                ["edit_setup"] = "🧠 Setup",
                ["edit_result"] = "🎯 Result",
                ["edit_rr"] = "⚖️ R:R",
                ["edit_risk"] = "⚠️ Risk %",
                ["edit_profit"] = "📈 Profit %",
                ["edit_emotions"] = "😃 Emotions",
                ["edit_entry"] = "🔍 Entry Details",
                ["edit_note"] = "📝 Note",
                ["language"] = "🌐 Language",
                ["notifications"] = "🔔 Notifications",
                ["favorite_tickers"] = "⭐ Favorite Tickers",
                ["notion_integration"] = "🌐 Notion Integration",
                ["reset_settings"] = "🔄 Reset Settings",
                ["back_to_main"] = "🏠 Back to Main",
                ["stats_today"] = "📊 Today's Stats",
                ["stats_week"] = "📊 Week's Stats",
                ["stats_month"] = "📊 Month's Stats",
                ["stats_all_time"] = "📊 All Time Stats",
                ["export_data"] = "📄 Export Data",
                ["clear_history"] = "🧹 Clear History",
                ["about_bot"] = "ℹ️ About Bot",
                ["contact_support"] = "📞 Contact Support",
                ["version_info"] = "ℹ️ Version",
                ["changelog"] = "📝 Changelog"
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
                    InlineKeyboardButton.WithCallbackData(GetText("add_trade", settings.Language), "start_trade"),
                    InlineKeyboardButton.WithCallbackData(GetText("my_stats", settings.Language), "stats")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(GetText("trade_history", settings.Language), "history"),
                    InlineKeyboardButton.WithCallbackData(GetText("settings", settings.Language), "settings")
                },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("help", settings.Language), "help") }
            };
            return new InlineKeyboardMarkup(buttons);
        }

        /// <summary>
        /// Получает улучшенное главное меню с персональными элементами
        /// </summary>
        public async Task<InlineKeyboardMarkup> GetSmartMainMenuAsync(UserSettings settings, long userId)
        {
            var buttons = new List<InlineKeyboardButton[]>();
            
            // Основные функции
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(GetText("add_trade", settings.Language), "start_trade"),
                InlineKeyboardButton.WithCallbackData(GetText("my_stats", settings.Language), "stats")
            });
            
            // Быстрые действия (на основе истории пользователя)
            var quickActions = await GetQuickActionsAsync(userId, settings.Language);
            if (quickActions.Any())
            {
                buttons.Add(quickActions.Select(a => InlineKeyboardButton.WithCallbackData(a.Text, a.Callback)).ToArray());
            }
            
            // Стандартные функции
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(GetText("trade_history", settings.Language), "history"),
                InlineKeyboardButton.WithCallbackData(GetText("settings", settings.Language), "settings")
            });
            
            // Помощь и поддержка
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(GetText("help", settings.Language), "help"),
                InlineKeyboardButton.WithCallbackData(GetText("support", settings.Language), "support")
            });
            
            return new InlineKeyboardMarkup(buttons);
        }

        /// <summary>
        /// Получает улучшенный текст главного меню с персональным приветствием
        /// </summary>
        public string GetEnhancedMainMenuText(UserSettings settings, int totalTrades = 0, decimal totalPnL = 0, double winRate = 0)
        {
            // Добавляем персональное приветствие
            var greeting = GetPersonalizedGreeting(settings.Language);
            
            // Используем цветовое кодирование для статистики
            var coloredPnL = GetColoredPnL((double)totalPnL);
            var coloredWinRate = GetColoredWinRate(winRate);
            
            var baseText = GetText("main_menu", settings.Language, totalTrades, coloredPnL, coloredWinRate);
            
            return $"{greeting}\n\n{baseText}";
        }

        /// <summary>
        /// Получает быстрые действия для пользователя
        /// </summary>
        private async Task<List<QuickAction>> GetQuickActionsAsync(long userId, string language)
        {
            var actions = new List<QuickAction>();
            
            // Здесь можно добавить логику для получения быстрых действий
            // Например, проверка активных сделок, уведомлений и т.д.
            
            // Пока возвращаем пустой список
            return actions;
        }

        public InlineKeyboardMarkup GetErrorKeyboard(UserSettings settings)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(GetText("retry", settings.Language), "retry"),
                    InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main")
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
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("next", language), $"onboarding_{step + 1}") });
            }
            else
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("start", language), "main") });
            }

            if (step > 1)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("back", language), $"onboarding_{step - 1}") });
            }

            // Добавляем кнопку "Пропустить обучение" на всех экранах
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("skip_tutorial", language), "main") });

            return (text, new InlineKeyboardMarkup(buttons));
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetTradeInputScreen(Trade trade, int step, UserSettings settings, string tradeId, Trade? lastTrade = null)
        {
            // Переписанный превью с эмодзи в значениях и локализацией
            var ctx = (trade.Context != null && trade.Context.Any()) ? string.Join(", ", trade.Context) : GetText("preview_empty_value", settings.Language);
            var setup = (trade.Setup != null && trade.Setup.Any()) ? string.Join(", ", trade.Setup) : GetText("preview_empty_value", settings.Language);
            var emos = (trade.Emotions != null && trade.Emotions.Any()) ? string.Join(", ", trade.Emotions) : GetText("preview_empty_value", settings.Language);
            string formattedPreview =
                GetText("preview_ticker", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Ticker) ? GetText("preview_empty_value", settings.Language) : trade.Ticker) + "\n" +
                GetText("preview_account", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Account) ? GetText("preview_empty_value", settings.Language) : trade.Account) + "\n" +
                GetText("preview_session", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Session) ? GetText("preview_empty_value", settings.Language) : trade.Session) + "\n" +
                GetText("preview_position", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Position) ? GetText("preview_empty_value", settings.Language) : trade.Position) + "\n" +
                GetText("preview_direction", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Direction) ? GetText("preview_empty_value", settings.Language) : trade.Direction) + "\n" +
                GetText("preview_context", settings.Language) + ": " + ctx + "\n" +
                GetText("preview_setup", settings.Language) + ": " + setup + "\n" +
                GetText("preview_result", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Result) ? GetText("preview_empty_value", settings.Language) : trade.Result) + "\n" +
                GetText("preview_rr", settings.Language) + " = " + (string.IsNullOrWhiteSpace(trade.RR) ? GetText("preview_empty_value", settings.Language) : trade.RR) + "\n" +
                GetText("preview_risk", settings.Language) + ": " + (trade.Risk?.ToString("0.##") ?? GetText("preview_empty_value", settings.Language)) + "%\n" +
                GetText("preview_profit", settings.Language) + ": " + trade.PnL.ToString("0.##") + "%\n" +
                GetText("preview_emotions", settings.Language) + ": " + emos + "\n" +
                GetText("preview_entry_details", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.EntryDetails) ? GetText("preview_empty_value", settings.Language) : trade.EntryDetails) + "\n" +
                GetText("preview_note", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Note) ? GetText("preview_empty_value", settings.Language) : trade.Note);

            // Безопасное получение текста шага с fallback на русский язык
            string prompt;
            if (_resources.ContainsKey(settings.Language) && _resources[settings.Language].ContainsKey($"step_{step}"))
            {
                prompt = _resources[settings.Language][$"step_{step}"];
            }
            else
            {
                // Fallback на русский язык, если нужный ключ отсутствует
                prompt = _resources["ru"][$"step_{step}"];
            }
            var buttons = new List<InlineKeyboardButton[]>();

            switch (step)
            {
                case 1: // Тикер
                    var fav = settings.FavoriteTickers ?? new List<string>();
                    var recent = settings.RecentTickers ?? new List<string>();
                    var tickers = fav.Concat(recent).Concat(PopularTickers).Distinct().Take(6).ToList();
                    foreach (var t in tickers)
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(t, $"set_ticker_{SanitizeCallbackData(t)}_trade_{tradeId}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("other", settings.Language), $"input_ticker_trade_{tradeId}") });
                    if (!string.IsNullOrEmpty(lastTrade?.Ticker))
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("prefill_last", settings.Language), $"set_ticker_{SanitizeCallbackData(lastTrade!.Ticker)}_trade_{tradeId}") });
                    // Добавляем кнопку Пропустить на первом шаге
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("skip", settings.Language), $"skip_trade_{tradeId}_step_{step}") });
                    break;

                case 2: // Аккаунт
                    var accounts = _accountOptions.Any() ? _accountOptions : DefaultAccounts;
                    foreach (var option in accounts)
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(option, $"set_account_{SanitizeCallbackData(option)}_trade_{tradeId}") });
                    //buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("skip", settings.Language), $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language), $"input_account_trade_{tradeId}") });
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

                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language), $"input_session_trade_{tradeId}") });
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
                    //buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("skip", settings.Language), $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language), $"input_context_trade_{tradeId}") });
                    break;

                case 7: // Сетап
                    var setups = _setupOptions.Any() ? _setupOptions : DefaultSetups;
                    foreach (var option in setups)
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(option, $"set_setup_{SanitizeCallbackData(option)}_trade_{tradeId}") });
                                       // buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("skip", settings.Language), $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language), $"input_setup_trade_{tradeId}") });
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
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language), $"input_{field}_trade_{tradeId}") });
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
                    //buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("skip", settings.Language), $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language), $"input_result_trade_{tradeId}") });
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
                    //buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("skip", settings.Language), $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language), $"input_emotions_trade_{tradeId}") });
                    break;

                case 13: // Детали входа
                    // Быстрые варианты типа входа
                    buttons.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🛒 Market", $"set_entry_market_trade_{tradeId}"),
                        InlineKeyboardButton.WithCallbackData("🏷 Limit",  $"set_entry_limit_trade_{tradeId}"),
                        InlineKeyboardButton.WithCallbackData("⛔ Stop",    $"set_entry_stop_trade_{tradeId}")
                    });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language), $"input_entry_trade_{tradeId}") });
                    break;

                case 14: // Заметка
                   // buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_resources[settings.Language]["skip"], $"skip_trade_{tradeId}_step_{step}") });
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("input_manually", settings.Language), $"input_note_trade_{tradeId}") });
                    break;
            }

            // Добавляем общие кнопки навигации
            if (step > 1 && step <= 14)
                buttons.Add(new[] {
                    InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), $"back_trade_{tradeId}_step_{step}"),
                    InlineKeyboardButton.WithCallbackData(GetText("skip", settings.Language), $"skip_trade_{tradeId}_step_{step}")
                });

            buttons.Add(new[] {
                InlineKeyboardButton.WithCallbackData(GetText("cancel", settings.Language), "cancel"),
                InlineKeyboardButton.WithCallbackData(GetText("save", settings.Language), $"save_trade_{tradeId}")
            });

            return ($"{formattedPreview}\n\n{prompt}", new InlineKeyboardMarkup(buttons));
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetTradeConfirmationScreen(Trade trade, string tradeId, UserSettings settings)
        {
            string text =
                GetText("preview_ticker", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Ticker) ? GetText("preview_empty_value", settings.Language) : trade.Ticker) + "\n" +
                GetText("preview_account", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Account) ? GetText("preview_empty_value", settings.Language) : trade.Account) + "\n" +
                GetText("preview_session", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Session) ? GetText("preview_empty_value", settings.Language) : trade.Session) + "\n" +
                GetText("preview_position", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Position) ? GetText("preview_empty_value", settings.Language) : trade.Position) + "\n" +
                GetText("preview_direction", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Direction) ? GetText("preview_empty_value", settings.Language) : trade.Direction) + "\n" +
                GetText("preview_context", settings.Language) + ": " + ((trade.Context != null && trade.Context.Any()) ? string.Join(", ", trade.Context) : GetText("preview_empty_value", settings.Language)) + "\n" +
                GetText("preview_setup", settings.Language) + ": " + ((trade.Setup != null && trade.Setup.Any()) ? string.Join(", ", trade.Setup) : GetText("preview_empty_value", settings.Language)) + "\n" +
                GetText("preview_result", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Result) ? GetText("preview_empty_value", settings.Language) : trade.Result) + "\n" +
                GetText("preview_rr", settings.Language) + " = " + (string.IsNullOrWhiteSpace(trade.RR) ? GetText("preview_empty_value", settings.Language) : trade.RR) + "\n" +
                GetText("preview_risk", settings.Language) + ": " + (trade.Risk?.ToString("0.##") ?? GetText("preview_empty_value", settings.Language)) + "%\n" +
                GetText("preview_profit", settings.Language) + ": " + trade.PnL.ToString("0.##") + "%\n" +
                GetText("preview_emotions", settings.Language) + ": " + ((trade.Emotions != null && trade.Emotions.Any()) ? string.Join(", ", trade.Emotions) : GetText("preview_empty_value", settings.Language)) + "\n" +
                GetText("preview_entry_details", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.EntryDetails) ? GetText("preview_empty_value", settings.Language) : trade.EntryDetails) + "\n" +
                GetText("preview_note", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Note) ? GetText("preview_empty_value", settings.Language) : trade.Note);

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("confirm", settings.Language), $"confirm_trade_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("edit", settings.Language), $"edit_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("delete", settings.Language), $"delete_{tradeId}") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") }
            });
            return (text, keyboard);
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetEditFieldMenu(Trade trade, string tradeId, UserSettings settings)
        {
            string preview =
                GetText("preview_ticker", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Ticker) ? GetText("preview_empty_value", settings.Language) : trade.Ticker) + "\n" +
                GetText("preview_account", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Account) ? GetText("preview_empty_value", settings.Language) : trade.Account) + "\n" +
                GetText("preview_session", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Session) ? GetText("preview_empty_value", settings.Language) : trade.Session) + "\n" +
                GetText("preview_position", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Position) ? GetText("preview_empty_value", settings.Language) : trade.Position) + "\n" +
                GetText("preview_direction", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Direction) ? GetText("preview_empty_value", settings.Language) : trade.Direction) + "\n" +
                GetText("preview_context", settings.Language) + ": " + ((trade.Context != null && trade.Context.Any()) ? string.Join(", ", trade.Context) : GetText("preview_empty_value", settings.Language)) + "\n" +
                GetText("preview_setup", settings.Language) + ": " + ((trade.Setup != null && trade.Setup.Any()) ? string.Join(", ", trade.Setup) : GetText("preview_empty_value", settings.Language)) + "\n" +
                GetText("preview_result", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Result) ? GetText("preview_empty_value", settings.Language) : trade.Result) + "\n" +
                GetText("preview_rr", settings.Language) + " = " + (string.IsNullOrWhiteSpace(trade.RR) ? GetText("preview_empty_value", settings.Language) : trade.RR) + "\n" +
                GetText("preview_risk", settings.Language) + ": " + (trade.Risk?.ToString("0.##") ?? GetText("preview_empty_value", settings.Language)) + "%\n" +
                GetText("preview_profit", settings.Language) + ": " + trade.PnL.ToString("0.##") + "%\n" +
                GetText("preview_emotions", settings.Language) + ": " + ((trade.Emotions != null && trade.Emotions.Any()) ? string.Join(", ", trade.Emotions) : GetText("preview_empty_value", settings.Language)) + "\n" +
                GetText("preview_entry_details", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.EntryDetails) ? GetText("preview_empty_value", settings.Language) : trade.EntryDetails) + "\n" +
                GetText("preview_note", settings.Language) + ": " + (string.IsNullOrWhiteSpace(trade.Note) ? GetText("preview_empty_value", settings.Language) : trade.Note);

            // Готовим плоский список кнопок с эмодзи
            var flat = new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(GetText("edit_ticker", settings.Language), $"editfield_ticker_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData(GetText("edit_account", settings.Language), $"editfield_account_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData(GetText("edit_session", settings.Language), $"editfield_session_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData(GetText("edit_position", settings.Language), $"editfield_position_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData(GetText("edit_direction", settings.Language), $"editfield_direction_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData(GetText("edit_context", settings.Language), $"editfield_context_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData(GetText("edit_setup", settings.Language), $"editfield_setup_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData(GetText("edit_result", settings.Language), $"editfield_result_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData(GetText("edit_rr", settings.Language), $"editfield_rr_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData(GetText("edit_risk", settings.Language), $"editfield_risk_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData(GetText("edit_profit", settings.Language), $"editfield_profit_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData(GetText("edit_emotions", settings.Language), $"editfield_emotions_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData(GetText("edit_entry", settings.Language), $"editfield_entry_trade_{tradeId}"),
                InlineKeyboardButton.WithCallbackData(GetText("edit_note", settings.Language), $"editfield_note_trade_{tradeId}")
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
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("main_menu_button", settings.Language), "main") });

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
            string text = GetText("history_filter_title", settings.Language) + "\n" + GetText("history_filter_period", settings.Language) + ": " + GetText($"period_{period}", settings.Language);
            var buttons = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("history_filter_all_tickers", settings.Language), "historyfilter_ticker_all") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("history_filter_pnl_gt_1", settings.Language), "historyfilter_pnl_gt_1") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("history_filter_pnl_lt_minus_1", settings.Language), "historyfilter_pnl_lt_-1") },
                // Long/Short — по две в ряд
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(GetText("history_filter_direction_long", settings.Language), "historyfilter_direction_Long"),
                    InlineKeyboardButton.WithCallbackData(GetText("history_filter_direction_short", settings.Language), "historyfilter_direction_Short")
                },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), "history") }
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
                            $"{GetText("history_filter_ticker_label", settings.Language)}: {t.Trade.Ticker}, {GetText("history_filter_pnl_label", settings.Language)}: {t.Trade.PnL}% ({t.CreatedAt:yyyy-MM-dd HH:mm})")));

            int pageSize = 5;
            int totalPages = (int)Math.Ceiling(total / (double)pageSize);
            var buttons = new List<InlineKeyboardButton[]>();

            if (pendingTrades.Count > 0)
            {
                foreach (var (tradeId, trade, _, _) in pendingTrades)
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData($"{trade.Ticker} ({trade.PnL}%)", $"edit_{tradeId}") });
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(GetText("history_filter_clear_active", settings.Language), "clearpending")
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
            double winRate = totalTrades > 0 ? (double)profitable / totalTrades * 100 : 0;
            string periodText = GetText($"period_{period}", settings.Language);
            
            // Используем цветовое кодирование для лучшего UX
            var coloredPnL = GetColoredPnL((double)totalPnL);
            var coloredWinRate = GetColoredWinRate(winRate);
            
            // Создаем улучшенный текст статистики
            var statsText = settings.Language == "ru" 
                ? $"📊 Статистика за {periodText}:\n\n📈 Всего сделок: {totalTrades}\n💰 Общий PnL: {coloredPnL}\n✅ Прибыльных: {profitable}\n❌ Убыточных: {losing}\n🎯 Винрейт: {coloredWinRate}"
                : $"📊 Statistics for {periodText}:\n\n📈 Total trades: {totalTrades}\n💰 Total PnL: {coloredPnL}\n✅ Profitable: {profitable}\n❌ Losing: {losing}\n🎯 Win rate: {coloredWinRate}";
            
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), "main") }
            });
            return (statsText, keyboard);
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
                if (page < totalPages) pag.Add(InlineKeyboardButton.WithCallbackData(GetText("more", settings.Language), $"history_page_{page + 1}_period_{period}_filter_{filter ?? "none"}"));
                if (pag.Any()) buttons.Add(pag.ToArray());

                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("history_filters", settings.Language), $"history_filter_menu") });
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("export", settings.Language), "export") });
            }

            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), "main") });
            return (sb.ToString(), new InlineKeyboardMarkup(buttons));
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetHistoryFiltersMenu(UserSettings settings)
        {
            string text = GetText("history_filters", settings.Language);
            var rows = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("filter_by_date", settings.Language), "historyfilter_date_menu") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("filter_by_ticker", settings.Language), "historyfilter_ticker_menu") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("filter_by_direction", settings.Language), "historyfilter_direction_menu") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("filter_by_result", settings.Language), "historyfilter_result_menu") },
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
                new[] { InlineKeyboardButton.WithCallbackData(GetText("support", settings.Language), "support") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("whatsnew", settings.Language), "whatsnew") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), "main") }
            };

            return (text, new InlineKeyboardMarkup(buttons));
        }

        public (string Text, InlineKeyboardMarkup Keyboard) GetSettingsMenu(UserSettings settings)
        {
            // Формируем текст настроек с учётом текущих значений
            string text;
            if (settings.Language == "ru")
            {
                text = "⚙️ Настройки:\n\n" +
                       $"🌐 Язык: {(settings.Language == "ru" ? "Русский" : "English")}\n" +
                       $"🔔 Уведомления: {(settings.NotificationsEnabled ? "Включены ✅" : "Выключены ❌")}\n" +
                       $"📈 Избранные тикеры: {settings.FavoriteTickers.Count}\n" +
                       $"🧩 Notion: {(settings.NotionEnabled ? "Подключен ✅" : "Отключен ❌")}";
            }
            else
            {
                text = "⚙️ Settings:\n\n" +
                       $"🌐 Language: {(settings.Language == "ru" ? "Russian" : "English")}\n" +
                       $"🔔 Notifications: {(settings.NotificationsEnabled ? "Enabled ✅" : "Disabled ❌")}\n" +
                       $"📈 Favorite tickers: {settings.FavoriteTickers.Count} items\n" +
                       $"🧩 Notion: {(settings.NotionEnabled ? "Connected ✅" : "Disconnected ❌")}";
            }

            // Основные кнопки меню настроек
            var rows = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("language", settings.Language), "settings_language") },
                new[] { InlineKeyboardButton.WithCallbackData(
                    settings.NotificationsEnabled 
                        ? GetText("notifications", settings.Language) + " 🔕"
                        : GetText("notifications", settings.Language) + " 🔔", 
                    "settings_notifications") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("favorite_tickers", settings.Language), "settings_tickers") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("notion_integration", settings.Language), "settings_notion") },
                new[] { InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), "main") }
            };

            return (text, new InlineKeyboardMarkup(rows));
        }

        /// <summary>
        /// Возвращает меню настроек Notion
        /// </summary>
        public (string Text, InlineKeyboardMarkup Keyboard) GetNotionSettingsMenu(UserSettings settings)
        {
            // Текст описания статуса интеграции
            var sb = new StringBuilder();
            sb.AppendLine("🌐 Настройки Notion:\n");
            if (settings.NotionEnabled && !string.IsNullOrEmpty(settings.NotionIntegrationToken))
            {
                sb.AppendLine("Интеграция включена ✅");
                string shortToken = settings.NotionIntegrationToken.Length > 6
                    ? settings.NotionIntegrationToken.Substring(0, 6) + "…"
                    : settings.NotionIntegrationToken;
                sb.AppendLine($"Токен: {shortToken}");
                if (!string.IsNullOrEmpty(settings.NotionDatabaseId))
                    sb.AppendLine($"Database ID: {settings.NotionDatabaseId}");
            }
            else
            {
                sb.AppendLine("Интеграция отключена ❌");
                sb.AppendLine("Вы можете подключить свой аккаунт Notion и использовать персональные справочники.");
            }

            var rows = new List<InlineKeyboardButton[]>();
            if (settings.NotionEnabled && !string.IsNullOrEmpty(settings.NotionIntegrationToken))
            {
                // опции для уже подключённого аккаунта
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("notion_disconnect", settings.Language), "notion_disconnect") });
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("notion_token", settings.Language), "notion_token") });
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("notion_database", settings.Language), "notion_database") });
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("notion_test", settings.Language), "notion_test") });
            }
            else
            {
                // опция для подключения
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("notion_connect", settings.Language), "notion_connect") });
            }
            // назад в основное меню настроек
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(GetText("back", settings.Language), "settings") });

            return (sb.ToString().TrimEnd(), new InlineKeyboardMarkup(rows));
        }

        /// <summary>
        /// Экран ввода токена Notion
        /// </summary>
        public (string Text, InlineKeyboardMarkup Keyboard) GetNotionTokenPrompt(UserSettings settings)
        {
            string text = GetText("notion_token_input", settings.Language);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("cancel", settings.Language), "notion_cancel") }
            });
            return (text, keyboard);
        }

        /// <summary>
        /// Экран ввода Database ID Notion
        /// </summary>
        public (string Text, InlineKeyboardMarkup Keyboard) GetNotionDatabasePrompt(UserSettings settings)
        {
            string text = GetText("notion_database_input", settings.Language);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(GetText("cancel", settings.Language), "notion_cancel") }
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

        // ==================== НОВЫЕ МЕТОДЫ ДЛЯ УЛУЧШЕНИЯ UX ====================

        /// <summary>
        /// Получает прогресс-индикатор для пошагового ввода сделок
        /// </summary>
        public string GetStepProgress(int currentStep, int totalSteps, string stepName, UserSettings settings)
        {
            var progress = (currentStep * 100) / totalSteps;
            var progressBar = string.Join("", Enumerable.Repeat("🟩", currentStep)) + string.Join("", Enumerable.Repeat("⬜", totalSteps - currentStep));
            
            return $"""
            📝 {stepName}
            
            Шаг {currentStep} из {totalSteps} ({progress}%)
            {progressBar}
            
            💡 Подсказка: {GetStepHint(currentStep, settings.Language)}
            """;
        }

        /// <summary>
        /// Получает подсказку для конкретного шага
        /// </summary>
        private string GetStepHint(int step, string language)
        {
            var hints = language == "ru" ? _ruStepHints : _enStepHints;
            return hints.TryGetValue(step, out var hint) ? hint : hints[0];
        }

        /// <summary>
        /// Подсказки для шагов на русском языке
        /// </summary>
        private readonly Dictionary<int, string> _ruStepHints = new()
        {
            { 1, "Выберите тикер из списка или введите новый" },
            { 2, "Укажите тип позиции (Long/Short)" },
            { 3, "Введите размер позиции в лотах" },
            { 4, "Укажите цену входа" },
            { 5, "Введите стоп-лосс в процентах" },
            { 6, "Укажите тейк-профит в процентах" },
            { 7, "Выберите торговую сессию" },
            { 8, "Опишите торговую идею" },
            { 9, "Укажите эмоциональное состояние" },
            { 10, "Добавьте скриншот (необязательно)" },
            { 11, "Проверьте все данные" },
            { 12, "Подтвердите сохранение" },
            { 13, "Выберите способ сохранения" },
            { 14, "Готово! Сделка сохранена" }
        };

        /// <summary>
        /// Подсказки для шагов на английском языке
        /// </summary>
        private readonly Dictionary<int, string> _enStepHints = new()
        {
            { 1, "Choose ticker from list or enter new one" },
            { 2, "Specify position type (Long/Short)" },
            { 3, "Enter position size in lots" },
            { 4, "Specify entry price" },
            { 5, "Enter stop loss in percentage" },
            { 6, "Specify take profit in percentage" },
            { 7, "Select trading session" },
            { 8, "Describe trading idea" },
            { 9, "Specify emotional state" },
            { 10, "Add screenshot (optional)" },
            { 11, "Check all data" },
            { 12, "Confirm saving" },
            { 13, "Choose saving method" },
            { 14, "Done! Trade saved" }
        };

        /// <summary>
        /// Получает цветовое кодирование для PnL
        /// </summary>
        public string GetColoredPnL(double pnl)
        {
            if (pnl > 0)
            {
                var emoji = pnl > 5 ? "🚀" : pnl > 2 ? "📈" : "🟢";
                return $"{emoji} +{pnl:F2}%";
            }
            if (pnl < 0)
            {
                var emoji = pnl < -5 ? "💥" : pnl < -2 ? "📉" : "🔴";
                return $"{emoji} {pnl:F2}%";
            }
            return $"⚪ {pnl:F2}%";
        }

        /// <summary>
        /// Получает цветовое кодирование для Win Rate
        /// </summary>
        public string GetColoredWinRate(double winRate)
        {
            if (winRate >= 70) return $"🏆 {winRate:F1}%";
            if (winRate >= 60) return $"🥇 {winRate:F1}%";
            if (winRate >= 50) return $"🥈 {winRate:F1}%";
            if (winRate >= 40) return $"🥉 {winRate:F1}%";
            return $"📊 {winRate:F1}%";
        }

        /// <summary>
        /// Получает цветовое кодирование для серий
        /// </summary>
        public string GetColoredStreak(int streak, bool isWin)
        {
            if (isWin)
            {
                if (streak >= 10) return $"🔥 {streak} побед подряд!";
                if (streak >= 5) return $"⚡ {streak} побед подряд";
                return $"✅ {streak} побед подряд";
            }
            else
            {
                if (streak >= 5) return $"💔 {streak} убытков подряд";
                return $"📉 {streak} убытков подряд";
            }
        }

        /// <summary>
        /// Получает персональное приветствие в зависимости от времени суток
        /// </summary>
        public string GetPersonalizedGreeting(string language)
        {
            var timeOfDay = GetTimeOfDay();
            var greeting = timeOfDay switch
            {
                "morning" => language == "ru" ? "🌅 Доброе утро" : "🌅 Good morning",
                "afternoon" => language == "ru" ? "☀️ Добрый день" : "☀️ Good afternoon",
                "evening" => language == "ru" ? "🌆 Добрый вечер" : "🌆 Good evening",
                "night" => language == "ru" ? "🌙 Доброй ночи" : "🌙 Good night",
                _ => language == "ru" ? "👋 Привет" : "👋 Hello"
            };
            
            return greeting;
        }

        /// <summary>
        /// Определяет время суток
        /// </summary>
        private string GetTimeOfDay()
        {
            var hour = DateTime.Now.Hour;
            return hour switch
            {
                >= 5 and < 12 => "morning",
                >= 12 and < 17 => "afternoon",
                >= 17 and < 22 => "evening",
                _ => "night"
            };
        }

        /// <summary>
        /// Получает дружелюбное сообщение об ошибке
        /// </summary>
        public string GetUserFriendlyErrorMessage(string errorCode, string details = null, string language = "ru")
        {
            var baseMessage = GetErrorMessageBase(errorCode, language);
            var suggestion = GetErrorSuggestion(errorCode, language);
            var action = GetErrorAction(errorCode, language);
            
            var message = $"""
            {baseMessage}
            
            🔍 {GetText("error_what_happened", language)}:
            {details ?? GetText("error_unknown", language)}
            
            💡 {GetText("error_what_to_do", language)}:
            {suggestion}
            
            🚀 {GetText("error_next_step", language)}:
            {action}
            """;
            
            return message;
        }

        /// <summary>
        /// Получает базовое сообщение об ошибке
        /// </summary>
        private string GetErrorMessageBase(string errorCode, string language)
        {
            return errorCode switch
            {
                "validation_error" => language == "ru" ? "⚠️ Проверьте введенные данные" : "⚠️ Check entered data",
                "database_error" => language == "ru" ? "💾 Проблема с базой данных" : "💾 Database problem",
                "network_error" => language == "ru" ? "🌐 Проблема с подключением" : "🌐 Connection problem",
                "permission_error" => language == "ru" ? "🔒 Недостаточно прав" : "🔒 Insufficient permissions",
                "timeout_error" => language == "ru" ? "⏰ Превышено время ожидания" : "⏰ Timeout exceeded",
                "notion_error" => language == "ru" ? "🌐 Проблема с Notion" : "🌐 Notion problem",
                "rate_limit_error" => language == "ru" ? "⏳ Слишком много запросов" : "⏳ Too many requests",
                _ => language == "ru" ? "❌ Произошла ошибка" : "❌ An error occurred"
            };
        }

        /// <summary>
        /// Получает предложения по решению ошибки
        /// </summary>
        private string GetErrorSuggestion(string errorCode, string language)
        {
            if (language == "ru")
            {
                return errorCode switch
                {
                    "validation_error" => "• Проверьте правильность ввода\n• Убедитесь, что все поля заполнены\n• Используйте правильный формат чисел",
                    "database_error" => "• Проверьте подключение к интернету\n• Попробуйте еще раз через минуту\n• Обратитесь в поддержку",
                    "network_error" => "• Проверьте интернет-соединение\n• Попробуйте перезагрузить бота\n• Проверьте настройки сети",
                    "permission_error" => "• Проверьте права доступа\n• Убедитесь, что вы авторизованы\n• Обратитесь к администратору",
                    "timeout_error" => "• Проверьте скорость интернета\n• Попробуйте еще раз\n• Уменьшите размер данных",
                    "notion_error" => "• Проверьте токен Notion\n• Убедитесь в правах доступа\n• Проверьте ID базы данных",
                    "rate_limit_error" => "• Подождите 1-2 минуты\n• Уменьшите частоту запросов\n• Используйте кэширование",
                    _ => "• Попробуйте еще раз\n• Обратитесь в поддержку\n• Проверьте логи ошибок"
                };
            }
            else
            {
                return errorCode switch
                {
                    "validation_error" => "• Check input correctness\n• Make sure all fields are filled\n• Use correct number format",
                    "database_error" => "• Check internet connection\n• Try again in a minute\n• Contact support",
                    "network_error" => "• Check internet connection\n• Try to restart the bot\n• Check network settings",
                    "permission_error" => "• Check access rights\n• Make sure you are authorized\n• Contact administrator",
                    "timeout_error" => "• Check internet speed\n• Try again\n• Reduce data size",
                    "notion_error" => "• Check Notion token\n• Make sure you have access\n• Check database ID",
                    "rate_limit_error" => "• Wait 1-2 minutes\n• Reduce request frequency\n• Use caching",
                    _ => "• Try again\n• Contact support\n• Check error logs"
                };
            }
        }

        /// <summary>
        /// Получает следующее действие для ошибки
        /// </summary>
        private string GetErrorAction(string errorCode, string language)
        {
            if (language == "ru")
            {
                return errorCode switch
                {
                    "validation_error" => "Исправьте данные и попробуйте снова",
                    "database_error" => "Попробуйте еще раз через минуту",
                    "network_error" => "Проверьте подключение и перезапустите",
                    "permission_error" => "Обратитесь к администратору",
                    "timeout_error" => "Уменьшите размер данных или подождите",
                    "notion_error" => "Проверьте настройки Notion",
                    "rate_limit_error" => "Подождите и попробуйте снова",
                    _ => "Обратитесь в поддержку"
                };
            }
            else
            {
                return errorCode switch
                {
                    "validation_error" => "Fix data and try again",
                    "database_error" => "Try again in a minute",
                    "network_error" => "Check connection and restart",
                    "permission_error" => "Contact administrator",
                    "timeout_error" => "Reduce data size or wait",
                    "notion_error" => "Check Notion settings",
                    "rate_limit_error" => "Wait and try again",
                    _ => "Contact support"
                };
            }
        }

        // ==================== МЕТОДЫ ДЛЯ ОТОБРАЖЕНИЯ ПРОГРЕССА ====================

        /// <summary>
        /// Отображает индикатор прогресса для процесса ввода сделок
        /// </summary>
        public void ShowProgressIndicator(string message, string language = "ru")
        {
            var progressMessage = language == "ru" 
                ? $"📊 [Прогресс] {message}"
                : $"📊 [Progress] {message}";
            
            // В реальном приложении здесь можно добавить логирование или отправку в чат
            Console.WriteLine(progressMessage);
        }

        /// <summary>
        /// Скрывает индикатор прогресса
        /// </summary>
        public void HideProgressIndicator(string language = "ru")
        {
            var completionMessage = language == "ru" 
                ? "✅ [Прогресс] Завершено"
                : "✅ [Progress] Completed";
            
            Console.WriteLine(completionMessage);
        }

        /// <summary>
        /// Отображает статистику с цветовым кодированием
        /// </summary>
        public void DisplayStatistics(Statistics stats, string language = "ru")
        {
            var header = language == "ru" ? "Статистика:" : "Statistics:";
            Console.WriteLine(header);

            DisplayStat("Прибыль", stats.Profit, language);
            DisplayStat("Убыток", stats.Loss, language);
            DisplayStat("Количество сделок", stats.TradeCount, language);
        }

        /// <summary>
        /// Отображает отдельную статистику с цветовым кодированием
        /// </summary>
        private void DisplayStat(string name, double value, string language)
        {
            var color = GetColorForValue(value);
            var resetColor = "\u001b[0m"; // Сброс цвета
            
            Console.WriteLine($"{name}: {color}{value}{resetColor}");
        }

        /// <summary>
        /// Получает цвет для значения
        /// </summary>
        private string GetColorForValue(double value)
        {
            if (value > 0)
                return "\u001b[32m"; // Зеленый
            else if (value < 0)
                return "\u001b[31m"; // Красный
            else
                return "\u001b[33m"; // Желтый
        }
    }
}
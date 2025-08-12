# Руководство по миграции на TradingBot v2.0

## 🚨 Важные изменения

### 1. Новая архитектура HTTP-клиентов
**Было**: Общий HttpClient с изменением DefaultRequestHeaders
**Стало**: Фабрика HTTP-клиентов для каждого пользователя

**Что изменилось**:
- Убрана проблема гонок между потоками
- Каждый пользователь получает отдельный HTTP-клиент
- Автоматическое освобождение ресурсов

**Миграция**: Автоматическая, не требует изменений в коде

### 2. Персональные настройки Notion
**Было**: Глобальные настройки Notion в appsettings.json
**Стало**: Индивидуальные настройки для каждого пользователя

**Что изменилось**:
- Новые поля в UserSettings: NotionEnabled, NotionIntegrationToken, NotionDatabaseId
- Каждый пользователь может подключить свою базу Notion
- Автоматическое переключение между глобальным и персональным Notion

**Миграция**: Требует обновления базы данных

### 3. Новое меню настроек
**Было**: Простое меню с языком и уведомлениями
**Стало**: Расширенное меню с настройками Notion

**Что изменилось**:
- Добавлен раздел "Настройки Notion"
- Возможность подключения/отключения интеграции
- Ввод токена и Database ID
- Проверка подключения

**Миграция**: Автоматическая, новые функции доступны сразу

### 4. Фоновые задачи
**Было**: Синхронное выполнение всех операций
**Стало**: Асинхронное выполнение тяжелых операций

**Что изменилось**:
- Создание страниц в Notion выполняется в фоне
- Автоматические повторы с экспоненциальным бэк-оффом
- Мониторинг выполнения задач

**Миграция**: Автоматическая, улучшает производительность

## 📋 Пошаговая миграция

### Шаг 1: Обновление кода
```bash
# Остановите текущий бот
# Сделайте backup базы данных
cp trades.db trades.db.backup

# Обновите код
git pull origin main

# Восстановите зависимости
dotnet restore
```

### Шаг 2: Обновление базы данных
```bash
# Применение миграций
dotnet ef database update

# Если миграции не работают, используйте автоматическую синхронизацию
dotnet run
```

### Шаг 3: Обновление конфигурации
```json
// appsettings.json
{
  "TelegramBot": {
    "Token": "YOUR_BOT_TOKEN"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=trades.db"
  },
  "UseNotion": true,
  "Notion": {
    "ApiToken": "YOUR_NOTION_TOKEN",
    "DatabaseId": "YOUR_DATABASE_ID"
  },
  "Developer": {
    "UserId": "YOUR_TELEGRAM_USER_ID"
  }
}
```

### Шаг 4: Тестирование
```bash
# Запуск в режиме разработки
dotnet run --environment Development

# Проверка логов
tail -f logs/tradingbot.log
```

## 🔧 Изменения в коде

### Обновленные сервисы
```csharp
// Было
services.AddHttpClient<NotionService>();

// Стало
services.AddHttpClient<NotionHttpClientFactory>();
services.AddSingleton<NotionHttpClientFactory>();
services.AddScoped<PersonalNotionService>();
services.AddScoped<NotionSettingsService>();
services.AddScoped<NotionSchemaCacheService>();
services.AddSingleton<BackgroundTaskService>();
```

### Новые интерфейсы
```csharp
// PersonalNotionService
public interface IPersonalNotionService
{
    Task<Dictionary<string, List<string>>> GetPersonalOptionsAsync(UserSettings userSettings);
    Task<List<string>> GetPersonalOptionsAsync(UserSettings userSettings, string fieldName);
    Task<bool> TestNotionConnectionAsync(UserSettings userSettings);
}

// NotionSettingsService
public interface INotionSettingsService
{
    Task<bool> EnableNotionAsync(UserSettings userSettings, string integrationToken, string databaseId);
    void DisableNotion(UserSettings userSettings);
    Task<bool> UpdateIntegrationTokenAsync(UserSettings userSettings, string newToken);
    Task<bool> UpdateDatabaseIdAsync(UserSettings userSettings, string newDatabaseId);
}
```

## 📊 Мониторинг миграции

### Логи для отслеживания
```bash
# Поиск ошибок миграции
grep -i "migration\|schema\|database" logs/tradingbot.log

# Проверка новых сервисов
grep -i "PersonalNotionService\|NotionSettingsService" logs/tradingbot.log

# Мониторинг фоновых задач
grep -i "BackgroundTaskService\|background" logs/tradingbot.log
```

### Метрики производительности
- Время ответа на команды
- Количество активных пользователей
- Статистика использования Notion API
- Производительность кеша

## 🚨 Возможные проблемы

### 1. Ошибки миграции базы данных
**Симптомы**: Ошибки при запуске, связанные с EF Core
**Решение**: Используйте автоматическую синхронизацию схемы

### 2. Проблемы с Notion API
**Симптомы**: Ошибки подключения к Notion
**Решение**: Проверьте токены и права доступа

### 3. Проблемы с кешем
**Симптомы**: Медленная загрузка опций
**Решение**: Очистите кеш и перезапустите бота

### 4. Проблемы с фоновыми задачами
**Симптомы**: Зависшие операции
**Решение**: Проверьте логи BackgroundTaskService

## ✅ Чек-лист миграции

- [ ] Сделан backup базы данных
- [ ] Обновлен код до последней версии
- [ ] Применены миграции базы данных
- [ ] Обновлена конфигурация
- [ ] Протестированы основные функции
- [ ] Проверены новые настройки Notion
- [ ] Протестированы фоновые задачи
- [ ] Проверены логи на ошибки
- [ ] Обновлена документация

## 🔄 Откат изменений

### Если что-то пошло не так
```bash
# Остановите бота
# Восстановите backup базы
cp trades.db.backup trades.db

# Откатитесь к предыдущей версии
git checkout v1.x

# Перезапустите
dotnet run
```

### Восстановление настроек
```sql
-- Сброс настроек Notion для всех пользователей
UPDATE UserSettings 
SET NotionEnabled = 0, 
    NotionIntegrationToken = NULL, 
    NotionDatabaseId = NULL;
```

## 📞 Поддержка при миграции

### Полезные команды
```bash
# Проверка состояния базы данных
dotnet ef database update --verbose

# Создание новой миграции
dotnet ef migrations add UpdateForNotionPersonalSettings

# Проверка схемы
dotnet ef dbcontext info
```

### Логи для диагностики
```bash
# Включение детального логирования
export Logging__LogLevel__Default=Debug
export Logging__LogLevel__TradingBot=Debug

# Запуск с детальными логами
dotnet run --environment Development
```

## 🎯 Следующие шаги

После успешной миграции:

1. **Обучение пользователей**: Расскажите о новых возможностях
2. **Мониторинг**: Следите за производительностью
3. **Оптимизация**: Настройте параметры под ваши нужды
4. **Масштабирование**: Подготовьтесь к росту пользователей

## 📚 Дополнительные ресурсы

- [Документация API](API_DOCS.md)
- [Примеры использования](EXAMPLES.md)
- [FAQ](FAQ.md)
- [Чат поддержки](https://t.me/your_support)

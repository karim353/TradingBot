# TradingBot - Модернизированная версия

Telegram-бот для ведения учета торговых сделок с поддержкой персональной интеграции с Notion.

## 🚀 Новые возможности

### 1. Безопасность и масштабирование HTTP-запросов
- **Фабрика HTTP-клиентов**: Каждый пользователь получает отдельный HTTP-клиент с персональными заголовками
- **Безопасность**: Исключены гонки между потоками при работе с Notion API
- **Масштабируемость**: Поддержка тысяч одновременных пользователей

### 2. Персональные базы Notion
- **Индивидуальные настройки**: Каждый пользователь может подключить свою базу данных Notion
- **Персональные справочники**: Использование собственных опций для полей сделок
- **Автоматическая синхронизация**: Сделки автоматически сохраняются в персональную базу

### 3. Улучшенное меню настроек
- **Подключение Notion**: Простой процесс подключения через токен и Database ID
- **Проверка подключения**: Тестирование соединения с Notion API
- **Управление интеграцией**: Включение/отключение, обновление настроек

### 4. Фоновые задачи
- **Асинхронная обработка**: Тяжелые операции выполняются в фоне
- **Повторные попытки**: Автоматические повторы с экспоненциальным бэк-оффом
- **Мониторинг**: Отслеживание выполнения фоновых задач

### 5. Умное кеширование
- **Персональный кеш**: Отдельный кеш для каждого пользователя
- **Автоматическая инвалидация**: Обновление кеша при изменении настроек
- **Оптимизация производительности**: Быстрый доступ к часто используемым данным

## 🛠 Установка и настройка

### Требования
- .NET 8.0
- SQLite
- Telegram Bot Token
- Notion API Token (опционально)

### Конфигурация

#### appsettings.json
```json
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

#### Переменные окружения
```bash
export TELEGRAM_BOT_TOKEN="your_bot_token"
export NOTION_API_TOKEN="your_notion_token"
export NOTION_DATABASE_ID="your_database_id"
```

### Запуск
```bash
dotnet restore
dotnet build
dotnet run
```

## 📱 Использование

### Первый запуск
1. Отправьте `/start` боту
2. Пройдите обучение (3 шага)
3. Перейдите в настройки
4. Подключите Notion (если нужно)

### Подключение Notion
1. **Получите Integration Token**:
   - Перейдите в [Notion Integrations](https://www.notion.so/my-integrations)
   - Создайте новую интеграцию
   - Скопируйте Internal Integration Token

2. **Получите Database ID**:
   - Откройте вашу базу данных в Notion
   - Скопируйте ID из URL (последняя часть после `/`)

3. **Подключите в боте**:
   - Настройки → Настройки Notion
   - Введите токен и Database ID
   - Проверьте подключение

### Основные команды
- `/start` - Начало работы с ботом
- `/menu` - Главное меню
- `/settings` - Настройки бота
- `/help` - Справка

## 🔧 Архитектура

### Основные компоненты
- **UpdateHandler** - Обработка сообщений Telegram
- **UIManager** - Управление интерфейсом
- **PersonalNotionService** - Работа с персональными базами Notion
- **NotionSettingsService** - Управление настройками Notion
- **BackgroundTaskService** - Выполнение фоновых задач
- **NotionSchemaCacheService** - Кеширование схемы Notion

### Схема данных
```
UserSettings
├── Language
├── NotificationsEnabled
├── FavoriteTickers
├── NotionEnabled
├── NotionIntegrationToken
└── NotionDatabaseId

Trade
├── Id
├── UserId
├── Ticker
├── Account
├── Session
├── Position
├── Direction
├── Context
├── Setup
├── Result
├── RR
├── Risk
├── PnL
├── Emotions
├── EntryDetails
├── Note
└── NotionPageId
```

## 📊 Производительность

### Оптимизации
- **Кеширование**: Схема Notion кешируется на 1 час
- **Фоновые задачи**: Тяжелые операции не блокируют UI
- **HTTP-клиенты**: Отдельные клиенты для каждого пользователя
- **База данных**: Индексы по UserId и Date

### Мониторинг
- **Логирование**: Детальные логи всех операций
- **Метрики**: Статистика выполнения фоновых задач
- **Ошибки**: Автоматические повторы с логированием

## 🔒 Безопасность

### Защита данных
- **Токены**: Хранение в зашифрованном виде
- **Изоляция**: Каждый пользователь работает со своей базой
- **Валидация**: Проверка всех входящих данных
- **Логирование**: Аудит всех операций

### API безопасность
- **Rate Limiting**: Ограничение запросов к Notion API
- **Таймауты**: Защита от зависших запросов
- **Повторные попытки**: Обработка временных сбоев

## 🚀 Развертывание

### Docker
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet build -c Release
EXPOSE 80
ENTRYPOINT ["dotnet", "TradingBot.dll"]
```

### Kubernetes
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tradingbot
spec:
  replicas: 3
  selector:
    matchLabels:
      app: tradingbot
  template:
    metadata:
      labels:
        app: tradingbot
    spec:
      containers:
      - name: tradingbot
        image: tradingbot:latest
        ports:
        - containerPort: 80
        env:
        - name: TELEGRAM_BOT_TOKEN
          valueFrom:
            secretKeyRef:
              name: bot-secrets
              key: token
```

## 📈 Мониторинг и логирование

### Логи
- **Уровни**: Debug, Information, Warning, Error
- **Формат**: Структурированные логи с контекстом
- **Хранение**: Локальные файлы + консоль

### Метрики
- **Производительность**: Время выполнения операций
- **Использование**: Количество активных пользователей
- **Ошибки**: Статистика сбоев и повторов

## 🤝 Вклад в проект

### Разработка
1. Форкните репозиторий
2. Создайте ветку для новой функции
3. Внесите изменения
4. Создайте Pull Request

### Тестирование
```bash
dotnet test
dotnet run --environment Development
```

### Сборка
```bash
dotnet build -c Release
dotnet publish -c Release -o ./publish
```

## 📞 Поддержка

- **Issues**: [GitHub Issues](https://github.com/your-repo/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-repo/discussions)
- **Telegram**: @your_support_username

## 📄 Лицензия

MIT License - см. файл [LICENSE](LICENSE) для деталей.

## 🙏 Благодарности

- Telegram Bot API
- Notion API
- .NET Community
- Всем участникам проекта

# TradingBot - Модернизированная версия

Telegram-бот для ведения учета торговых сделок с поддержкой персональной интеграции с Notion.

## 🚀 Новые возможности

### 1. **Redis кеширование (ВЫСОКИЙ ПРИОРИТЕТ)**
- **Производительность**: Значительное ускорение работы с часто запрашиваемыми данными
- **Масштабируемость**: Поддержка распределенного кеширования для кластеров
- **Fallback**: Автоматический переход на MemoryCache при недоступности Redis
- **Гибкость**: Настраиваемое время жизни кеша для разных типов данных

### 2. **Комплексное тестирование (ВЫСОКИЙ ПРИОРИТЕТ)**
- **Unit тесты**: Покрытие всех основных сервисов
- **Интеграционные тесты**: Тестирование с реальной базой данных в памяти
- **Mock объекты**: Изоляция тестов от внешних зависимостей
- **Автоматизация**: Скрипты для запуска тестов и проверки качества

### 3. **Docker инфраструктура**
- **Redis**: Кеширование и сессии
- **PostgreSQL**: Готовая замена SQLite для продакшена
- **Prometheus**: Сбор и хранение метрик
- **Grafana**: Визуализация данных и дашборды

### 4. Безопасность и масштабирование HTTP-запросов
- **Фабрика HTTP-клиентов**: Каждый пользователь получает отдельный HTTP-клиент с персональными заголовками
- **Безопасность**: Исключены гонки между потоками при работе с Notion API
- **Масштабируемость**: Поддержка тысяч одновременных пользователей

### 5. Персональные базы Notion
- **Индивидуальные настройки**: Каждый пользователь может подключить свою базу данных Notion
- **Персональные справочники**: Использование собственных опций для полей сделок
- **Автоматическая синхронизация**: Сделки автоматически сохраняются в персональную базу

### 6. Улучшенное меню настроек
- **Подключение Notion**: Простой процесс подключения через токен и Database ID
- **Проверка подключения**: Тестирование соединения с Notion API
- **Управление интеграцией**: Включение/отключение, обновление настроек

### 7. Фоновые задачи
- **Асинхронная обработка**: Тяжелые операции выполняются в фоне
- **Повторные попытки**: Автоматические повторы с экспоненциальным бэк-оффом
- **Мониторинг**: Отслеживание выполнения фоновых задач

### 8. Умное кеширование
- **Персональный кеш**: Отдельный кеш для каждого пользователя
- **Автоматическая инвалидация**: Обновление кеша при изменении настроек
- **Оптимизация производительности**: Быстрый доступ к часто используемым данным

### 9. **Полная локализация интерфейса (НОВОЕ!)**
- **Автоматическое переключение**: Все кнопки и меню меняются при смене языка
- **Поддержка языков**: Русский и английский с полным покрытием
- **Консистентность**: Единый стиль для всех элементов интерфейса
- **Тестируемость**: Автоматические тесты для проверки корректности локализации

## 🛠 Установка и настройка

### Требования
- .NET 8.0
- SQLite
- Telegram Bot Token
- Notion API Token (опционально)
- **Docker Desktop** (для Redis кеширования)

### 🚀 Быстрый старт с Redis

#### 1. Установите Docker Desktop
```bash
# Скачайте с https://www.docker.com/products/docker-desktop/
# Или используйте winget (если доступен):
winget install Docker.DockerDesktop
```

#### 2. Запустите Redis
```bash
# В корневой папке проекта:
.\start_redis.ps1
```

#### 3. Запустите TradingBot
```bash
cd TradingBot
dotnet run
```

**Redis автоматически включится** и значительно ускорит работу бота!

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

## 📊 Мониторинг и метрики

### Запуск метрик
```bash
# Простой запуск
dotnet run

# Через скрипты
.\start_metrics.ps1        # PowerShell
.\start_metrics.bat        # Windows Batch
```

### Доступ к метрикам
- **Prometheus метрики**: http://localhost:5000/metrics
- **Health Check**: http://localhost:5000/health
- **Главная страница**: http://localhost:5000/

### Проверка доступности
```bash
# PowerShell
.\check_metrics.ps1

# Ручная проверка
Invoke-WebRequest -Uri "http://localhost:5000/metrics"
```

### Собираемые метрики
- **Счетчики сообщений** (входящие, колбэки, фото, документы)
- **Счетчики сделок** (сохранение, получение, обновление)
- **Счетчики ошибок** (валидация, БД, Telegram, Notion)
- **Время выполнения операций** (запросы, ответы БД)
- **Системные метрики** (память, CPU, размер БД, активные пользователи)

📖 **Подробная документация**: [README_METRICS.md](README_METRICS.md)

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

### Кроссплатформенная публикация (.NET 8)

```bash
# Windows (PowerShell)
dotnet publish -c Release -r win-x64 --self-contained -o ./publish/win

# Linux
dotnet publish -c Release -r linux-x64 --self-contained -o ./publish/linux

# Запуск
# Windows
./publish/win/TradingBot.exe

# Linux (дать права и запустить)
chmod +x ./publish/linux/TradingBot
./publish/linux/TradingBot
```

Примечания:
- Используется относительный путь к Tesseract `tessdata`, который работает и на Windows, и на Linux.
- URL сервера настраивается через ASPNETCORE_URLS (например, `http://0.0.0.0:5000`).

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

## 🌐 Локализация интерфейса

### Автоматическое переключение языков
TradingBot теперь поддерживает **полную локализацию** всех кнопок и элементов интерфейса:

✅ **Главное меню** - все кнопки автоматически меняются  
✅ **Onboarding экраны** - навигация и действия  
✅ **Меню настроек** - все опции конфигурации  
✅ **Меню помощи** - разделы поддержки  
✅ **История сделок** - фильтры и навигация  
✅ **Формы ввода** - кнопки действий  

### Поддерживаемые языки
- **Русский** (`"ru"`) - язык по умолчанию
- **Английский** (`"en"`) - полная поддержка

### Тестирование локализации
```bash
# Запуск тестов локализации
dotnet test TradingBot.Tests --filter "UIManagerLocalizationTests"

# Результат: 8 тестов пройдено ✅
```

### Документация
Подробная информация по локализации: [LOCALIZATION.md](LOCALIZATION.md)

---

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

## 🚀 Запуск и тестирование

### Быстрый старт
```bash
# 1. Запуск бота
dotnet run

# 2. Запуск тестов
dotnet test

# 3. Запуск конкретного теста
dotnet test --filter "FullyQualifiedName~CacheServiceTests"
```

### Тестирование
```bash
# Запуск всех тестов
dotnet test

# Запуск тестов с покрытием
dotnet test --collect:"XPlat Code Coverage"

# Запуск тестов локализации
dotnet test --filter "UIManagerLocalizationTests"
```

### Мониторинг
- **Веб-интерфейс**: http://localhost:5000
- **Redis**: localhost:6379 (если включен)
- **База данных**: SQLite (trades.db)

---

## 🧹 Отчет об очистке неиспользуемых элементов

### ✅ Что было удалено
- **Неиспользуемые интерфейсы:** `IUserService`, `ITradeService`
- **Неиспользуемые сервисы:** `KeyboardService`, `ErrorHandlingService`, `BackgroundTaskService`
- **Неиспользуемые реализации:** `NotionSchemaCacheService`, `PersonalNotionTradeStorage`

### 📊 Результаты очистки
- **Удалено файлов:** 7
- **Упрощено регистраций в DI:** 4
- **Экономия места:** ~400+ строк неиспользуемого кода
- **Упрощена структура:** Да

### 🧪 Проверка после очистки
- **Сборка:** ✅ Успешно
- **Тесты:** 33/33 пройдено ✅
- **Функциональность:** 100% сохранена

**Подробный отчет:** [CLEANUP_REPORT.md](CLEANUP_REPORT.md)

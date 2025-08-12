# Руководство по развертыванию TradingBot

## Обзор модернизации

TradingBot был модернизирован для обеспечения масштабируемости на тысячи одновременных пользователей с поддержкой индивидуального подключения к Notion.

## Ключевые улучшения

### 1. Безопасность и масштабирование HTTP-запросов
- ✅ Реализована фабрика HTTP-клиентов (`NotionHttpClientFactory`)
- ✅ Каждый пользователь получает изолированный HTTP-клиент
- ✅ Устранены гонки между потоками при работе с заголовками

### 2. Персональные базы Notion
- ✅ Модель `UserSettings` расширена полями Notion
- ✅ Меню настроек включает управление Notion
- ✅ Поддержка индивидуальных токенов и баз данных
- ✅ Тестирование подключения к Notion

### 3. Архитектурные улучшения
- ✅ Разделение `UIManager` на модули (`KeyboardService`)
- ✅ Сервис фоновых задач (`BackgroundTaskService`)
- ✅ Сервис управления настройками (`UserSettingsService`)
- ✅ Кеширование схемы Notion для каждого пользователя

### 4. Производительность
- ✅ Индексы в SQLite для ускорения выборок
- ✅ Фоновые задачи для тяжелых операций
- ✅ Улучшенное кеширование

## Требования к системе

- .NET 8.0 или выше
- SQLite 3.x
- Минимум 2 ГБ RAM
- Рекомендуется: 4+ ГБ RAM для высоких нагрузок

## Конфигурация

### appsettings.json

```json
{
  "Telegram": {
    "BotToken": "YOUR_BOT_TOKEN"
  },
  "Notion": {
    "ApiToken": "YOUR_NOTION_TOKEN",
    "DatabaseId": "YOUR_DATABASE_ID"
  },
  "BackgroundTasks": {
    "MaxConcurrent": 3,
    "MaxQueueSize": 100
  },
  "Developer": {
    "UserId": "YOUR_TELEGRAM_USER_ID"
  },
  "ConnectionStrings": {
    "Default": "Data Source=trades.db"
  }
}
```

### Переменные окружения

```bash
# Telegram Bot
export TELEGRAM__BOTTOKEN="your_bot_token"

# Notion (глобальные настройки)
export NOTION__APITOKEN="your_notion_token"
export NOTION__DATABASEID="your_database_id"

# База данных
export CONNECTIONSTRINGS__DEFAULT="Data Source=trades.db"

# Фоновые задачи
export BACKGROUNDTASKS__MAXCONCURRENT="5"
export BACKGROUNDTASKS__MAXQUEUESIZE="200"
```

## Развертывание

### 1. Сборка проекта

```bash
dotnet restore
dotnet build -c Release
dotnet publish -c Release -o ./publish
```

### 2. Применение миграций

```bash
cd publish
dotnet ef database update
```

### 3. Запуск

```bash
dotnet TradingBot.dll
```

### 4. Запуск как служба (Linux)

```bash
# Создание systemd сервиса
sudo nano /etc/systemd/system/tradingbot.service

[Unit]
Description=TradingBot Telegram Bot
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /opt/tradingbot/TradingBot.dll
WorkingDirectory=/opt/tradingbot
User=tradingbot
Group=tradingbot
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=tradingbot
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target

# Активация сервиса
sudo systemctl enable tradingbot
sudo systemctl start tradingbot
```

### 5. Запуск как служба Windows

```powershell
# Создание Windows Service
sc create "TradingBot" binPath="C:\path\to\TradingBot.exe"
sc description "TradingBot" "Telegram Trading Bot"
sc start "TradingBot"
```

## Масштабирование

### Горизонтальное масштабирование

Для развертывания на нескольких серверах:

1. **База данных**: Переход на PostgreSQL или SQL Server
2. **Кеш**: Замена `IMemoryCache` на Redis
3. **Очереди**: Использование RabbitMQ или Azure Service Bus

### Конфигурация Redis

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "TradingBot"
  }
}
```

### Конфигурация PostgreSQL

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=tradingbot;Username=user;Password=pass"
  }
}
```

## Мониторинг

### Логирование

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "TradingBot.Services": "Debug"
    }
  }
}
```

### Метрики

- Количество активных пользователей
- Размер очереди фоновых задач
- Время ответа API
- Количество ошибок

## Безопасность

### Рекомендации

1. **Токены**: Храните в переменных окружения или Azure Key Vault
2. **База данных**: Используйте SSL соединения
3. **Логи**: Не логируйте чувствительные данные
4. **Сеть**: Ограничьте доступ к портам

### Проверка безопасности

```bash
# Проверка открытых портов
netstat -tulpn | grep :5000

# Проверка логов на чувствительные данные
grep -r "password\|token" /var/log/tradingbot/
```

## Резервное копирование

### База данных

```bash
# SQLite
cp trades.db trades.db.backup.$(date +%Y%m%d_%H%M%S)

# PostgreSQL
pg_dump tradingbot > backup_$(date +%Y%m%d_%H%M%S).sql
```

### Автоматизация

```bash
#!/bin/bash
# backup.sh
DATE=$(date +%Y%m%d_%H%M%S)
cp trades.db "backups/trades.db.$DATE"
find backups/ -name "*.db.*" -mtime +7 -delete
```

## Устранение неполадок

### Частые проблемы

1. **Ошибка подключения к Notion**
   - Проверьте токен API
   - Убедитесь в правах доступа к базе данных

2. **Медленная работа**
   - Проверьте индексы в базе данных
   - Увеличьте лимиты фоновых задач

3. **Ошибки памяти**
   - Уменьшите размер очереди фоновых задач
   - Проверьте утечки памяти в коде

### Логи

```bash
# Просмотр логов в реальном времени
tail -f /var/log/tradingbot/app.log

# Поиск ошибок
grep "ERROR" /var/log/tradingbot/app.log
```

## Обновления

### Процесс обновления

1. Остановите сервис
2. Создайте резервную копию
3. Разверните новую версию
4. Примените миграции
5. Запустите сервис
6. Проверьте работоспособность

### Откат

```bash
# Восстановление из резервной копии
cp trades.db.backup trades.db
dotnet ef database update --target-migration PreviousMigration
```

## Поддержка

При возникновении проблем:

1. Проверьте логи
2. Убедитесь в корректности конфигурации
3. Проверьте доступность внешних сервисов
4. Обратитесь к документации
5. Создайте issue в репозитории

## Заключение

Модернизированный TradingBot готов к масштабированию и обеспечивает:
- Безопасную работу с множественными пользователями
- Индивидуальные настройки Notion
- Высокую производительность
- Надежность и отказоустойчивость

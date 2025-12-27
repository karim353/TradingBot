# TradingBot

Минимальная сборка Telegram-бота для учёта сделок (SQLite) + простой web endpoint для метрик.

## Запуск

Требуется .NET 8.

```powershell
cd .\TradingBot
$env:Telegram__BotToken = "<YOUR_TELEGRAM_BOT_TOKEN>"
dotnet run
```

Откроется web-часть на `http://localhost:5000` (в т.ч. `/metrics`).

## Конфиг

- `Telegram__BotToken` — обязателен.
- `UseNotion=true` требует `Notion__DatabaseId` (и токен на стороне пользователя/настроек).
- OCR включается через `Tesseract__Enabled=true` и требует корректного `Tesseract__DataPath` (tessdata).



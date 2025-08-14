# 🚀 Запуск метрик в TradingBot

## 📊 Что такое метрики

В TradingBot реализована система метрик Prometheus, которая собирает:
- **Счетчики сообщений** (входящие сообщения, колбэки, фото, документы)
- **Счетчики сделок** (сохранение, получение, обновление)
- **Счетчики ошибок** (валидация, база данных, Telegram, Notion)
- **Время выполнения операций** (запросы, ответы БД)
- **Системные метрики** (память, CPU, размер БД, активные пользователи)

## 🎯 Как запустить метрики

### 1. Запуск основного приложения

```bash
cd TradingBot
dotnet run
```

Приложение запустится на **http://localhost:5000**

### 2. Доступ к метрикам

#### 📈 Prometheus метрики
```
http://localhost:5000/metrics
```

#### 🏥 Health Check
```
http://localhost:5000/health
```

#### 🏠 Главная страница
```
http://localhost:5000/
```

## 🔧 Настройка метрик

### Конфигурация в appsettings.json

```json
{
  "HealthChecks": {
    "Enabled": true,
    "IntervalSeconds": 30
  },
  "Caching": {
    "DefaultExpirationMinutes": 15,
    "MaxSize": 1000
  }
}
```

### Интервалы сбора метрик

- **Системные метрики**: каждые 30 секунд
- **Health checks**: каждые 30 секунд
- **Prometheus scraping**: каждые 10 секунд (если настроен)

## 📊 Примеры метрик

### Счетчики сообщений
```
# HELP tradingbot_messages_total Total number of messages processed
# TYPE tradingbot_messages_total counter
tradingbot_messages_total{type="text"} 15
tradingbot_messages_total{type="callback"} 8
tradingbot_messages_total{type="photo"} 3
```

### Системные метрики
```
# HELP tradingbot_memory_usage_bytes Memory usage in bytes
# TYPE tradingbot_memory_usage_bytes gauge
tradingbot_memory_usage_bytes 52428800

# HELP tradingbot_cpu_usage_percentage CPU usage percentage
# TYPE tradingbot_cpu_usage_percentage gauge
tradingbot_cpu_usage_percentage 2.5
```

### Время выполнения операций
```
# HELP tradingbot_request_duration_seconds Request duration in seconds
# TYPE tradingbot_request_duration_seconds histogram
tradingbot_request_duration_seconds_bucket{operation="save_trade",le="0.001"} 0
tradingbot_request_duration_seconds_bucket{operation="save_trade",le="0.002"} 0
tradingbot_request_duration_seconds_bucket{operation="save_trade",le="0.004"} 1
```

## 🐳 Запуск с Docker (опционально)

Если у вас установлен Docker, можно запустить полную систему мониторинга:

```bash
# Запуск Prometheus + Grafana + AlertManager
docker-compose -f docker-compose.monitoring.yml up -d
```

### Порты по умолчанию:
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000 (admin/admin123)
- **AlertManager**: http://localhost:9093

## 🔍 Отладка метрик

### Проверка доступности
```bash
# Windows PowerShell
Invoke-WebRequest -Uri "http://localhost:5000/metrics"

# Проверка портов
netstat -an | findstr :5000
```

### Логи приложения
Метрики логируются в:
- Консоль (уровень Information)
- Файлы в папке `logs/`

## 📈 Использование метрик

### 1. Мониторинг в реальном времени
Откройте http://localhost:5000/metrics в браузере для просмотра текущих метрик

### 2. Интеграция с Prometheus
Добавьте в prometheus.yml:
```yaml
scrape_configs:
  - job_name: 'tradingbot'
    static_configs:
      - targets: ['localhost:5000']
    metrics_path: '/metrics'
    scrape_interval: 10s
```

### 3. Визуализация в Grafana
Импортируйте `grafana_dashboard.json` для готовых дашбордов

## 🚨 Устранение неполадок

### Приложение не запускается
1. Проверьте логи: `dotnet run`
2. Убедитесь, что порт 5000 свободен
3. Проверьте конфигурацию в appsettings.json

### Метрики не отображаются
1. Убедитесь, что приложение запущено
2. Проверьте доступность http://localhost:5000/metrics
3. Проверьте логи на наличие ошибок

### Низкая производительность
1. Увеличьте интервалы сбора метрик
2. Проверьте настройки кэширования
3. Мониторьте использование памяти и CPU

## 🔮 Дальнейшее развитие

### Планируемые улучшения:
- [ ] Redis кэширование для метрик
- [ ] REST API для управления метриками
- [ ] Автоматические алерты
- [ ] Интеграция с внешними системами мониторинга
- [ ] Кастомные дашборды для трейдинга

---

**Примечание**: Метрики автоматически собираются при запуске приложения. Для остановки используйте Ctrl+C в терминале.

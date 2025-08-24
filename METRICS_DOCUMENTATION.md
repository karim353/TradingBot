# 📊 Документация по метрикам TradingBot

## 🎯 Обзор

TradingBot теперь включает **максимально расширенную систему метрик** для полного мониторинга производительности, торговой активности и системного здоровья.

## 🚀 Новые возможности

### ✅ **Расширенные торговые метрики**
- PnL по сделкам и тикерам
- Размеры позиций
- Риск/доходность
- Время удержания позиций
- Win Rate и серии выигрышей/проигрышей
- Максимальный проигрыш

### ✅ **Детальные метрики производительности**
- Время отклика Telegram API
- Время обработки Notion API
- Производительность Redis кэша
- Время выполнения команд
- Параллельные запросы
- Сборка мусора

### ✅ **Системные метрики**
- Время работы системы
- Использование диска
- Сетевые соединения
- Здоровье компонентов
- Версия приложения

### ✅ **Пользовательские метрики**
- Активность пользователей
- Длительность сессий
- Уведомления и их доставка
- Отмененные операции

## 📈 Доступные метрики

### 🔢 **Счетчики (Counters)**

| Метрика | Описание | Метки |
|---------|----------|-------|
| `tradingbot_messages_total` | Общее количество сообщений | `type`: text, callback, photo, document, video, audio, sticker, location |
| `tradingbot_trades_total` | Общее количество сделок | `type`: buy, sell, close, modify, cancel, partial |
| `tradingbot_errors_total` | Общее количество ошибок | `type`: validation, database, telegram, notion, redis, network, timeout, rate_limit |
| `tradingbot_cache_hits_total` | Попадания в кэш | `operation`: get, set, delete |
| `tradingbot_cache_misses_total` | Промахи кэша | `operation`: get, set, delete |
| `tradingbot_exceptions_total` | Исключения | `exception_type`, `source` |
| `tradingbot_user_activity_total` | Активность пользователей | `user_id`, `action` |
| `tradingbot_notifications_total` | Уведомления | `type`: trade_executed, price_alert, news_update, system_maintenance, error_notification |
| `tradingbot_cancelled_operations_total` | Отмененные операции | `operation_type` |
| `tradingbot_system_restarts_total` | Перезапуски системы | - |

### 📊 **Гистограммы (Histograms)**

| Метрика | Описание | Метки | Buckets |
|---------|----------|-------|---------|
| `tradingbot_request_duration_seconds` | Время выполнения запросов | `operation` | 1ms - 16s |
| `tradingbot_database_response_time_seconds` | Время ответа БД | - | 1ms - 4s |
| `tradingbot_telegram_api_latency_seconds` | Задержка Telegram API | - | 1ms - 4s |
| `tradingbot_notion_api_latency_seconds` | Задержка Notion API | - | 1ms - 4s |
| `tradingbot_redis_cache_latency_seconds` | Задержка Redis | - | 1ms - 1s |
| `tradingbot_command_execution_time_seconds` | Время выполнения команд | `command` | 1ms - 4s |
| `tradingbot_notification_delivery_time_seconds` | Время доставки уведомлений | `type` | 1ms - 1s |
| `tradingbot_queue_processing_time_seconds` | Время обработки очереди | `queue_name` | 1ms - 1s |
| `tradingbot_external_api_latency_seconds` | Задержка внешних API | `api_name` | 1ms - 4s |
| `tradingbot_trade_pnl` | Распределение PnL | `ticker`, `direction` | -10k до +10k |
| `tradingbot_position_size` | Размер позиций | `ticker` | 0.01 до 32768 |
| `tradingbot_risk_reward_ratio` | Риск/доходность | `ticker` | 0 до 10 |
| `tradingbot_position_hold_time_seconds` | Время удержания | `ticker` | 1s до 16h |
| `tradingbot_user_session_duration_seconds` | Длительность сессий | `user_id` | 1s до 16h |
| `tradingbot_gc_time_seconds` | Время сборки мусора | - | 1ms до 1s |

### 📏 **Gauge (Текущие значения)**

| Метрика | Описание | Метки |
|---------|----------|-------|
| `tradingbot_database_size_mb` | Размер БД в МБ | - |
| `tradingbot_active_users` | Активные пользователи | - |
| `tradingbot_memory_usage_bytes` | Использование памяти | - |
| `tradingbot_cpu_usage_percentage` | Использование CPU % | - |
| `tradingbot_concurrent_requests` | Параллельные запросы | - |
| `tradingbot_queue_size` | Размер очереди | `queue_name` |
| `tradingbot_network_connections` | Сетевые соединения | - |
| `tradingbot_disk_usage_bytes` | Использование диска | `path` |
| `tradingbot_component_health` | Здоровье компонентов | `component_name` |
| `tradingbot_system_uptime_seconds` | Время работы системы | - |
| `tradingbot_application_version` | Версия приложения | - |
| `tradingbot_last_update_timestamp` | Время последнего обновления | - |
| `tradingbot_win_rate_percentage` | Процент выигрышных сделок | - |
| `tradingbot_average_pnl` | Средний PnL | - |
| `tradingbot_max_drawdown` | Максимальный проигрыш | - |
| `tradingbot_consecutive_wins` | Серия выигрышей | - |
| `tradingbot_consecutive_losses` | Серия проигрышей | - |

## 🌐 Доступные эндпоинты

### **Prometheus метрики**
- **URL**: `/metrics`
- **Формат**: Prometheus text format
- **Обновление**: В реальном времени

### **Health Check**
- **URL**: `/health`
- **Формат**: JSON
- **Проверка**: Состояние системы

### **Веб-дашборд**
- **URL**: `/metrics-dashboard`
- **Функции**: Интерактивный дашборд с графиками
- **Автообновление**: Каждые 30 секунд

### **Тестовый эндпоинт**
- **URL**: `/test`
- **Функция**: Проверка работоспособности

## 🔧 Настройка и использование

### **1. Автоматический сбор метрик**

Метрики собираются автоматически через:
- `AdvancedMetricsCollector` - системные метрики каждые 30 сек
- `MetricsMiddleware` - HTTP запросы в реальном времени
- Встроенные счетчики в бизнес-логике

### **2. Интеграция с Prometheus**

```yaml
# prometheus.yml
scrape_configs:
  - job_name: 'tradingbot-app'
    static_configs:
      - targets: ['localhost:5000']
    metrics_path: '/metrics'
    scrape_interval: 5s
```

### **3. Интеграция с Grafana**

1. Добавить источник данных Prometheus
2. Импортировать готовые дашборды:
   - `grafana-dashboard-advanced.json` (14 панелей)
   - `grafana-trading-dashboard.json` (12 панелей)

## 📊 Примеры запросов PromQL

### **Производительность**
```promql
# Среднее время ответа за последние 5 минут
rate(tradingbot_request_duration_seconds_sum[5m]) / rate(tradingbot_request_duration_seconds_count[5m])

# 95-й процентиль времени ответа
histogram_quantile(0.95, rate(tradingbot_request_duration_seconds_bucket[5m]))

# Количество параллельных запросов
tradingbot_concurrent_requests
```

### **Торговые метрики**
```promql
# Общее количество сделок
sum(tradingbot_trades_total)

# Win Rate
tradingbot_win_rate_percentage

# Средний PnL
tradingbot_average_pnl

# Распределение PnL по тикерам
tradingbot_trade_pnl_bucket{ticker="AAPL"}
```

### **Системные метрики**
```promql
# Использование памяти в МБ
tradingbot_memory_usage_bytes / 1024 / 1024

# Время работы системы в часах
tradingbot_system_uptime_seconds / 3600

# Здоровье компонентов
tradingbot_component_health{component_name="database"}
```

### **Ошибки и исключения**
```promql
# Количество ошибок по типам
sum by (type) (rate(tradingbot_errors_total[5m]))

# Исключения по источникам
sum by (source) (rate(tradingbot_exceptions_total[5m]))

# Ошибки HTTP запросов
rate(tradingbot_errors_total{type="http_request"}[5m])
```

## 🚨 Алерты и мониторинг

### **Критические метрики**
- Время ответа > 5 секунд
- Использование памяти > 80%
- CPU > 90%
- Количество ошибок > 10/мин
- Здоровье компонентов = 0

### **Предупреждения**
- Время ответа > 2 секунды
- Использование памяти > 60%
- CPU > 70%
- Количество ошибок > 5/мин

## 📱 Веб-интерфейс

### **Функции дашборда**
- ✅ Реальное время обновления
- ✅ Адаптивный дизайн
- ✅ Интерактивные графики
- ✅ Статус компонентов
- ✅ Системные метрики
- ✅ Торговые метрики
- ✅ Графики производительности

### **Доступ**
- **URL**: http://localhost:5000/metrics-dashboard
- **Автообновление**: Каждые 30 секунд
- **Графики**: Chart.js с реальными данными

## 🔄 Автообновление метрик

### **Частота сбора**
- **Системные метрики**: каждые 30 секунд
- **Торговые метрики**: каждую минуту
- **Метрики производительности**: каждые 15 секунд
- **HTTP запросы**: в реальном времени

### **Источники данных**
- Системные вызовы (.NET)
- HTTP middleware
- Бизнес-логика приложения
- Внешние API (Telegram, Notion)
- База данных и кэш

## 📈 Преимущества новой системы

1. **Полная видимость** - все аспекты системы под мониторингом
2. **Реальное время** - метрики обновляются мгновенно
3. **Детализация** - метки для категоризации и анализа
4. **Автоматизация** - сбор без вмешательства разработчика
5. **Интеграция** - готовые дашборды Grafana
6. **Производительность** - минимальное влияние на систему
7. **Масштабируемость** - легко добавлять новые метрики

## 🎯 Заключение

Новая система метрик TradingBot предоставляет **максимально полный мониторинг** для:
- 🚀 **Производительности** - время ответа, использование ресурсов
- 📊 **Торговой активности** - сделки, PnL, статистика
- 🔍 **Системного здоровья** - компоненты, ошибки, исключения
- 👥 **Пользовательского опыта** - активность, сессии, уведомления

Все метрики доступны через Prometheus, Grafana и веб-интерфейс для полного контроля над системой.


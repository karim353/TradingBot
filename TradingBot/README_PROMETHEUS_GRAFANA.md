# Интеграция Prometheus и Grafana с TradingBot

## Обзор

TradingBot теперь включает в себя комплексную систему мониторинга с использованием Prometheus для сбора метрик и Grafana для визуализации данных.

## Архитектура мониторинга

```
TradingBot → Prometheus → Grafana
     ↓           ↓         ↓
  Метрики    Сбор данных  Дашборды
```

## Компоненты

### 1. PrometheusMetricsService
- **Назначение**: Сбор и экспорт метрик в формате Prometheus
- **Метрики**:
  - Счетчики сообщений и сделок
  - Время обработки запросов
  - Использование системных ресурсов
  - Размер базы данных
  - Количество ошибок

### 2. SystemMetricsCollector
- **Назначение**: Фоновый сбор системных метрик
- **Интервал**: Каждую минуту
- **Метрики**: CPU, память, активные пользователи

### 3. Prometheus
- **Назначение**: Сбор и хранение метрик
- **Порт**: 9090
- **Интервал сбора**: 10 секунд

### 4. Grafana
- **Назначение**: Визуализация метрик
- **Порт**: 3000
- **Логин**: admin/admin123

## Установка и настройка

### 1. Запуск сервисов мониторинга

```bash
# Запуск Prometheus, Grafana и Alertmanager
docker-compose -f docker-compose.monitoring.yml up -d

# Проверка статуса
docker-compose -f docker-compose.monitoring.yml ps
```

### 2. Доступ к сервисам

- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000
- **Alertmanager**: http://localhost:9093

### 3. Настройка Grafana

1. Откройте http://localhost:3000
2. Войдите с логином `admin` и паролем `admin123`
3. Добавьте источник данных Prometheus:
   - URL: `http://prometheus:9090`
   - Access: Server (default)
4. Импортируйте дашборд из `grafana_dashboard.json`

## Метрики TradingBot

### Счетчики (Counters)
- `tradingbot_messages_total` - общее количество сообщений
- `tradingbot_trades_total` - общее количество сделок
- `tradingbot_errors_total` - общее количество ошибок

### Гистограммы (Histograms)
- `tradingbot_request_duration_seconds` - время обработки запросов
- `tradingbot_database_response_time_seconds` - время отклика БД

### Gauge
- `tradingbot_database_size_mb` - размер базы данных
- `tradingbot_active_users` - активные пользователи
- `tradingbot_memory_usage_bytes` - использование памяти
- `tradingbot_cpu_usage_percentage` - использование CPU

## Алерты

### Настроенные алерты
1. **HighCPUUsage** - CPU > 80% более 5 минут
2. **HighMemoryUsage** - Память > 500MB более 5 минут
3. **HighErrorRate** - > 0.1 ошибок/сек более 2 минут
4. **SlowDatabaseQueries** - 95% запросов > 1 сек
5. **LargeDatabaseSize** - Размер БД > 100MB
6. **NoMetrics** - TradingBot недоступен

### Настройка алертов
Алерты настраиваются в файле `prometheus_rules.yml` и автоматически загружаются в Prometheus.

## Дашборд Grafana

### Панели дашборда
1. **Общая статистика** - общее количество сообщений
2. **Использование CPU** - текущее использование CPU
3. **Использование памяти** - текущее использование памяти
4. **Размер базы данных** - размер БД в MB
5. **Сообщения по типам** - распределение сообщений
6. **Сделки по типам** - распределение сделок
7. **Время отклика БД** - производительность БД
8. **Ошибки по типам** - частота ошибок
9. **Активные пользователи** - количество пользователей
10. **Время обработки запросов** - производительность API

## Расширение метрик

### Добавление новой метрики

1. **Создайте метрику в PrometheusMetricsService**:
```csharp
private readonly Counter _newMetric;

public PrometheusMetricsService()
{
    _newMetric = Metrics.CreateCounter(
        "tradingbot_new_metric_total",
        "Description of new metric");
}

public void IncrementNewMetric()
{
    _newMetric.Inc();
}
```

2. **Добавьте метод в интерфейс**:
```csharp
public interface IMetricsService
{
    void IncrementNewMetric();
}
```

3. **Используйте в коде**:
```csharp
_metricsService.IncrementNewMetric();
```

### Добавление нового алерта

1. **Создайте правило в prometheus_rules.yml**:
```yaml
- alert: NewAlert
  expr: tradingbot_new_metric_total > 100
  for: 5m
  labels:
    severity: warning
  annotations:
    summary: "New alert summary"
    description: "New alert description"
```

2. **Перезапустите Prometheus**:
```bash
docker-compose -f docker-compose.monitoring.yml restart prometheus
```

## Производительность

### Рекомендации
- **Интервал сбора метрик**: 1 минута для системных, 5 минут для health checks
- **Хранение данных**: 200 часов для Prometheus (настраивается)
- **Обновление дашбордов**: каждые 30 секунд

### Мониторинг самого мониторинга
- Проверяйте статус контейнеров: `docker ps`
- Просматривайте логи: `docker logs tradingbot-prometheus`
- Мониторьте использование ресурсов контейнерами

## Troubleshooting

### Проблемы с подключением
1. **Prometheus не может подключиться к TradingBot**:
   - Проверьте, что TradingBot запущен на порту 5000
   - Убедитесь, что метрики доступны по `/metrics`

2. **Grafana не может подключиться к Prometheus**:
   - Проверьте URL: `http://prometheus:9090` (внутри Docker сети)
   - Убедитесь, что контейнеры в одной сети

3. **Метрики не обновляются**:
   - Проверьте логи TradingBot на наличие ошибок
   - Убедитесь, что SystemMetricsCollector запущен

### Полезные команды
```bash
# Проверка метрик TradingBot
curl http://localhost:5000/metrics

# Проверка статуса Prometheus
curl http://localhost:9090/api/v1/status/targets

# Проверка правил алертов
curl http://localhost:9090/api/v1/rules
```

## Заключение

Интеграция с Prometheus и Grafana обеспечивает:
- **Полную видимость** производительности системы
- **Автоматические алерты** при проблемах
- **Исторические данные** для анализа трендов
- **Профессиональный мониторинг** production-систем

Это позволяет поддерживать высокое качество обслуживания и быстро реагировать на возникающие проблемы.

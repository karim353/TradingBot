# Мониторинг здоровья системы TradingBot

## Обзор

TradingBot теперь включает в себя комплексную систему мониторинга здоровья, которая отслеживает состояние различных компонентов системы в реальном времени.

## Компоненты мониторинга

### 1. HealthCheckService
- **Назначение**: Базовые проверки здоровья системы
- **Функции**: 
  - Проверка подключения к базе данных
  - Проверка доступности таблиц
  - Мониторинг размера базы данных
- **Тип**: Scoped сервис

### 2. HealthMonitoringService
- **Назначение**: Фоновый мониторинг и агрегация данных о здоровье
- **Функции**:
  - Автоматические проверки каждые 5 минут
  - Сбор метрик производительности
  - Логирование статуса здоровья
  - Агрегация информации о компонентах
- **Тип**: Hosted Service (фоновый сервис)

## Статусы здоровья

### SystemHealthStatus
- **Healthy**: Система работает нормально
- **Degraded**: Система работает с ограничениями
- **Unhealthy**: Критические проблемы в системе

### Компоненты
- **Database**: Состояние базы данных SQLite
- **EF Core**: Состояние Entity Framework Core

## Метрики

Система собирает следующие метрики:
- **TotalComponents**: Общее количество компонентов
- **HealthyComponents**: Количество здоровых компонентов
- **DegradedComponents**: Количество компонентов с ограничениями
- **UnhealthyComponents**: Количество нездоровых компонентов
- **DatabaseSizeMB**: Размер базы данных в МБ
- **AverageResponseTime**: Среднее время отклика компонентов

## Логирование

### Уровни логирования
- **Information**: ✅ Система здорова
- **Warning**: ⚠️ Система работает с ограничениями
- **Error**: ❌ Система нездорова

### Примеры логов
```
info: TradingBot.Services.HealthMonitoringService[0]
      ✅ Система здорова. Компонентов: 1, Здоровых: 1

warn: TradingBot.Services.HealthMonitoringService[0]
      ⚠️ Система работает с ограничениями. Предупреждения: Database: База данных превышает рекомендуемый размер

error: TradingBot.Services.HealthMonitoringService[0]
      ❌ Система нездорова. Ошибки: Database: Не удается подключиться к базе данных
```

## Конфигурация

### Интервалы проверки
- **Первичная проверка**: Через 10 секунд после запуска
- **Периодические проверки**: Каждые 5 минут
- **Проверки по требованию**: При вызове методов API

### Настройки
```csharp
// В HealthMonitoringService
private readonly TimeSpan _monitoringInterval = TimeSpan.FromMinutes(5);

// Задержка перед первым health check
_monitoringTimer?.Change(TimeSpan.FromSeconds(10), _monitoringInterval);
```

## Использование

### Автоматический мониторинг
Система автоматически запускается при старте приложения и работает в фоновом режиме.

### Программный доступ
```csharp
// Получить статус здоровья
var status = await healthService.GetSystemHealthAsync();

// Получить детальную информацию
var healthInfo = await healthService.GetDetailedHealthInfoAsync();
```

### Логи
Мониторинг автоматически записывает результаты в логи приложения с соответствующими уровнями важности.

## Преимущества

1. **Автоматизация**: Не требует ручного вмешательства
2. **Реальное время**: Актуальная информация о состоянии системы
3. **Детализация**: Подробная информация о каждом компоненте
4. **Логирование**: Полная история состояния системы
5. **Метрики**: Количественные показатели производительности
6. **Масштабируемость**: Легко добавлять новые компоненты для мониторинга

## Расширение

### Добавление нового компонента
1. Создать метод проверки здоровья
2. Добавить компонент в `GetDetailedHealthInfoAsync()`
3. Обновить логику определения общего статуса

### Пример
```csharp
private async Task<ComponentHealth> CheckNewComponentAsync()
{
    var stopwatch = Stopwatch.StartNew();
    try
    {
        // Проверка компонента
        var isHealthy = await CheckComponentHealth();
        stopwatch.Stop();
        
        return new ComponentHealth
        {
            Name = "NewComponent",
            Status = isHealthy ? SystemHealthStatus.Healthy : SystemHealthStatus.Unhealthy,
            Description = isHealthy ? "Компонент работает" : "Проблемы с компонентом",
            ResponseTime = stopwatch.Elapsed,
            LastCheck = DateTime.UtcNow
        };
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        return new ComponentHealth
        {
            Name = "NewComponent",
            Status = SystemHealthStatus.Unhealthy,
            Description = $"Ошибка: {ex.Message}",
            ResponseTime = stopwatch.Elapsed,
            LastCheck = DateTime.UtcNow
        };
    }
}
```

## Заключение

Система мониторинга здоровья TradingBot обеспечивает:
- **Надежность**: Автоматическое обнаружение проблем
- **Прозрачность**: Полная видимость состояния системы
- **Проактивность**: Раннее предупреждение о проблемах
- **Аналитика**: Исторические данные для оптимизации

Это позволяет поддерживать высокое качество обслуживания пользователей и быстро реагировать на возникающие проблемы.

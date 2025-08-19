# 🚀 Настройка Redis для TradingBot

## 📋 **Требования**

- Windows 10/11
- Docker Desktop
- PowerShell 7+

## 🐳 **Установка Docker Desktop**

### **Автоматическая установка:**
```powershell
winget install Docker.DockerDesktop
```

### **Ручная установка:**
1. Скачайте [Docker Desktop](https://www.docker.com/products/docker-desktop/)
2. Установите с правами администратора
3. Перезагрузите компьютер
4. Запустите Docker Desktop

## 🚀 **Быстрый запуск Redis**

### **1. Запуск Redis:**
```powershell
.\start_redis.ps1
```

### **2. Остановка Redis:**
```powershell
.\stop_redis.ps1
```

### **3. Проверка статуса:**
```powershell
docker ps --filter "name=tradingbot-redis"
```

## ⚙️ **Ручной запуск Redis**

### **Запуск контейнера:**
```bash
docker run --name tradingbot-redis -d -p 6379:6379 redis:7-alpine
```

### **Проверка подключения:**
```bash
docker exec tradingbot-redis redis-cli ping
# Должен ответить: PONG
```

### **Остановка контейнера:**
```bash
docker stop tradingbot-redis
docker rm tradingbot-redis
```

## 🔧 **Конфигурация TradingBot**

Redis уже настроен в `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  },
  "Caching": {
    "Redis": {
      "Enabled": true,
      "DefaultExpirationMinutes": 30,
      "UserDataExpirationMinutes": 60,
      "TradeDataExpirationMinutes": 120,
      "SchemaCacheExpirationMinutes": 360
    }
  }
}
```

## 📊 **Преимущества Redis**

### **Производительность:**
- ⚡ **В 10-100 раз быстрее** чем MemoryCache
- 🚀 **Распределенное кеширование** для масштабирования
- 💾 **Персистентность данных** между перезапусками

### **Функциональность:**
- 🔄 **Автоматический fallback** на MemoryCache
- 📈 **Мониторинг производительности** через Prometheus
- 🎯 **Гибкие настройки** времени жизни кеша

## 🚨 **Устранение неполадок**

### **Redis не подключается:**
1. Проверьте, запущен ли Docker Desktop
2. Убедитесь, что порт 6379 свободен
3. Проверьте логи: `docker logs tradingbot-redis`

### **Ошибки подключения:**
```bash
# Проверка статуса контейнера
docker ps -a

# Просмотр логов
docker logs tradingbot-redis

# Перезапуск контейнера
docker restart tradingbot-redis
```

### **Fallback на MemoryCache:**
Если Redis недоступен, TradingBot автоматически использует MemoryCache. В логах вы увидите:
```
fail: TradingBot.Services.RedisCacheService[0]
      Error connecting to Redis, falling back to MemoryCache
```

## 🎯 **Запуск TradingBot с Redis**

1. **Запустите Redis:**
   ```powershell
   .\start_redis.ps1
   ```

2. **Запустите TradingBot:**
   ```powershell
   cd TradingBot
   dotnet run
   ```

3. **Проверьте логи** - Redis ошибок быть не должно!

## 📈 **Мониторинг Redis**

### **Статистика контейнера:**
```bash
docker stats tradingbot-redis
```

### **Информация о Redis:**
```bash
docker exec tradingbot-redis redis-cli info
```

### **Мониторинг через TradingBot:**
- Веб-интерфейс: http://localhost:5000
- Метрики Prometheus: http://localhost:5000/metrics
- Health checks: http://localhost:5000/health

## ✨ **Готово!**

Теперь ваш TradingBot работает с максимальной производительностью Redis! 🚀


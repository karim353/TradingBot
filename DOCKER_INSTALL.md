# 🐳 Установка Docker Desktop для Windows

## 📋 **Системные требования**

- Windows 10/11 (64-bit)
- WSL 2 (Windows Subsystem for Linux 2)
- Hyper-V поддержка
- Минимум 4GB RAM

## 🚀 **Пошаговая установка**

### **Шаг 1: Скачивание Docker Desktop**

1. Перейдите на [https://www.docker.com/products/docker-desktop/](https://www.docker.com/products/docker-desktop/)
2. Нажмите **"Download for Windows"**
3. Скачайте файл `Docker Desktop Installer.exe`

### **Шаг 2: Установка WSL 2 (если не установлен)**

1. Откройте PowerShell от имени администратора
2. Выполните команду:
   ```powershell
   wsl --install
   ```
3. Перезагрузите компьютер
4. После перезагрузки WSL 2 автоматически завершит установку

### **Шаг 3: Установка Docker Desktop**

1. Запустите `Docker Desktop Installer.exe` от имени администратора
2. Следуйте инструкциям установщика
3. **Важно:** Убедитесь, что отмечены опции:
   - ✅ Use WSL 2 instead of Hyper-V
   - ✅ Add shortcut to desktop
   - ✅ Use WSL 2 based engine

### **Шаг 4: Запуск Docker Desktop**

1. Запустите Docker Desktop с рабочего стола
2. Дождитесь завершения инициализации (значок Docker станет зеленым)
3. Примите условия лицензии

## 🔧 **Проверка установки**

### **Проверка версии Docker:**
```powershell
docker --version
```

### **Проверка статуса Docker:**
```powershell
docker info
```

### **Тестовый запуск:**
```powershell
docker run hello-world
```

## 🚨 **Устранение неполадок**

### **Ошибка "WSL 2 installation is incomplete"**
1. Обновите WSL 2:
   ```powershell
   wsl --update
   ```
2. Перезапустите Docker Desktop

### **Ошибка "Hyper-V is not enabled"**
1. Включите Hyper-V в Windows Features
2. Или используйте WSL 2 (рекомендуется)

### **Docker не запускается**
1. Проверьте, что служба Docker Desktop запущена
2. Перезапустите Docker Desktop
3. Проверьте логи в Event Viewer

## 🎯 **После установки Docker**

1. **Запустите Redis:**
   ```powershell
   .\start_redis.ps1
   ```

2. **Запустите TradingBot:**
   ```powershell
   cd TradingBot
   dotnet run
   ```

## 📚 **Полезные команды Docker**

### **Управление контейнерами:**
```bash
# Список запущенных контейнеров
docker ps

# Список всех контейнеров
docker ps -a

# Остановка контейнера
docker stop <container_name>

# Удаление контейнера
docker rm <container_name>

# Просмотр логов
docker logs <container_name>
```

### **Управление образами:**
```bash
# Список образов
docker images

# Удаление образа
docker rmi <image_name>

# Очистка неиспользуемых ресурсов
docker system prune
```

## ✨ **Готово!**

После установки Docker Desktop вы сможете запустить Redis и получить максимальную производительность TradingBot! 🚀


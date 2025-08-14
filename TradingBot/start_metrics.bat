@echo off
echo 🚀 Запуск TradingBot с метриками...
echo.
echo 📊 Метрики будут доступны по адресам:
echo    - http://localhost:5000/metrics (Prometheus)
echo    - http://localhost:5000/health (Health Check)
echo    - http://localhost:5000/ (Главная страница)
echo.
echo ⏳ Запуск приложения...
echo.
dotnet run
echo.
echo ✅ Приложение остановлено
pause

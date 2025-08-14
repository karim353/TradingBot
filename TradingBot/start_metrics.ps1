# 🚀 Запуск TradingBot с метриками
Write-Host "🚀 Запуск TradingBot с метриками..." -ForegroundColor Green
Write-Host ""

Write-Host "📊 Метрики будут доступны по адресам:" -ForegroundColor Cyan
Write-Host "   - http://localhost:5000/metrics (Prometheus)" -ForegroundColor Yellow
Write-Host "   - http://localhost:5000/health (Health Check)" -ForegroundColor Yellow
Write-Host "   - http://localhost:5000/ (Главная страница)" -ForegroundColor Yellow
Write-Host ""

Write-Host "⏳ Запуск приложения..." -ForegroundColor Green
Write-Host ""

try {
    dotnet run
}
catch {
    Write-Host "❌ Ошибка при запуске приложения: $_" -ForegroundColor Red
}
finally {
    Write-Host ""
    Write-Host "✅ Приложение остановлено" -ForegroundColor Green
}

Read-Host "Нажмите Enter для выхода"

# 🔍 Проверка доступности метрик TradingBot
Write-Host "🔍 Проверка доступности метрик TradingBot..." -ForegroundColor Green
Write-Host ""

$baseUrl = "http://localhost:5000"
$endpoints = @(
    @{ Path = "/"; Name = "Главная страница" },
    @{ Path = "/health"; Name = "Health Check" },
    @{ Path = "/metrics"; Name = "Prometheus метрики" }
)

foreach ($endpoint in $endpoints) {
    $url = "$baseUrl$($endpoint.Path)"
    Write-Host "🔗 Проверяю $($endpoint.Name): $url" -ForegroundColor Cyan
    
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ $($endpoint.Name) доступен (HTTP $($response.StatusCode))" -ForegroundColor Green
            
            if ($endpoint.Path -eq "/metrics") {
                $metricsCount = ($response.Content -split "`n" | Where-Object { $_ -match "^[^#]" -and $_ -notmatch "^\s*$" }).Count
                Write-Host "   📊 Найдено метрик: $metricsCount" -ForegroundColor Yellow
            }
        } else {
            Write-Host "⚠️ $($endpoint.Name) вернул код: $($response.StatusCode)" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "❌ $($endpoint.Name) недоступен: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
}

Write-Host "🔍 Проверка завершена" -ForegroundColor Green
Write-Host ""
Write-Host "💡 Для запуска приложения используйте: dotnet run" -ForegroundColor Yellow
Write-Host "💡 Или запустите: .\start_metrics.ps1" -ForegroundColor Yellow

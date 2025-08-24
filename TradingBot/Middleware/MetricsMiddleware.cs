using TradingBot.Services.Interfaces;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;

namespace TradingBot.Middleware;

/// <summary>
/// Middleware для сбора метрик HTTP-запросов
/// </summary>
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMetricsService _metricsService;
    private readonly ILogger<MetricsMiddleware> _logger;

    public MetricsMiddleware(
        RequestDelegate next,
        IMetricsService metricsService,
        ILogger<MetricsMiddleware> logger)
    {
        _next = next;
        _metricsService = metricsService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;

        try
        {
            // Записываем начало запроса
            _logger.LogDebug("📥 HTTP запрос начат: {Method} {Path}", 
                context.Request.Method, context.Request.Path);

            // Увеличиваем счетчик параллельных запросов
            var concurrentRequests = GetConcurrentRequests();
            _metricsService.RecordConcurrentRequests(concurrentRequests);

            // Записываем активность пользователя (если есть)
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = GetUserIdFromContext(context);
                if (userId.HasValue)
                {
                    _metricsService.RecordUserActivity(userId.Value, $"http_{context.Request.Method.ToLower()}");
                }
            }

            // Выполняем следующий middleware
            await _next(context);

            // Записываем успешное завершение
            stopwatch.Stop();
            var duration = stopwatch.Elapsed;
            var operation = GetOperationName(context.Request);

            _metricsService.RecordRequestDuration(operation, duration);

            // Записываем метрики успешного запроса
            _logger.LogDebug("✅ HTTP запрос завершен: {Method} {Path} за {Duration}ms", 
                context.Request.Method, context.Request.Path, duration.TotalMilliseconds);

            // Записываем время доставки уведомлений (если это уведомление)
            if (IsNotificationRequest(context.Request))
            {
                var notificationType = GetNotificationType(context.Request);
                _metricsService.RecordNotificationDeliveryTime(notificationType, duration);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed;
            var operation = GetOperationName(context.Request);

            // Записываем метрики ошибки
            _metricsService.RecordRequestDuration(operation, duration);
            _metricsService.IncrementErrorCounter("http_request");
            _metricsService.RecordExceptionCount(ex.GetType().Name, "http_middleware");

            _logger.LogError(ex, "❌ HTTP запрос завершился с ошибкой: {Method} {Path} за {Duration}ms", 
                context.Request.Method, context.Request.Path, duration.TotalMilliseconds);

            // Перебрасываем исключение дальше
            throw;
        }
        finally
        {
            // Обновляем счетчик параллельных запросов
            var finalConcurrentRequests = GetConcurrentRequests() - 1;
            if (finalConcurrentRequests >= 0)
            {
                _metricsService.RecordConcurrentRequests(finalConcurrentRequests);
            }

            // Записываем время сессии пользователя (если есть)
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = GetUserIdFromContext(context);
                if (userId.HasValue)
                {
                    var sessionDuration = DateTime.UtcNow - startTime;
                    _metricsService.RecordUserSessionDuration(userId.Value, sessionDuration);
                }
            }
        }
    }

    /// <summary>
    /// Получение имени операции для метрик
    /// </summary>
    private string GetOperationName(HttpRequest request)
    {
        var path = request.Path.Value?.ToLower() ?? "";
        var method = request.Method.ToLower();

        // Определяем тип операции на основе пути и метода
        if (path.Contains("/metrics"))
            return "get_metrics";
        if (path.Contains("/health"))
            return "health_check";
        if (path.Contains("/trades"))
            return method == "get" ? "get_trades" : "modify_trades";
        if (path.Contains("/users"))
            return "user_management";
        if (path.Contains("/notifications"))
            return "notification_service";
        if (path.Contains("/api/"))
            return "api_request";
        if (path.Contains("/webhook"))
            return "webhook_processing";

        return $"{method}_{path.Replace("/", "_").TrimStart('_')}";
    }

    /// <summary>
    /// Получение ID пользователя из контекста
    /// </summary>
    private long? GetUserIdFromContext(HttpContext context)
    {
        try
        {
            // Попытка получить ID пользователя из различных источников
            if (context.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader))
            {
                if (long.TryParse(userIdHeader, out var userId))
                    return userId;
            }

            // Из query параметров
            if (context.Request.Query.TryGetValue("userId", out var userIdQuery))
            {
                if (long.TryParse(userIdQuery, out var userId))
                    return userId;
            }

            // Из claims пользователя
            var userIdClaim = context.User?.FindFirst("userId")?.Value;
            if (long.TryParse(userIdClaim, out var userIdFromClaim))
                return userIdFromClaim;

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Проверка, является ли запрос уведомлением
    /// </summary>
    private bool IsNotificationRequest(HttpRequest request)
    {
        var path = request.Path.Value?.ToLower() ?? "";
        return path.Contains("/notification") || 
               path.Contains("/webhook") || 
               path.Contains("/callback");
    }

    /// <summary>
    /// Получение типа уведомления
    /// </summary>
    private string GetNotificationType(HttpRequest request)
    {
        var path = request.Path.Value?.ToLower() ?? "";
        
        if (path.Contains("/telegram"))
            return "telegram_webhook";
        if (path.Contains("/notion"))
            return "notion_webhook";
        if (path.Contains("/price_alert"))
            return "price_alert";
        if (path.Contains("/trade_executed"))
            return "trade_executed";
        if (path.Contains("/system"))
            return "system_maintenance";
        
        return "other";
    }

    /// <summary>
    /// Получение количества параллельных запросов
    /// </summary>
    private int GetConcurrentRequests()
    {
        try
        {
            // Простая оценка на основе количества активных потоков
            return Process.GetCurrentProcess().Threads.Count;
        }
        catch
        {
            return 0;
        }
    }
}

/// <summary>
/// Расширения для регистрации middleware
/// </summary>
public static class MetricsMiddlewareExtensions
{
    public static IApplicationBuilder UseMetricsMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MetricsMiddleware>();
    }
}

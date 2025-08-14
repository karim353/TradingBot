using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace TradingBot.Services;

public class RateLimitingService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingService> _logger;
    private readonly TimeSpan _window = TimeSpan.FromMinutes(1);
    private readonly int _maxRequestsPerMinute = 20;

    public RateLimitingService(IMemoryCache cache, ILogger<RateLimitingService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public bool IsRateLimited(long userId, string action = "default")
    {
        var key = $"rate_limit:{userId}:{action}";
        
        if (_cache.TryGetValue(key, out RateLimitInfo? info) && info != null)
        {
            if (DateTime.UtcNow - info.WindowStart > _window)
            {
                // Сброс окна
                info = new RateLimitInfo
                {
                    Count = 1,
                    WindowStart = DateTime.UtcNow
                };
                _cache.Set(key, info, _window);
                return false;
            }
            
            if (info.Count >= _maxRequestsPerMinute)
            {
                _logger.LogWarning("Пользователь {UserId} превысил лимит запросов для действия {Action}", userId, action);
                return true;
            }
            
            info.Count++;
            _cache.Set(key, info, _window);
            return false;
        }
        
        // Первый запрос
        var newInfo = new RateLimitInfo
        {
            Count = 1,
            WindowStart = DateTime.UtcNow
        };
        _cache.Set(key, newInfo, _window);
        return false;
    }

    public TimeSpan GetTimeUntilReset(long userId, string action = "default")
    {
        var key = $"rate_limit:{userId}:{action}";
        
        if (_cache.TryGetValue(key, out RateLimitInfo? info) && info != null)
        {
            var timePassed = DateTime.UtcNow - info.WindowStart;
            return _window - timePassed;
        }
        
        return TimeSpan.Zero;
    }

    public int GetRemainingRequests(long userId, string action = "default")
    {
        var key = $"rate_limit:{userId}:{action}";
        
        if (_cache.TryGetValue(key, out RateLimitInfo? info) && info != null)
        {
            if (DateTime.UtcNow - info.WindowStart > _window)
            {
                return _maxRequestsPerMinute;
            }
            
            return Math.Max(0, _maxRequestsPerMinute - info.Count);
        }
        
        return _maxRequestsPerMinute;
    }

    private class RateLimitInfo
    {
        public int Count { get; set; }
        public DateTime WindowStart { get; set; }
    }
}

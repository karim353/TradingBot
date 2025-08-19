using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using TradingBot.Models;
using TradingBot.Validators;
using FluentValidation.Results;

namespace TradingBot.Services
{
    /// <summary>
    /// Сервис валидации данных
    /// </summary>
    public class ValidationService
    {
        private readonly ILogger<ValidationService> _logger;
        private readonly TradeValidator _tradeValidator;

        public ValidationService(ILogger<ValidationService> logger)
        {
            _logger = logger;
            _tradeValidator = new TradeValidator();
        }

        public async Task<ValidationResult> ValidateTradeAsync(Trade trade)
        {
            try
            {
                var result = await _tradeValidator.ValidateAsync(trade);
                
                if (!result.IsValid)
                {
                    _logger.LogWarning("Валидация сделки не прошла: {Errors}", 
                        string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при валидации сделки");
                throw;
            }
        }

        public bool IsValidTicker(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker))
                return false;
                
            return ticker.Length <= 10 && 
                   ticker.All(c => char.IsLetterOrDigit(c) && char.IsUpper(c));
        }

        public bool IsValidPnL(decimal pnl)
        {
            return pnl >= -1000000 && pnl <= 1000000;
        }

        public bool IsValidRisk(decimal risk)
        {
            return risk >= 0 && risk <= 100;
        }

        public bool IsValidDate(DateTime date)
        {
            return date <= DateTime.UtcNow.AddDays(1);
        }
    }

    /// <summary>
    /// Сервис ограничения скорости запросов
    /// </summary>
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
}

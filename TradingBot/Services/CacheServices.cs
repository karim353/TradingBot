using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TradingBot.Services
{
    /// <summary>
    /// Интерфейс для сервиса кэширования
    /// </summary>
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task<long> IncrementAsync(string key, long value = 1);
        Task<Dictionary<string, string>> GetHashAsync(string key);
        Task SetHashAsync(string key, Dictionary<string, string> values);
    }

    /// <summary>
    /// Интерфейс для Redis сервиса кэширования
    /// </summary>
    public interface IRedisCacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task<long> IncrementAsync(string key, long value = 1);
        Task<Dictionary<string, string>> GetHashAsync(string key);
        Task SetHashAsync(string key, Dictionary<string, string> values);
    }

    /// <summary>
    /// Сервис кэширования в памяти
    /// </summary>
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public Task<T?> GetAsync<T>(string key)
        {
            try
            {
                if (_cache.TryGetValue(key, out var value))
                {
                    _logger.LogDebug("Memory cache hit for key: {Key}", key);
                    if (value is string jsonValue)
                    {
                        return Task.FromResult(JsonSerializer.Deserialize<T>(jsonValue, _jsonOptions));
                    }
                    return Task.FromResult((T?)value);
                }

                _logger.LogDebug("Memory cache miss for key: {Key}", key);
                return Task.FromResult<T?>(default);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting value from memory cache for key: {Key}", key);
                return Task.FromResult<T?>(default);
            }
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var options = new MemoryCacheEntryOptions();
                if (expiration.HasValue)
                {
                    options.AbsoluteExpirationRelativeToNow = expiration;
                }

                // Всегда сериализуем в JSON для консистентности
                var jsonValue = JsonSerializer.Serialize(value, _jsonOptions);
                _cache.Set(key, jsonValue, options);

                _logger.LogDebug("Value cached in memory for key: {Key}, expiration: {Expiration}", key, expiration);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting value in memory cache for key: {Key}", key);
                return Task.CompletedTask;
            }
        }

        public Task RemoveAsync(string key)
        {
            try
            {
                _cache.Remove(key);
                _logger.LogDebug("Memory cache entry removed for key: {Key}", key);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing memory cache entry for key: {Key}", key);
                return Task.CompletedTask;
            }
        }

        public Task<bool> ExistsAsync(string key)
        {
            try
            {
                return Task.FromResult(_cache.TryGetValue(key, out _));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking memory cache existence for key: {Key}", key);
                return Task.FromResult(false);
            }
        }

        public async Task<long> IncrementAsync(string key, long value = 1)
        {
            try
            {
                var currentValue = await GetAsync<long?>(key) ?? 0;
                var newValue = currentValue + value;
                await SetAsync(key, newValue);
                return newValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing memory cache value for key: {Key}", key);
                return 0;
            }
        }

        public async Task<Dictionary<string, string>> GetHashAsync(string key)
        {
            try
            {
                var value = await GetAsync<Dictionary<string, string>>(key);
                return value ?? new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hash from memory cache for key: {Key}", key);
                return new Dictionary<string, string>();
            }
        }

        public Task SetHashAsync(string key, Dictionary<string, string> values)
        {
            return SetAsync(key, values);
        }
    }

    /// <summary>
    /// Сервис кэширования в Redis
    /// </summary>
    public class RedisCacheService : IRedisCacheService, ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var value = await _cache.GetStringAsync(key);
                if (string.IsNullOrEmpty(value))
                {
                    _logger.LogDebug("Cache miss for key: {Key}", key);
                    return default;
                }

                _logger.LogDebug("Cache hit for key: {Key}", key);
                return JsonSerializer.Deserialize<T>(value, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting value from cache for key: {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var jsonValue = JsonSerializer.Serialize(value, _jsonOptions);
                var options = new DistributedCacheEntryOptions();
                
                if (expiration.HasValue)
                {
                    options.SetAbsoluteExpiration(expiration.Value);
                }

                await _cache.SetStringAsync(key, jsonValue, options);
                _logger.LogDebug("Value cached for key: {Key}, expiration: {Expiration}", key, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting value in cache for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
                _logger.LogDebug("Cache entry removed for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache entry for key: {Key}", key);
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                var value = await _cache.GetStringAsync(key);
                return !string.IsNullOrEmpty(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
                return false;
            }
        }

        public async Task<long> IncrementAsync(string key, long value = 1)
        {
            try
            {
                var currentValue = await GetAsync<long?>(key) ?? 0;
                var newValue = currentValue + value;
                await SetAsync(key, newValue);
                return newValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing cache value for key: {Key}", key);
                return 0;
            }
        }

        public async Task<Dictionary<string, string>> GetHashAsync(string key)
        {
            try
            {
                var value = await GetAsync<Dictionary<string, string>>(key);
                return value ?? new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hash from cache for key: {Key}", key);
                return new Dictionary<string, string>();
            }
        }

        public async Task SetHashAsync(string key, Dictionary<string, string> values)
        {
            try
            {
                await SetAsync(key, values);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting hash in cache for key: {Key}", key);
            }
        }
    }
}

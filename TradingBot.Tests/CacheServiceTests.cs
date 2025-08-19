using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TradingBot.Services;
using Xunit;

namespace TradingBot.Tests
{
    public class CacheServiceTests : TestBase
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<CacheServiceTests> _logger;

        public CacheServiceTests()
        {
            _cacheService = GetService<ICacheService>();
            _logger = LoggerFactory.CreateLogger<CacheServiceTests>();
        }

        [Fact]
        public async Task SetAsync_ShouldStoreValue()
        {
            // Arrange
            var key = "test_key";
            var value = "test_value";

            // Act
            await _cacheService.SetAsync(key, value);

            // Assert
            var result = await _cacheService.GetAsync<string>(key);
            result.Should().Be(value);
        }

        [Fact]
        public async Task GetAsync_WithNonExistentKey_ShouldReturnDefault()
        {
            // Arrange
            var key = "non_existent_key";

            // Act
            var result = await _cacheService.GetAsync<string>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task RemoveAsync_ShouldRemoveValue()
        {
            // Arrange
            var key = "test_key";
            var value = "test_value";
            await _cacheService.SetAsync(key, value);

            // Act
            await _cacheService.RemoveAsync(key);

            // Assert
            var result = await _cacheService.GetAsync<string>(key);
            result.Should().BeNull();
        }

        [Fact]
        public async Task ExistsAsync_WithExistingKey_ShouldReturnTrue()
        {
            // Arrange
            var key = "test_key";
            var value = "test_value";
            await _cacheService.SetAsync(key, value);

            // Act
            var exists = await _cacheService.ExistsAsync(key);

            // Assert
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistentKey_ShouldReturnFalse()
        {
            // Arrange
            var key = "non_existent_key";

            // Act
            var exists = await _cacheService.ExistsAsync(key);

            // Assert
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task IncrementAsync_ShouldIncrementValue()
        {
            // Arrange
            var key = "counter_key";

            // Act
            var result1 = await _cacheService.IncrementAsync(key, 5);
            var result2 = await _cacheService.IncrementAsync(key, 3);

            // Assert
            result1.Should().Be(5);
            result2.Should().Be(8);
        }

        [Fact]
        public async Task SetAsync_WithExpiration_ShouldExpire()
        {
            // Arrange
            var key = "expiring_key";
            var value = "expiring_value";
            var expiration = TimeSpan.FromMilliseconds(100);

            // Act
            await _cacheService.SetAsync(key, value, expiration);

            // Assert - value should exist initially
            var initialResult = await _cacheService.GetAsync<string>(key);
            initialResult.Should().Be(value);

            // Wait for expiration
            await Task.Delay(200);

            // Assert - value should be expired
            var expiredResult = await _cacheService.GetAsync<string>(key);
            expiredResult.Should().BeNull();
        }

        [Fact]
        public async Task GetHashAsync_ShouldReturnHash()
        {
            // Arrange
            var key = "hash_key";
            var hash = new Dictionary<string, string>
            {
                {"key1", "value1"},
                {"key2", "value2"}
            };

            // Act
            await _cacheService.SetHashAsync(key, hash);
            var result = await _cacheService.GetHashAsync(key);

            // Assert
            result.Should().BeEquivalentTo(hash);
        }

        [Fact]
        public async Task ComplexObject_ShouldBeSerializedCorrectly()
        {
            // Arrange
            var key = "complex_key";
            var complexObject = new TestComplexObject
            {
                Id = 1,
                Name = "Test",
                Values = new[] { 1, 2, 3 }
            };

            // Act
            await _cacheService.SetAsync(key, complexObject);
            var result = await _cacheService.GetAsync<TestComplexObject>(key);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(complexObject.Id);
            result.Name.Should().Be(complexObject.Name);
            result.Values.Should().BeEquivalentTo(complexObject.Values);
        }

        private class TestComplexObject
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int[] Values { get; set; } = Array.Empty<int>();
        }
    }
}


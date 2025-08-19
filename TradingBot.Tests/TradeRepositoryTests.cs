using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TradingBot.Models;
using TradingBot.Services;
using Xunit;

namespace TradingBot.Tests
{
    public class TradeRepositoryTests : TestBase
    {
        private readonly TradeRepository _tradeRepository;

        public TradeRepositoryTests()
        {
            _tradeRepository = GetService<TradeRepository>();
        }

        [Fact]
        public async Task AddTradeAsync_ShouldSaveTrade()
        {
            // Arrange
            var trade = new Trade
            {
                UserId = 12345,
                Ticker = "AAPL",
                Account = "Test Account",
                Session = "Morning",
                Position = "Long",
                Direction = "Buy",
                Context = new List<string> { "Test Context" },
                Setup = new List<string> { "Test Setup" },
                Result = "Win",
                RR = "2:1",
                Risk = 1.0m,
                PnL = 100.0m,
                Emotions = new List<string> { "Confident" },
                EntryDetails = "Test Entry",
                Note = "Test Note",
                Date = DateTime.UtcNow
            };

            // Act
            await _tradeRepository.AddTradeAsync(trade);

            // Assert
            trade.Id.Should().BeGreaterThan(0);
            trade.Ticker.Should().Be("AAPL");
            trade.UserId.Should().Be(12345);

            // Verify it's actually saved in database
            var dbTrade = await DbContext.Trades.FirstOrDefaultAsync(t => t.Id == trade.Id);
            dbTrade.Should().NotBeNull();
            dbTrade!.Ticker.Should().Be("AAPL");
        }

        [Fact]
        public async Task GetTradesAsync_ShouldReturnUserTrades()
        {
            // Arrange
            var userId = 12345;
            await CreateTestTradeAsync(userId);
            await CreateTestTradeAsync(userId);
            await CreateTestTradeAsync(99999); // Different user

            // Act
            var trades = await _tradeRepository.GetTradesAsync(userId);

            // Assert
            trades.Should().HaveCount(2);
            trades.Should().OnlyContain(t => t.UserId == userId);
        }

        [Fact]
        public async Task GetTradesInDateRangeAsync_ShouldFilterByDate()
        {
            // Arrange
            var userId = 12345;
            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            var trade1 = await CreateTestTradeAsync(userId);
            trade1.Date = today;
            DbContext.SaveChanges();

            var trade2 = await CreateTestTradeAsync(userId);
            trade2.Date = yesterday;
            DbContext.SaveChanges();

            // Act
            var trades = await _tradeRepository.GetTradesInDateRangeAsync(userId, yesterday, today);

            // Assert
            trades.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetTradesAsync_WithNonExistentUser_ShouldReturnEmpty()
        {
            // Arrange
            var nonExistentUserId = 99999L;

            // Act
            var trades = await _tradeRepository.GetTradesAsync(nonExistentUserId);

            // Assert
            trades.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateTradeAsync_ShouldUpdateTrade()
        {
            // Arrange
            var trade = await CreateTestTradeAsync();
            var originalPnL = trade.PnL;
            trade.PnL = 200.0m;
            trade.Note = "Updated note";

            // Act
            await _tradeRepository.UpdateTradeAsync(trade);

            // Assert
            trade.PnL.Should().Be(200.0m);
            trade.Note.Should().Be("Updated note");

            // Verify database is updated
            var dbTrade = await DbContext.Trades.FirstOrDefaultAsync(t => t.Id == trade.Id);
            dbTrade.Should().NotBeNull();
            dbTrade!.PnL.Should().Be(200.0m);
            dbTrade.Note.Should().Be("Updated note");
        }

        [Fact]
        public async Task DeleteTradeAsync_ShouldDeleteTrade()
        {
            // Arrange
            var trade = await CreateTestTradeAsync();
            var tradeId = trade.Id;

            // Act
            await _tradeRepository.DeleteTradeAsync(trade);

            // Assert
            var dbTrade = await DbContext.Trades.FirstOrDefaultAsync(t => t.Id == tradeId);
            dbTrade.Should().BeNull();
        }

        [Fact]
        public async Task GetTradeByIdAsync_ShouldReturnTrade()
        {
            // Arrange
            var trade = await CreateTestTradeAsync();
            var tradeId = trade.Id;
            var userId = trade.UserId;

            // Act
            var foundTrade = await _tradeRepository.GetTradeByIdAsync(userId, tradeId);

            // Assert
            foundTrade.Should().NotBeNull();
            foundTrade!.Id.Should().Be(tradeId);
            foundTrade.Ticker.Should().Be("AAPL");
        }

        [Fact]
        public async Task GetTradeByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var nonExistentId = 99999;
            var userId = 12345L;

            // Act
            var trade = await _tradeRepository.GetTradeByIdAsync(userId, nonExistentId);

            // Assert
            trade.Should().BeNull();
        }

        [Fact]
        public async Task GetAllUserIdsAsync_ShouldReturnAllUserIds()
        {
            // Arrange
            await CreateTestTradeAsync(12345);
            await CreateTestTradeAsync(12345);
            await CreateTestTradeAsync(99999);

            // Act
            var userIds = await _tradeRepository.GetAllUserIdsAsync();

            // Assert
            userIds.Should().HaveCount(2);
            userIds.Should().Contain(12345);
            userIds.Should().Contain(99999);
        }

        [Fact]
        public async Task GetLastTradeAsync_ShouldReturnMostRecentTrade()
        {
            // Arrange
            var userId = 12345;
            var trade1 = await CreateTestTradeAsync(userId);
            trade1.Date = DateTime.UtcNow.AddDays(-1);
            DbContext.SaveChanges();

            var trade2 = await CreateTestTradeAsync(userId);
            trade2.Date = DateTime.UtcNow;
            DbContext.SaveChanges();

            // Act
            var lastTrade = await _tradeRepository.GetLastTradeAsync(userId);

            // Assert
            lastTrade.Should().NotBeNull();
            lastTrade!.Id.Should().Be(trade2.Id);
        }
    }
}

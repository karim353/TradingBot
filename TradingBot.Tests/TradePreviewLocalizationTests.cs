using Xunit;
using TradingBot.Services;
using TradingBot.Models;
using System.Linq;
using System.Globalization;

namespace TradingBot.Tests
{
    public class TradePreviewLocalizationTests
    {
        private readonly UIManager _uiManager;

        public TradePreviewLocalizationTests()
        {
            _uiManager = new UIManager();
        }

        [Fact]
        public void GetTradeInputScreen_ShouldReturnLocalizedPreview_Russian()
        {
            // Arrange
            var trade = new Trade
            {
                Ticker = "BTC/USDT",
                Account = "Binance",
                Session = "ASIA",
                Position = "Scalp",
                Direction = "Long",
                Context = new List<string> { "Uptrend" },
                Setup = new List<string> { "Breakout" },
                Result = "TP",
                RR = "1:2",
                Risk = 1.5m,
                PnL = 2.5m,
                Emotions = new List<string> { "Calm" },
                EntryDetails = "Market",
                Note = "Good setup"
            };
            var settings = new UserSettings { Language = "ru" };

            // Act
            var (text, keyboard) = _uiManager.GetTradeInputScreen(trade, 1, settings, "test_id");

            // Assert
            Assert.NotNull(text);
            Assert.Contains("Тикер: BTC/USDT", text);
            Assert.Contains("Аккаунт: Binance", text);
            Assert.Contains("Сессия: ASIA", text);
            Assert.Contains("Позиция: Scalp", text);
            Assert.Contains("Направление: Long", text);
            Assert.Contains("Контекст: Uptrend", text);
            Assert.Contains("Сетап: Breakout", text);
            Assert.Contains("Результат: TP", text);
            Assert.Contains("R:R = 1:2", text);
            Assert.Contains("Риск: 1,5%", text);
            Assert.Contains("Прибыль: 2,5%", text);
            Assert.Contains("Эмоции: Calm", text);
            Assert.Contains("Детали входа: Market", text);
            Assert.Contains("Заметка: Good setup", text);
        }

        [Fact]
        public void GetTradeInputScreen_ShouldReturnLocalizedPreview_English()
        {
            // Arrange
            var trade = new Trade
            {
                Ticker = "ETH/USDT",
                Account = "Bybit",
                Session = "LONDON",
                Position = "Intraday",
                Direction = "Short",
                Context = new List<string> { "Downtrend" },
                Setup = new List<string> { "Reversal" },
                Result = "SL",
                RR = "1:3",
                Risk = 2.0m,
                PnL = -1.5m,
                Emotions = new List<string> { "Fear" },
                EntryDetails = "Limit",
                Note = "Risk management"
            };
            var settings = new UserSettings { Language = "en" };

            // Act
            var (text, keyboard) = _uiManager.GetTradeInputScreen(trade, 1, settings, "test_id");

            // Assert
            Assert.NotNull(text);
            Assert.Contains("Ticker: ETH/USDT", text);
            Assert.Contains("Account: Bybit", text);
            Assert.Contains("Session: LONDON", text);
            Assert.Contains("Position: Intraday", text);
            Assert.Contains("Direction: Short", text);
            Assert.Contains("Context: Downtrend", text);
            Assert.Contains("Setup: Reversal", text);
            Assert.Contains("Result: SL", text);
            Assert.Contains("R:R = 1:3", text);
            Assert.Contains("Risk: 2%", text);
            Assert.Contains("Profit: -1,5%", text);
            Assert.Contains("Emotions: Fear", text);
            Assert.Contains("Entry Details: Limit", text);
            Assert.Contains("Note: Risk management", text);
        }

        [Fact]
        public void GetTradeConfirmationScreen_ShouldReturnLocalizedPreview_Russian()
        {
            // Arrange
            var trade = new Trade
            {
                Ticker = "SOL/USDT",
                Account = "MEXC",
                Session = "NEW YORK",
                Position = "Swing",
                Direction = "Long",
                Context = new List<string> { "Range" },
                Setup = new List<string> { "Continuation" },
                Result = "BE",
                RR = "1:1",
                Risk = 0.5m,
                PnL = 0.0m,
                Emotions = new List<string> { "Focused" },
                EntryDetails = "Stop",
                Note = "Conservative approach"
            };
            var settings = new UserSettings { Language = "ru" };

            // Act
            var (text, keyboard) = _uiManager.GetTradeConfirmationScreen(trade, "test_id", settings);

            // Assert
            Assert.NotNull(text);
            Assert.Contains("Тикер: SOL/USDT", text);
            Assert.Contains("Аккаунт: MEXC", text);
            Assert.Contains("Сессия: NEW YORK", text);
            Assert.Contains("Позиция: Swing", text);
            Assert.Contains("Направление: Long", text);
            Assert.Contains("Контекст: Range", text);
            Assert.Contains("Сетап: Continuation", text);
            Assert.Contains("Результат: BE", text);
            Assert.Contains("R:R = 1:1", text);
            Assert.Contains("Риск: 0,5%", text);
            Assert.Contains("Прибыль: 0%", text);
            Assert.Contains("Эмоции: Focused", text);
            Assert.Contains("Детали входа: Stop", text);
            Assert.Contains("Заметка: Conservative approach", text);
        }

        [Fact]
        public void GetTradeConfirmationScreen_ShouldReturnLocalizedPreview_English()
        {
            // Arrange
            var trade = new Trade
            {
                Ticker = "BNB/USDT",
                Account = "Demo",
                Session = "FRANKFURT",
                Position = "Position",
                Direction = "Short",
                Context = new List<string> { "Uptrend" },
                Setup = new List<string> { "Head & Shoulders" },
                Result = "TP",
                RR = "1:4",
                Risk = 3.0m,
                PnL = 8.0m,
                Emotions = new List<string> { "FOMO" },
                EntryDetails = "Market",
                Note = "Aggressive trade"
            };
            var settings = new UserSettings { Language = "en" };

            // Act
            var (text, keyboard) = _uiManager.GetTradeConfirmationScreen(trade, "test_id", settings);

            // Assert
            Assert.NotNull(text);
            Assert.Contains("Ticker: BNB/USDT", text);
            Assert.Contains("Account: Demo", text);
            Assert.Contains("Session: FRANKFURT", text);
            Assert.Contains("Position: Position", text);
            Assert.Contains("Direction: Short", text);
            Assert.Contains("Context: Uptrend", text);
            Assert.Contains("Setup: Head & Shoulders", text);
            Assert.Contains("Result: TP", text);
            Assert.Contains("R:R = 1:4", text);
            Assert.Contains("Risk: 3%", text);
            Assert.Contains("Profit: 8%", text);
            Assert.Contains("Emotions: FOMO", text);
            Assert.Contains("Entry Details: Market", text);
            Assert.Contains("Note: Aggressive trade", text);
        }

        [Fact]
        public void GetEditFieldMenu_ShouldReturnLocalizedPreview_Russian()
        {
            // Arrange
            var trade = new Trade
            {
                Ticker = "XAU/USD",
                Account = "BingX",
                Session = "ASIA",
                Position = "Scalp",
                Direction = "Long",
                Context = new List<string> { "Uptrend" },
                Setup = new List<string> { "Breakout" },
                Result = "TP",
                RR = "1:2",
                Risk = 1.0m,
                PnL = 3.0m,
                Emotions = new List<string> { "Calm" },
                EntryDetails = "Market",
                Note = "Gold setup"
            };
            var settings = new UserSettings { Language = "ru" };

            // Act
            var (text, keyboard) = _uiManager.GetEditFieldMenu(trade, "test_id", settings);

            // Assert
            Assert.NotNull(text);
            Assert.Contains("Тикер: XAU/USD", text);
            Assert.Contains("Аккаунт: BingX", text);
            Assert.Contains("Сессия: ASIA", text);
            Assert.Contains("Позиция: Scalp", text);
            Assert.Contains("Направление: Long", text);
            Assert.Contains("Контекст: Uptrend", text);
            Assert.Contains("Сетап: Breakout", text);
            Assert.Contains("Результат: TP", text);
            Assert.Contains("R:R = 1:2", text);
            Assert.Contains("Риск: 1%", text);
            Assert.Contains("Прибыль: 3%", text);
            Assert.Contains("Эмоции: Calm", text);
            Assert.Contains("Детали входа: Market", text);
            Assert.Contains("Заметка: Gold setup", text);
        }

        [Fact]
        public void GetEditFieldMenu_ShouldReturnLocalizedPreview_English()
        {
            // Arrange
            var trade = new Trade
            {
                Ticker = "EUR/USD",
                Account = "Binance",
                Session = "LONDON",
                Position = "Intraday",
                Direction = "Short",
                Context = new List<string> { "Downtrend" },
                Setup = new List<string> { "Reversal" },
                Result = "SL",
                RR = "1:1",
                Risk = 0.5m,
                PnL = -0.5m,
                Emotions = new List<string> { "Fear" },
                EntryDetails = "Limit",
                Note = "Forex trade"
            };
            var settings = new UserSettings { Language = "en" };

            // Act
            var (text, keyboard) = _uiManager.GetEditFieldMenu(trade, "test_id", settings);

            // Assert
            Assert.NotNull(text);
            Assert.Contains("Ticker: EUR/USD", text);
            Assert.Contains("Account: Binance", text);
            Assert.Contains("Session: LONDON", text);
            Assert.Contains("Position: Intraday", text);
            Assert.Contains("Direction: Short", text);
            Assert.Contains("Context: Downtrend", text);
            Assert.Contains("Setup: Reversal", text);
            Assert.Contains("Result: SL", text);
            Assert.Contains("R:R = 1:1", text);
            Assert.Contains("Risk: 0,5%", text);
            Assert.Contains("Profit: -0,5%", text);
            Assert.Contains("Emotions: Fear", text);
            Assert.Contains("Entry Details: Limit", text);
            Assert.Contains("Note: Forex trade", text);
        }
    }
}

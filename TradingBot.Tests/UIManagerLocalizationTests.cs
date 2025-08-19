using Xunit;
using TradingBot.Services;
using TradingBot.Models;
using System.Linq;

namespace TradingBot.Tests
{
    public class UIManagerLocalizationTests
    {
        private readonly UIManager _uiManager;

        public UIManagerLocalizationTests()
        {
            _uiManager = new UIManager();
        }

        [Fact]
        public void GetMainMenu_ShouldReturnLocalizedButtons_Russian()
        {
            // Arrange
            var settings = new UserSettings { Language = "ru" };

            // Act
            var keyboard = _uiManager.GetMainMenu(settings);

            // Assert
            Assert.NotNull(keyboard);
            Assert.NotNull(keyboard.InlineKeyboard);
            Assert.True(keyboard.InlineKeyboard.Any());

            // Проверяем, что кнопки на русском языке
            var allButtons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();
            Assert.Contains(allButtons, btn => btn.Text.Contains("Добавить сделку"));
            Assert.Contains(allButtons, btn => btn.Text.Contains("Моя статистика"));
            Assert.Contains(allButtons, btn => btn.Text.Contains("История сделок"));
            Assert.Contains(allButtons, btn => btn.Text.Contains("Настройки"));
            Assert.Contains(allButtons, btn => btn.Text.Contains("Помощь"));
        }

        [Fact]
        public void GetMainMenu_ShouldReturnLocalizedButtons_English()
        {
            // Arrange
            var settings = new UserSettings { Language = "en" };

            // Act
            var keyboard = _uiManager.GetMainMenu(settings);

            // Assert
            Assert.NotNull(keyboard);
            Assert.NotNull(keyboard.InlineKeyboard);
            Assert.True(keyboard.InlineKeyboard.Any());

            // Проверяем, что кнопки на английском языке
            var allButtons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();
            Assert.Contains(allButtons, btn => btn.Text.Contains("Add Trade"));
            Assert.Contains(allButtons, btn => btn.Text.Contains("My Statistics"));
            Assert.Contains(allButtons, btn => btn.Text.Contains("Trade History"));
            Assert.Contains(allButtons, btn => btn.Text.Contains("Settings"));
            Assert.Contains(allButtons, btn => btn.Text.Contains("Help"));
        }

        [Fact]
        public void GetOnboardingScreen_ShouldReturnLocalizedButtons_Russian()
        {
            // Arrange
            var language = "ru";

            // Act & Assert для каждого шага
            for (int step = 1; step <= 3; step++)
            {
                var (text, keyboard) = _uiManager.GetOnboardingScreen(step, language);
                
                Assert.NotNull(keyboard);
                Assert.NotNull(keyboard.InlineKeyboard);
                
                var allButtons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();
                
                if (step < 3)
                {
                    Assert.Contains(allButtons, btn => btn.Text.Contains("Далее"));
                }
                else
                {
                    Assert.Contains(allButtons, btn => btn.Text.Contains("Начать"));
                }
                
                if (step > 1)
                {
                    Assert.Contains(allButtons, btn => btn.Text.Contains("Назад"));
                }
                
                Assert.Contains(allButtons, btn => btn.Text.Contains("Пропустить обучение"));
            }
        }

        [Fact]
        public void GetOnboardingScreen_ShouldReturnLocalizedButtons_English()
        {
            // Arrange
            var language = "en";

            // Act & Assert для каждого шага
            for (int step = 1; step <= 3; step++)
            {
                var (text, keyboard) = _uiManager.GetOnboardingScreen(step, language);
                
                Assert.NotNull(keyboard);
                Assert.NotNull(keyboard.InlineKeyboard);
                
                var allButtons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();
                
                if (step < 3)
                {
                    Assert.Contains(allButtons, btn => btn.Text.Contains("Next"));
                }
                else
                {
                    Assert.Contains(allButtons, btn => btn.Text.Contains("Start"));
                }
                
                if (step > 1)
                {
                    Assert.Contains(allButtons, btn => btn.Text.Contains("Back"));
                }
                
                Assert.Contains(allButtons, btn => btn.Text.Contains("Skip Tutorial"));
            }
        }

        [Fact]
        public void GetSettingsMenu_ShouldReturnLocalizedButtons_Russian()
        {
            // Arrange
            var settings = new UserSettings { Language = "ru" };

            // Act
            var (text, keyboard) = _uiManager.GetSettingsMenu(settings);

            // Assert
            Assert.NotNull(keyboard);
            Assert.NotNull(keyboard.InlineKeyboard);
            
            var allButtons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();
            Assert.Contains(allButtons, btn => btn.Text.Contains("Язык"));
            Assert.Contains(allButtons, btn => btn.Text.Contains("Уведомления"));
            Assert.Contains(allButtons, btn => btn.Text.Contains("Избранные тикеры"));
            Assert.Contains(allButtons, btn => btn.Text.Contains("Интеграция с Notion"));
        }

        [Fact]
        public void GetSettingsMenu_ShouldReturnLocalizedButtons_English()
        {
            // Arrange
            var settings = new UserSettings { Language = "en" };

            // Act
            var (text, keyboard) = _uiManager.GetSettingsMenu(settings);

            // Assert
            Assert.NotNull(keyboard);
            Assert.NotNull(keyboard.InlineKeyboard);
            
            var allButtons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();
            Assert.Contains(allButtons, btn => btn.Text.Contains("Language"));
            Assert.Contains(allButtons, btn => btn.Text.Contains("Notifications"));
            Assert.Contains(allButtons, btn => btn.Text.Contains("Favorite Tickers"));
            Assert.Contains(allButtons, btn => btn.Text.Contains("Notion Integration"));
        }

        [Fact]
        public void GetHelpMenu_ShouldReturnLocalizedButtons_Russian()
        {
            // Arrange
            var settings = new UserSettings { Language = "ru" };

            // Act
            var (text, keyboard) = _uiManager.GetHelpMenu(settings);

            // Assert
            Assert.NotNull(keyboard);
            Assert.NotNull(keyboard.InlineKeyboard);
            
            var allButtons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();
            Assert.Contains(allButtons, btn => btn.Text.Contains("Техподдержка"));
            Assert.Contains(allButtons, btn => btn.Text.Contains("Что нового"));
        }

        [Fact]
        public void GetHelpMenu_ShouldReturnLocalizedButtons_English()
        {
            // Arrange
            var settings = new UserSettings { Language = "en" };

            // Act
            var (text, keyboard) = _uiManager.GetHelpMenu(settings);

            // Assert
            Assert.NotNull(keyboard);
            Assert.NotNull(keyboard.InlineKeyboard);
            
            var allButtons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();
            Assert.Contains(allButtons, btn => btn.Text.Contains("Support"));
            Assert.Contains(allButtons, btn => btn.Text.Contains("What's new"));
        }
    }
}

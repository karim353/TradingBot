// UserSettings.cs
using System.Collections.Generic;

namespace TradingBot.Models
{
    public class UserSettings
    {
        public string Language { get; set; }
        public bool NotificationsEnabled { get; set; }
        public List<string> FavoriteTickers { get; set; }
        public List<string> RecentTickers { get; set; }
        public List<string> RecentDirections { get; set; }
        public List<string> RecentComments { get; set; }

        // Новые списки предпочтений
        public List<string> RecentAccounts { get; set; }
        public List<string> RecentSessions { get; set; }
        public List<string> RecentPositions { get; set; }
        public List<string> RecentResults { get; set; }
        public List<string> RecentSetups { get; set; }
        public List<string> RecentContexts { get; set; }
        public List<string> RecentEmotions { get; set; }

        // Notion personal DB settings (used by PersonalNotionService)
        public bool NotionEnabled { get; set; }
        public string? NotionDatabaseId { get; set; }
        public string? NotionIntegrationToken { get; set; }

        // Ежедневная сводка
        public bool DailySummaryEnabled { get; set; } = false;
        public string DailySummaryTime { get; set; } = "21:00"; // локальное время, HH:mm

        public UserSettings()
        {
            Language = "ru";
            NotificationsEnabled = true;
            FavoriteTickers = new List<string>();
            RecentTickers = new List<string>();
            RecentDirections = new List<string>();
            RecentComments = new List<string>();
            RecentAccounts = new List<string>();
            RecentSessions = new List<string>();
            RecentPositions = new List<string>();
            RecentResults = new List<string>();
            RecentSetups = new List<string>();
            RecentContexts = new List<string>();
            RecentEmotions = new List<string>();
            NotionEnabled = false;
            NotionDatabaseId = null;
            NotionIntegrationToken = null;
            DailySummaryEnabled = false;
            DailySummaryTime = "21:00";
        }
    }
}
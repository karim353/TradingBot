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
        }
    }
}
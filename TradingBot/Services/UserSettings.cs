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

        public UserSettings()
        {
            Language = "ru";
            NotificationsEnabled = true;
            FavoriteTickers = new List<string>();
            RecentTickers = new List<string>();
            RecentDirections = new List<string>();
            RecentComments = new List<string>();
        }
    }
}
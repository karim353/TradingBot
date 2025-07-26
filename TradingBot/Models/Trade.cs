using System;
using System.ComponentModel.DataAnnotations;

namespace TradingBot.Models
{
    public class Trade
    {
        [Key]
        public int Id { get; set; }
        public long ChatId { get; set; }
        public DateTime Date { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public decimal PnL { get; set; }
        public decimal? Entry { get; set; } // Close Price from PnLData
        public decimal? OpenPrice { get; set; } // Avg. Open Price from PnLData
        public decimal? SL { get; set; }
        public decimal? TP { get; set; }
        public decimal? Volume { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string NotionPageId { get; set; } = string.Empty;
    }
}
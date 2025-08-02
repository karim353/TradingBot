namespace TradingBot.Models
{
    public class Trade
    {
        public int Id { get; set; }
        public long UserId { get; set; }
        public DateTime Date { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public decimal PnL { get; set; }
        public decimal? OpenPrice { get; set; }
        public decimal? Entry { get; set; }
        public decimal? SL { get; set; }
        public decimal? TP { get; set; }
        public decimal? Volume { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string Strategy { get; set; } = string.Empty;
        public string Emotion { get; set; } = string.Empty;
        public string Session { get; set; } = string.Empty;
        public string? ScreenshotPath { get; set; }
        public string? NotionPageId { get; set; }
        public decimal? Exit { get; set; }
        public string? DepositPercent { get; set; } // % Депозита (НУЖНО ДОБАВИТЬ)
        public string? Error { get; set; }    

        public decimal? Profit { get; set; }                      // PnL в валюте, например в долларах
        public List<string> Mistakes { get; set; } = new List<string>();  // Список ошибок
        
        public decimal? PercentDeposit { get; set; } // Если нужен процент

    }
}
namespace TradingBot.Models
{
    /// <summary>
    /// Результат распознавания PnL из изображения (OCR).
    /// </summary>
    public class PnLData
    {
        public string Ticker { get; set; }
        public string Direction { get; set; }
        public decimal? Leverage { get; set; }
        public decimal? PnLPercent { get; set; }
        public decimal? Close { get; set; }
        public decimal? Open { get; set; }
        public string UserName { get; set; }
        public string ReferralCode { get; set; }
        public DateTime? TradeDate { get; set; }
    }

}
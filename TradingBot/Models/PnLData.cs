namespace TradingBot.Models
{
    /// <summary>
    /// Результат распознавания PnL из изображения (OCR).
    /// </summary>
    public class PnLData
    {
        public required string Ticker { get; set; }
        public required string Direction { get; set; }
        public decimal? Leverage { get; set; }
        public decimal? PnLPercent { get; set; }
        public decimal? Close { get; set; }
        public decimal? Open { get; set; }
        public required string UserName { get; set; }
        public required string ReferralCode { get; set; }
        public DateTime? TradeDate { get; set; }

        // 🔴 Недостающие свойства добавлены:
        public decimal? SL { get; set; }
        public decimal? TP { get; set; }
        public decimal? Volume { get; set; }
        public string? Comment { get; set; }
    }
}
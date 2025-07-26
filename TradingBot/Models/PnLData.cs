namespace TradingBot.Models
{
    /// <summary>
    /// Результат распознавания PnL из изображения (OCR).
    /// </summary>
    public class PnLData
    {
        public string Ticker { get; set; } = string.Empty;    // Распознанный тикер
        public string Direction { get; set; } = string.Empty; // Распознанное направление (Long/Short)
        public decimal PnL { get; set; }                      // Распознанное значение прибыли/убытка
    }
}
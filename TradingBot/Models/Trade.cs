using System;
using System.ComponentModel.DataAnnotations;

namespace TradingBot.Models
{
    /// <summary>
    /// Модель Trade представляет торговую сделку.
    /// </summary>
    public class Trade
    {
        [Key]
        public int Id { get; set; }          // Первичный ключ (Id сделки)

        public long ChatId { get; set; }     // Идентификатор пользователя/чата Telegram
        public DateTime Date { get; set; }   // Дата и время сделки

        public string Ticker { get; set; } = string.Empty;   // Тикер инструмента
        public string Direction { get; set; } = string.Empty; // Направление сделки: "Long" или "Short"
        public decimal PnL { get; set; }      // Финансовый результат сделки

        public decimal? Entry { get; set; }   // Цена входа (может быть null)
        public decimal? SL { get; set; }      // Стоп-лосс (может быть null)
        public decimal? TP { get; set; }      // Тейк-профит (может быть null)
        public decimal? Volume { get; set; }  // Объём сделки (может быть null)
        public string Comment { get; set; } = string.Empty;   // Комментарий к сделке
        public string NotionPageId { get; set; } = string.Empty; // Идентификатор страницы в Notion
    }
}
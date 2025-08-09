using System;
using System.Collections.Generic;

namespace TradingBot.Models
{
    /// <summary>
    /// Финальная модель сделки под новую таблицу Notion.
    /// Multi-select как List<string>, select как string, числа как decimal/decimal?.
    /// </summary>
    public class Trade
    {
        public int Id { get; set; }
        public long UserId { get; set; }

        /// <summary>Идентификатор страницы в Notion (если есть синхронизация)</summary>
        public string? NotionPageId { get; set; }

        /// <summary>Дата сделки</summary>
        public DateTime Date { get; set; } = DateTime.UtcNow;

        /// <summary>Pair (Title) — тикер/пара, например BTC/USDT</summary>
        public string? Ticker { get; set; }

        /// <summary>Select</summary>
        public string? Account { get; set; }

        /// <summary>Select</summary>
        public string? Session { get; set; }

        /// <summary>Select (тип позиции, например LONG/SHORT)</summary>
        public string? Position { get; set; }

        /// <summary>Select (направление, например LONG/SHORT)</summary>
        public string? Direction { get; set; }

        /// <summary>Multi-select</summary>
        public List<string>? Context { get; set; } = new();

        /// <summary>Multi-select</summary>
        public List<string>? Setup { get; set; } = new();

        /// <summary>Select</summary>
        public string? Result { get; set; }

        /// <summary>R:R</summary>
        public decimal? RR { get; set; }

        /// <summary>Риск (%)</summary>
        public decimal? Risk { get; set; }

        /// <summary>Текстовые детали входа</summary>
        public string? EntryDetails { get; set; }

        /// <summary>Текстовая заметка</summary>
        public string? Note { get; set; }

        /// <summary>Multi-select</summary>
        public List<string>? Emotions { get; set; } = new();

        /// <summary>% Profit</summary>
        public decimal PnL { get; set; }
    }
}
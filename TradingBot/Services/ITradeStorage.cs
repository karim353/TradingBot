using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TradingBot.Models;

namespace TradingBot.Services
{
    /// <summary>
    /// Абстракция хранилища сделок.
    /// Реализации: SQLiteTradeStorage, NotionTradeStorage и т.п.
    /// </summary>
    public interface ITradeStorage
    {
        /// <summary>
        /// Добавить сделку в хранилище (и связанный внешний источник, например Notion).
        /// </summary>
        Task AddTradeAsync(Trade trade);

        /// <summary>
        /// Обновить существующую сделку (локально и во внешнем источнике, если применимо).
        /// </summary>
        Task UpdateTradeAsync(Trade trade);

        /// <summary>
        /// Удалить сделку (архивировать во внешнем источнике, если применимо, и удалить локально).
        /// </summary>
        Task DeleteTradeAsync(Trade trade);

        /// <summary>
        /// Вернуть последнюю (по дате) сделку пользователя.
        /// </summary>
        Task<Trade?> GetLastTradeAsync(long userId);

        /// <summary>
        /// Вернуть все сделки пользователя из локального хранилища.
        /// Для быстрых операций статистики и истории.
        /// </summary>
        Task<List<Trade>> GetTradesAsync(long userId);

        /// <summary>
        /// Вернуть сделки пользователя в заданном диапазоне дат (включительно).
        /// </summary>
        Task<List<Trade>> GetTradesInDateRangeAsync(long userId, DateTime from, DateTime to);

        /// <summary>
        /// Получить список опций для поля select/multi-select из схемы базы (например, Notion).
        /// Примеры propertyName: "Account", "Session", "Position", "Direction", "Context", "Setup", "Result", "Emotions".
        /// Возвращает пустой список, если поле не найдено или опций нет.
        /// </summary>
        Task<List<string>> GetSelectOptionsAsync(string propertyName, Trade? current = null);

        /// <summary>
        /// Умные подсказки для пользователя с учётом истории и контекста текущей сделки.
        /// </summary>
        Task<List<string>> GetSuggestedOptionsAsync(string propertyName, long userId, Trade? current = null, int topN = 12);
    }
}
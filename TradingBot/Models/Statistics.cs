namespace TradingBot.Models
{
    /// <summary>
    /// Модель для отображения статистики с цветовым кодированием
    /// </summary>
    public class Statistics
    {
        /// <summary>
        /// Прибыль
        /// </summary>
        public double Profit { get; set; }
        
        /// <summary>
        /// Убыток
        /// </summary>
        public double Loss { get; set; }
        
        /// <summary>
        /// Количество сделок
        /// </summary>
        public int TradeCount { get; set; }
        
        /// <summary>
        /// Win Rate в процентах
        /// </summary>
        public double WinRate { get; set; }
        
        /// <summary>
        /// Средний PnL
        /// </summary>
        public double AveragePnL { get; set; }
        
        /// <summary>
        /// Лучший результат
        /// </summary>
        public double BestResult { get; set; }
        
        /// <summary>
        /// Худший результат
        /// </summary>
        public double WorstResult { get; set; }
        
        /// <summary>
        /// Создает новый экземпляр Statistics
        /// </summary>
        public Statistics() { }
        
        /// <summary>
        /// Создает новый экземпляр Statistics с параметрами
        /// </summary>
        public Statistics(double profit, double loss, int tradeCount, double winRate = 0, double averagePnL = 0, double bestResult = 0, double worstResult = 0)
        {
            Profit = profit;
            Loss = loss;
            TradeCount = tradeCount;
            WinRate = winRate;
            AveragePnL = averagePnL;
            BestResult = bestResult;
            WorstResult = worstResult;
        }
        
        /// <summary>
        /// Создает Statistics из списка сделок
        /// </summary>
        public static Statistics FromTrades(List<Trade> trades)
        {
            if (trades == null || !trades.Any())
                return new Statistics();
            
            var profitableTrades = trades.Where(t => t.PnL > 0).ToList();
            var losingTrades = trades.Where(t => t.PnL < 0).ToList();
            
            var profit = profitableTrades.Sum(t => (double)t.PnL);
            var loss = Math.Abs(losingTrades.Sum(t => (double)t.PnL));
            var tradeCount = trades.Count;
            var winRate = tradeCount > 0 ? (double)profitableTrades.Count / tradeCount * 100 : 0;
            var averagePnL = tradeCount > 0 ? trades.Average(t => (double)t.PnL) : 0;
            var bestResult = trades.Max(t => (double)t.PnL);
            var worstResult = trades.Min(t => (double)t.PnL);
            
            return new Statistics(profit, loss, tradeCount, winRate, averagePnL, bestResult, worstResult);
        }
    }
}

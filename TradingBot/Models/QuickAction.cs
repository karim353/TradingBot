namespace TradingBot.Models
{
    /// <summary>
    /// Модель для быстрых действий в меню
    /// </summary>
    public class QuickAction
    {
        /// <summary>
        /// Текст кнопки
        /// </summary>
        public string Text { get; set; } = string.Empty;
        
        /// <summary>
        /// Callback данные для кнопки
        /// </summary>
        public string Callback { get; set; } = string.Empty;
        
        /// <summary>
        /// Дополнительная информация (например, количество уведомлений)
        /// </summary>
        public string Badge { get; set; } = string.Empty;
        
        /// <summary>
        /// Иконка для кнопки
        /// </summary>
        public string Icon { get; set; } = string.Empty;
        
        /// <summary>
        /// Приоритет отображения (меньше = выше)
        /// </summary>
        public int Priority { get; set; } = 0;
        
        /// <summary>
        /// Активна ли кнопка
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Создает новый экземпляр QuickAction
        /// </summary>
        public QuickAction() { }
        
        /// <summary>
        /// Создает новый экземпляр QuickAction с параметрами
        /// </summary>
        public QuickAction(string text, string callback, string badge = "", string icon = "", int priority = 0)
        {
            Text = text;
            Callback = callback;
            Badge = badge;
            Icon = icon;
            Priority = priority;
        }
        
        /// <summary>
        /// Получает полный текст кнопки с иконкой и бейджем
        /// </summary>
        public string GetFullText()
        {
            var result = Icon;
            if (!string.IsNullOrEmpty(Icon) && !string.IsNullOrEmpty(Text))
                result += " ";
            result += Text;
            if (!string.IsNullOrEmpty(Badge))
                result += $" {Badge}";
            return result;
        }
    }
}

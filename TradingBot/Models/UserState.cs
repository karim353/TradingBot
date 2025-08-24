namespace TradingBot.Models;

public class UserState
{
    public int Step { get; set; }
    public Trade? Trade { get; set; }           // nullable: создаём по мере ввода
    public string? Action { get; set; }         // nullable: может отсутствовать
    public int MessageId { get; set; }
    public string Language { get; set; } = "ru";
    public string? TradeId { get; set; }        // nullable: создаём по мере ввода
    public DateTime LastInputTime { get; set; } = DateTime.UtcNow;
    public int ErrorCount { get; set; } = 0;
    public bool IsProcessing { get; set; } = false; // индикатор занятости для UX
}

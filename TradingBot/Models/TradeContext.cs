using Microsoft.EntityFrameworkCore;

namespace TradingBot.Models
{
    /// <summary>
    /// Контекст базы данных для хранения трейдов (SQLite через EF Core).
    /// </summary>
    public class TradeContext : DbContext
    {
        public TradeContext(DbContextOptions<TradeContext> options) : base(options) { }

        public DbSet<Trade> Trades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Первичный ключ для Trade задаётся автоматически (Id). Добавим индекс по полю ChatId для ускорения запросов по пользователям.
            modelBuilder.Entity<Trade>().HasKey(t => t.Id);
            modelBuilder.Entity<Trade>().HasIndex(t => t.ChatId);
        }
    }
}
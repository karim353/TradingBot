using Microsoft.EntityFrameworkCore;

namespace TradingBot.Models
{
    public class TradeContext : DbContext
    {
        public TradeContext(DbContextOptions<TradeContext> options) : base(options) { }

        public DbSet<Trade> Trades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var d = modelBuilder.Entity<Trade>();
            d.Property(p => p.PnL)       .HasColumnType("REAL");
            d.Property(p => p.Entry)     .HasColumnType("REAL");
            d.Property(p => p.OpenPrice) .HasColumnType("REAL");
            d.Property(p => p.SL)        .HasColumnType("REAL");
            d.Property(p => p.TP)        .HasColumnType("REAL");
            d.Property(p => p.Volume)    .HasColumnType("REAL");
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Trade>().HasKey(t => t.Id);
            modelBuilder.Entity<Trade>().HasIndex(t => t.UserId);
        }
    }
}

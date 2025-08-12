using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TradingBot.Models
{
    public class TradeContext : DbContext
    {
        // ВАЖНО: статическое поле, чтобы не захватывать локальные переменные в expression tree
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

        public TradeContext(DbContextOptions<TradeContext> options) : base(options) { }

        public DbSet<Trade> Trades { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Конвертер List<string>? <-> string? с поддержкой legacy-значений (не-JSON)
            var listConverter = new ValueConverter<List<string>?, string?>(
                v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
                v => ParseListFromDb(v) // вызов статического метода допустим в expression tree
            );

            // ValueComparer через статические методы (никаких блочных лямбд)
            var listComparer = new ValueComparer<List<string>?>(
                (a, b) => ListEquals(a, b),
                v => ListHashCode(v),
                v => ListSnapshot(v)
            );

            var e = modelBuilder.Entity<Trade>();
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.UserId);

            // Числа → REAL для SQLite
            e.Property(t => t.PnL).HasColumnType("REAL");
            e.Property(t => t.Risk).HasColumnType("REAL");

            // Multi-select (JSON) + сравнение коллекций
            e.Property(t => t.Context).HasConversion(listConverter);
            e.Property(t => t.Context).Metadata.SetValueComparer(listComparer);

            e.Property(t => t.Setup).HasConversion(listConverter);
            e.Property(t => t.Setup).Metadata.SetValueComparer(listComparer);

            e.Property(t => t.Emotions).HasConversion(listConverter);
            e.Property(t => t.Emotions).Metadata.SetValueComparer(listComparer);

            // Дата — помечаем как UTC при чтении
            e.Property(t => t.Date).HasConversion(
                v => v,
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );

            base.OnModelCreating(modelBuilder);
        }

        // ===== Статические хелперы (можно вызывать из expression trees) =====

        private static List<string> ParseListFromDb(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
            var s = raw.Trim();

            // Попытка распарсить как JSON-массив
            if (s.StartsWith("[") || s.StartsWith("{"))
            {
                try
                {
                    var fromJson = JsonSerializer.Deserialize<List<string>>(s, JsonOptions);
                    return fromJson ?? new List<string>();
                }
                catch
                {
                    // не JSON — падаем в разбор как простой список
                }
            }

            // Не JSON: разделители ',', ';', '|'
            var parts = s.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(x => x.Trim())
                         .Where(x => x.Length > 0)
                         .ToList();
            return parts;
        }

        private static bool ListEquals(List<string>? a, List<string>? b)
        {
            if (ReferenceEquals(a, b)) return true;
            var aa = a ?? new List<string>();
            var bb = b ?? new List<string>();
            if (aa.Count != bb.Count) return false;
            for (int i = 0; i < aa.Count; i++)
            {
                if (!string.Equals(aa[i], bb[i], StringComparison.Ordinal)) return false;
            }
            return true;
        }

        private static int ListHashCode(List<string>? v)
        {
            unchecked
            {
                int hash = 17;
                if (v != null)
                {
                    for (int i = 0; i < v.Count; i++)
                        hash = hash * 23 + (v[i]?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }

        private static List<string>? ListSnapshot(List<string>? v)
            => v == null ? null : new List<string>(v);
    }
}

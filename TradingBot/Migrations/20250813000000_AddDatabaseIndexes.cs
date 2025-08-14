using Microsoft.EntityFrameworkCore.Migrations;

namespace TradingBot.Migrations
{
    /// <summary>
    /// Миграция для добавления индексов в таблицы SQLite
    /// Ускоряет выборки по UserId и Date для масштабируемости
    /// </summary>
    public partial class AddDatabaseIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Индекс по UserId для быстрого поиска сделок пользователя
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_Trades_UserId ON Trades(UserId);");
            
            // Индекс по Date для быстрого поиска сделок по дате
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_Trades_Date ON Trades(Date);");
            
            // Составной индекс по UserId и Date для статистики
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_Trades_UserId_Date ON Trades(UserId, Date);");
            
            // Индекс по UserId для настроек пользователя
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_UserSettings_UserId ON UserSettings(UserId);");
            
            // Индекс по BotId для изоляции ботов
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_UserSettings_BotId ON UserSettings(BotId);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_UserStates_BotId ON UserStates(BotId);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_PendingTrades_BotId ON PendingTrades(BotId);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Удаляем созданные индексы
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Trades_UserId;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Trades_Date;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Trades_UserId_Date;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_UserSettings_UserId;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_UserSettings_BotId;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_UserStates_BotId;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_PendingTrades_BotId;");
        }
    }
}

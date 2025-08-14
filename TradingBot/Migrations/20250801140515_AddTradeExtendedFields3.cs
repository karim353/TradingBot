using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Migrations
{
    /// <inheritdoc />
    public partial class AddTradeExtendedFields3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NotionPageId",
                table: "Trades",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "Emotion",
                table: "Trades",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ScreenshotPath",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Session",
                table: "Trades",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Strategy",
                table: "Trades",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Emotion",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "ScreenshotPath",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "Session",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "Strategy",
                table: "Trades");

            migrationBuilder.AlterColumn<string>(
                name: "NotionPageId",
                table: "Trades",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}

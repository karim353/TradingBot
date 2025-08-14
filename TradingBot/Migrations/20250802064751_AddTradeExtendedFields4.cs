using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Migrations
{
    /// <inheritdoc />
    public partial class AddTradeExtendedFields4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DepositPercent",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Exit",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mistakes",
                table: "Trades",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<decimal>(
                name: "Profit",
                table: "Trades",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepositPercent",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "Error",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "Exit",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "Mistakes",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "Profit",
                table: "Trades");
        }
    }
}

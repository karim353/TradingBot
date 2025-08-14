using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Migrations
{
    public partial class AddNewFieldsToTrade : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns to the Trades table
            migrationBuilder.AddColumn<string>(
                name: "Account",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Session",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Context",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Setup",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Emotions",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntryDetails",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Result",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RR",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Risk",
                table: "Trades",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PnL",
                table: "Trades",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "NotionPageId",
                table: "Trades",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Account", table: "Trades");
            migrationBuilder.DropColumn(name: "Session", table: "Trades");
            migrationBuilder.DropColumn(name: "Position", table: "Trades");
            migrationBuilder.DropColumn(name: "Direction", table: "Trades");
            migrationBuilder.DropColumn(name: "Context", table: "Trades");
            migrationBuilder.DropColumn(name: "Setup", table: "Trades");
            migrationBuilder.DropColumn(name: "Emotions", table: "Trades");
            migrationBuilder.DropColumn(name: "EntryDetails", table: "Trades");
            migrationBuilder.DropColumn(name: "Note", table: "Trades");
            migrationBuilder.DropColumn(name: "Result", table: "Trades");
            migrationBuilder.DropColumn(name: "RR", table: "Trades");
            migrationBuilder.DropColumn(name: "Risk", table: "Trades");
            migrationBuilder.DropColumn(name: "PnL", table: "Trades");
            migrationBuilder.DropColumn(name: "NotionPageId", table: "Trades");
        }
    }
}

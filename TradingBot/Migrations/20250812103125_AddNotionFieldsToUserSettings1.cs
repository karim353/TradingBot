using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Migrations
{
    /// <inheritdoc />
    public partial class AddNotionFieldsToUserSettings1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comment",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "Entry",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "OpenPrice",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "SL",
                table: "Trades");

            migrationBuilder.RenameColumn(
                name: "Volume",
                table: "Trades",
                newName: "Risk");

            migrationBuilder.RenameColumn(
                name: "TP",
                table: "Trades",
                newName: "RR");

            migrationBuilder.AlterColumn<string>(
                name: "Ticker",
                table: "Trades",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "NotionPageId",
                table: "Trades",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Direction",
                table: "Trades",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "Account",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Context",
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
                name: "Position",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Result",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Session",
                table: "Trades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Setup",
                table: "Trades",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Account",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "Context",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "Emotions",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "EntryDetails",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "Result",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "Session",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "Setup",
                table: "Trades");

            migrationBuilder.RenameColumn(
                name: "Risk",
                table: "Trades",
                newName: "Volume");

            migrationBuilder.RenameColumn(
                name: "RR",
                table: "Trades",
                newName: "TP");

            migrationBuilder.AlterColumn<string>(
                name: "Ticker",
                table: "Trades",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NotionPageId",
                table: "Trades",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Direction",
                table: "Trades",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "Trades",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Entry",
                table: "Trades",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OpenPrice",
                table: "Trades",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SL",
                table: "Trades",
                type: "REAL",
                nullable: true);
        }
    }
}

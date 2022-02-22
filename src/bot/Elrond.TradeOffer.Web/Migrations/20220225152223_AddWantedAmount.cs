using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elrond.TradeOffer.Web.Migrations
{
    public partial class AddWantedAmount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TokenAmountWant",
                table: "Offers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TokenIdWant",
                table: "Offers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TokenNameWant",
                table: "Offers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<ulong>(
                name: "TokenNonceWant",
                table: "Offers",
                type: "bigint unsigned",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TokenPrecisionWant",
                table: "Offers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WantsSomethingSpecific",
                table: "Offers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TokenAmountWant",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "TokenIdWant",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "TokenNameWant",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "TokenNonceWant",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "TokenPrecisionWant",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "WantsSomethingSpecific",
                table: "Offers");
        }
    }
}

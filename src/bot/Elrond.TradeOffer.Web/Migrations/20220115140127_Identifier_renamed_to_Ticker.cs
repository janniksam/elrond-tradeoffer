using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elrond.TradeOffer.Web.Migrations
{
    public partial class Identifier_renamed_to_Ticker : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TokenIdentifier",
                table: "Offers",
                newName: "TokenTicker");

            migrationBuilder.RenameColumn(
                name: "TokenIdentifier",
                table: "Bids",
                newName: "TokenTicker");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TokenTicker",
                table: "Offers",
                newName: "TokenIdentifier");

            migrationBuilder.RenameColumn(
                name: "TokenTicker",
                table: "Bids",
                newName: "TokenIdentifier");
        }
    }
}

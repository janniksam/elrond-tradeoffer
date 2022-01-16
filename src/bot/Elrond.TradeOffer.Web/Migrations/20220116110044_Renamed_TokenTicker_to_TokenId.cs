using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elrond.TradeOffer.Web.Migrations
{
    public partial class Renamed_TokenTicker_to_TokenId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TokenTicker",
                table: "Offers",
                newName: "TokenId");

            migrationBuilder.RenameColumn(
                name: "TokenTicker",
                table: "Bids",
                newName: "TokenId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TokenId",
                table: "Offers",
                newName: "TokenTicker");

            migrationBuilder.RenameColumn(
                name: "TokenId",
                table: "Bids",
                newName: "TokenTicker");
        }
    }
}

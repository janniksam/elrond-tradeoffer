using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elrond.TradeOffer.Web.Migrations
{
    public partial class DbUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Address = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Network = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_CreatorUserId",
                table: "Offers",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bids_CreatorUserId",
                table: "Bids",
                column: "CreatorUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bids_Users_CreatorUserId",
                table: "Bids",
                column: "CreatorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Users_CreatorUserId",
                table: "Offers",
                column: "CreatorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bids_Users_CreatorUserId",
                table: "Bids");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Users_CreatorUserId",
                table: "Offers");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Offers_CreatorUserId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Bids_CreatorUserId",
                table: "Bids");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fptu_cup.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchForfeit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ForfeitReason",
                table: "Matches",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ForfeitTeamId",
                table: "Matches",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_ForfeitTeamId",
                table: "Matches",
                column: "ForfeitTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_ForfeitTeamId",
                table: "Matches",
                column: "ForfeitTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_ForfeitTeamId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_ForfeitTeamId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ForfeitReason",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ForfeitTeamId",
                table: "Matches");
        }
    }
}

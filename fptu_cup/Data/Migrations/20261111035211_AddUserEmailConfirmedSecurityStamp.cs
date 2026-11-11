using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fptu_cup.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEmailConfirmedSecurityStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true); // tài khoản đã có trước coi như đã xác nhận email

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            // Identity cần SecurityStamp khác null để sinh token đặt lại mật khẩu / xác nhận email.
            migrationBuilder.Sql("UPDATE Users SET SecurityStamp = CONVERT(nvarchar(36), NEWID()) WHERE SecurityStamp IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Users");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_Register_BE.Migrations
{
    /// <inheritdoc />
    public partial class moredetailstouser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "user_first_name",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "user_last_name",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "user_middle_name",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "user_first_name",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "user_last_name",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "user_middle_name",
                table: "AspNetUsers");
        }
    }
}

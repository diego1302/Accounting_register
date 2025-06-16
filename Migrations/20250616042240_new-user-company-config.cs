using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_Register_BE.Migrations
{
    /// <inheritdoc />
    public partial class newusercompanyconfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_company_clients_CompanyClientId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_SecuritiesIssuer_company_clients_CompanyClientId",
                table: "SecuritiesIssuer");

            migrationBuilder.AlterColumn<int>(
                name: "CompanyClientId",
                table: "SecuritiesIssuer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyClientId",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_company_clients_CompanyClientId",
                table: "AspNetUsers",
                column: "CompanyClientId",
                principalTable: "company_clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SecuritiesIssuer_company_clients_CompanyClientId",
                table: "SecuritiesIssuer",
                column: "CompanyClientId",
                principalTable: "company_clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_company_clients_CompanyClientId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_SecuritiesIssuer_company_clients_CompanyClientId",
                table: "SecuritiesIssuer");

            migrationBuilder.AlterColumn<int>(
                name: "CompanyClientId",
                table: "SecuritiesIssuer",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "CompanyClientId",
                table: "AspNetUsers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_company_clients_CompanyClientId",
                table: "AspNetUsers",
                column: "CompanyClientId",
                principalTable: "company_clients",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SecuritiesIssuer_company_clients_CompanyClientId",
                table: "SecuritiesIssuer",
                column: "CompanyClientId",
                principalTable: "company_clients",
                principalColumn: "Id");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_Register_BE.Migrations
{
    /// <inheritdoc />
    public partial class clientcompanydetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyClientId",
                table: "SecuritiesIssuer",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyClientId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "company_clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ABN = table.Column<long>(type: "bigint", nullable: false),
                    ACN = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    phone_country_code = table.Column<int>(type: "int", nullable: false),
                    phone_number = table.Column<int>(type: "int", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    webpage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    registered_office_address_postal_delivery_number_line_1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    registered_office_address_postal_delivery_number_line_2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    registered_office_address_postal_delivery_number_line_3 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    registered_office_address_country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    registered_office_address_sub_unit_and_or_Level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    registered_office_address_street_number = table.Column<int>(type: "int", nullable: false),
                    registered_office_address_street_name = table.Column<int>(type: "int", nullable: false),
                    registered_office_address_australian_address_postal_delivery_street_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    registered_office_address_australian_address_suburb = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    registered_office_address_australian_address_state = table.Column<int>(type: "int", nullable: false),
                    registered_office_address_australian_address_australia_post_code = table.Column<int>(type: "int", nullable: false),
                    registered_office_address_OverseasAddress_overseas_post_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    postal_office_address_postal_delivery_number_line_1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    postal_office_address_postal_delivery_number_line_2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    postal_office_address_postal_delivery_number_line_3 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    postal_office_address_country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    postal_office_address_sub_unit_and_or_Level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    postal_office_address_street_number = table.Column<int>(type: "int", nullable: false),
                    postal_office_address_street_name = table.Column<int>(type: "int", nullable: false),
                    postal_office_address_australian_address_postal_delivery_street_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    postal_office_address_australian_address_suburb = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    postal_office_address_australian_address_state = table.Column<int>(type: "int", nullable: false),
                    postal_office_address_australian_address_australia_post_code = table.Column<int>(type: "int", nullable: false),
                    postal_office_address_OverseasAddress_overseas_post_code = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_clients", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SecuritiesIssuer_CompanyClientId",
                table: "SecuritiesIssuer",
                column: "CompanyClientId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CompanyClientId",
                table: "AspNetUsers",
                column: "CompanyClientId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_company_clients_CompanyClientId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_SecuritiesIssuer_company_clients_CompanyClientId",
                table: "SecuritiesIssuer");

            migrationBuilder.DropTable(
                name: "company_clients");

            migrationBuilder.DropIndex(
                name: "IX_SecuritiesIssuer_CompanyClientId",
                table: "SecuritiesIssuer");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CompanyClientId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CompanyClientId",
                table: "SecuritiesIssuer");

            migrationBuilder.DropColumn(
                name: "CompanyClientId",
                table: "AspNetUsers");
        }
    }
}

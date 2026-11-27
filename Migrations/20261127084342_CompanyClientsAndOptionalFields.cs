using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_Register_BE.Migrations
{
    /// <inheritdoc />
    public partial class CompanyClientsAndOptionalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "webpage",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "registered_office_address_sub_unit_and_or_Level",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "registered_office_address_street_number",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "registered_office_address_street_name",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "registered_office_address_postal_delivery_number_line_3",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "registered_office_address_postal_delivery_number_line_2",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "registered_office_address_postal_delivery_number_line_1",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "registered_office_address_country",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "registered_office_address_australian_address_suburb",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "registered_office_address_australian_address_state",
                table: "SecuritiesIssuer",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "registered_office_address_australian_address_postal_delivery_street_type",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "registered_office_address_australian_address_australia_post_code",
                table: "SecuritiesIssuer",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "registered_office_address_OverseasAddress_overseas_post_code",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "postal_office_address_sub_unit_and_or_Level",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "postal_office_address_street_number",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "postal_office_address_street_name",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "postal_office_address_postal_delivery_number_line_3",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "postal_office_address_postal_delivery_number_line_2",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "postal_office_address_postal_delivery_number_line_1",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "postal_office_address_country",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "postal_office_address_australian_address_suburb",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "postal_office_address_australian_address_state",
                table: "SecuritiesIssuer",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "postal_office_address_australian_address_postal_delivery_street_type",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "postal_office_address_australian_address_australia_post_code",
                table: "SecuritiesIssuer",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "postal_office_address_OverseasAddress_overseas_post_code",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<long>(
                name: "phone_number",
                table: "SecuritiesIssuer",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "phone_country_code",
                table: "SecuritiesIssuer",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SecuritiesIssuer",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<long>(
                name: "ACN",
                table: "SecuritiesIssuer",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "ABN",
                table: "SecuritiesIssuer",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "user_middle_name",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "user_last_name",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "user_first_name",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "CompanyClientId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanyClients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ABN = table.Column<long>(type: "bigint", nullable: true),
                    ACN = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone_country_code = table.Column<int>(type: "int", nullable: true),
                    phone_number = table.Column<long>(type: "bigint", nullable: true),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    webpage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    registered_office_address_postal_delivery_number_line_1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    registered_office_address_postal_delivery_number_line_2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    registered_office_address_postal_delivery_number_line_3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    registered_office_address_country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    registered_office_address_sub_unit_and_or_Level = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    registered_office_address_street_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    registered_office_address_street_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    registered_office_address_australian_address_postal_delivery_street_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    registered_office_address_australian_address_suburb = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    registered_office_address_australian_address_state = table.Column<int>(type: "int", nullable: true),
                    registered_office_address_australian_address_australia_post_code = table.Column<int>(type: "int", nullable: true),
                    registered_office_address_OverseasAddress_overseas_post_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    postal_office_address_postal_delivery_number_line_1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    postal_office_address_postal_delivery_number_line_2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    postal_office_address_postal_delivery_number_line_3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    postal_office_address_country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    postal_office_address_sub_unit_and_or_Level = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    postal_office_address_street_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    postal_office_address_street_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    postal_office_address_australian_address_postal_delivery_street_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    postal_office_address_australian_address_suburb = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    postal_office_address_australian_address_state = table.Column<int>(type: "int", nullable: true),
                    postal_office_address_australian_address_australia_post_code = table.Column<int>(type: "int", nullable: true),
                    postal_office_address_OverseasAddress_overseas_post_code = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyClients", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CompanyClientId",
                table: "AspNetUsers",
                column: "CompanyClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_CompanyClients_CompanyClientId",
                table: "AspNetUsers",
                column: "CompanyClientId",
                principalTable: "CompanyClients",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_CompanyClients_CompanyClientId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "CompanyClients");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CompanyClientId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CompanyClientId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "webpage",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "registered_office_address_sub_unit_and_or_Level",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "registered_office_address_street_number",
                table: "SecuritiesIssuer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "registered_office_address_street_name",
                table: "SecuritiesIssuer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "registered_office_address_postal_delivery_number_line_3",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "registered_office_address_postal_delivery_number_line_2",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "registered_office_address_postal_delivery_number_line_1",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "registered_office_address_country",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "registered_office_address_australian_address_suburb",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "registered_office_address_australian_address_state",
                table: "SecuritiesIssuer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "registered_office_address_australian_address_postal_delivery_street_type",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "registered_office_address_australian_address_australia_post_code",
                table: "SecuritiesIssuer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "registered_office_address_OverseasAddress_overseas_post_code",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "postal_office_address_sub_unit_and_or_Level",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "postal_office_address_street_number",
                table: "SecuritiesIssuer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "postal_office_address_street_name",
                table: "SecuritiesIssuer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "postal_office_address_postal_delivery_number_line_3",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "postal_office_address_postal_delivery_number_line_2",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "postal_office_address_postal_delivery_number_line_1",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "postal_office_address_country",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "postal_office_address_australian_address_suburb",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "postal_office_address_australian_address_state",
                table: "SecuritiesIssuer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "postal_office_address_australian_address_postal_delivery_street_type",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "postal_office_address_australian_address_australia_post_code",
                table: "SecuritiesIssuer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "postal_office_address_OverseasAddress_overseas_post_code",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "phone_number",
                table: "SecuritiesIssuer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "phone_country_code",
                table: "SecuritiesIssuer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "SecuritiesIssuer",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ACN",
                table: "SecuritiesIssuer",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ABN",
                table: "SecuritiesIssuer",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "user_middle_name",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "user_last_name",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "user_first_name",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}

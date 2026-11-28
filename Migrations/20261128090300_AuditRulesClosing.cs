using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_Register_BE.Migrations
{
    /// <inheritdoc />
    public partial class AuditRulesClosing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "JournalEntries",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountingSettings",
                columns: table => new
                {
                    CompanyClientId = table.Column<int>(type: "int", nullable: false),
                    LockDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingSettings", x => x.CompanyClientId);
                    table.ForeignKey(
                        name: "FK_AccountingSettings_CompanyClients_CompanyClientId",
                        column: x => x.CompanyClientId,
                        principalTable: "CompanyClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyClientId = table.Column<int>(type: "int", nullable: true),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApiKeyId = table.Column<int>(type: "int", nullable: true),
                    Actor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_CompanyClients_CompanyClientId",
                        column: x => x.CompanyClientId,
                        principalTable: "CompanyClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyClientId = table.Column<int>(type: "int", nullable: false),
                    BankAccountId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    MatchField = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MatchType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MatchText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AmountMin = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AmountMax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    TaxType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LineDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AutoReconcile = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankRules_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankRules_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankRules_CompanyClients_CompanyClientId",
                        column: x => x.CompanyClientId,
                        principalTable: "CompanyClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CompanyClientId_At",
                table: "AuditLogs",
                columns: new[] { "CompanyClientId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CompanyClientId_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "CompanyClientId", "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_BankRules_AccountId",
                table: "BankRules",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankRules_BankAccountId",
                table: "BankRules",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankRules_CompanyClientId",
                table: "BankRules",
                column: "CompanyClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingSettings");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BankRules");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "JournalEntries");
        }
    }
}

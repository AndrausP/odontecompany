using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Convenios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CodigoExterno = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Convenios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Faturas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgendamentoId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: true),
                    TipoFatura = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConvenioId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValorTotal = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    FormaPagamento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ComissaoDentistaPercentual = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProtocoloConvenio = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faturas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parcelas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FaturaId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroParcela = table.Column<int>(type: "integer", nullable: false),
                    ValorParcela = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    DataVencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataPagamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parcelas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parcelas_Faturas_FaturaId",
                        column: x => x.FaturaId,
                        principalTable: "Faturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Convenios_OrganizationId_Nome",
                table: "Convenios",
                columns: new[] { "OrganizationId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faturas_AgendamentoId",
                table: "Faturas",
                column: "AgendamentoId",
                unique: true,
                filter: "\"AgendamentoId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Faturas_OrganizationId_PacienteId",
                table: "Faturas",
                columns: new[] { "OrganizationId", "PacienteId" });

            migrationBuilder.CreateIndex(
                name: "IX_Parcelas_FaturaId",
                table: "Parcelas",
                column: "FaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_Parcelas_OrganizationId_FaturaId",
                table: "Parcelas",
                columns: new[] { "OrganizationId", "FaturaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Convenios");

            migrationBuilder.DropTable(
                name: "Parcelas");

            migrationBuilder.DropTable(
                name: "Faturas");
        }
    }
}

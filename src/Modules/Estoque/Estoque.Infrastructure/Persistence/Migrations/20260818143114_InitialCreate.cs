using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Estoque.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItensEstoque",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UnidadeMedida = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    QuantidadeAtual = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    QuantidadeMinima = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensEstoque", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItensEstoque_OrganizationId_BranchId_Nome",
                table: "ItensEstoque",
                columns: new[] { "OrganizationId", "BranchId", "Nome" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItensEstoque");
        }
    }
}

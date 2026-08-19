using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Records.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnexosMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProntuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeArquivo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TipoConteudo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    CaminhoArmazenamento = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DataUpload = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnexosMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvolucoesClinicas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProntuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoProcedimento = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DescricaoClinicaCifrada = table.Column<string>(type: "text", nullable: false),
                    DataRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvolucoesClinicas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Prontuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadoPorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    Odontograma = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prontuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecordsAuditLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProntuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Acao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordsAuditLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnexosMetadata_OrganizationId_ProntuarioId",
                table: "AnexosMetadata",
                columns: new[] { "OrganizationId", "ProntuarioId" });

            migrationBuilder.CreateIndex(
                name: "IX_EvolucoesClinicas_OrganizationId_ProntuarioId",
                table: "EvolucoesClinicas",
                columns: new[] { "OrganizationId", "ProntuarioId" });

            migrationBuilder.CreateIndex(
                name: "IX_Prontuarios_OrganizationId_PacienteId",
                table: "Prontuarios",
                columns: new[] { "OrganizationId", "PacienteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecordsAuditLog_OrganizationId_ProntuarioId_Timestamp",
                table: "RecordsAuditLog",
                columns: new[] { "OrganizationId", "ProntuarioId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnexosMetadata");

            migrationBuilder.DropTable(
                name: "EvolucoesClinicas");

            migrationBuilder.DropTable(
                name: "Prontuarios");

            migrationBuilder.DropTable(
                name: "RecordsAuditLog");
        }
    }
}

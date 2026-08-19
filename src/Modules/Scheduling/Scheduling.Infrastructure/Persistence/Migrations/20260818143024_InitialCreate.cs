using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Agendamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Periodo_Inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Periodo_Fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MotivoCancelamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ValorConsulta = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agendamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profissionais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Especialidade = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profissionais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Salas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CapacidadeMaxima = table.Column<int>(type: "integer", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchedulingOutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulingOutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_OrganizationId_ProfissionalId",
                table: "Agendamentos",
                columns: new[] { "OrganizationId", "ProfissionalId" });

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_PeriodoInicio",
                table: "Agendamentos",
                column: "Periodo_Inicio");

            migrationBuilder.CreateIndex(
                name: "IX_Profissionais_OrganizationId_Ativo",
                table: "Profissionais",
                columns: new[] { "OrganizationId", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_Salas_OrganizationId_Ativa",
                table: "Salas",
                columns: new[] { "OrganizationId", "Ativa" });

            migrationBuilder.CreateIndex(
                name: "IX_SchedulingOutboxMessages_PublishedOn",
                table: "SchedulingOutboxMessages",
                column: "PublishedOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Agendamentos");

            migrationBuilder.DropTable(
                name: "Profissionais");

            migrationBuilder.DropTable(
                name: "Salas");

            migrationBuilder.DropTable(
                name: "SchedulingOutboxMessages");
        }
    }
}

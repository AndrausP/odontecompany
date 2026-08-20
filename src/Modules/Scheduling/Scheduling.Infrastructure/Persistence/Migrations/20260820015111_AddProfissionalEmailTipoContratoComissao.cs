using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfissionalEmailTipoContratoComissao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Profissionais",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PercentualComissaoDefault",
                table: "Profissionais",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoContrato",
                table: "Profissionais",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Profissionais_OrganizationId_Email",
                table: "Profissionais",
                columns: new[] { "OrganizationId", "Email" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Profissionais_OrganizationId_Email",
                table: "Profissionais");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Profissionais");

            migrationBuilder.DropColumn(
                name: "PercentualComissaoDefault",
                table: "Profissionais");

            migrationBuilder.DropColumn(
                name: "TipoContrato",
                table: "Profissionais");
        }
    }
}

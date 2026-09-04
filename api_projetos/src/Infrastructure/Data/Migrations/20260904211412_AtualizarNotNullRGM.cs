using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Gestao_Eventos.src.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AtualizarNotNullRGM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_UniqueIdentifier_InstitutionId",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UniqueIdentifier_InstitutionId",
                table: "Users",
                columns: new[] { "UniqueIdentifier", "InstitutionId" },
                unique: true,
                filter: "\"UniqueIdentifier\" IS NOT NULL AND \"InstitutionId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_UniqueIdentifier_InstitutionId",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UniqueIdentifier_InstitutionId",
                table: "Users",
                columns: new[] { "UniqueIdentifier", "InstitutionId" },
                unique: true,
                filter: "'UniqueIdentifier' IS NOT NULL AND 'InstitutionId' IS NOT NULL");
        }
    }
}

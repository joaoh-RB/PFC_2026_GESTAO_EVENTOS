using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Gestao_Eventos.src.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoverIsInstitutionAdminTeacher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsInstitutionAdmin",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInstitutionAdmin",
                table: "Users",
                type: "boolean",
                nullable: true);
        }
    }
}

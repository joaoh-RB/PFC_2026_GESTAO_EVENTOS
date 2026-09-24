using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Gestao_Eventos.src.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserNameFromAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserName",
                table: "audit_logs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "audit_logs",
                type: "text",
                nullable: true);
        }
    }
}

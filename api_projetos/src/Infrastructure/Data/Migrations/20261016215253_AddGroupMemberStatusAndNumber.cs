using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Gestao_Eventos.src.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupMemberStatusAndNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Groups_EventId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_GroupMembers_StudentId_EventId",
                table: "GroupMembers");

            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "Groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "GroupMembers",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_EventId_Number",
                table: "Groups",
                columns: new[] { "EventId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_StudentId_EventId",
                table: "GroupMembers",
                columns: new[] { "StudentId", "EventId" },
                unique: true,
                filter: "\"Status\" = 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Groups_EventId_Number",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_GroupMembers_StudentId_EventId",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "GroupMembers");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_EventId",
                table: "Groups",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_StudentId_EventId",
                table: "GroupMembers",
                columns: new[] { "StudentId", "EventId" },
                unique: true);
        }
    }
}

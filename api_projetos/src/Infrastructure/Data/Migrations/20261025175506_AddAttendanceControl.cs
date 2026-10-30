using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Gestao_Eventos.src.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttendanceMethod",
                table: "EventStudents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AttendedAt",
                table: "EventStudents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttendanceCode",
                table: "Events",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendanceMethod",
                table: "EventStudents");

            migrationBuilder.DropColumn(
                name: "AttendedAt",
                table: "EventStudents");

            migrationBuilder.DropColumn(
                name: "AttendanceCode",
                table: "Events");
        }
    }
}

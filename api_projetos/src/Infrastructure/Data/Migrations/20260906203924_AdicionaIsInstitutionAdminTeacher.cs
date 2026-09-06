using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Gestao_Eventos.src.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaIsInstitutionAdminTeacher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ApprovedByUserId",
                table: "Users");

            migrationBuilder.AddColumn<bool>(
                name: "IsInstitutionAdmin",
                table: "Users",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ApprovedByUserId",
                table: "Users",
                column: "ApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ApprovedByUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsInstitutionAdmin",
                table: "Users");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ApprovedByUserId",
                table: "Users",
                column: "ApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
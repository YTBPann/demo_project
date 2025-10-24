using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo_project.Migrations
{
    /// <inheritdoc />
    public partial class FixRelationsAndLastNameChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_students_users_UserId1",
                table: "students");

            migrationBuilder.DropForeignKey(
                name: "FK_teachers_users_UserId1",
                table: "teachers");

            migrationBuilder.DropIndex(
                name: "IX_teachers_UserId1",
                table: "teachers");

            migrationBuilder.DropIndex(
                name: "IX_students_UserId1",
                table: "students");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "teachers");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "students");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastNameChange",
                table: "users",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastNameChange",
                table: "users");

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "teachers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "students",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_teachers_UserId1",
                table: "teachers",
                column: "UserId1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_students_UserId1",
                table: "students",
                column: "UserId1",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_students_users_UserId1",
                table: "students",
                column: "UserId1",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_teachers_users_UserId1",
                table: "teachers",
                column: "UserId1",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}

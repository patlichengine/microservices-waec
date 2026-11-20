using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentService.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentNumberToModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StudentNumber",
                table: "Students",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentNumber",
                table: "Students",
                column: "StudentNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_StudentNumber",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "StudentNumber",
                table: "Students");
        }
    }
}

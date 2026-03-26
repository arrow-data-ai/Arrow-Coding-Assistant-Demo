using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronocode.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCompleteToProjectTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsComplete",
                table: "ProjectTasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsComplete",
                table: "ProjectTasks");
        }
    }
}

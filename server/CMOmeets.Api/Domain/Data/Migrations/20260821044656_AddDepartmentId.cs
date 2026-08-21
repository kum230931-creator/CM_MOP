using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMOmeets.Domain.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "tb_meetingMembers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "tb_meetingMembers");
        }
    }
}

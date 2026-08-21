using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMOmeets.Domain.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentIdTOTbMeetingAgenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DepartmentIDs",
                table: "tb_meetingAgendas",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartmentIDs",
                table: "tb_meetingAgendas");
        }
    }
}

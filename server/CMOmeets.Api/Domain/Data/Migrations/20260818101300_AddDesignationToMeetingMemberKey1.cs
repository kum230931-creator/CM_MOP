using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMOmeets.Domain.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDesignationToMeetingMemberKey1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_tb_meetingMembers",
                table: "tb_meetingMembers");

            migrationBuilder.AddColumn<long>(
                name: "RID",
                table: "tb_meetingMembers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tb_meetingMembers",
                table: "tb_meetingMembers",
                column: "RID");

            migrationBuilder.CreateIndex(
                name: "IX_tb_meetingMembers_meetingRID",
                table: "tb_meetingMembers",
                column: "meetingRID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_tb_meetingMembers",
                table: "tb_meetingMembers");

            migrationBuilder.DropIndex(
                name: "IX_tb_meetingMembers_meetingRID",
                table: "tb_meetingMembers");

            migrationBuilder.DropColumn(
                name: "RID",
                table: "tb_meetingMembers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tb_meetingMembers",
                table: "tb_meetingMembers",
                columns: new[] { "meetingRID", "memberRID" });
        }
    }
}

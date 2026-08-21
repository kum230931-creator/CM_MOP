using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMOmeets.Domain.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropMeetingMemberOfficerFK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropForeignKey(
                name: "FK_members_officer",
                table: "tb_meetingMembers");

        }
        /// <inheritdoc />
       

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_members_officer",
                table: "tb_meetingMembers",
                column: "memberRID",
                principalTable: "tbl_Officers",
                principalColumn: "RID");
        }
    }
}

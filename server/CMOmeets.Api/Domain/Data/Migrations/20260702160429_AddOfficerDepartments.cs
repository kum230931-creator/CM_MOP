using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMOmeets.Domain.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficerDepartments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_officerDepartments",
                columns: table => new
                {
                    RID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    officerID = table.Column<int>(type: "int", nullable: false),
                    deptID = table.Column<int>(type: "int", nullable: false),
                    active = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false, defaultValue: "Y")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_officerDepartments", x => x.RID);
                    table.ForeignKey(
                        name: "FK_officerDept_department",
                        column: x => x.deptID,
                        principalTable: "departmentMas",
                        principalColumn: "RID");
                    table.ForeignKey(
                        name: "FK_officerDept_officer",
                        column: x => x.officerID,
                        principalTable: "tbl_Officers",
                        principalColumn: "RID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tb_officerDepartments_deptID",
                table: "tb_officerDepartments",
                column: "deptID");

            migrationBuilder.CreateIndex(
                name: "IX_tb_officerDepartments_officerID_deptID",
                table: "tb_officerDepartments",
                columns: new[] { "officerID", "deptID" },
                unique: true);

            // Seed each officer's home department as its first served department (idempotent).
            migrationBuilder.Sql(@"
INSERT INTO tb_officerDepartments (officerID, deptID, active)
SELECT o.RID, o.deptID, 'Y'
FROM tbl_Officers o
WHERE NOT EXISTS (
    SELECT 1 FROM tb_officerDepartments od
    WHERE od.officerID = o.RID AND od.deptID = o.deptID);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_officerDepartments");
        }
    }
}

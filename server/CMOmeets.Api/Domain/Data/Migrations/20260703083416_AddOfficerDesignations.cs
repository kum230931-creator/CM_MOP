using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMOmeets.Domain.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficerDesignations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_officerDesignations",
                columns: table => new
                {
                    RID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    officerID = table.Column<int>(type: "int", nullable: false),
                    desigID = table.Column<int>(type: "int", nullable: false),
                    active = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false, defaultValue: "Y")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_officerDesignations", x => x.RID);
                    table.ForeignKey(
                        name: "FK_officerDesig_designation",
                        column: x => x.desigID,
                        principalTable: "MAS_DeptDesignation",
                        principalColumn: "RID");
                    table.ForeignKey(
                        name: "FK_officerDesig_officer",
                        column: x => x.officerID,
                        principalTable: "tbl_Officers",
                        principalColumn: "RID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tb_officerDesignations_desigID",
                table: "tb_officerDesignations",
                column: "desigID");

            migrationBuilder.CreateIndex(
                name: "IX_tb_officerDesignations_officerID_desigID",
                table: "tb_officerDesignations",
                columns: new[] { "officerID", "desigID" },
                unique: true);

            // Seed each officer's current (primary) designation as its first held designation
            // (idempotent; skips officers whose designation row no longer exists, to satisfy the FK).
            migrationBuilder.Sql(@"
INSERT INTO tb_officerDesignations (officerID, desigID, active)
SELECT o.RID, o.desigID, 'Y'
FROM tbl_Officers o
WHERE o.desigID IS NOT NULL AND o.desigID > 0
  AND EXISTS (SELECT 1 FROM MAS_DeptDesignation d WHERE d.RID = o.desigID)
  AND NOT EXISTS (
    SELECT 1 FROM tb_officerDesignations od
    WHERE od.officerID = o.RID AND od.desigID = o.desigID);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_officerDesignations");
        }
    }
}

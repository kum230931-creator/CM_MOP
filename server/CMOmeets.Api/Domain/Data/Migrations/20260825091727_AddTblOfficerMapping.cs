using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMOmeets.Domain.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTblOfficerMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_officermapping",
                columns: table => new
                {
                    RID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OfficerID = table.Column<int>(type: "int", nullable: false),
                    DeptID = table.Column<int>(type: "int", nullable: false),
                    DesigID = table.Column<int>(type: "int", nullable: true),
                    Active = table.Column<string>(type: "char(1)", nullable: false, defaultValue: "1"),
                    IsPrimary = table.Column<string>(type: "char(1)", nullable: false, defaultValue: "1"),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_officermapping", x => x.RID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfficerMapping_Officer_Active",
                table: "tb_officermapping",
                columns: new[] { "OfficerID", "Active" });

            migrationBuilder.CreateIndex(
                name: "UX_OfficerMapping_ActiveDesig",
                table: "tb_officermapping",
                column: "DesigID",
                unique: true,
                filter: "[Active] = '1' AND [DesigID] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_officermapping");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMOmeets.Domain.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActionPointViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_actionPointViews",
                columns: table => new
                {
                    RID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    agendaRID = table.Column<long>(type: "bigint", nullable: false),
                    deptID = table.Column<int>(type: "int", nullable: false),
                    firstViewedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    lastViewedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    viewedBy = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_actionPointViews", x => x.RID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tb_actionPointViews_agendaRID_deptID",
                table: "tb_actionPointViews",
                columns: new[] { "agendaRID", "deptID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_actionPointViews");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMOmeets.Domain.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAtrExplanation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "explainedAt",
                table: "tb_remarksOnAgendas",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "explainedBy",
                table: "tb_remarksOnAgendas",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "explanationDoc",
                table: "tb_remarksOnAgendas",
                type: "varchar(350)",
                unicode: false,
                maxLength: 350,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "explanationRequest",
                table: "tb_remarksOnAgendas",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "explanationRequestedAt",
                table: "tb_remarksOnAgendas",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "explanationRequestedBy",
                table: "tb_remarksOnAgendas",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "explanationText",
                table: "tb_remarksOnAgendas",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "explainedAt",
                table: "tb_remarksOnAgendas");

            migrationBuilder.DropColumn(
                name: "explainedBy",
                table: "tb_remarksOnAgendas");

            migrationBuilder.DropColumn(
                name: "explanationDoc",
                table: "tb_remarksOnAgendas");

            migrationBuilder.DropColumn(
                name: "explanationRequest",
                table: "tb_remarksOnAgendas");

            migrationBuilder.DropColumn(
                name: "explanationRequestedAt",
                table: "tb_remarksOnAgendas");

            migrationBuilder.DropColumn(
                name: "explanationRequestedBy",
                table: "tb_remarksOnAgendas");

            migrationBuilder.DropColumn(
                name: "explanationText",
                table: "tb_remarksOnAgendas");
        }
    }
}

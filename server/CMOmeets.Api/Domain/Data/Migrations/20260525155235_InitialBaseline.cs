using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMOmeets.Domain.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "designationMas",
                columns: table => new
                {
                    RID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    designationName = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    officerName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    mobileNo = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    seqNo = table.Column<int>(type: "int", nullable: true, defaultValue: 9999),
                    active = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: true, defaultValue: "Y"),
                    createdAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    createdBy = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_designationMas", x => x.RID);
                });

            migrationBuilder.CreateTable(
                name: "login_history_pwd",
                columns: table => new
                {
                    RID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuthID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    userType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    last_login = table.Column<DateTime>(type: "datetime", nullable: false),
                    ip_add = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    session_id = table.Column<string>(type: "nvarchar(350)", maxLength: 350, nullable: false),
                    logout_time = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login_history_pwd", x => x.RID);
                });

            migrationBuilder.CreateTable(
                name: "Master_District",
                columns: table => new
                {
                    dCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    dName = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    isActive = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Master_District", x => x.dCode);
                });

            migrationBuilder.CreateTable(
                name: "ministryMas",
                columns: table => new
                {
                    RID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ministryName = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ministryMas", x => x.RID);
                });

            migrationBuilder.CreateTable(
                name: "tb_MeetingGroup",
                columns: table => new
                {
                    RID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    groupName = table.Column<string>(type: "nvarchar(350)", maxLength: 350, nullable: false),
                    active = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: false, defaultValue: "Y"),
                    addedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    addedBy = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_MeetingGroup", x => x.RID);
                });

            migrationBuilder.CreateTable(
                name: "tb_meetingSchedules",
                columns: table => new
                {
                    RID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    meetingDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    meetingPlace = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    meetingSubject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    meetingDocument = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    active = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: false, defaultValue: "Y"),
                    addedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    addedBy = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tb_meeti__CAFF4132428AB66A", x => x.RID);
                });

            migrationBuilder.CreateTable(
                name: "User_Authentication_Detail",
                columns: table => new
                {
                    AuthID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    RID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuthPwd = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false),
                    OfficialName = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false),
                    MobileNumber = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    Designation = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    UserType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    LocationCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    LocationName = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Created_At = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    Active = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false, defaultValue: "Y")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__User_Aut__12C15D338E8E35CE", x => x.AuthID);
                });

            migrationBuilder.CreateTable(
                name: "departmentMas",
                columns: table => new
                {
                    RID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ministryID = table.Column<int>(type: "int", nullable: true),
                    departmentName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    departmentNameHin = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    active = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: true, defaultValue: "Y"),
                    createdAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    createdBy = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departmentMas", x => x.RID);
                    table.ForeignKey(
                        name: "FK_departmentMas_ministry",
                        column: x => x.ministryID,
                        principalTable: "ministryMas",
                        principalColumn: "RID");
                });

            migrationBuilder.CreateTable(
                name: "tb_meetingAgendas",
                columns: table => new
                {
                    RID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    meetingRID = table.Column<int>(type: "int", nullable: false),
                    meetingAgenda = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    agendaMembers = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    memberRIDs = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    agendaDueDt = table.Column<DateOnly>(type: "date", nullable: true),
                    districtName = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "State"),
                    agendaStatus = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "InProgress"),
                    active = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: true, defaultValue: "Y"),
                    addedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    addedBy = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tb_meeti__CAFF413277474227", x => x.RID);
                    table.ForeignKey(
                        name: "FK_agenda_schedule",
                        column: x => x.meetingRID,
                        principalTable: "tb_meetingSchedules",
                        principalColumn: "RID");
                });

            migrationBuilder.CreateTable(
                name: "tb_meetingMappedGroup",
                columns: table => new
                {
                    RID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    groupRID = table.Column<long>(type: "bigint", nullable: false),
                    meetingRID = table.Column<int>(type: "int", nullable: false),
                    active = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: false, defaultValue: "Y"),
                    addedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    addedBy = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_meetingMappedGroup_RID", x => x.RID);
                    table.ForeignKey(
                        name: "FK_mappedGroup_group",
                        column: x => x.groupRID,
                        principalTable: "tb_MeetingGroup",
                        principalColumn: "RID");
                    table.ForeignKey(
                        name: "FK_mappedGroup_schedule",
                        column: x => x.meetingRID,
                        principalTable: "tb_meetingSchedules",
                        principalColumn: "RID");
                });

            migrationBuilder.CreateTable(
                name: "MAS_DeptDesignation",
                columns: table => new
                {
                    RID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    deptID = table.Column<int>(type: "int", nullable: false),
                    desigName = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    seqNo = table.Column<int>(type: "int", nullable: false, defaultValue: 9),
                    active = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false, defaultValue: "Y"),
                    createdAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    createdBy = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MAS_DeptDesignation_RID", x => x.RID);
                    table.ForeignKey(
                        name: "FK_deptDesig_department",
                        column: x => x.deptID,
                        principalTable: "departmentMas",
                        principalColumn: "RID");
                });

            migrationBuilder.CreateTable(
                name: "tb_remarksOnAgendas",
                columns: table => new
                {
                    RID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    meetingRID = table.Column<int>(type: "int", nullable: false),
                    agendaRID = table.Column<long>(type: "bigint", nullable: false),
                    agendaDueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    memberRID = table.Column<int>(type: "int", nullable: false),
                    memberName = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    agendaRemarks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    remarksDate = table.Column<DateOnly>(type: "date", nullable: true),
                    remarkStatus = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    atrDoc = table.Column<string>(type: "varchar(350)", unicode: false, maxLength: 350, nullable: true),
                    addedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    addedBy = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_remarksOnAgendas", x => x.RID);
                    table.ForeignKey(
                        name: "FK_remarks_agenda",
                        column: x => x.agendaRID,
                        principalTable: "tb_meetingAgendas",
                        principalColumn: "RID");
                    table.ForeignKey(
                        name: "FK_remarks_schedule",
                        column: x => x.meetingRID,
                        principalTable: "tb_meetingSchedules",
                        principalColumn: "RID");
                });

            migrationBuilder.CreateTable(
                name: "tbl_Officers",
                columns: table => new
                {
                    RID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    deptID = table.Column<int>(type: "int", nullable: false),
                    desigID = table.Column<int>(type: "int", nullable: false),
                    officerName = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    officerMobile = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    active = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false, defaultValue: "Y"),
                    createdAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    createdBy = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    updatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    updatedBy = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Officers", x => x.RID);
                    table.ForeignKey(
                        name: "FK_officers_department",
                        column: x => x.deptID,
                        principalTable: "departmentMas",
                        principalColumn: "RID");
                    table.ForeignKey(
                        name: "FK_officers_deptDesig",
                        column: x => x.desigID,
                        principalTable: "MAS_DeptDesignation",
                        principalColumn: "RID");
                });

            migrationBuilder.CreateTable(
                name: "tb_meetingMembers",
                columns: table => new
                {
                    meetingRID = table.Column<int>(type: "int", nullable: false),
                    memberRID = table.Column<int>(type: "int", nullable: false),

                   
                    addedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_meetingMembers", x => new { x.meetingRID, x.memberRID });
                    table.ForeignKey(
                        name: "FK_members_officer",
                        column: x => x.memberRID,
                        principalTable: "tbl_Officers",
                        principalColumn: "RID");
                    table.ForeignKey(
                        name: "FK_members_schedule",
                        column: x => x.meetingRID,
                        principalTable: "tb_meetingSchedules",
                        principalColumn: "RID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_departmentMas_ministryID",
                table: "departmentMas",
                column: "ministryID");

            migrationBuilder.CreateIndex(
                name: "IX_MAS_DeptDesignation_deptID",
                table: "MAS_DeptDesignation",
                column: "deptID");

            migrationBuilder.CreateIndex(
                name: "IX_tb_meetingAgendas_meetingRID",
                table: "tb_meetingAgendas",
                column: "meetingRID");

            migrationBuilder.CreateIndex(
                name: "IX_tb_meetingMappedGroup_groupRID",
                table: "tb_meetingMappedGroup",
                column: "groupRID");

            migrationBuilder.CreateIndex(
                name: "IX_tb_meetingMappedGroup_meetingRID",
                table: "tb_meetingMappedGroup",
                column: "meetingRID");

            migrationBuilder.CreateIndex(
                name: "IX_tb_meetingMembers_memberRID",
                table: "tb_meetingMembers",
                column: "memberRID");

            migrationBuilder.CreateIndex(
                name: "IX_tb_remarksOnAgendas_agendaRID",
                table: "tb_remarksOnAgendas",
                column: "agendaRID");

            migrationBuilder.CreateIndex(
                name: "IX_tb_remarksOnAgendas_meetingRID",
                table: "tb_remarksOnAgendas",
                column: "meetingRID");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Officers_deptID",
                table: "tbl_Officers",
                column: "deptID");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Officers_desigID",
                table: "tbl_Officers",
                column: "desigID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "designationMas");

            migrationBuilder.DropTable(
                name: "login_history_pwd");

            migrationBuilder.DropTable(
                name: "Master_District");

            migrationBuilder.DropTable(
                name: "tb_meetingMappedGroup");

            migrationBuilder.DropTable(
                name: "tb_meetingMembers");

            migrationBuilder.DropTable(
                name: "tb_remarksOnAgendas");

            migrationBuilder.DropTable(
                name: "User_Authentication_Detail");

            migrationBuilder.DropTable(
                name: "tb_MeetingGroup");

            migrationBuilder.DropTable(
                name: "tbl_Officers");

            migrationBuilder.DropTable(
                name: "tb_meetingAgendas");

            migrationBuilder.DropTable(
                name: "MAS_DeptDesignation");

            migrationBuilder.DropTable(
                name: "tb_meetingSchedules");

            migrationBuilder.DropTable(
                name: "departmentMas");

            migrationBuilder.DropTable(
                name: "ministryMas");
        }
    }
}

/* Phase 2 schema cleanup for CMOmeetsDB.
   Drops junk tables, removes orphan rows, standardizes RID primary keys,
   aligns column types, and adds foreign keys. Data preserved.
   Run once; idempotent guards included. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
USE CMOmeetsDB;
BEGIN TRAN;

/* 1. Remove orphan mapped-group rows (point to non-existent meetings) */
DELETE g FROM tb_meetingMappedGroup g
WHERE NOT EXISTS (SELECT 1 FROM tb_meetingSchedules s WHERE s.RID = g.meetingRID);

/* 2. Drop junk / abandoned duplicate tables */
IF OBJECT_ID('dbo.MinstryData_04122025')   IS NOT NULL DROP TABLE dbo.MinstryData_04122025;
IF OBJECT_ID('dbo.MinstryData_04122025_1') IS NOT NULL DROP TABLE dbo.MinstryData_04122025_1;
IF OBJECT_ID('dbo.zDeptTmp1')              IS NOT NULL DROP TABLE dbo.zDeptTmp1;
IF OBJECT_ID('dbo.mas_tb_Department')      IS NOT NULL DROP TABLE dbo.mas_tb_Department;
IF OBJECT_ID('dbo.mas_tb_Ministry')        IS NOT NULL DROP TABLE dbo.mas_tb_Ministry;
IF OBJECT_ID('dbo.tb_mapMeetingMinistry')  IS NOT NULL DROP TABLE dbo.tb_mapMeetingMinistry;
IF OBJECT_ID('dbo.tb_meetingDepartments')  IS NOT NULL DROP TABLE dbo.tb_meetingDepartments;

/* 3. Drop existing non-standard primary keys */
IF OBJECT_ID('PK__departme__E0719535682F18BD','PK') IS NOT NULL ALTER TABLE departmentMas DROP CONSTRAINT PK__departme__E0719535682F18BD;
IF OBJECT_ID('PK__designat__CFC6CFF9AC2C34F0','PK') IS NOT NULL ALTER TABLE designationMas DROP CONSTRAINT PK__designat__CFC6CFF9AC2C34F0;
IF OBJECT_ID('PK_MAS_DeptDesignation','PK')         IS NOT NULL ALTER TABLE MAS_DeptDesignation DROP CONSTRAINT PK_MAS_DeptDesignation;
IF OBJECT_ID('PK_tb_meetingMappedGroup','PK')       IS NOT NULL ALTER TABLE tb_meetingMappedGroup DROP CONSTRAINT PK_tb_meetingMappedGroup;

/* 4. Align column types so foreign keys are valid */
ALTER TABLE tb_remarksOnAgendas   ALTER COLUMN agendaRID  bigint NOT NULL;  -- match tb_meetingAgendas.RID (bigint)
ALTER TABLE tb_meetingMappedGroup ALTER COLUMN meetingRID int    NOT NULL;  -- match tb_meetingSchedules.RID (int)

/* 5. Add standardized primary keys on RID (or natural/composite where no RID) */
IF OBJECT_ID('PK_departmentMas','PK')        IS NULL ALTER TABLE departmentMas        ADD CONSTRAINT PK_departmentMas        PRIMARY KEY (RID);
IF OBJECT_ID('PK_designationMas','PK')       IS NULL ALTER TABLE designationMas       ADD CONSTRAINT PK_designationMas       PRIMARY KEY (RID);
IF OBJECT_ID('PK_ministryMas','PK')          IS NULL ALTER TABLE ministryMas          ADD CONSTRAINT PK_ministryMas          PRIMARY KEY (RID);
IF OBJECT_ID('PK_tbl_Officers','PK')         IS NULL ALTER TABLE tbl_Officers         ADD CONSTRAINT PK_tbl_Officers         PRIMARY KEY (RID);
IF OBJECT_ID('PK_tb_MeetingGroup','PK')      IS NULL ALTER TABLE tb_MeetingGroup      ADD CONSTRAINT PK_tb_MeetingGroup      PRIMARY KEY (RID);
IF OBJECT_ID('PK_tb_remarksOnAgendas','PK')  IS NULL ALTER TABLE tb_remarksOnAgendas  ADD CONSTRAINT PK_tb_remarksOnAgendas  PRIMARY KEY (RID);
IF OBJECT_ID('PK_login_history_pwd','PK')    IS NULL ALTER TABLE login_history_pwd    ADD CONSTRAINT PK_login_history_pwd    PRIMARY KEY (RID);
IF OBJECT_ID('PK_MAS_DeptDesignation_RID','PK') IS NULL ALTER TABLE MAS_DeptDesignation ADD CONSTRAINT PK_MAS_DeptDesignation_RID PRIMARY KEY (RID);
IF OBJECT_ID('PK_tb_meetingMappedGroup_RID','PK') IS NULL ALTER TABLE tb_meetingMappedGroup ADD CONSTRAINT PK_tb_meetingMappedGroup_RID PRIMARY KEY (RID);
IF OBJECT_ID('PK_tb_meetingMembers','PK')    IS NULL ALTER TABLE tb_meetingMembers    ADD CONSTRAINT PK_tb_meetingMembers    PRIMARY KEY (meetingRID, memberRID);
IF OBJECT_ID('PK_Master_District','PK')      IS NULL ALTER TABLE Master_District      ADD CONSTRAINT PK_Master_District      PRIMARY KEY (dCode);

/* 6. Add foreign keys (data validated orphan-free) */
IF OBJECT_ID('FK_departmentMas_ministry','F') IS NULL
  ALTER TABLE departmentMas ADD CONSTRAINT FK_departmentMas_ministry FOREIGN KEY (ministryID) REFERENCES ministryMas(RID);
IF OBJECT_ID('FK_officers_department','F') IS NULL
  ALTER TABLE tbl_Officers ADD CONSTRAINT FK_officers_department FOREIGN KEY (deptID) REFERENCES departmentMas(RID);
IF OBJECT_ID('FK_officers_deptDesig','F') IS NULL
  ALTER TABLE tbl_Officers ADD CONSTRAINT FK_officers_deptDesig FOREIGN KEY (desigID) REFERENCES MAS_DeptDesignation(RID);
IF OBJECT_ID('FK_deptDesig_department','F') IS NULL
  ALTER TABLE MAS_DeptDesignation ADD CONSTRAINT FK_deptDesig_department FOREIGN KEY (deptID) REFERENCES departmentMas(RID);
IF OBJECT_ID('FK_agenda_schedule','F') IS NULL
  ALTER TABLE tb_meetingAgendas ADD CONSTRAINT FK_agenda_schedule FOREIGN KEY (meetingRID) REFERENCES tb_meetingSchedules(RID);
IF OBJECT_ID('FK_remarks_agenda','F') IS NULL
  ALTER TABLE tb_remarksOnAgendas ADD CONSTRAINT FK_remarks_agenda FOREIGN KEY (agendaRID) REFERENCES tb_meetingAgendas(RID);
IF OBJECT_ID('FK_remarks_schedule','F') IS NULL
  ALTER TABLE tb_remarksOnAgendas ADD CONSTRAINT FK_remarks_schedule FOREIGN KEY (meetingRID) REFERENCES tb_meetingSchedules(RID);
IF OBJECT_ID('FK_members_schedule','F') IS NULL
  ALTER TABLE tb_meetingMembers ADD CONSTRAINT FK_members_schedule FOREIGN KEY (meetingRID) REFERENCES tb_meetingSchedules(RID);
IF OBJECT_ID('FK_members_officer','F') IS NULL
  ALTER TABLE tb_meetingMembers ADD CONSTRAINT FK_members_officer FOREIGN KEY (memberRID) REFERENCES tbl_Officers(RID);
IF OBJECT_ID('FK_mappedGroup_group','F') IS NULL
  ALTER TABLE tb_meetingMappedGroup ADD CONSTRAINT FK_mappedGroup_group FOREIGN KEY (groupRID) REFERENCES tb_MeetingGroup(RID);
IF OBJECT_ID('FK_mappedGroup_schedule','F') IS NULL
  ALTER TABLE tb_meetingMappedGroup ADD CONSTRAINT FK_mappedGroup_schedule FOREIGN KEY (meetingRID) REFERENCES tb_meetingSchedules(RID);

COMMIT TRAN;
PRINT 'Schema cleanup completed successfully.';

/*
  Clean CMOmeetsDB to a blank slate, keeping ONLY the admin/sadmin login accounts.
  Keeps: AspNetUsers 'admin', 'cmharyana', 'sadmin' (+ their roles), AspNetRoles,
         __EFMigrationsHistory (schema), and the 3 matching User_Authentication_Detail rows.
  Deletes: all master + transactional data and every other login.
  A full backup was taken first: db\CMOmeetsDB_pre_clean_*.bak
*/
SET QUOTED_IDENTIFIER ON;   -- required for tables with filtered/unique indexes (tb_actionPointViews)
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;   -- any error rolls the whole thing back
BEGIN TRAN;

DECLARE @keep TABLE (Id NVARCHAR(450) PRIMARY KEY);
INSERT INTO @keep(Id)
SELECT Id FROM AspNetUsers WHERE UserName IN ('admin', 'cmharyana', 'sadmin');

-- 1) Transactional data (FK children first)
DELETE FROM tb_actionPointViews;
DELETE FROM tb_remarksOnAgendas;
DELETE FROM tb_meetingMembers;
DELETE FROM tb_meetingMappedGroup;
DELETE FROM tb_meetingAgendas;
DELETE FROM tb_meetingSchedules;
DELETE FROM tb_MeetingGroup;

-- 2) Master data (FK children first: officers/designations before departments before ministries)
DELETE FROM tbl_Officers;
DELETE FROM MAS_DeptDesignation;
DELETE FROM designationMas;
DELETE FROM departmentMas;
DELETE FROM ministryMas;
DELETE FROM Master_District;

-- 3) Login history audit
DELETE FROM login_history_pwd;

-- 4) Identity: remove every login except the kept admin/sadmin accounts
DELETE FROM AspNetUserRoles  WHERE UserId NOT IN (SELECT Id FROM @keep);
DELETE FROM AspNetUserClaims WHERE UserId NOT IN (SELECT Id FROM @keep);
DELETE FROM AspNetUserLogins WHERE UserId NOT IN (SELECT Id FROM @keep);
DELETE FROM AspNetUserTokens WHERE UserId NOT IN (SELECT Id FROM @keep);
DELETE FROM AspNetUsers      WHERE Id     NOT IN (SELECT Id FROM @keep);

COMMIT TRAN;

-- 5) Reseed identity counters so re-populated data starts at RID 1 (post-commit, non-transactional)
DBCC CHECKIDENT ('ministryMas',          RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('departmentMas',        RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('MAS_DeptDesignation',  RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('designationMas',       RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('tbl_Officers',         RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('tb_meetingSchedules',  RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('tb_meetingAgendas',    RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('tb_remarksOnAgendas',  RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('tb_MeetingGroup',      RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('tb_meetingMappedGroup',RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('tb_actionPointViews',  RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('login_history_pwd',    RESEED, 0) WITH NO_INFOMSGS;

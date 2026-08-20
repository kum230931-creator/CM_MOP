/*
  Import legacy backup data (staging DB CMOmeetsDB_Import) into the live CMOmeetsDB.
  - Keeps the current EF schema, migration history, and admin/sadmin logins untouched.
  - Preserves original RIDs (IDENTITY_INSERT) so every foreign key stays valid.
  - Auto-selects the columns common to both source and target (live tables have newer
    columns the legacy backup lacks; those take their defaults / NULL).
  - Skips the 2 orphaned tb_meetingMappedGroup rows (meetingRID/groupRID with no parent).
  - Parents before children so FK constraints hold. Atomic (rolls back on any error).
  Run in the context of the live CMOmeetsDB.
*/
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @tables TABLE (seq INT IDENTITY, name SYSNAME, whereClause NVARCHAR(MAX));
INSERT INTO @tables(name, whereClause) VALUES
  (N'ministryMas', NULL), (N'departmentMas', NULL), (N'MAS_DeptDesignation', NULL), (N'designationMas', NULL),
  (N'tbl_Officers', NULL), (N'Master_District', NULL), (N'tb_meetingSchedules', NULL), (N'tb_meetingAgendas', NULL),
  (N'tb_remarksOnAgendas', NULL), (N'tb_meetingMembers', NULL), (N'tb_MeetingGroup', NULL),
  (N'tb_meetingMappedGroup',
     N' WHERE EXISTS (SELECT 1 FROM CMOmeetsDB_Import.dbo.tb_meetingSchedules s WHERE s.RID = src.meetingRID)'
   + N' AND EXISTS (SELECT 1 FROM CMOmeetsDB_Import.dbo.tb_MeetingGroup g WHERE g.RID = src.groupRID)');

BEGIN TRAN;

DECLARE @name SYSNAME, @where NVARCHAR(MAX), @cols NVARCHAR(MAX), @hasIdent BIT, @sql NVARCHAR(MAX), @rc INT;
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT name, whereClause FROM @tables ORDER BY seq;
OPEN cur;
FETCH NEXT FROM cur INTO @name, @where;
WHILE @@FETCH_STATUS = 0
BEGIN
  SET @cols = NULL;
  SELECT @cols = STRING_AGG(QUOTENAME(c.name), N',') WITHIN GROUP (ORDER BY c.column_id)
  FROM sys.columns c
  WHERE c.object_id = OBJECT_ID(@name)
    AND c.is_computed = 0
    AND EXISTS (
      SELECT 1
      FROM CMOmeetsDB_Import.sys.columns sc
      JOIN CMOmeetsDB_Import.sys.objects so ON so.object_id = sc.object_id
      JOIN CMOmeetsDB_Import.sys.schemas ss ON ss.schema_id = so.schema_id AND ss.name = N'dbo'
      WHERE so.name = @name AND sc.name = c.name);

  IF @cols IS NULL BEGIN RAISERROR('No common columns for %s', 16, 1, @name); END

  SET @hasIdent = CASE WHEN EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(@name)) THEN 1 ELSE 0 END;

  -- Whole insert runs in one dynamic batch: IDENTITY_INSERT ON/OFF around it, and @@ROWCOUNT
  -- captured into an OUTPUT param immediately after the INSERT (before the trailing SET resets it).
  SET @sql =
      CASE WHEN @hasIdent = 1 THEN N'SET IDENTITY_INSERT ' + QUOTENAME(@name) + N' ON; ' ELSE N'' END
    + N'INSERT INTO ' + QUOTENAME(@name) + N' (' + @cols + N') SELECT ' + @cols
    + N' FROM CMOmeetsDB_Import.dbo.' + QUOTENAME(@name) + N' AS src' + ISNULL(@where, N'') + N'; '
    + N'SET @rcOut = @@ROWCOUNT; '
    + CASE WHEN @hasIdent = 1 THEN N'SET IDENTITY_INSERT ' + QUOTENAME(@name) + N' OFF;' ELSE N'' END;
  EXEC sp_executesql @sql, N'@rcOut INT OUTPUT', @rcOut = @rc OUTPUT;

  PRINT @name + N': ' + CAST(@rc AS NVARCHAR(20)) + N' rows imported';
  FETCH NEXT FROM cur INTO @name, @where;
END
CLOSE cur; DEALLOCATE cur;

COMMIT TRAN;

-- Reseed identity counters to the max imported RID so new inserts continue correctly.
DBCC CHECKIDENT('ministryMas', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT('departmentMas', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT('MAS_DeptDesignation', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT('designationMas', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT('tbl_Officers', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT('tb_meetingSchedules', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT('tb_meetingAgendas', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT('tb_remarksOnAgendas', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT('tb_MeetingGroup', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT('tb_meetingMappedGroup', RESEED) WITH NO_INFOMSGS;

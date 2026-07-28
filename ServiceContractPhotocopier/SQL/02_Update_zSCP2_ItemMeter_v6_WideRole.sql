SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v6: widen MeterRole CHAR(2) -> VARCHAR(10). The role list grew past BK/CL/NA (user decision
-- 2026-07-27: RENTAL / WAIVE / COMMIT are real roles now) and 'RENTAL' no longer fits — saves
-- died with "String or binary data would be truncated ... Truncated value: 'RE'".
-- EVERYTHING hanging off the column must step aside for the ALTER and is recreated after:
-- the (auto-named) DEFAULT, the CK role check (rewritten with the new role set), and the three
-- role indexes. Idempotent: guarded on the current column width.
IF EXISTS (SELECT 1 FROM sys.columns c
           WHERE c.object_id = OBJECT_ID('dbo.zSCP2_ItemMeter')
             AND c.name = 'MeterRole' AND c.max_length < 10)
BEGIN
    DECLARE @df sysname;
    SELECT @df = dc.name FROM sys.default_constraints dc
    JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.zSCP2_ItemMeter') AND c.name = 'MeterRole';
    IF @df IS NOT NULL EXEC('ALTER TABLE dbo.zSCP2_ItemMeter DROP CONSTRAINT [' + @df + ']');

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.zSCP2_ItemMeter') AND name = 'CK_zSCP2_ItemMeter_Role')
        ALTER TABLE dbo.zSCP2_ItemMeter DROP CONSTRAINT CK_zSCP2_ItemMeter_Role;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.zSCP2_ItemMeter') AND name = 'UX_zSCP2_ItemMeter_BK')
        DROP INDEX UX_zSCP2_ItemMeter_BK ON dbo.zSCP2_ItemMeter;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.zSCP2_ItemMeter') AND name = 'UX_zSCP2_ItemMeter_CL')
        DROP INDEX UX_zSCP2_ItemMeter_CL ON dbo.zSCP2_ItemMeter;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.zSCP2_ItemMeter') AND name = 'IX_zSCP2_ItemMeter_RoleCover')
        DROP INDEX IX_zSCP2_ItemMeter_RoleCover ON dbo.zSCP2_ItemMeter;

    ALTER TABLE dbo.zSCP2_ItemMeter ALTER COLUMN MeterRole VARCHAR(10) NOT NULL;

    -- CHAR(2) rows arrive space-padded on short values — normalise.
    UPDATE dbo.zSCP2_ItemMeter SET MeterRole = RTRIM(MeterRole) WHERE MeterRole <> RTRIM(MeterRole);

    ALTER TABLE dbo.zSCP2_ItemMeter ADD CONSTRAINT DF_zSCP2_ItemMeter_Role DEFAULT('NA') FOR MeterRole;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.zSCP2_ItemMeter') AND name = 'CK_zSCP2_ItemMeter_Role')
    ALTER TABLE dbo.zSCP2_ItemMeter ADD CONSTRAINT CK_zSCP2_ItemMeter_Role
        CHECK (MeterRole IN ('BK','CL','NA','RENTAL','WAIVE','COMMIT'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.zSCP2_ItemMeter') AND name = 'UX_zSCP2_ItemMeter_BK')
    CREATE UNIQUE NONCLUSTERED INDEX UX_zSCP2_ItemMeter_BK
        ON dbo.zSCP2_ItemMeter (ItemKey, MachineSerialNo) WHERE MeterRole = 'BK';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.zSCP2_ItemMeter') AND name = 'UX_zSCP2_ItemMeter_CL')
    CREATE UNIQUE NONCLUSTERED INDEX UX_zSCP2_ItemMeter_CL
        ON dbo.zSCP2_ItemMeter (ItemKey, MachineSerialNo) WHERE MeterRole = 'CL';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.zSCP2_ItemMeter') AND name = 'IX_zSCP2_ItemMeter_RoleCover')
    CREATE NONCLUSTERED INDEX IX_zSCP2_ItemMeter_RoleCover
        ON dbo.zSCP2_ItemMeter (ItemKey, MeterRole) INCLUDE (MeterTypeCode);
GO

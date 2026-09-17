SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v7: allow Source='INTERBILL' on zSCP2_MeterEntry.
--
-- A reading brought across from another account book is not any of the four the column already knows.
-- It was not keyed here (MANUAL), it did not come off a machine into THIS book (ONLINE / OFFLINE),
-- and it was not invented by the stamp path (INVOICE). It was read by another company, in their book,
-- on their machine, and taken by this one.
--
-- Naming it properly is not tidiness. Where a reading came from is the first question asked when a
-- customer disputes a copy count, and calling it ONLINE would say this book read the machine itself --
-- which is the one thing it cannot do for an inter-billed counter.
--
-- The column is CHAR(8), which is one character short of the word, so it is widened first. CHAR also
-- means every value stored so far is padded with spaces ('ONLINE  '); the trim below removes that, so
-- code that compares the value in C# -- where 'ONLINE  ' is not 'ONLINE' -- gets what it expects.
--
-- Idempotent: guarded on the column type, and the constraint is dropped and rebuilt.

IF EXISTS (SELECT 1 FROM sys.columns c
            JOIN sys.types t ON t.user_type_id = c.user_type_id
           WHERE c.object_id = OBJECT_ID(N'[dbo].[zSCP2_MeterEntry]')
             AND c.name = 'Source' AND (t.name = 'char' OR c.max_length < 20))
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_zSCP2_MeterEntry_Source')
        ALTER TABLE [dbo].[zSCP2_MeterEntry] DROP CONSTRAINT [CK_zSCP2_MeterEntry_Source];

    -- Source rides along as an INCLUDE column on the period covering index, and a column cannot be
    -- altered while an index carries it. Dropped here and recreated by
    -- 02_CreateIndex_zSCP2_Performance.sql, which runs later in the same load.
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_zSCP2_MeterEntry_PeriodCover'
                 AND object_id = OBJECT_ID(N'[dbo].[zSCP2_MeterEntry]'))
        DROP INDEX [IX_zSCP2_MeterEntry_PeriodCover] ON [dbo].[zSCP2_MeterEntry];

    -- ...and the DEFAULT holds it too. Its name was left to SQL Server when the table was made
    -- ('DF__zSCP2_Met__Sourc__66B60677' here), so it is looked up rather than named: the same book
    -- restored elsewhere carries a different generated name.
    DECLARE @df SYSNAME = (SELECT d.name FROM sys.default_constraints d
                             JOIN sys.columns c ON c.object_id = d.parent_object_id
                                               AND c.column_id = d.parent_column_id
                            WHERE d.parent_object_id = OBJECT_ID(N'[dbo].[zSCP2_MeterEntry]')
                              AND c.name = 'Source');
    IF @df IS NOT NULL
    BEGIN
        DECLARE @cmd NVARCHAR(400) =
            N'ALTER TABLE [dbo].[zSCP2_MeterEntry] DROP CONSTRAINT ' + QUOTENAME(@df);
        EXEC sp_executesql @cmd;
    END

    ALTER TABLE [dbo].[zSCP2_MeterEntry] ALTER COLUMN [Source] VARCHAR(20) NOT NULL;

    ALTER TABLE [dbo].[zSCP2_MeterEntry]
        ADD CONSTRAINT [DF_zSCP2_MeterEntry_Source] DEFAULT ('MANUAL') FOR [Source];
END
GO

-- Take the CHAR padding off what is already stored.
--
-- Its own statement, and compared with DATALENGTH rather than '<>', because T-SQL string comparison
-- ignores trailing spaces: 'ONLINE  ' = 'ONLINE' is TRUE, so the obvious WHERE clause matches nothing
-- and the trim quietly does not happen. DATALENGTH counts the bytes and sees the difference.
UPDATE [dbo].[zSCP2_MeterEntry]
   SET [Source] = RTRIM([Source])
 WHERE DATALENGTH([Source]) <> DATALENGTH(RTRIM([Source]));
GO

-- Guarded on what the constraint already SAYS, not on whether it exists. Every one of these files
-- runs on every plugin load, so an unconditional drop-and-recreate is a constraint that is briefly
-- absent thousands of times over a book's life -- and, as v5 found out the hard way, one that can be
-- rebuilt narrower than the data already stored.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                WHERE name = 'CK_zSCP2_MeterEntry_Source'
                  AND parent_object_id = OBJECT_ID(N'[dbo].[zSCP2_MeterEntry]')
                  AND definition LIKE '%INTERBILL%')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints
                WHERE name = 'CK_zSCP2_MeterEntry_Source'
                  AND parent_object_id = OBJECT_ID(N'[dbo].[zSCP2_MeterEntry]'))
        ALTER TABLE [dbo].[zSCP2_MeterEntry] DROP CONSTRAINT [CK_zSCP2_MeterEntry_Source];

    ALTER TABLE [dbo].[zSCP2_MeterEntry] WITH CHECK
        ADD CONSTRAINT [CK_zSCP2_MeterEntry_Source]
        CHECK ([Source] IN ('MANUAL','ONLINE','OFFLINE','INVOICE','INTERBILL'));
END
GO

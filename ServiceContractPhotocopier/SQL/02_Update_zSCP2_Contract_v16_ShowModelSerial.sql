-- zSCP2_Contract v16 -- "show the model" and "show the serial numbers" become two questions.
--
-- They used to be one column with three answers:
--
--     B  both        the duty label on the charge line, then Model, then S/N
--     M  model only  Model, then S/N          <- still prints serials
--     L  label only  the duty label, nothing else
--
-- Which left one shape unsayable: MODEL YES, SERIALS NO. It is the shape a large fleet needs --
-- MBJB's 55 cpm line covers thirty-six machines, and naming all thirty-six serial numbers turns
-- one invoice line into a paragraph nobody reads. Of 37,179 rental lines in the old book, 247
-- carried a serial; of 114,361 meter lines, twelve did. The serial block is ours, not theirs.
--
-- So the two facts get a column each. MachineLineShows keeps its own job -- whether the DUTY LABEL
-- rides on the charge line -- and stops answering for the other two.
--
-- Backfilled so no existing invoice changes: a contract that said "label only" printed neither,
-- everything else printed both.

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_Contract]') AND name = 'ShowModelOnLine')
BEGIN
    ALTER TABLE [dbo].[zSCP2_Contract]
        ADD [ShowModelOnLine] [char](1) NOT NULL CONSTRAINT DF_zSCP2_Contract_ShowModelOnLine DEFAULT ('Y');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_Contract]') AND name = 'ShowSerialOnLine')
BEGIN
    ALTER TABLE [dbo].[zSCP2_Contract]
        ADD [ShowSerialOnLine] [char](1) NOT NULL CONSTRAINT DF_zSCP2_Contract_ShowSerialOnLine DEFAULT ('Y');
END
GO

-- One-time backfill from the old three-value column. Guarded on the marker below so re-running the
-- migration cannot undo a choice somebody has since made on the screen.
--
-- The UPDATE is inside EXEC on purpose. SQL Server compiles a whole batch before it runs any of it,
-- so a plain UPDATE naming MachineLineShows fails on a book where that column does not exist yet --
-- even though the IF around it would have skipped it. That is exactly a NEW book: MachineLineShows
-- is created by v15, and on a fresh install nothing has created it by the time this line is read.
-- Every book we develop against has had the column for months, which is why this never showed
-- until the plugin went on to a customer's book and refused to load (18/9).
--
-- No column means no contract ever chose "label only", so there is nothing to carry across: the
-- defaults above ('Y', 'Y') are already the right answer, and the marker is still set so this
-- never runs again.
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties
                WHERE major_id = OBJECT_ID(N'[dbo].[zSCP2_Contract]')
                  AND name = N'ATP_ShowModelSerial_Backfilled')
BEGIN
    IF COL_LENGTH(N'dbo.zSCP2_Contract', N'MachineLineShows') IS NOT NULL
        EXEC(N'UPDATE dbo.zSCP2_Contract
                  SET ShowModelOnLine  = CASE WHEN UPPER(ISNULL(MachineLineShows,''B'')) = ''L'' THEN ''N'' ELSE ''Y'' END,
                      ShowSerialOnLine = CASE WHEN UPPER(ISNULL(MachineLineShows,''B'')) = ''L'' THEN ''N'' ELSE ''Y'' END');

    EXEC sys.sp_addextendedproperty
         @name = N'ATP_ShowModelSerial_Backfilled', @value = N'v16',
         @level0type = N'SCHEMA', @level0name = N'dbo',
         @level1type = N'TABLE',  @level1name = N'zSCP2_Contract';
END
GO

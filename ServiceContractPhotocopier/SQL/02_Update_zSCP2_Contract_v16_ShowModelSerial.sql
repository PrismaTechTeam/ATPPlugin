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
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties
                WHERE major_id = OBJECT_ID(N'[dbo].[zSCP2_Contract]')
                  AND name = N'ATP_ShowModelSerial_Backfilled')
BEGIN
    UPDATE dbo.zSCP2_Contract
       SET ShowModelOnLine  = CASE WHEN UPPER(ISNULL(MachineLineShows,'B')) = 'L' THEN 'N' ELSE 'Y' END,
           ShowSerialOnLine = CASE WHEN UPPER(ISNULL(MachineLineShows,'B')) = 'L' THEN 'N' ELSE 'Y' END;

    EXEC sys.sp_addextendedproperty
         @name = N'ATP_ShowModelSerial_Backfilled', @value = N'v16',
         @level0type = N'SCHEMA', @level0name = N'dbo',
         @level1type = N'TABLE',  @level1name = N'zSCP2_Contract';
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v8: remember that somebody typed the reading date themselves (feedback ATP-5).
--
-- ReadingDate is the day the counter was read. Until now nobody could say when that was: a manual
-- key-in stamped GETDATE(), so a meter read on the 10th and keyed on the 23rd was recorded as the
-- 23rd -- and that date is what the invoice prints and what the meter transaction carries, so next
-- period's "Last Read Date" inherited the mistake too. The operator can now correct it.
--
-- A corrected date has to be told apart from the stamped one, or the next Fetch quietly throws it
-- away: when the API returns the SAME number the row is restaged with the API's audit date. This
-- flag is what makes that decision possible --
--
--   ReadingDateEdited = 'Y'  the date was typed by a person; keep it while the READING it belongs
--                            to does not change (a different reading is a different reading, and
--                            takes a fresh date)
--   ReadingDateEdited = 'N'  the date came with the reading (stamped at key-in, or the API's audit
--                            date) and may be overwritten freely
--
-- Idempotent: guarded on the column, and the constraint is only rebuilt when it is missing.

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_MeterEntry]')
                  AND name = 'ReadingDateEdited')
BEGIN
    ALTER TABLE [dbo].[zSCP2_MeterEntry]
        ADD [ReadingDateEdited] CHAR(1) NOT NULL
            CONSTRAINT [DF_zSCP2_MeterEntry_ReadingDateEdited] DEFAULT ('N');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                WHERE name = 'CK_zSCP2_MeterEntry_ReadingDateEdited'
                  AND parent_object_id = OBJECT_ID(N'[dbo].[zSCP2_MeterEntry]'))
BEGIN
    ALTER TABLE [dbo].[zSCP2_MeterEntry] WITH CHECK
        ADD CONSTRAINT [CK_zSCP2_MeterEntry_ReadingDateEdited]
        CHECK ([ReadingDateEdited] IN ('Y','N'));
END
GO

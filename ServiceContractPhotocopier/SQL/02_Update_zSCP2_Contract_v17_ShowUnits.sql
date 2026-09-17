-- zSCP2_Contract v17 -- and the unit count becomes a question too.
--
-- "(2 UNIT)" under a charge answers "how many machines is this line for". On a RENTAL line the Qty
-- column already answers it, so the engine drops it there. On a METER line it does not -- Qty is
-- 10,239 copies -- so the count is the only thing on the line that says two machines printed them.
--
-- Some customers want it, some read it as clutter. It joins the model and the serials as a tick of
-- its own rather than a rule somebody has to remember.
--
-- Default 'Y': every contract keeps printing what it printed.

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_Contract]') AND name = 'ShowUnitsOnLine')
BEGIN
    ALTER TABLE [dbo].[zSCP2_Contract]
        ADD [ShowUnitsOnLine] [char](1) NOT NULL CONSTRAINT DF_zSCP2_Contract_ShowUnitsOnLine DEFAULT ('Y');
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v5: allow Source='INVOICE' on zSCP2_MeterEntry.
-- When an invoice is generated for a meter that was never STAGED, the stamp path (MeterInvoiceGenerator
-- WriteMeterTrans / WriteNoCharge) finds no staging row to UPDATE (@@ROWCOUNT=0) and INSERTs a fresh
-- entry with Source='INVOICE'. This is the NORMAL case for rentals/flat meters (they are never fetched
-- or keyed - fetch skips role<>BK/CL and there is no manual key-in), and also for any usage meter whose
-- reading was typed straight into the grid rather than staged. The original CHECK only allowed
-- MANUAL/ONLINE/OFFLINE, so that INSERT violated the constraint, the stamp transaction rolled back, and
-- the invoice was left SAVED but the period UN-STAMPED -> the next Generate billed the same period again
-- (duplicate invoice). Widen the constraint to accept 'INVOICE'.
--
-- It runs on EVERY plugin load, and that is what the guard below is about.
--
-- Written first as an unconditional drop-and-recreate, it later broke the whole load. v7 widens the
-- same constraint again for 'INTERBILL' and runs AFTER this file; so on the next load this one dropped
-- the wide constraint, tried to put the narrow one back, and WITH CHECK refused because rows already
-- said INTERBILL. The plugin then reported "failed to initialize database schema" and every migration
-- after this point was skipped.
--
-- So it asks what the constraint already SAYS rather than whether it exists. A constraint that already
-- permits INVOICE -- this file's own work, or a later file's wider list -- is left exactly alone. A
-- migration that widens something must never be able to narrow it.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                WHERE name = 'CK_zSCP2_MeterEntry_Source'
                  AND parent_object_id = OBJECT_ID(N'[dbo].[zSCP2_MeterEntry]')
                  AND definition LIKE '%INVOICE%')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints
                WHERE name = 'CK_zSCP2_MeterEntry_Source'
                  AND parent_object_id = OBJECT_ID(N'[dbo].[zSCP2_MeterEntry]'))
        ALTER TABLE [dbo].[zSCP2_MeterEntry] DROP CONSTRAINT [CK_zSCP2_MeterEntry_Source];

    ALTER TABLE [dbo].[zSCP2_MeterEntry] WITH CHECK
        ADD CONSTRAINT [CK_zSCP2_MeterEntry_Source] CHECK ([Source] IN ('MANUAL','ONLINE','OFFLINE','INVOICE'));
END
GO

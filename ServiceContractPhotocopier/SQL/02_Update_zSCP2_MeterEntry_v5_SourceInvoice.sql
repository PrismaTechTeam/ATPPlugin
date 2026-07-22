SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v5: allow Source='INVOICE' on zSCP2_MeterEntry.
-- When an invoice is generated for a meter that was never STAGED, the stamp path (MeterInvoiceGenerator
-- WriteMeterTrans / WriteNoCharge) finds no staging row to UPDATE (@@ROWCOUNT=0) and INSERTs a fresh
-- entry with Source='INVOICE'. This is the NORMAL case for rentals/flat meters (they are never fetched
-- or keyed — fetch skips role<>BK/CL and there is no manual key-in), and also for any usage meter whose
-- reading was typed straight into the grid rather than staged. The original CHECK only allowed
-- MANUAL/ONLINE/OFFLINE, so that INSERT violated the constraint, the stamp transaction rolled back, and
-- the invoice was left SAVED but the period UN-STAMPED -> the next Generate billed the same period again
-- (duplicate invoice). Widen the constraint to accept 'INVOICE'. Idempotent.
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_zSCP2_MeterEntry_Source')
    ALTER TABLE [dbo].[zSCP2_MeterEntry] DROP CONSTRAINT [CK_zSCP2_MeterEntry_Source];
GO

ALTER TABLE [dbo].[zSCP2_MeterEntry] WITH CHECK
    ADD CONSTRAINT [CK_zSCP2_MeterEntry_Source] CHECK ([Source] IN ('MANUAL','ONLINE','OFFLINE','INVOICE'));
GO

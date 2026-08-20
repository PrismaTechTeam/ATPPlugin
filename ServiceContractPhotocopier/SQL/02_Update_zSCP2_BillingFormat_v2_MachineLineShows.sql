SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v2: what the machine line under a charge names -- the model, the duty label, or both.
--
--   B  MONTHLY RENTAL (13/36)  MEDIUM HEAVY DUTY "50 ppm"      (the default: both)
--      MODEL:imageFORCE C5150  (2 UNIT)
--
--   M  MONTHLY RENTAL (13/36)                                  (model only)
--      MODEL:imageFORCE C5150  (2 UNIT)
--
--   L  MONTHLY RENTAL (13/36)                                  (label only)
--      MEDIUM HEAVY DUTY "50 ppm"  (2 UNIT)
--
-- 'L' is closest to what this business printed before: its old invoices read
-- MEDIUM HEAVY DUTY ''45cpm'' - MONTHLY RENTAL - and never named the model in the description
-- at all, because the model was carried by the item code instead.
--
-- It belongs on the format, not the contract: it answers "what does this invoice look like",
-- which is the one question a Billing Format exists to answer. Defaulting to 'B' leaves every
-- existing format printing exactly what it prints today.

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_BillingFormat]')
                  AND name = 'MachineLineShows')
BEGIN
    ALTER TABLE [dbo].[zSCP2_BillingFormat]
        ADD [MachineLineShows] CHAR(1) NOT NULL CONSTRAINT [DF_zSCP2_BillingFormat_MachineLineShows] DEFAULT 'B';
END
GO

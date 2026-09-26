SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v9: how a meter prices its tier ladder, per meter (feedback ATP-3, moved 26/9).
--
--   TierMode = 'T'  WHOLE MONTH AT THE TIER REACHED -- every billed copy at the rate of the tier the
--                   month's copies reach. The rule every meter had before; the default.
--   TierMode = 'I'  EACH TIER AT ITS OWN RATE -- the copies after Free Qty are laid over the tiers,
--                   each tier's share at its own rate: 1,580 over "up to 1,000 at 0.023, then 0.021"
--                   is 1,000 x 0.023 + 580 x 0.021 = 35.18.
--
-- It was one setting on the contract header (zSCP2_Contract.TierMode, v19). The user asked for it
-- where the tiers are set, so each machine can take its own: it now lives on the meter, and the
-- contract's column is no longer read. Each meter starts on what its contract said, so no invoice
-- changes; the copy runs once, with the column.
--
-- Idempotent: guarded on the column; the constraint only when missing.

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_ItemMeter]') AND name = 'TierMode')
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    ALTER TABLE [dbo].[zSCP2_ItemMeter]
        ADD [TierMode] CHAR(1) NOT NULL
            CONSTRAINT [DF_zSCP2_ItemMeter_TierMode] DEFAULT ('T');

    -- Dynamic: the column does not exist when this batch compiles.
    IF COL_LENGTH(N'dbo.zSCP2_Contract', N'TierMode') IS NOT NULL
        EXEC (N'UPDATE m SET m.TierMode = ''I''
                  FROM [dbo].[zSCP2_ItemMeter] m
                  JOIN [dbo].[zSCP2_Item] i ON i.ItemKey = m.ItemKey
                  JOIN [dbo].[zSCP2_Contract] c ON c.ContractKey = i.ContractKey
                 WHERE c.TierMode = ''I''');

    COMMIT TRANSACTION;
    SET XACT_ABORT OFF;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                WHERE name = 'CK_zSCP2_ItemMeter_TierMode'
                  AND parent_object_id = OBJECT_ID(N'[dbo].[zSCP2_ItemMeter]'))
BEGIN
    ALTER TABLE [dbo].[zSCP2_ItemMeter] WITH CHECK
        ADD CONSTRAINT [CK_zSCP2_ItemMeter_TierMode] CHECK ([TierMode] IN ('T','I'));
END
GO

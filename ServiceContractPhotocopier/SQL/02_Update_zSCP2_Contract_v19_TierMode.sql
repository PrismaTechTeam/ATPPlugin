SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v19: how a contract prices its tier ladders (feedback ATP-3).
--
--   TierMode = 'T'  THRESHOLD -- the month's copies decide which band applies, and every billed copy
--                   is charged at that band's rate. The rule every contract had before; the default.
--   TierMode = 'I'  BAND BY BAND -- the copies left after the meter's Free Qty are laid over the
--                   bands, each band's share at its own rate, and the invoice prints one row per band.
--                   1,648 copies, 100 free, "1,000 at 0.024 then 0.020" -> 1,000 x 0.024 + 548 x 0.020
--                   = 34.96 (threshold: 1,548 x 0.020 = 30.96).
--
-- Per contract, as the customer asked: some deals are struck one way, some the other.
--
-- Once, when the column arrives, two Free Qty values that were never used are cleared, because
-- from this release they count and would give copies away that nobody agreed to:
--   * a meter on a ladder that starts at a PAID rate: the old screen greyed its Free Qty and the
--     engine ignored it. (A ladder that starts at 0.00 needs nothing here: the next script moves
--     that band into Free Qty, overwriting the cell with what the band gave.)
--   * a machine priced by its copy group's ladder (Billing Setup's group tiers, new layout only):
--     the group's free copies were the ladder's 0.00 band and the machines' own Free Qty was
--     ignored; the group now pools its machines' Free Qty.
-- Rental and waive meters are left alone -- their Free Qty is free MONTHS.
-- It runs inside the column guard and in one transaction with the column, so it happens on the
-- first load of this release and never again: a Free Qty keyed in afterwards is the user's and stays.
--
-- Idempotent: guarded on the column; the constraint only when missing.

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_Contract]') AND name = 'TierMode')
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    ALTER TABLE [dbo].[zSCP2_Contract]
        ADD [TierMode] CHAR(1) NOT NULL
            CONSTRAINT [DF_zSCP2_Contract_TierMode] DEFAULT ('T');

    -- (1) the meter's ladder: its own tiers, else its own code, else its meter type's code
    UPDATE m SET m.FOCQty = 0
    FROM [dbo].[zSCP2_ItemMeter] m
    LEFT JOIN [dbo].[zSCP_MeterType] mt ON mt.MeterTypeCode = m.MeterTypeCode
    WHERE ISNULL(m.FOCQty, 0) <> 0
      AND ISNULL(mt.IsFlatCharge, 'N') <> 'Y' AND ISNULL(mt.IsRentalWaive, 'N') <> 'Y'
      AND (
            (EXISTS (SELECT 1 FROM [dbo].[zSCP2_ItemMeterPrice] p WHERE p.ItemMeterKey = m.ItemMeterKey)
             AND (SELECT TOP 1 p.UnitPrice FROM [dbo].[zSCP2_ItemMeterPrice] p
                   WHERE p.ItemMeterKey = m.ItemMeterKey ORDER BY p.MeterReading) <> 0)
         OR (NOT EXISTS (SELECT 1 FROM [dbo].[zSCP2_ItemMeterPrice] p WHERE p.ItemMeterKey = m.ItemMeterKey)
             AND (SELECT TOP 1 i.UnitPrice FROM [dbo].[zSCP_MeterMultiPriceItem] i
                   WHERE i.MeterMultiPriceCode = CASE WHEN ISNULL(m.MeterMultiPriceCode, '') <> ''
                                                      THEN m.MeterMultiPriceCode ELSE mt.MeterMultiPriceCode END
                   ORDER BY i.MeterReading) <> 0)
          );

    -- (2) a machine priced by its copy group's ladder: a black or colour meter with no ladder of its
    -- own, on a new-layout contract, whose machine is in a copy group that agreed a ladder for that
    -- colour -- as ScpGroupLadder.Apply picks them, and only where that ladder resolves.
    UPDATE m SET m.FOCQty = 0
    FROM [dbo].[zSCP2_ItemMeter] m
    JOIN [dbo].[zSCP2_Item] it ON it.ItemKey = m.ItemKey
    JOIN [dbo].[zSCP2_Contract] c ON c.ContractKey = it.ContractKey
    JOIN [dbo].[zSCP2_ContractRentalPrice] g
      ON g.ContractKey = it.ContractKey AND ISNULL(g.Side, 'R') = 'M'
     AND LTRIM(RTRIM(g.GroupCode)) = LTRIM(RTRIM(ISNULL(it.MergeGroupCodeMeter, '')))
    LEFT JOIN [dbo].[zSCP_MeterType] mt ON mt.MeterTypeCode = m.MeterTypeCode
    WHERE ISNULL(m.FOCQty, 0) <> 0
      AND LTRIM(RTRIM(ISNULL(it.MergeGroupCodeMeter, ''))) <> ''
      AND (ISNULL(c.UseNewLayout, 'N') = 'Y' OR ISNULL(c.BillingFormatCode, '') <> '')
      AND ISNULL(mt.IsFlatCharge, 'N') <> 'Y' AND ISNULL(mt.IsRentalWaive, 'N') <> 'Y'
      AND ISNULL(m.MeterMultiPriceCode, '') = '' AND ISNULL(mt.MeterMultiPriceCode, '') = ''
      AND NOT EXISTS (SELECT 1 FROM [dbo].[zSCP2_ItemMeterPrice] p WHERE p.ItemMeterKey = m.ItemMeterKey)
      AND (
            (m.MeterRole = 'BK' AND ISNULL(g.LadderBk, '') <> ''
             AND (CHARINDEX('|', g.LadderBk) > 0
                  OR EXISTS (SELECT 1 FROM [dbo].[zSCP_MeterMultiPriceItem] x WHERE x.MeterMultiPriceCode = LTRIM(RTRIM(g.LadderBk)))))
         OR (m.MeterRole = 'CL' AND ISNULL(g.LadderCl, '') <> ''
             AND (CHARINDEX('|', g.LadderCl) > 0
                  OR EXISTS (SELECT 1 FROM [dbo].[zSCP_MeterMultiPriceItem] x WHERE x.MeterMultiPriceCode = LTRIM(RTRIM(g.LadderCl)))))
          );

    COMMIT TRANSACTION;
    SET XACT_ABORT OFF;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                WHERE name = 'CK_zSCP2_Contract_TierMode'
                  AND parent_object_id = OBJECT_ID(N'[dbo].[zSCP2_Contract]'))
BEGIN
    ALTER TABLE [dbo].[zSCP2_Contract] WITH CHECK
        ADD CONSTRAINT [CK_zSCP2_Contract_TierMode] CHECK ([TierMode] IN ('T','I'));
END
GO

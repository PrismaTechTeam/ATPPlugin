SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v2: free copies move out of the tier ladders and into the meters' Free Qty (feedback ATP-3).
--
-- The customer's rule from 23/09: a ladder's first tier may not be 0.00; free copies are set in ONE
-- place, the meter's Free Qty on the contract's meter grid. Until now "first 2,500 free, then 0.025"
-- was written as a 0.00 first band, and a meter on a ladder ignored its own Free Qty.
--
-- For every ladder whose FIRST band is 0.00 and which has at least one paid band after it:
--   * each meter that prices on it (its own code, or its meter type's) takes the band's width as its
--     Free Qty -- what the ladder was giving it;
--   * a meter type whose default ladder it is takes the width as its default Free Qty, so a meter
--     added later starts with the same allowance;
--   * the 0.00 row is removed from the ladder.
-- The same for a meter's own tiers (zSCP2_ItemMeterPrice): the width becomes that meter's Free Qty.
--
-- The threshold charge is unchanged by construction: the band reached is still decided on the raw
-- copies, and the same number of copies comes off free (the engine scales Free Qty by the FOC reset
-- periods exactly as it scaled the band). Checked on the migrated V8 book before release: 98,005
-- meter/usage charges identical before and after.
--
-- NOT converted:
--   * a ladder that is ALL free (one 0.00 band, nothing paid -- "this meter is free");
--   * a ladder a copy GROUP names as its tiers (zSCP2_ContractRentalPrice.LadderBk / LadderCl = the
--     code): the group's free copies are that band, and no single meter could take them over;
--   * a group's own tiers kept as text in LadderBk / LadderCl.
-- The engine still reads a 0.00 band as the allowance it always was, so all of those bill as before.
-- The tier editors refuse a NEW 0.00 first tier. Rental and waive meters are never touched: their
-- Free Qty is free months.
--
-- Idempotent: once converted, no ladder qualifies and every statement does nothing.

-- ── shared ladders ──────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID(N'tempdb..#freeband') IS NOT NULL DROP TABLE #freeband;
;WITH b AS (
    SELECT i.MeterMultiPriceCode, i.MeterReading, i.UnitPrice,
           ROW_NUMBER() OVER (PARTITION BY i.MeterMultiPriceCode ORDER BY i.MeterReading) AS n,
           SUM(CASE WHEN i.UnitPrice <> 0 THEN 1 ELSE 0 END) OVER (PARTITION BY i.MeterMultiPriceCode) AS paid
    FROM [dbo].[zSCP_MeterMultiPriceItem] i)
SELECT MeterMultiPriceCode, MeterReading AS FreeWidth
INTO #freeband
FROM b
WHERE n = 1 AND UnitPrice = 0 AND paid > 0
  AND NOT EXISTS (SELECT 1 FROM [dbo].[zSCP2_ContractRentalPrice] g
                   WHERE LTRIM(RTRIM(ISNULL(g.LadderBk, ''))) = b.MeterMultiPriceCode
                      OR LTRIM(RTRIM(ISNULL(g.LadderCl, ''))) = b.MeterMultiPriceCode);

IF EXISTS (SELECT 1 FROM #freeband)
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    -- meters on the ladder by their own code (and without tiers of their own, which outrank it)
    UPDATE m SET m.FOCQty = f.FreeWidth
    FROM [dbo].[zSCP2_ItemMeter] m
    JOIN #freeband f ON f.MeterMultiPriceCode = m.MeterMultiPriceCode
    LEFT JOIN [dbo].[zSCP_MeterType] ft ON ft.MeterTypeCode = m.MeterTypeCode
    WHERE NOT EXISTS (SELECT 1 FROM [dbo].[zSCP2_ItemMeterPrice] p WHERE p.ItemMeterKey = m.ItemMeterKey)
      AND ISNULL(ft.IsFlatCharge, 'N') <> 'Y' AND ISNULL(ft.IsRentalWaive, 'N') <> 'Y';

    -- meters on it through their meter type
    UPDATE m SET m.FOCQty = f.FreeWidth
    FROM [dbo].[zSCP2_ItemMeter] m
    JOIN [dbo].[zSCP_MeterType] mt ON mt.MeterTypeCode = m.MeterTypeCode
    JOIN #freeband f ON f.MeterMultiPriceCode = mt.MeterMultiPriceCode
    WHERE ISNULL(m.MeterMultiPriceCode, '') = ''
      AND NOT EXISTS (SELECT 1 FROM [dbo].[zSCP2_ItemMeterPrice] p WHERE p.ItemMeterKey = m.ItemMeterKey)
      AND ISNULL(mt.IsFlatCharge, 'N') <> 'Y' AND ISNULL(mt.IsRentalWaive, 'N') <> 'Y';

    -- a meter type whose default ladder it is: new meters start with the allowance
    UPDATE mt SET mt.FOCQty = f.FreeWidth
    FROM [dbo].[zSCP_MeterType] mt
    JOIN #freeband f ON f.MeterMultiPriceCode = mt.MeterMultiPriceCode
    WHERE ISNULL(mt.IsFlatCharge, 'N') <> 'Y' AND ISNULL(mt.IsRentalWaive, 'N') <> 'Y';

    -- and the ladder loses its free band
    DELETE i
    FROM [dbo].[zSCP_MeterMultiPriceItem] i
    JOIN #freeband f ON f.MeterMultiPriceCode = i.MeterMultiPriceCode AND f.FreeWidth = i.MeterReading
    WHERE i.UnitPrice = 0;

    COMMIT TRANSACTION;
    SET XACT_ABORT OFF;
END
DROP TABLE #freeband;
GO

-- ── a meter's own tiers ─────────────────────────────────────────────────────────────────────────
IF OBJECT_ID(N'tempdb..#ownfree') IS NOT NULL DROP TABLE #ownfree;
;WITH b AS (
    SELECT p.ItemMeterKey, p.MeterReading, p.UnitPrice,
           ROW_NUMBER() OVER (PARTITION BY p.ItemMeterKey ORDER BY p.MeterReading) AS n,
           SUM(CASE WHEN p.UnitPrice <> 0 THEN 1 ELSE 0 END) OVER (PARTITION BY p.ItemMeterKey) AS paid
    FROM [dbo].[zSCP2_ItemMeterPrice] p)
SELECT ItemMeterKey, MeterReading AS FreeWidth
INTO #ownfree
FROM b
WHERE n = 1 AND UnitPrice = 0 AND paid > 0;

IF EXISTS (SELECT 1 FROM #ownfree)
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    -- (a rental or waive meter bills flat and never reads its tiers: its Free Qty is free months)
    UPDATE m SET m.FOCQty = o.FreeWidth
    FROM [dbo].[zSCP2_ItemMeter] m
    JOIN #ownfree o ON o.ItemMeterKey = m.ItemMeterKey
    LEFT JOIN [dbo].[zSCP_MeterType] ft ON ft.MeterTypeCode = m.MeterTypeCode
    WHERE ISNULL(ft.IsFlatCharge, 'N') <> 'Y' AND ISNULL(ft.IsRentalWaive, 'N') <> 'Y';

    DELETE p
    FROM [dbo].[zSCP2_ItemMeterPrice] p
    JOIN #ownfree o ON o.ItemMeterKey = p.ItemMeterKey AND o.FreeWidth = p.MeterReading
    WHERE p.UnitPrice = 0;

    COMMIT TRANSACTION;
    SET XACT_ABORT OFF;
END
DROP TABLE #ownfree;
GO

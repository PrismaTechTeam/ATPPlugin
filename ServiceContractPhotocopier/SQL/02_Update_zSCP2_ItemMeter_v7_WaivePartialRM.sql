SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v7: partial waive re-expressed in RM (demo feedback 28/07 #24). The old WaivePartialPct
-- ("reach 90% of the target -> waive 90% of the rent") was hard to explain — the customer
-- thinks in RM. New semantics on the waive meter row:
--   WaivePartialThreshold = 0 -> no partial band; > 0 -> when the scoped print charges reach
--                               THIS RM amount (but miss the full target)...
--   WaivePartialAmount    = ...waive THIS RM amount off the rental (capped at the rental itself).
-- Full target unchanged: charges >= WaiveTargetAmount -> the whole rental is waived.
-- WaivePartialPct stays on the table (dormant) — v7 backfills it into the two RM columns once.
IF COL_LENGTH('dbo.zSCP2_ItemMeter', 'WaivePartialThreshold') IS NULL
    ALTER TABLE dbo.zSCP2_ItemMeter ADD WaivePartialThreshold DECIMAL(18,4) NOT NULL CONSTRAINT DF_zSCP2_ItemMeter_WaivePartTh DEFAULT(0);
GO
IF COL_LENGTH('dbo.zSCP2_ItemMeter', 'WaivePartialAmount') IS NULL
    ALTER TABLE dbo.zSCP2_ItemMeter ADD WaivePartialAmount DECIMAL(18,4) NOT NULL CONSTRAINT DF_zSCP2_ItemMeter_WaivePartAmt DEFAULT(0);
GO
-- One-time backfill of existing percent deals into the RM columns (idempotent: only rows whose
-- RM columns are still 0). Threshold = pct of the target; amount = pct of the waive magnitude
-- (MinimumCharges, else ChargesRate — same source the engine bills from).
UPDATE m SET
    WaivePartialThreshold = ROUND(m.WaiveTargetAmount * m.WaivePartialPct / 100, 2),
    WaivePartialAmount    = ROUND(ABS(CASE WHEN m.MinimumCharges <> 0 THEN m.MinimumCharges ELSE m.ChargesRate END) * m.WaivePartialPct / 100, 2)
FROM dbo.zSCP2_ItemMeter m
WHERE m.WaivePartialPct > 0 AND m.WaivePartialPct < 100
  AND m.WaiveTargetAmount > 0
  AND m.WaivePartialThreshold = 0 AND m.WaivePartialAmount = 0;
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v5: per-meter Waive Configuration (user decision 2026-07-27). Lives on the MACHINE's waive
-- meter row (same meter type can carry different deals on different machines). Semantics:
--   WaiveFirstNMonths = 0  -> no window limit;  > 0 -> fires only in months 1..N (anchor =
--                            RentalStartDate, else the machine's effective service start)
--   WaiveTargetAmount = 0  -> no usage condition; > 0 -> fires when the MACHINE's scoped print
--                            charges reach the target (WaivePartialPct % band = partial contra)
--   Both = 0               -> ALWAYS waive (the legacy master behavior: contra every month)
--   Both set (SEQUENTIAL)  -> months 1..N always waived, AFTER the window the target takes over.
IF COL_LENGTH('dbo.zSCP2_ItemMeter', 'WaiveFirstNMonths') IS NULL
    ALTER TABLE dbo.zSCP2_ItemMeter ADD WaiveFirstNMonths INT NOT NULL CONSTRAINT DF_zSCP2_ItemMeter_WaiveN DEFAULT(0);
GO
IF COL_LENGTH('dbo.zSCP2_ItemMeter', 'WaiveTargetAmount') IS NULL
    ALTER TABLE dbo.zSCP2_ItemMeter ADD WaiveTargetAmount DECIMAL(18,4) NOT NULL CONSTRAINT DF_zSCP2_ItemMeter_WaiveTarget DEFAULT(0);
GO
IF COL_LENGTH('dbo.zSCP2_ItemMeter', 'WaivePartialPct') IS NULL
    ALTER TABLE dbo.zSCP2_ItemMeter ADD WaivePartialPct DECIMAL(9,4) NOT NULL CONSTRAINT DF_zSCP2_ItemMeter_WaivePct DEFAULT(100);
GO
IF COL_LENGTH('dbo.zSCP2_ItemMeter', 'WaiveScope') IS NOT NULL
    PRINT 'WaiveScope exists';
ELSE
    ALTER TABLE dbo.zSCP2_ItemMeter ADD WaiveScope VARCHAR(10) NOT NULL CONSTRAINT DF_zSCP2_ItemMeter_WaiveScope DEFAULT('BKCL');
GO

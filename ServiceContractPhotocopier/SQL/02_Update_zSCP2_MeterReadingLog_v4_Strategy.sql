-- v4: strategy traceability on the immutable reading log — which strategy was in force for the
-- event and its human-readable outcome (e.g. 'WAIVED: meter charges 512.40 >= target 500.00').
-- NULL on pre-v4 rows. Idempotent.
SET NOCOUNT ON;

IF COL_LENGTH('dbo.zSCP2_MeterReadingLog','StrategyCode') IS NULL
    ALTER TABLE dbo.zSCP2_MeterReadingLog ADD [StrategyCode] NVARCHAR(20) NULL;
GO

IF COL_LENGTH('dbo.zSCP2_MeterReadingLog','StrategyNote') IS NULL
    ALTER TABLE dbo.zSCP2_MeterReadingLog ADD [StrategyNote] NVARCHAR(200) NULL;
GO

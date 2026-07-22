-- v4: the strategy in force when this meter+period was stamped/invoiced (traceability). Idempotent.
SET NOCOUNT ON;

IF COL_LENGTH('dbo.zSCP2_MeterEntry','StrategyCode') IS NULL
    ALTER TABLE dbo.zSCP2_MeterEntry ADD [StrategyCode] NVARCHAR(20) NOT NULL
        CONSTRAINT DF_zSCP2ME_Strategy DEFAULT('');
GO

-- v5: strategy attach (soft code, no FK — MeterMultiPriceCode precedent) + "rental billed on its own
-- invoice" flag. Idempotent.
SET NOCOUNT ON;

IF COL_LENGTH('dbo.zSCP2_Contract','StrategyCode') IS NULL
    ALTER TABLE dbo.zSCP2_Contract ADD [StrategyCode] NVARCHAR(20) NOT NULL
        CONSTRAINT DF_zSCP2C_Strategy DEFAULT('');
GO

IF COL_LENGTH('dbo.zSCP2_Contract','RentalSeparateInvoice') IS NULL
    ALTER TABLE dbo.zSCP2_Contract ADD [RentalSeparateInvoice] CHAR(1) NOT NULL
        CONSTRAINT DF_zSCP2C_RentSep DEFAULT('N');
GO

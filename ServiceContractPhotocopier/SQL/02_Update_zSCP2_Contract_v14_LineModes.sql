SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v14: how the lines of an invoice group, and which named Billing Format that came from.
--
-- BillingMode (G/S) and RentalSeparateInvoice already decide how many invoices a contract produces.
-- What they cannot say is how the lines INSIDE one invoice collapse, which is the other half of what
-- the July-2026 invoices differ by:
--
--   A  across model  — merge regardless of model; how many lines you get is data
--                      (Pontian: 6 machines / 2 models -> one BK line of 74,722)
--   M  same model    — one line per model
--                      (JPJ: 12 machines / 3 models -> 3 rental lines, 1/5/6, matching exactly)
--   S  per machine   — no merge
--                      (MARA: 5 machines -> 10 meter lines, BK and CL paired per machine)
--
-- Both A and M are real and neither can stand in for the other: Pasir Gudang's MEDIUM DUTY line
-- merges four different models because they share a rate, while MBJB groups 52 machines into 7
-- meter lines strictly by model with every BK at the same 0.020.
--
-- Defaults reproduce today exactly. MeterLineMode 'S' is what the engine does now (one line per
-- meter, never merged). RentalLineMode inherits the retiring global GROUP_RENTAL_BY_METER, which
-- is on by default and folds rentals to one row per meter type -- that is 'A'.

IF COL_LENGTH('dbo.zSCP2_Contract', 'BillingFormatCode') IS NULL
    ALTER TABLE dbo.zSCP2_Contract ADD BillingFormatCode NVARCHAR(20) NOT NULL
        CONSTRAINT DF_zSCP2_Contract_BillingFormatCode DEFAULT('');
GO
IF COL_LENGTH('dbo.zSCP2_Contract', 'RentalLineMode') IS NULL
    ALTER TABLE dbo.zSCP2_Contract ADD RentalLineMode CHAR(1) NOT NULL
        CONSTRAINT DF_zSCP2_Contract_RentalLineMode DEFAULT('A');
GO
IF COL_LENGTH('dbo.zSCP2_Contract', 'MeterLineMode') IS NULL
    ALTER TABLE dbo.zSCP2_Contract ADD MeterLineMode CHAR(1) NOT NULL
        CONSTRAINT DF_zSCP2_Contract_MeterLineMode DEFAULT('S');
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_zSCP2_Contract_RentalLineMode')
    ALTER TABLE dbo.zSCP2_Contract ADD CONSTRAINT CK_zSCP2_Contract_RentalLineMode
        CHECK ([RentalLineMode] IN ('A','M','S'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_zSCP2_Contract_MeterLineMode')
    ALTER TABLE dbo.zSCP2_Contract ADD CONSTRAINT CK_zSCP2_Contract_MeterLineMode
        CHECK ([MeterLineMode] IN ('A','M','S'));
GO

-- One-shot inheritance of the global that RentalLineMode replaces, so an existing book keeps
-- billing the way it does today without anyone touching a contract. Marked done in Z_PumsConfig so
-- a later plugin load cannot undo a per-contract edit made in between.
IF OBJECT_ID('dbo.Z_PumsConfig', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.Z_PumsConfig WHERE ConfigKey = 'LINEMODE_BACKFILL_DONE')
BEGIN
    DECLARE @groupRental NVARCHAR(50) =
        ISNULL((SELECT TOP 1 CONVERT(NVARCHAR(50), ConfigValue) FROM dbo.Z_PumsConfig
                WHERE ConfigKey = 'GROUP_RENTAL_BY_METER'), '1');

    UPDATE dbo.zSCP2_Contract
       SET RentalLineMode = CASE WHEN @groupRental IN ('1','Y','T','true','True') THEN 'A' ELSE 'S' END;

    INSERT INTO dbo.Z_PumsConfig (ConfigKey, ConfigValue) VALUES ('LINEMODE_BACKFILL_DONE', '1');
END
GO

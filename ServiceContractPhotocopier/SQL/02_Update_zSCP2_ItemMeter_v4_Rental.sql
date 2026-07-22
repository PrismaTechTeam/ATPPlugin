-- v4: rental period tracking on flat (rental) meter rows — drives the "MONTHLY RENTAL (n/N)"
-- invoice line text. RentalMonths 0 = open-ended (no n/N shown). Basis 'A' accrual (period n) /
-- 'P' prepayment (period n+1). Idempotent.
SET NOCOUNT ON;

IF COL_LENGTH('dbo.zSCP2_ItemMeter','RentalStartDate') IS NULL
    ALTER TABLE dbo.zSCP2_ItemMeter ADD [RentalStartDate] DATE NULL;
GO

IF COL_LENGTH('dbo.zSCP2_ItemMeter','RentalMonths') IS NULL
    ALTER TABLE dbo.zSCP2_ItemMeter ADD [RentalMonths] INT NOT NULL
        CONSTRAINT DF_zSCP2IM_RentalMonths DEFAULT(0);
GO

IF COL_LENGTH('dbo.zSCP2_ItemMeter','RentalBasis') IS NULL
    ALTER TABLE dbo.zSCP2_ItemMeter ADD [RentalBasis] CHAR(1) NOT NULL
        CONSTRAINT DF_zSCP2IM_RentalBasis DEFAULT('A');
GO

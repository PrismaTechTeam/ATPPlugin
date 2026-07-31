SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v9: contract-level RENTAL invoice day (demo feedback 28/07 #18). When "Rental separate
-- invoice" is ON, the rental-only ("Rental- [ref]") invoice is dated on THIS day instead of
-- riding the meter invoice's billing day — "rental 是跟头标的,meter 是跟尾标的":
--   0    = follow the meter invoice date (old behavior)
--   1-28 = the rental job's own day; accrual rentals date in the BILLING month, prepayment
--          rentals in the NEXT month (June meter run on the 28th -> rental invoice July 1st).
IF COL_LENGTH('dbo.zSCP2_Contract', 'RentalBillingDay') IS NULL
    ALTER TABLE dbo.zSCP2_Contract ADD RentalBillingDay INT NOT NULL CONSTRAINT DF_zSCP2_Contract_RentalBillingDay DEFAULT(0);
GO

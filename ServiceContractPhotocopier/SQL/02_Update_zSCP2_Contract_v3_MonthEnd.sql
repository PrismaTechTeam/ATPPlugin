-- "Bill on last day of month" option. When 'Y', billing uses the true last day of each billing
-- month (28/29/30/31) regardless of the stored BillingDay, so a day like 31 never falls off a short
-- month. Idempotent (COL_LENGTH guard).
SET NOCOUNT ON;
IF COL_LENGTH('dbo.zSCP2_Contract','BillOnMonthEnd') IS NULL
    ALTER TABLE dbo.zSCP2_Contract ADD [BillOnMonthEnd] CHAR(1) NOT NULL CONSTRAINT DF_zSCP2C_MonthEnd DEFAULT('N');
GO

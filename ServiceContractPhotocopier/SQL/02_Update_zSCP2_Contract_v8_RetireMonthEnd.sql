SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v8: "Last day of month" RETIRED (user decision 2026-07-27). Billing Day is a plain 1..28
-- everywhere. The month-end option used to store BillingDay=31 + BillOnMonthEnd='Y' internally,
-- which read as a contradiction next to the 28 cap (the editor showed 28, the DB said 31, the
-- billing screen filed it under the "31" day button) and confused everyone. Legacy month-end
-- contracts become plain day-28 contracts; per-item overrides above 28 clamp the same way.
-- The BillOnMonthEnd column is KEPT (always 'N') so old code paths stay harmless. Idempotent.
IF COL_LENGTH('dbo.zSCP2_Contract', 'BillingDay') IS NOT NULL
    UPDATE dbo.zSCP2_Contract SET BillingDay = 28 WHERE BillingDay > 28;
GO
IF COL_LENGTH('dbo.zSCP2_Contract', 'BillOnMonthEnd') IS NOT NULL
    UPDATE dbo.zSCP2_Contract SET BillOnMonthEnd = 'N' WHERE BillOnMonthEnd = 'Y';
GO
IF COL_LENGTH('dbo.zSCP2_Item', 'BillingDayOverride') IS NOT NULL
    UPDATE dbo.zSCP2_Item SET BillingDayOverride = 28 WHERE BillingDayOverride > 28;
GO

-- v3: LockedAt — set by the billing-day AUTO-FETCH snapshot. A locked staged reading is FROZEN as
-- of the configured cutoff (e.g. day-before-billing-day 23:59): later fetches and manual key-ins
-- can no longer override it. Invoicing still stamps it normally; deleting the invoice reconciles as
-- usual. NULL = normal (unlocked) staging row. Idempotent.
IF COL_LENGTH('dbo.zSCP2_MeterEntry', 'LockedAt') IS NULL
    ALTER TABLE dbo.zSCP2_MeterEntry ADD LockedAt DATETIME NULL;
GO

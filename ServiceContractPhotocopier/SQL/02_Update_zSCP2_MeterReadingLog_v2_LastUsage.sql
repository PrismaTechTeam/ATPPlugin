-- v2: the audit log also captures the BASELINE (LastReading) and the billed USAGE of each event,
-- so an INVOICE row fully explains its amount (current - last = usage x rate) without having to
-- reconstruct baselines across deleted/re-generated invoices. NULL on rows written before v2 and
-- on event types where a baseline is meaningless (CLEARED etc.). Idempotent.
IF COL_LENGTH('dbo.zSCP2_MeterReadingLog', 'LastReading') IS NULL
    ALTER TABLE dbo.zSCP2_MeterReadingLog ADD LastReading DECIMAL(18,2) NULL;
IF COL_LENGTH('dbo.zSCP2_MeterReadingLog', 'Usage') IS NULL
    ALTER TABLE dbo.zSCP2_MeterReadingLog ADD [Usage] DECIMAL(18,2) NULL;
GO

-- v3: the audit log captures the meter's PRICING AS OF the event (unit price / minimum charges /
-- FOC / rebate) plus the resulting charge amount — the full historical meter record per invoice,
-- immune to later price changes on the meter master. NULL on rows written before v3. Idempotent.
IF COL_LENGTH('dbo.zSCP2_MeterReadingLog', 'UnitPrice') IS NULL
    ALTER TABLE dbo.zSCP2_MeterReadingLog ADD UnitPrice DECIMAL(18,6) NULL;
IF COL_LENGTH('dbo.zSCP2_MeterReadingLog', 'MinCharges') IS NULL
    ALTER TABLE dbo.zSCP2_MeterReadingLog ADD MinCharges DECIMAL(18,2) NULL;
IF COL_LENGTH('dbo.zSCP2_MeterReadingLog', 'FOCQty') IS NULL
    ALTER TABLE dbo.zSCP2_MeterReadingLog ADD FOCQty DECIMAL(18,2) NULL;
IF COL_LENGTH('dbo.zSCP2_MeterReadingLog', 'RebatePct') IS NULL
    ALTER TABLE dbo.zSCP2_MeterReadingLog ADD RebatePct DECIMAL(18,2) NULL;
IF COL_LENGTH('dbo.zSCP2_MeterReadingLog', 'Charge') IS NULL
    ALTER TABLE dbo.zSCP2_MeterReadingLog ADD Charge DECIMAL(18,2) NULL;
GO

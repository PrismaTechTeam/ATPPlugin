SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v2: "Rental Waive" meter TYPE flag (user decision 2026-07-27, master-style waive automation).
-- A waive meter is the master convention's contra line (e.g. RA-MONTH(W)-13MTH, -150.00 on the
-- invoice) — kept as a REAL meter with its own item code, but from now on the billing engine
-- decides each Generate whether it fires (per-meter Waive Configuration on the machine).
-- Backfill: the whole legacy "(W)" family is auto-tagged so customer data needs no edits.
IF COL_LENGTH('dbo.zSCP_MeterType', 'IsRentalWaive') IS NULL
    ALTER TABLE dbo.zSCP_MeterType ADD IsRentalWaive CHAR(1) NOT NULL CONSTRAINT DF_zSCP_MeterType_IsRentalWaive DEFAULT('N');
GO
UPDATE dbo.zSCP_MeterType SET IsRentalWaive = 'Y'
WHERE MeterTypeCode LIKE '%(W)%' AND IsRentalWaive = 'N';
GO

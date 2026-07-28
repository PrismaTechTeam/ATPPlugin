SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v6: OFFLINE readings carry a TrackingId ("MR-yymmdd-nnn") from the meter API's monthly report.
-- It is persisted with the staged reading so invoice generation can stamp it into the invoice's
-- Reference No (comma-joined, distinct, when one invoice covers several machines/reports).
-- '' = no tracking id (manual key-ins and ONLINE readings). Idempotent.
IF COL_LENGTH('dbo.zSCP2_MeterEntry', 'TrackingId') IS NULL
    ALTER TABLE dbo.zSCP2_MeterEntry ADD TrackingId NVARCHAR(50) NOT NULL CONSTRAINT DF_zSCP2_MeterEntry_TrackingId DEFAULT('');
GO

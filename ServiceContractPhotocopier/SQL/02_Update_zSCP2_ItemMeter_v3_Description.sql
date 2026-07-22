-- v3: per-meter Description. Defaults from the Meter Type's description when a type is picked, but is
-- editable per machine (an override that never writes back to the Meter Type master). Idempotent.
SET NOCOUNT ON;

IF COL_LENGTH('dbo.zSCP2_ItemMeter','Description') IS NULL
    ALTER TABLE dbo.zSCP2_ItemMeter ADD [Description] NVARCHAR(200) NOT NULL CONSTRAINT DF_zSCP2_ItemMeter_Desc DEFAULT('');
GO

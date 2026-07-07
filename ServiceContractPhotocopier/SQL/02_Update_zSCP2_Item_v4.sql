SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Add per-item service expiry date (mirrors master serviceitem.serviceexpirydate). Used by the
-- Meter Reading list to optionally exclude expired machines from billing (Meter Reading > Setting >
-- "Include expired Service Item"). Idempotent: safe on every plugin load and on fresh DBs.
IF COL_LENGTH('dbo.zSCP2_Item','ServiceExpiryDate') IS NULL
    ALTER TABLE [dbo].[zSCP2_Item] ADD [ServiceExpiryDate] DATE NULL;
GO

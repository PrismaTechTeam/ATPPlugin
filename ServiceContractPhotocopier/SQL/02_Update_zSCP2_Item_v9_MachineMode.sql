SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v9: per-machine ONLINE/OFFLINE definition (user request 2026-07-27). A DEFINED mode on the
-- machine ('' = undefined) drives the ADVANCED invoice number format deterministically — the
-- live API fetch status is only the fallback when no mode is defined.
IF COL_LENGTH('dbo.zSCP2_Item', 'MachineMode') IS NULL
    ALTER TABLE dbo.zSCP2_Item ADD MachineMode VARCHAR(10) NOT NULL CONSTRAINT DF_zSCP2_Item_MachineMode DEFAULT('');
GO

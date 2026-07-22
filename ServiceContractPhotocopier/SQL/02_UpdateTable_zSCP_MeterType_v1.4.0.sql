-- v1.4.0: Add IsFlatCharge flag to zSCP_MeterType.
-- 'Y' = a FLAT / RENTAL meter: it has no meter reading. In Meter Reading it is auto-billed as
-- quantity 1 x Unit Price (the rental amount) every period with no manual key-in. 'N' = a normal
-- usage meter (BK/CL) billed on reading. Guarded for idempotent re-run.

IF COL_LENGTH('dbo.zSCP_MeterType', 'IsFlatCharge') IS NULL
    ALTER TABLE [dbo].[zSCP_MeterType] ADD IsFlatCharge char(1) NOT NULL CONSTRAINT DF_zSCP_MeterType_IsFlat DEFAULT('N');
GO

-- Auto-mark the fixed-charge meter types as flat: rentals (code starts with 'RA') and minimum
-- committed charges (code starts with 'MIN'). Both are billed as a fixed amount with NO meter
-- reading. Other types (fax, plotter, …) are left for the user to tick in Meter Type maintenance.
UPDATE [dbo].[zSCP_MeterType] SET IsFlatCharge = 'Y'
WHERE ISNULL(IsFlatCharge,'N') <> 'Y' AND (MeterTypeCode LIKE 'RA%' OR MeterTypeCode LIKE 'MIN%');
GO

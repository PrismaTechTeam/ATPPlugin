-- v1.5.0: Broaden the flat/rental auto-mark. v1.4.0 only caught meter type codes that START with
-- 'RA'/'MIN', which missed the real-world convention where the rental code is prefixed with a
-- document sequence, e.g. "01.RA.2JC10897", "01.RA-1 UNIT", "01.1 UNIT RENTAL" (description
-- "MONTHLY RENTAL"/"RENTAL CHARGE"). Those slipped through as IsFlatCharge='N' and therefore did
-- NOT auto-bill as rentals in Meter Reading. This pass marks them flat by the rental token pattern.
--
-- SAFETY: a type is only marked flat when it carries NO black/colour (BK/CL) usage meter — a rental
-- type never does, so this cannot silently turn a real usage meter into a no-reading flat meter.
-- Idempotent (skips rows already 'Y'). Verified on AED_ATPTEST: 87 rental types marked, 0 BK/CL types
-- affected. Plotters ("PLO-M"/"PLOTTER-A0") and duplicate .MR.BK/.MR.CL meters are intentionally NOT
-- touched — they are usage meters, not rentals, and are handled by manual key-in.
SET NOCOUNT ON;

UPDATE mt SET IsFlatCharge = 'Y'
FROM [dbo].[zSCP_MeterType] mt
WHERE ISNULL(mt.IsFlatCharge, 'N') <> 'Y'
  AND ( mt.MeterTypeCode LIKE '%RENTAL%'
     OR mt.MeterTypeCode LIKE '%.RA%'
     OR mt.MeterTypeCode LIKE '%-RA%'
     OR mt.MeterTypeCode LIKE '% RA%'
     OR mt.MeterTypeCode LIKE '%.MIN%'
     OR mt.MeterTypeCode LIKE '%-MIN%'
     OR mt.MeterTypeCode LIKE '% MIN%'
     OR mt.[Description]  LIKE '%RENTAL%' )
  AND NOT EXISTS (SELECT 1 FROM [dbo].[zSCP2_ItemMeter] m
                  WHERE m.MeterTypeCode = mt.MeterTypeCode AND m.MeterRole IN ('BK','CL'));
GO

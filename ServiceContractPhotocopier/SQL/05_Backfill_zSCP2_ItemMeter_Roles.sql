-- Backfill BK/CL roles on legacy USAGE meters still tagged 'NA' (rentals/min/fax stay NA — they are
-- flat). Mirrors the UI's InferMeterRole: CL patterns first, then BK. Guards: only one meter per
-- (machine, role) may flip per pass (MIN ItemMeterKey) and never when that machine already has the
-- role (filtered unique indexes UX_*_BK / UX_*_CL). Idempotent — flipped rows are no longer 'NA'.
SET NOCOUNT ON;

-- CL pass
UPDATE m SET MeterRole = 'CL', LastModified = GETDATE()
FROM dbo.zSCP2_ItemMeter m
JOIN dbo.zSCP_MeterType mt ON mt.MeterTypeCode = m.MeterTypeCode
WHERE m.MeterRole = 'NA' AND ISNULL(mt.IsFlatCharge, 'N') = 'N'
  AND (UPPER(m.MeterTypeCode) LIKE '%.CL.%' OR UPPER(m.MeterTypeCode) LIKE '%-CL%'
       OR UPPER(mt.[Description]) LIKE '%COLOUR%' OR UPPER(mt.[Description]) LIKE '%COLOR%')
  AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_ItemMeter x
                  WHERE x.ItemKey = m.ItemKey AND ISNULL(x.MachineSerialNo,'') = ISNULL(m.MachineSerialNo,'')
                    AND x.MeterRole = 'CL')
  AND m.ItemMeterKey = (SELECT MIN(x2.ItemMeterKey)
                        FROM dbo.zSCP2_ItemMeter x2
                        JOIN dbo.zSCP_MeterType mt2 ON mt2.MeterTypeCode = x2.MeterTypeCode
                        WHERE x2.ItemKey = m.ItemKey AND ISNULL(x2.MachineSerialNo,'') = ISNULL(m.MachineSerialNo,'')
                          AND x2.MeterRole = 'NA' AND ISNULL(mt2.IsFlatCharge, 'N') = 'N'
                          AND (UPPER(x2.MeterTypeCode) LIKE '%.CL.%' OR UPPER(x2.MeterTypeCode) LIKE '%-CL%'
                               OR UPPER(mt2.[Description]) LIKE '%COLOUR%' OR UPPER(mt2.[Description]) LIKE '%COLOR%'));
GO

-- BK pass (colour-matched rows already flipped above, so plain BK patterns are safe)
UPDATE m SET MeterRole = 'BK', LastModified = GETDATE()
FROM dbo.zSCP2_ItemMeter m
JOIN dbo.zSCP_MeterType mt ON mt.MeterTypeCode = m.MeterTypeCode
WHERE m.MeterRole = 'NA' AND ISNULL(mt.IsFlatCharge, 'N') = 'N'
  AND (UPPER(m.MeterTypeCode) LIKE '%.BK.%' OR UPPER(m.MeterTypeCode) LIKE '%-BK%'
       OR UPPER(m.MeterTypeCode) LIKE '% BK%' OR UPPER(mt.[Description]) LIKE '%BLACK%'
       OR UPPER(mt.[Description]) LIKE '%BK %')
  AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_ItemMeter x
                  WHERE x.ItemKey = m.ItemKey AND ISNULL(x.MachineSerialNo,'') = ISNULL(m.MachineSerialNo,'')
                    AND x.MeterRole = 'BK')
  AND m.ItemMeterKey = (SELECT MIN(x2.ItemMeterKey)
                        FROM dbo.zSCP2_ItemMeter x2
                        JOIN dbo.zSCP_MeterType mt2 ON mt2.MeterTypeCode = x2.MeterTypeCode
                        WHERE x2.ItemKey = m.ItemKey AND ISNULL(x2.MachineSerialNo,'') = ISNULL(m.MachineSerialNo,'')
                          AND x2.MeterRole = 'NA' AND ISNULL(mt2.IsFlatCharge, 'N') = 'N'
                          AND (UPPER(x2.MeterTypeCode) LIKE '%.BK.%' OR UPPER(x2.MeterTypeCode) LIKE '%-BK%'
                               OR UPPER(x2.MeterTypeCode) LIKE '% BK%' OR UPPER(mt2.[Description]) LIKE '%BLACK%'
                               OR UPPER(mt2.[Description]) LIKE '%BK %'));
GO

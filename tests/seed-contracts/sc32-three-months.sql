SET NOCOUNT ON;
GO

-- SC 000000032 (AED_ATPTEST): three months of readings to bill against, for testing.
--
-- The contract runs from 01/08/2026 on billing day 1 with three machines, each carrying a rental,
-- a black counter and a colour counter, all at 0.00. An invoice for nothing proves nothing, so this
-- also puts a price on each counter -- only where none was set -- and an opening reading under each
-- month's figures, so August, September and October each show a month's copies.
--
--   rental      300.00 a month
--   black        0.0300 a copy
--   colour       0.2500 a copy
--
-- Undo with sc32-three-months-restore.sql.

DECLARE @ck BIGINT = (SELECT ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'SC 000000032');
IF @ck IS NULL BEGIN RAISERROR('SC 000000032 not found in this book.', 16, 1); RETURN; END

-- A price, where the counter has none.
UPDATE m
   SET m.ChargesRate = CASE UPPER(ISNULL(m.MeterRole,''))
                            WHEN 'RENTAL' THEN 300.00
                            WHEN 'BK'     THEN 0.03
                            WHEN 'CL'     THEN 0.25
                            ELSE m.ChargesRate END,
       m.LastModified = GETDATE()
  FROM dbo.zSCP2_ItemMeter m
  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
 WHERE i.ContractKey = @ck
   AND ISNULL(m.ChargesRate, 0) = 0
   AND ISNULL(m.MinimumCharges, 0) = 0
   AND UPPER(ISNULL(m.MeterRole,'')) IN ('RENTAL','BK','CL');

-- Where each counter stood when the contract started. Machines differ so the invoices do too.
UPDATE m
   SET m.InitialReading = CASE UPPER(ISNULL(m.MeterRole,''))
                               WHEN 'BK' THEN 100000 + 50000 * i.Pos
                               WHEN 'CL' THEN  20000 + 10000 * i.Pos
                               ELSE 0 END,
       m.LastModified = GETDATE()
  FROM dbo.zSCP2_ItemMeter m
  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
 WHERE i.ContractKey = @ck
   AND ISNULL(m.InitialReading, 0) = 0
   AND UPPER(ISNULL(m.MeterRole,'')) IN ('BK','CL');

-- August, September and October 2026, one reading per counter per month: the opening figure plus a
-- month's copies each time, so every month bills the same steady usage.
DECLARE @months TABLE (Seq INT, Yr INT, Mth INT);
INSERT INTO @months (Seq, Yr, Mth) VALUES (1, 2026, 8), (2, 2026, 9), (3, 2026, 10);

INSERT INTO dbo.zSCP2_MeterEntry
    (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
SELECT m.ItemMeterKey, p.Yr, p.Mth,
       CASE UPPER(m.MeterRole)
            WHEN 'BK' THEN (100000 + 50000 * i.Pos) + p.Seq * (3000 + 500 * i.Pos)
            ELSE        (20000 + 10000 * i.Pos) + p.Seq * (700 + 100 * i.Pos) END,
       EOMONTH(DATEFROMPARTS(p.Yr, p.Mth, 1)), 'MANUAL', 'N', GETDATE()
  FROM dbo.zSCP2_ItemMeter m
  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
  CROSS JOIN @months p
 WHERE i.ContractKey = @ck
   AND UPPER(ISNULL(m.MeterRole,'')) IN ('BK','CL')
   AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry e
                    WHERE e.ItemMeterKey = m.ItemMeterKey AND e.PeriodYear = p.Yr AND e.PeriodMonth = p.Mth);

SELECT i.ServiceItemNo, m.MeterRole, m.ChargesRate, m.InitialReading,
       MAX(CASE WHEN e.PeriodMonth = 8  THEN e.CurrentReading END) AS Aug2026,
       MAX(CASE WHEN e.PeriodMonth = 9  THEN e.CurrentReading END) AS Sep2026,
       MAX(CASE WHEN e.PeriodMonth = 10 THEN e.CurrentReading END) AS Oct2026
  FROM dbo.zSCP2_Item i
  JOIN dbo.zSCP2_ItemMeter m ON m.ItemKey = i.ItemKey
  LEFT JOIN dbo.zSCP2_MeterEntry e ON e.ItemMeterKey = m.ItemMeterKey AND e.PeriodYear = 2026
 WHERE i.ContractKey = @ck
 GROUP BY i.ServiceItemNo, i.Pos, m.MeterRole, m.ChargesRate, m.InitialReading
 ORDER BY i.Pos, m.MeterRole;
GO

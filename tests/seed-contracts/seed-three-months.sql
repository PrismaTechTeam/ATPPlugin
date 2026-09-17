-- Three months of readings, without moving the month that matches the invoice.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -v only="" force=""
--          -i tests\seed-contracts\seed-three-months.sql
--
-- Both -v values must be passed -- sqlcmd stops on an undefined one. Empty means "every DEMO
-- contract, and refuse if July is already there".
--   sqlcmd ... -v only="DEMO-MBJB"            just that contract
--   sqlcmd ... -v only="DEMO-MBJB" force="1"  and do it even though July exists
--
-- Every demo contract carries ONE month, September 2026, and its figures are the ones off the
-- customer's real invoice. One month is enough to check a total and not enough to check anything
-- else: whether last month's reading becomes this month's baseline, whether n/36 moves, whether the
-- second invoice refuses to bill a period the first one already did.
--
-- So July and August are put in FRONT of September rather than after it. Each counter's opening is
-- moved back two months of its own usage, and the three readings step up from there:
--
--     opening        = old opening - 2 x usage
--     July current   = old opening -     usage
--     August current = old opening
--     Sept current   = old opening +     usage      <- unchanged, still the invoice's figure
--
-- Bill them in order and each month's baseline is the month before it. September still prints
-- exactly what the PDF prints, which is the whole point -- the proof must not move to make room for
-- the demonstration.
--
-- A counter whose opening is too small to move back twice is left where it is and reported at the
-- end; its September is still right, its July and August simply start from zero.
--
-- RUN IT ONCE. It is NOT re-runnable, whatever an earlier version of this comment claimed: the
-- usage it measures is September minus the OPENING, and it moves the opening. Run it twice and
-- the second pass measures three months as one, pushes the opening back six more, and every
-- contract quietly bills three times its real usage -- with the months still evenly spaced, so
-- nothing in the data looks wrong. It refuses a second run for that reason.
--
-- To redo it: re-run the contract's own seed first (that restores the opening off the PDF), then
-- this. @force = 1 overrides the refusal and is there for that flow, not for a second helping.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @only NVARCHAR(50) = NULLIF(N'$(only)', N'');   -- one contract, or all of them
DECLARE @force BIT = CASE WHEN N'$(force)' = N'1' THEN 1 ELSE 0 END;
IF @force = 0 AND EXISTS (SELECT 1
                            FROM dbo.zSCP2_MeterEntry e
                            JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey
                            JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
                            JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey
                           WHERE c.ContractNo LIKE 'DEMO-%'
                             AND (@only IS NULL OR c.ContractNo = @only)
                             AND e.PeriodYear = 2026 AND e.PeriodMonth = 7)
BEGIN
    SELECT 'July already exists -- the openings have been moved once. Re-run the contract seeds ' +
           'first, then set @force = 1. Running this again would treble the usage.' AS Refused;
    RETURN;
END

-- What each counter reads in September, and what it used to get there. Usage is measured from the
-- opening, which is what the seeds set -- not from an earlier month, which may not exist yet.
IF OBJECT_ID('tempdb..#c') IS NOT NULL DROP TABLE #c;
SELECT m.ItemMeterKey,
       CAST(m.InitialReading AS DECIMAL(18,2)) AS OldOpen,
       CAST(e.CurrentReading AS DECIMAL(18,2)) AS SepRead,
       CAST(e.CurrentReading - m.InitialReading AS DECIMAL(18,2)) AS Usage
  INTO #c
  FROM dbo.zSCP2_MeterEntry e
  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey
  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
  JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey
 WHERE e.PeriodYear = 2026 AND e.PeriodMonth = 9
   AND c.ContractNo LIKE 'DEMO-%'
   AND (@only IS NULL OR c.ContractNo = @only)
   AND e.CurrentReading > m.InitialReading;

-- Rebuild, so a second run starts from the same place the first one did.
DELETE e FROM dbo.zSCP2_MeterEntry e JOIN #c c ON c.ItemMeterKey = e.ItemMeterKey
 WHERE e.PeriodYear = 2026 AND e.PeriodMonth IN (7, 8);

-- The opening moves back two months -- unless there is not room, in which case it stays and the
-- earlier months simply start lower. Never negative: a meter has never read less than nothing.
UPDATE m
   SET m.InitialReading = CASE WHEN c.OldOpen - 2 * c.Usage >= 0
                               THEN c.OldOpen - 2 * c.Usage ELSE 0 END,
       m.LastModified = GETDATE()
  FROM dbo.zSCP2_ItemMeter m JOIN #c c ON c.ItemMeterKey = m.ItemMeterKey;

INSERT INTO dbo.zSCP2_MeterEntry
 (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
SELECT c.ItemMeterKey, 2026, 7,
       CASE WHEN c.OldOpen - 2 * c.Usage >= 0 THEN c.OldOpen - c.Usage ELSE c.Usage END,
       '2026-07-31', 'MANUAL', 'N', GETDATE()
  FROM #c c;

INSERT INTO dbo.zSCP2_MeterEntry
 (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
SELECT c.ItemMeterKey, 2026, 8,
       CASE WHEN c.OldOpen - 2 * c.Usage >= 0 THEN c.OldOpen ELSE 2 * c.Usage END,
       '2026-08-31', 'MANUAL', 'N', GETDATE()
  FROM #c c;

-- A counter whose opening could not move back twice now reads u, 2u, ... and its September was
-- left where it was. When September is BELOW 2u -- which happens whenever the meter has barely
-- run longer than the period being demonstrated -- August ends up higher than September and the
-- third month reads BACKWARDS. Twenty-five of JABATAN KASTAM's counters are like that.
--
-- So September moves to 3u for those, and only those. The monthly USAGE is still u in all three
-- months, so every month still bills exactly what the customer's invoice billed; what changes is
-- the printed reading on that one counter, which had nowhere else to go.
UPDATE e
   SET e.CurrentReading = 3 * c.Usage, e.LastModified = GETDATE()
  FROM dbo.zSCP2_MeterEntry e
  JOIN #c c ON c.ItemMeterKey = e.ItemMeterKey
 WHERE e.PeriodYear = 2026 AND e.PeriodMonth = 9
   AND c.OldOpen - 2 * c.Usage < 0
   AND e.CurrentReading < 2 * c.Usage;

SELECT 'counters given three months : ' + CAST(COUNT(*) AS VARCHAR) AS Result FROM #c
UNION ALL
SELECT 'too small to move back twice : ' + CAST(SUM(CASE WHEN OldOpen - 2 * Usage < 0 THEN 1 ELSE 0 END) AS VARCHAR)
  FROM #c;

-- What each contract now has, month by month. September must still read what it read before.
SELECT c.ContractNo,
       SUM(CASE WHEN e.PeriodMonth = 7 THEN 1 ELSE 0 END) AS Jul,
       SUM(CASE WHEN e.PeriodMonth = 8 THEN 1 ELSE 0 END) AS Aug,
       SUM(CASE WHEN e.PeriodMonth = 9 THEN 1 ELSE 0 END) AS Sep
  FROM dbo.zSCP2_MeterEntry e
  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey
  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
  JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey
 WHERE c.ContractNo LIKE 'DEMO-%' AND e.PeriodYear = 2026 AND e.PeriodMonth IN (7,8,9)
 GROUP BY c.ContractNo ORDER BY c.ContractNo;
GO

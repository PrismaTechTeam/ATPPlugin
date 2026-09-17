-- DEMO-PG's free-months deal, set on the screen and kept here so a re-seed does not lose it.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-pasir-gudang-foc.sql
--
-- "Free for the first 12 months" on all three rental lines, which is what makes the rental invoice
-- come to 0.00 -- the shape HOSPITAL PASIR GUDANG.pdf is kept for. It was entered through Meters &
-- Pricing -> Minimum / waive; seed-pasir-gudang.sql rebuilds the contract from the PDF and knows
-- nothing about it, so run this straight after that one.
--
-- Scope 'G' means the waive speaks for the whole rental line, not for the machine whose meter row
-- happens to carry it. The amount is the line's full rental: 900.00 for the three-machine line,
-- 300.00 for each single.
--
-- Re-runnable: the waive meters are removed and rewritten.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-PG');
IF @ck IS NULL
BEGIN
    RAISERROR('DEMO-PG does not exist -- run seed-pasir-gudang.sql first.', 16, 1);
    RETURN;
END

DELETE m FROM dbo.zSCP2_ItemMeter m
  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
 WHERE i.ContractKey = @ck AND m.MeterRole = 'WAIVE';

INSERT INTO dbo.zSCP2_ItemMeter
 (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
  MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
  RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
  WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
SELECT i.ItemKey, N'WAIVE', N'RENTAL WAIVE', N'WAIVE', '',
       CASE i.ServiceItemNo WHEN N'DEMO-PG-001' THEN -900.00 ELSE -300.00 END,
       0, '', 0, 0, 0, NULL, 0, 'A', 12, 0, 100, 'BKCL', 0, 0, 'G', GETDATE()
  FROM dbo.zSCP2_Item i
 WHERE i.ContractKey = @ck
   AND i.ServiceItemNo IN (N'DEMO-PG-001', N'DEMO-PG-004', N'DEMO-PG-005');

SELECT 'DEMO-PG free months restored on ' +
       CAST(@@ROWCOUNT AS VARCHAR) + ' rental line(s)' AS Result;
GO

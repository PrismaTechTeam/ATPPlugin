-- DEMO-MRA -- the MAJLIS AMANAH RAKYAT (MARA) shape, rebuilt as a contract.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-mara.sql
--
-- Taken from "Type Of BillingFormat/MAJLIS AMANAH RAKYAT (MARA).pdf" -- TWO invoices:
--
--   AMR2607.0020  rental   RA-5 UNIT   5 MTH @ 250.00 = 1,250.00   (+ 6% = 1,325.00)
--   AMR2607.0089  meters   TEN lines, one per machine per colour   = 3,103.86
--
-- This is the opposite end of the range from MBJB. There, 52 machines fold into 11 lines. Here,
-- FIVE machines produce TEN, because nothing is merged at all: every machine prints its own black
-- line and its own colour line, with its serial written into the item code (MR.BK.4MU10545).
--
--     4MU10545  U.PENTADBIRAN (4TH FL)  BK 6,975 @ 0.025 = 174.38   CL 1,115 @ 0.40 =   446.00
--     4MU10548  U.KOMERSIAL             BK 2,724         =  68.10   CL 1,284         =   513.60
--     4MU10549  UKK&P / UPP             BK 6,098         = 152.45   CL 1,817         =   726.80
--     4MU10551  U.PENTADBIRAN (1ST FL)  BK 4,378         = 109.45   CL 1,535         =   614.00
--     4MU10558  PUSMA                   BK 4,203         = 105.08   CL   485         =   194.00
--                                                          ------                      --------
--                                                          609.46                      2,494.40
--                                                                          together     3,103.86
--
-- The rental goes the other way: all five on ONE line at 250.00 each. So the same contract merges
-- the rental completely and the copies not at all -- which is why the two sides are separate
-- settings and not one "grouping" switch.
--
-- Re-runnable: an existing DEMO-MRA is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-MRA');
IF @ck IS NOT NULL
BEGIN
    DELETE e FROM dbo.zSCP2_MeterEntry e
      JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey
      JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey WHERE i.ContractKey = @ck;
    DELETE m FROM dbo.zSCP2_ItemMeter m
      JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey WHERE i.ContractKey = @ck;
    DELETE FROM dbo.zSCP2_Item WHERE ContractKey = @ck;
    DELETE FROM dbo.zSCP2_ContractRentalPrice WHERE ContractKey = @ck;
    DELETE FROM dbo.zSCP2_Contract WHERE ContractKey = @ck;
END

-- RentalSeparateInvoice = 'Y' for the two documents. MeterLineMode 'S' is "one line per machine",
-- which is what makes ten lines out of five machines; the rental stays on 'A' and folds into one.
INSERT INTO dbo.zSCP2_Contract
 (ContractNo, ContractTypeCode, DebtorCode, [Description], BillingFormatCode, UseNewLayout,
  MachineLineShows, ShowModelOnLine, ShowSerialOnLine, ShowUnitsOnLine,
  RentalLineMode, MeterLineMode, BillingMode, BillingDay, ContractDate,
  ServiceStartDate, ServiceExpiryDate, RentalSeparateInvoice, Inactive, CreatedBy, ModifiedBy,
  Created, Modified, LastModified)
VALUES (N'DEMO-MRA', '', N'3000-M0001',
        N'MARA Johor - rental all on one line, copies one line per machine per colour',
        '', 'Y', 'B', 'Y', 'Y', 'Y', 'A', 'S', 'G', 1, GETDATE(), '2025-07-01', '2028-06-30',
        'Y', 'N', 'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

DECLARE @m TABLE (Pos INT, Serial NVARCHAR(60), Site NVARCHAR(80),
                  BkPrev DECIMAL(18,2), BkCurr DECIMAL(18,2),
                  ClPrev DECIMAL(18,2), ClCurr DECIMAL(18,2));
INSERT INTO @m VALUES
 (1, N'4MU10545', N'U.PENTADBIRAN (4TH FL)', 114943, 121918, 25050, 26165),
 (2, N'4MU10548', N'U.KOMERSIAL',             51519,  54243, 22722, 24006),
 (3, N'4MU10549', N'UKK&P / UPP',             91163,  97261, 16432, 18249),
 (4, N'4MU10551', N'U.PENTADBIRAN (1ST FL)',  58769,  63147, 17108, 18643),
 (5, N'4MU10558', N'PUSMA',                   36367,  40570,  5272,  5757);

DECLARE @pos INT, @serial NVARCHAR(60), @site NVARCHAR(80),
        @bkPrev DECIMAL(18,2), @bkCurr DECIMAL(18,2),
        @clPrev DECIMAL(18,2), @clCurr DECIMAL(18,2), @ik BIGINT, @imk BIGINT;

DECLARE mc CURSOR LOCAL FAST_FORWARD FOR
  SELECT Pos, Serial, Site, BkPrev, BkCurr, ClPrev, ClCurr FROM @m ORDER BY Pos;
OPEN mc;
FETCH NEXT FROM mc INTO @pos, @serial, @site, @bkPrev, @bkCurr, @clPrev, @clCurr;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.zSCP2_Item
     (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
      MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
      ServiceStartDate, ServiceExpiryDate, LastModified)
    VALUES (@ck, N'DEMO-MRA-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), N'IRADV DX C3922i', @serial,
            N'MARA - ' + @site, @pos, N'RA', '', '', '',
            'N', 'N', 'ONLINE', '2025-07-01', '2028-06-30', GETDATE());
    SET @ik = SCOPE_IDENTITY();

    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, 250.00, '', 0, 0, 0,
            '2025-07-01', 36, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES (@ik, N'BK', N'BK COPY + PRINT', N'BK', @serial, 0, 0.025, '', 0, 0, @bkPrev,
            NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
    SET @imk = SCOPE_IDENTITY();
    INSERT INTO dbo.zSCP2_MeterEntry
     (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
    VALUES (@imk, 2026, 9, @bkCurr, '2026-09-30', 'MANUAL', 'N', GETDATE());

    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES (@ik, N'CL', N'CL COPY + PRINT', N'CL', @serial, 0, 0.40, '', 0, 0, @clPrev,
            NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
    SET @imk = SCOPE_IDENTITY();
    INSERT INTO dbo.zSCP2_MeterEntry
     (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
    VALUES (@imk, 2026, 9, @clCurr, '2026-09-30', 'MANUAL', 'N', GETDATE());

    FETCH NEXT FROM mc INTO @pos, @serial, @site, @bkPrev, @bkCurr, @clPrev, @clCurr;
END
CLOSE mc;
DEALLOCATE mc;

INSERT INTO dbo.zSCP2_ContractRentalPrice
 (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
VALUES (@ck, 'R', N'RA', 250.00, 0, 0, '', '', GETDATE());

SELECT 'DEMO-MRA built: 5 machines, expect 2 invoices -- rental 1,250.00 and ten meter lines 3,103.86'
       AS Result;
GO

-- DEMO-F19 -- the FASTROCOM / TAG shape, rebuilt as a contract.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-f0019.sql
--
-- Taken from "Type Of BillingFormat/3000-F0019 INV 2607.pdf" -- TWO invoices in the one file:
--
--   the rental   RA-10 UNIT   RENTAL - 07/2026   1 UNIT @ 5,097.00 =  5,097.00
--   the meters   NINETEEN lines, one per machine per colour        = 10,008.94
--
-- The extreme of both settings at once: the rental is a SINGLE lump for ten machines (quantity 1,
-- not 10), and the copies are not merged at all -- every machine prints its own black line and its
-- own colour line, serial written into the item code.
--
--     4WG01119   60,674 ->  72,199   11,525 bk    21,491 -> 26,598   5,107 cl
--     4WG01529   55,120 ->  63,851    8,731       29,101 -> 35,263   6,162
--     4WG01701   48,008 ->  55,573    7,565       32,894 -> 41,890   8,996
--     4WG01700   40,228 ->  45,367    5,139        8,179 ->  9,577   1,398
--     4WG01597   29,069 ->  36,146    7,077       10,132 -> 13,406   3,274
--     4MD21039    8,406 ->  10,231    1,825        7,873 ->  9,472   1,599
--     4MD21127    4,944 ->   5,717      773        1,419 ->  1,580     161
--     4PE20298   24,140 ->  34,800   10,660       colour counter, never moved
--     4PE20299    8,385 ->  10,989    2,604       colour counter, never moved
--     35E32717    2,326 ->   2,808      482       black only -- an IR1643IF II
--                                     ------                        ------
--                                     56,381 @ 0.026 = 1,465.90      26,697 @ 0.32 = 8,543.04
--                                                     together                      10,008.94
--
-- Also worth having: the rental line prints NO (n/N). Their invoice writes "RENTAL - 07/2026", the
-- month, not a position in a term -- so the contract carries no term length and the system must
-- print nothing rather than invent one.
--
-- Re-runnable: an existing DEMO-F19 is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-F19');
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

INSERT INTO dbo.zSCP2_Contract
 (ContractNo, ContractTypeCode, DebtorCode, [Description], BillingFormatCode, UseNewLayout,
  MachineLineShows, ShowModelOnLine, ShowSerialOnLine, ShowUnitsOnLine,
  RentalLineMode, MeterLineMode, BillingMode, BillingDay, ContractDate,
  ServiceStartDate, ServiceExpiryDate, RentalSeparateInvoice, Inactive, CreatedBy, ModifiedBy,
  Created, Modified, LastModified)
VALUES (N'DEMO-F19', '', N'3000-F0019',
        N'Fastrocom / TAG - one lump rental for ten machines, copies one line per machine per colour',
        '', 'Y', 'B', 'Y', 'Y', 'Y', 'A', 'S', 'G', 1, GETDATE(), '2025-01-01', '2027-12-31',
        'Y', 'N', 'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

-- Rental: the whole 5,097.00 on the first machine, quantity 1, and NO term length -- the invoice
-- prints the month, not an (n/N).
DECLARE @m TABLE (Pos INT, Serial NVARCHAR(60), Model NVARCHAR(60), Site NVARCHAR(80),
                  Rental DECIMAL(18,2),
                  BkPrev DECIMAL(18,2), BkCurr DECIMAL(18,2),
                  ClPrev DECIMAL(18,2), ClCurr DECIMAL(18,2));
INSERT INTO @m VALUES
 ( 1, N'4WG01119', N'imageFORCE C5140', N'B.KHIDMAT PENGURUSAN',      5097.00, 60674, 72199, 21491, 26598),
 ( 2, N'4WG01529', N'imageFORCE C5140', N'U.PEMBANGUNAN & PELUPUSAN',       0, 55120, 63851, 29101, 35263),
 ( 3, N'4WG01701', N'imageFORCE C5140', N'U.PEMBANGUNAN FIZIKAL',           0, 48008, 55573, 32894, 41890),
 ( 4, N'4WG01700', N'imageFORCE C5140', N'U.PENDAFTARAN',                   0, 40228, 45367,  8179,  9577),
 ( 5, N'4WG01597', N'imageFORCE C5140', N'U.HASIL',                         0, 29069, 36146, 10132, 13406),
 ( 6, N'4MD21039', N'IRA DVDXC3930',    N'U.PENGUATKUASA / TEKNIKAL',       0,  8406, 10231,  7873,  9472),
 ( 7, N'4MD21127', N'IRA DVDXC3930',    N'CWGN MASJID TANAH',               0,  4944,  5717,  1419,  1580),
 ( 8, N'4PE20298', N'IRADVDX4935I',     N'PEMBANGUNAN MASYARAKAT',          0, 24140, 34800,  3000,  3000),
 ( 9, N'4PE20299', N'IRADVDX4935I',     N'BILIK PA 1',                      0,  8385, 10989,  1200,  1200),
 (10, N'35E32717', N'IR1643IF II',      N'BILIK PA 2',                      0,  2326,  2808,     0,     0);

DECLARE @pos INT, @serial NVARCHAR(60), @model NVARCHAR(60), @site NVARCHAR(80),
        @rental DECIMAL(18,2), @bkPrev DECIMAL(18,2), @bkCurr DECIMAL(18,2),
        @clPrev DECIMAL(18,2), @clCurr DECIMAL(18,2), @ik BIGINT, @imk BIGINT;

DECLARE mc CURSOR LOCAL FAST_FORWARD FOR
  SELECT Pos, Serial, Model, Site, Rental, BkPrev, BkCurr, ClPrev, ClCurr FROM @m ORDER BY Pos;
OPEN mc;
FETCH NEXT FROM mc INTO @pos, @serial, @model, @site, @rental, @bkPrev, @bkCurr, @clPrev, @clCurr;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.zSCP2_Item
     (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
      MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
      ServiceStartDate, ServiceExpiryDate, LastModified)
    VALUES (@ck, N'DEMO-F19-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), @model, @serial,
            N'TAG ' + @site, @pos, N'RA', '', '', '',
            'N', 'N', 'ONLINE', '2025-01-01', '2027-12-31', GETDATE());
    SET @ik = SCOPE_IDENTITY();

    -- RentalMonths 0 -- no term, so no (n/N) on the line.
    IF @rental > 0
        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES (@ik, N'RENTAL', N'RENTAL', N'RENTAL', '', 0, @rental, '', 0, 0, 0,
                NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES (@ik, N'BK', N'BK COPY+PRINT A4&A3', N'BK', @serial, 0, 0.026, '', 0, 0, @bkPrev,
            NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
    SET @imk = SCOPE_IDENTITY();
    INSERT INTO dbo.zSCP2_MeterEntry
     (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
    VALUES (@imk, 2026, 9, @bkCurr, '2026-09-30', 'MANUAL', 'N', GETDATE());

    -- The IR1643IF II is a mono machine and has no colour counter at all. The two 4935I machines
    -- have one that has not moved: it is read, it prints, and it costs nothing.
    IF @clPrev > 0 OR @clCurr > 0
    BEGIN
        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES (@ik, N'CL', N'CL COPY+PRINT A4&A3', N'CL', @serial, 0, 0.32, '', 0, 0, @clPrev,
                NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
        SET @imk = SCOPE_IDENTITY();
        INSERT INTO dbo.zSCP2_MeterEntry
         (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
        VALUES (@imk, 2026, 9, @clCurr, '2026-09-30', 'MANUAL', 'N', GETDATE());
    END

    FETCH NEXT FROM mc INTO @pos, @serial, @model, @site, @rental, @bkPrev, @bkCurr, @clPrev, @clCurr;
END
CLOSE mc;
DEALLOCATE mc;

INSERT INTO dbo.zSCP2_ContractRentalPrice
 (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
VALUES (@ck, 'R', N'RA', 5097.00, 0, 0, '', '', GETDATE());

SELECT 'DEMO-F19 built: 10 machines, expect 2 invoices -- rental 5,097.00 and 19 meter lines 10,008.94'
       AS Result;
GO

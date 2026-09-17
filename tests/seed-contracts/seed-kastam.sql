-- DEMO-KST -- the JABATAN KASTAM TANJUNG KUPANG shape, rebuilt as a contract.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-kastam.sql
--
-- Taken from "Type Of BillingFormat/JABATAN KASTAM_TotalAmountFromEachMachine-butrentallampsum.pdf"
-- -- TWO invoices, and the largest line counts in the whole folder:
--
--   AMR2607.0074  rental  TWENTY-THREE lines, one per machine   =  8,600.00   (5 pages)
--                           1 x 900.00 + 11 x 420.00 + 11 x 280.00
--   AMR2607.0124  meters  THIRTY-SEVEN lines                    =  3,691.20   (17 pages)
--
-- Nothing merges on either side. Twenty-four machines produce 23 rental lines (one is not rented)
-- and 37 meter lines, each carrying its own serial in the item code -- RA-4WE04767_E on the rental,
-- MR.BK.4WE04767 on the copies.
--
-- WHAT THIS CONTRACT PROVES: free copies and a rebate, both, on a fleet this size.
--
--     500 free black copies a month, per machine, then 3% off what is left, cut to a whole copy.
--     Colour has no allowance and the same 3%.
--
--   4WE04767  black  5,232 less 500 = 4,732 less 3% (141) = 4,591 @ 0.03 = 137.73
--             colour   811                  less 3% ( 24) =   787 @ 0.30 = 236.10
--   4MD20341  black  4,033 less 500 = 3,533 less 3% (105) = 3,428 @ 0.03 = 102.84
--
-- Three machines print less than their 500 free copies (4MD21647, 4PE00949 and 22W03058) and bill
-- nothing at all -- their lines are still on the invoice.
--
--     black   42,980 charged copies @ 0.03 = 1,289.40
--     colour   8,006                @ 0.30 = 2,401.80
--                                            --------
--                                            3,691.20   over 50,986 copies
--
-- 22W03058 is the odd one out: no rental, and an allowance on BOTH counters, so it bills 0.00 on
-- two lines. It is in the fleet, it is read, and the invoice says so.
--
-- Re-runnable: an existing DEMO-KST is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-KST');
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

-- Nothing merges: RentalLineMode 'S' and MeterLineMode 'S' both mean "one line per machine".
INSERT INTO dbo.zSCP2_Contract
 (ContractNo, ContractTypeCode, DebtorCode, [Description], BillingFormatCode, UseNewLayout,
  MachineLineShows, ShowModelOnLine, ShowSerialOnLine, ShowUnitsOnLine,
  RentalLineMode, MeterLineMode, BillingMode, BillingDay, ContractDate,
  ServiceStartDate, ServiceExpiryDate, RentalSeparateInvoice, Inactive, CreatedBy, ModifiedBy,
  Created, Modified, LastModified)
VALUES (N'DEMO-KST', '', N'3000-J0026',
        N'Jabatan Kastam Tg Kupang - nothing merged: 23 rental lines and 37 meter lines, 500 free copies and 3% off',
        '', 'Y', 'B', 'Y', 'Y', 'Y', 'S', 'S', 'G', 1, GETDATE(), '2025-01-01', '2027-12-31',
        'Y', 'N', 'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

-- HasCl says whether the machine has a colour counter at all; eleven of them do not.
DECLARE @m TABLE (Pos INT, Serial NVARCHAR(60), Model NVARCHAR(60), Site NVARCHAR(80),
                  Rental DECIMAL(18,2), ClFoc DECIMAL(18,2), HasCl CHAR(1),
                  BkPrev DECIMAL(18,2), BkCurr DECIMAL(18,2),
                  ClPrev DECIMAL(18,2), ClCurr DECIMAL(18,2));
INSERT INTO @m VALUES
 ( 1, N'4WE04767', N'imageFORCE C5170', N'KPSM PENTADBIRAN',        900.00,   0, 'Y', 2630,  7862,  691, 1502),
 ( 2, N'4MD20341', N'IRADVDXC3930I',    N'KPSM KEWANGAN',           420.00,   0, 'Y', 2526,  6559,  310,  667),
 ( 3, N'4MD20360', N'IRADVDXC3930I',    N'KPSM ASET / STOR',        420.00,   0, 'Y',  991,  2272,  428,  964),
 ( 4, N'4MD20361', N'IRADVDXC3930I',    N'KPSM U.SULIT / LATIHAN',  420.00,   0, 'Y', 1609,  3149,  214, 1378),
 ( 5, N'4MD21633', N'IRADVDXC3930I',    N'PENGUATKUASAAN 1',        420.00,   0, 'Y', 1864,  3530,  495, 1356),
 ( 6, N'4MD21637', N'IRADVDXC3930I',    N'IMP. URUSETIA',           420.00,   0, 'Y', 1580,  3746,  513, 1214),
 ( 7, N'4MD21642', N'IRADVDXC3930I',    N'EKS. URUSETIA',           420.00,   0, 'Y',  983,  2457,  570, 1450),
 ( 8, N'4MD21647', N'IRADVDXC3930I',    N'EKS. OPERASI PENAKSIR',   420.00,   0, 'Y',  548,   950,   56,  125),
 ( 9, N'4MD21651', N'IRADVDXC3930I',    N'PENGUATKUASAAN 2',        420.00,   0, 'Y', 1560,  4178,  781, 1382),
 (10, N'4MD21652', N'IRADVDXC3930I',    N'PENUMPANG URUSETIA',      420.00,   0, 'Y',  395,  1057,  357, 1563),
 (11, N'4MD21653', N'IRADVDXC3930I',    N'URUSETIA PTP',            420.00,   0, 'Y', 1015,  3227, 1051, 2026),
 (12, N'4MD21654', N'IRADVDXC3930I',    N'PENGURUSAN PUTERI',       420.00,   0, 'Y', 1913,  3472,   67,  155),
 (13, N'4PE20707', N'IRADVDX4935I',     N'KPSM TEK. MAKLUMAT',      280.00,   0, 'N',  586,  1513,    0,    0),
 (14, N'4PE20704', N'IRADVDX4935I',     N'IMP. OPERASI PENAKSIR',   280.00,   0, 'N',  952,  1732,    0,    0),
 (15, N'4PE20719', N'IRADVDX4935I',     N'EKS. OPERASI JURUWANG',   280.00,   0, 'N', 3724,  7663,    0,    0),
 (16, N'4PE20709', N'IRADVDX4935I',     N'KPSM U.HASIL',            280.00,   0, 'N', 2443,  4770,    0,    0),
 (17, N'4PE20722', N'IRADVDX4935I',     N'OPERASI PTP',             280.00,   0, 'N', 5600, 11835,    0,    0),
 (18, N'4PE20714', N'IRADVDX4935I',     N'IMP. OPERASI JURUWANG',   280.00,   0, 'N', 4581, 12521,    0,    0),
 (19, N'4PE00949', N'IRADVDX4935I',     N'KPSM PENG. ASET',         280.00,   0, 'N',  100,   578,    0,    0),
 (20, N'4PE00940', N'IRADVDX4935I',     N'PENGURUSAN FOREST',       280.00,   0, 'N', 1640,  3272,    0,    0),
 (21, N'4PE00948', N'IRADVDX4935I',     N'CBM URUSETIA',            280.00,   0, 'N', 1528,  4786,    0,    0),
 (22, N'4PE00926', N'IRADVDX4935I',     N'PENUMPANG PKR',           280.00,   0, 'N', 1711,  3349,    0,    0),
 (23, N'4PE00938', N'IRADVDX4935I',     N'PENUMPANG UKPP',          280.00,   0, 'N', 2164,  3842,    0,    0),
 (24, N'22W03058', N'IR C3226',         N'BILIK SERVER',                 0, 500, 'Y',  175,   344,  110,  309);

DECLARE @pos INT, @serial NVARCHAR(60), @model NVARCHAR(60), @site NVARCHAR(80),
        @rental DECIMAL(18,2), @clFoc DECIMAL(18,2), @hasCl CHAR(1),
        @bkPrev DECIMAL(18,2), @bkCurr DECIMAL(18,2),
        @clPrev DECIMAL(18,2), @clCurr DECIMAL(18,2), @ik BIGINT, @imk BIGINT;

DECLARE mc CURSOR LOCAL FAST_FORWARD FOR
  SELECT Pos, Serial, Model, Site, Rental, ClFoc, HasCl, BkPrev, BkCurr, ClPrev, ClCurr
    FROM @m ORDER BY Pos;
OPEN mc;
FETCH NEXT FROM mc INTO @pos, @serial, @model, @site, @rental, @clFoc, @hasCl,
                        @bkPrev, @bkCurr, @clPrev, @clCurr;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.zSCP2_Item
     (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
      MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
      ServiceStartDate, ServiceExpiryDate, LastModified)
    VALUES (@ck, N'DEMO-KST-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), @model, @serial,
            N'KASTAM TG - ' + @site, @pos, '', '', '', '',
            'N', 'N', 'ONLINE', '2025-01-01', '2027-12-31', GETDATE());
    SET @ik = SCOPE_IDENTITY();

    IF @rental > 0
        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', @serial, 0, @rental, '', 0, 0, 0,
                NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

    -- 500 free black copies a month, then 3% off what is left.
    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES (@ik, N'BK', N'BK COPY+PRINT A4&A3', N'BK', @serial, 0, 0.03, '', 3, 500, @bkPrev,
            NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
    SET @imk = SCOPE_IDENTITY();
    INSERT INTO dbo.zSCP2_MeterEntry
     (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
    VALUES (@imk, 2026, 9, @bkCurr, '2026-09-30', 'MANUAL', 'N', GETDATE());

    IF @hasCl = 'Y'
    BEGIN
        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES (@ik, N'CL', N'CL COPY+PRINT A4&A3', N'CL', @serial, 0, 0.30, '', 3, @clFoc, @clPrev,
                NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
        SET @imk = SCOPE_IDENTITY();
        INSERT INTO dbo.zSCP2_MeterEntry
         (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
        VALUES (@imk, 2026, 9, @clCurr, '2026-09-30', 'MANUAL', 'N', GETDATE());
    END

    FETCH NEXT FROM mc INTO @pos, @serial, @model, @site, @rental, @clFoc, @hasCl,
                            @bkPrev, @bkCurr, @clPrev, @clCurr;
END
CLOSE mc;
DEALLOCATE mc;

SELECT 'DEMO-KST built: 24 machines, expect 2 invoices -- rental 8,600.00 and 37 meter lines 3,691.20'
       AS Result;
GO

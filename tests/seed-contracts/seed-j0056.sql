-- DEMO-J56 -- the JABATAN PERLINDUNGAN HIDUPAN LIAR shape, rebuilt as a contract.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-j0056.sql
--
-- Taken from "Type Of BillingFormat/Normal_3000-J0056 INV 2607.pdf", invoice MR2607.1134, and the
-- appendix that supports it ("3000-J0056 METER 2607.pdf" -- a worksheet, not a second invoice).
-- Four machines, ONE invoice, three lines:
--
--     201-BK C+ P    BK COPY + PRINT A4&A3     4,187 @ 0.03   =   125.61
--     223-COLOR C+ P COLOR COPY + PRINT A4&A3    272 @ 0.35   =    95.20
--     RA-34MTH       MONTHLY RENTAL (31/34)        1 @ 808.00 =   808.00
--                                                Net Total        1,028.81
--
-- The plainest of all the shapes and worth having as the baseline: everything merges, one invoice,
-- and the rental is a single figure for the fleet. Three lines from four machines.
--
--     UMU02418  MUAR            13,465 ->  14,211      746 black
--     2RH00983  BANDAR PENAWAR  17,580 ->  18,558      978
--     2RH00988  MERSING         21,486 ->  22,082      596
--     4MD02031  SANTUARI GAJAH  59,565 ->  61,432    1,867 black + 272 colour
--                                                    -----
--                                                    4,187   and 112,096 -> 116,283 on the line
--
-- Only one of the four has a colour counter, so the colour line stands for one machine while the
-- black line stands for all four -- on the same invoice, from the same rule.
--
-- The rental term is 34 months, not the usual 36. Nothing in the system should assume 36.
--
-- Re-runnable: an existing DEMO-J56 is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-J56');
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
VALUES (N'DEMO-J56', '', N'3000-J0056',
        N'Jabatan Perlindungan Hidupan Liar - one invoice, everything merged, 34-month term',
        '', 'Y', 'L', 'N', 'N', 'N', 'A', 'A', 'G', 1, GETDATE(), '2024-03-01', '2027-01-31',
        'N', 'N', 'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

-- Rental is a lump for the fleet, so it sits on the first machine only: quantity 1 at 808.00.
DECLARE @m TABLE (Pos INT, Serial NVARCHAR(60), Model NVARCHAR(60), Site NVARCHAR(80),
                  Rental DECIMAL(18,2),
                  BkPrev DECIMAL(18,2), BkCurr DECIMAL(18,2),
                  ClPrev DECIMAL(18,2), ClCurr DECIMAL(18,2));
INSERT INTO @m VALUES
 (1, N'UMU02418', N'iR-ADV C4535i',    N'PERHILITAN DAERAH MUAR',    808.00, 13465, 14211,     0,     0),
 (2, N'2RH00983', N'iR-ADV C4535i',    N'PERHILITAN BANDAR PENAWAR',      0, 17580, 18558,     0,     0),
 (3, N'2RH00988', N'iR-ADV C4535i',    N'PERHILITAN DAERAH MERSING',      0, 21486, 22082,     0,     0),
 (4, N'4MD02031', N'IR ADV DX C3930i', N'PUSAT SANTUARI GAJAH JOHOR',     0, 59565, 61432, 12012, 12284);

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
    VALUES (@ck, N'DEMO-J56-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), @model, @serial,
            @model + N' / ' + @site, @pos, N'RA', N'FLEET', '', '',
            'N', 'N', 'ONLINE', '2024-03-01', '2027-01-31', GETDATE());
    SET @ik = SCOPE_IDENTITY();

    IF @rental > 0
        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, @rental, '', 0, 0, 0,
                '2024-03-01', 34, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES (@ik, N'BK', N'BK COPY + PRINT A4&A3', N'BK', '', 0, 0.03, '', 0, 0, @bkPrev,
            NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
    SET @imk = SCOPE_IDENTITY();
    INSERT INTO dbo.zSCP2_MeterEntry
     (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
    VALUES (@imk, 2026, 9, @bkCurr, '2026-09-30', 'MANUAL', 'N', GETDATE());

    IF @clPrev > 0 OR @clCurr > 0
    BEGIN
        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES (@ik, N'CL', N'COLOR COPY + PRINT A4&A3', N'CL', '', 0, 0.35, '', 0, 0, @clPrev,
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
VALUES (@ck, 'R', N'RA',    808.00, 0, 0, '', '', GETDATE()),
       (@ck, 'M', N'FLEET', 0, 0.03, 0.35, '', '', GETDATE());

SELECT 'DEMO-J56 built: 4 machines, expect 3 lines and 1,028.81' AS Result;
GO

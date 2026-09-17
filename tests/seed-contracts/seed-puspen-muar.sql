-- DEMO-PPM -- the PUSPEN MUAR shape, rebuilt as a contract.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-puspen-muar.sql
--
-- Taken from "Type Of BillingFormat/PUSAT PEMULIHAN PENAGIHAN NARKOTIK (PUSPEN MUAR).pdf", invoice
-- AMR2607.0138. Four machines, one invoice, four lines:
--
--     109-BK C+ P      BK COPY + PRINT      18,518 @ 0.03   =   555.54
--     124-COLOR C+ P   COLOR COPY + PRINT      922 @ 0.30   =   276.60
--     RA - 1 UNIT      IRADVC5860I               1 @ 820.00 =   820.00
--     RA - 2 UNIT      IRADV4945I                2 @ 425.00 =   850.00
--                                              Net Total       2,502.14
--
-- The rental is grouped BY MODEL and the copies are one line for the whole fleet -- so again the
-- two groupings do not line up.
--
-- WHAT THIS CONTRACT IS FOR: free copies AND a rebate, in that order.
--
--     2XP70035   7,037 black   less 1,000 free = 6,037   less 3% (181) = 5,856
--     4NL20188   6,807         less 1,000      = 5,807   less 3% (174) = 5,633
--     4NL20201   4,733         less 1,000      = 3,733   less 3% (111) = 3,622
--     WSG02689   3,512         no allowance    = 3,512   less 3% (105) = 3,407
--                                                                     -------
--                                                                      18,518
--
-- The rebate is worked out on what is LEFT after the free copies, not on the meter's usage, and it
-- is cut down to a whole copy each time. Take the 3% first and the invoice is out; take it on the
-- fleet total and it is out again.
--
-- The fourth machine has a colour counter that has not moved (5 to 5) and no rental. It still
-- prints its black copies.
--
-- Re-runnable: an existing DEMO-PPM is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-PPM');
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
VALUES (N'DEMO-PPM', '', N'3000-P0013',
        N'Puspen Muar - free copies then a 3% rebate, rental by model, copies as one line',
        '', 'Y', 'L', 'Y', 'N', 'Y', 'A', 'A', 'G', 1, GETDATE(), '2026-01-01', '2028-12-31',
        'N', 'N', 'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

-- Grp is the rental line (by model); FLEET is the meter line. Foc is the free black copies a month.
DECLARE @m TABLE (Pos INT, Serial NVARCHAR(60), Model NVARCHAR(60), Grp NVARCHAR(20),
                  Rental DECIMAL(18,2), Foc DECIMAL(18,2),
                  BkPrev DECIMAL(18,2), BkCurr DECIMAL(18,2),
                  ClPrev DECIMAL(18,2), ClCurr DECIMAL(18,2));
INSERT INTO @m VALUES
 (1, N'2XP70035', N'iRADVDXC5860', N'RA5860', 820.00, 1000, 38508, 45545, 4071, 5021),
 (2, N'4NL20188', N'IRADVDX4945I', N'RA4945', 425.00, 1000, 20131, 26938,    0,    0),
 (3, N'4NL20201', N'IRADVDX4945I', N'RA4945', 425.00, 1000, 40994, 45727,    0,    0),
 (4, N'WSG02689', N'IRADC3530I',   '',            0,     0,  6185,  9697,    5,    5);

DECLARE @pos INT, @serial NVARCHAR(60), @model NVARCHAR(60), @grp NVARCHAR(20),
        @rental DECIMAL(18,2), @foc DECIMAL(18,2),
        @bkPrev DECIMAL(18,2), @bkCurr DECIMAL(18,2),
        @clPrev DECIMAL(18,2), @clCurr DECIMAL(18,2), @ik BIGINT, @imk BIGINT;

DECLARE mc CURSOR LOCAL FAST_FORWARD FOR
  SELECT Pos, Serial, Model, Grp, Rental, Foc, BkPrev, BkCurr, ClPrev, ClCurr FROM @m ORDER BY Pos;
OPEN mc;
FETCH NEXT FROM mc INTO @pos, @serial, @model, @grp, @rental, @foc, @bkPrev, @bkCurr, @clPrev, @clCurr;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.zSCP2_Item
     (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
      MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
      ServiceStartDate, ServiceExpiryDate, LastModified)
    VALUES (@ck, N'DEMO-PPM-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), @model, @serial,
            @model + N' / PUSPEN MUAR', @pos, @grp, N'FLEET', '', '',
            'N', 'N', 'ONLINE', '2026-01-01', '2028-12-31', GETDATE());
    SET @ik = SCOPE_IDENTITY();

    IF @rental > 0
        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, @rental, '', 0, 0, 0,
                '2026-01-01', 36, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES (@ik, N'BK', N'BK COPY + PRINT A4&A3', N'BK', '', 0, 0.03, '', 3, @foc, @bkPrev,
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
        VALUES (@ik, N'CL', N'COLOR COPY + PRINT A4&A3', N'CL', '', 0, 0.30, '', 3, 0, @clPrev,
                NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
        SET @imk = SCOPE_IDENTITY();
        INSERT INTO dbo.zSCP2_MeterEntry
         (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
        VALUES (@imk, 2026, 9, @clCurr, '2026-09-30', 'MANUAL', 'N', GETDATE());
    END

    FETCH NEXT FROM mc INTO @pos, @serial, @model, @grp, @rental, @foc, @bkPrev, @bkCurr, @clPrev, @clCurr;
END
CLOSE mc;
DEALLOCATE mc;

INSERT INTO dbo.zSCP2_ContractRentalPrice
 (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
VALUES (@ck, 'R', N'RA5860', 820.00, 0, 0, '', '', GETDATE()),
       (@ck, 'R', N'RA4945', 425.00, 0, 0, '', '', GETDATE()),
       (@ck, 'M', N'FLEET',  0, 0.03, 0.30, '', '', GETDATE());

SELECT 'DEMO-PPM built: 4 machines, expect 4 lines and 2,502.14' AS Result;
GO

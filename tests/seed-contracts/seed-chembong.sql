-- DEMO-IKT -- the IKTBN CHEMBONG shape, rebuilt as a contract.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-chembong.sql
--
-- Taken from "Type Of BillingFormat/INSTITUT KEMAHIRAN TINGGI BELIA NEGARA (IKTBN) CHEMBONG...pdf",
-- invoice AMR2607.0108. Four machines, one invoice, four lines:
--
--     01.RA-1 UNIT  IRADVDXC3935I 1 UNIT - RENTAL (11/36)   1 @ 654.00 =   654.00
--     02.RA-2 UNIT  IRADVDX6855I  2 UNIT - RENTAL (11/36)   1 @ 984.00 =   984.00
--     109-BK C+ P   BK COPY + PRINT A4&A3               61,548 @ 0.03  = 1,846.44
--     124-COLOR C+P COLOR COPY + PRINT A4&A3             5,317 @ 0.30  = 1,595.10
--                                                       Net Total        5,079.54
--
-- THE RENTAL LINE HERE IS A LUMP SUM. Line 2 covers TWO machines and prints "1 MTH @ 984.00", not
-- "2 @ 492.00" -- even though the worksheet charges each of them 492. That is a different way of
-- writing the same money from MBJB (36 units x 540.00) and from PERMAI LAMA (7 units x 334.40),
-- and the difference is visible to the customer, so the system has to be able to say both.
--
-- We do it by giving one machine of the pair the whole 984.00 and the other none: quantity 1, price
-- 984.00, exactly as printed.
--
-- The fourth machine (XYM01741) is not rented at all -- it prints copies and nothing else. So the
-- rental lines cover three machines while the meter lines cover four.
--
--     4LS20112   65,164 ->  74,955   9,791 black + 5,317 colour
--     36A01664  123,135 -> 144,336  21,201
--     36A70005  125,785 -> 146,935  21,150
--     XYM01741   77,267 ->  86,673   9,406
--                                   ------
--                                   61,548   and 391,351 -> 452,899 on the printed line
--
-- Re-runnable: an existing DEMO-IKT is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-IKT');
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
VALUES (N'DEMO-IKT', '', N'3000-I0001',
        N'IKTBN Chembong - the rental of a pair printed as ONE amount, copies as one line for all four',
        '', 'Y', 'L', 'Y', 'N', 'Y', 'A', 'A', 'G', 1, GETDATE(), '2025-11-01', '2028-10-31',
        'N', 'N', 'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

-- Rental holds the LUMP for its line and sits on one machine of the group; the other carries none.
DECLARE @m TABLE (Pos INT, Serial NVARCHAR(60), Model NVARCHAR(60), Grp NVARCHAR(20),
                  Rental DECIMAL(18,2),
                  BkPrev DECIMAL(18,2), BkCurr DECIMAL(18,2),
                  ClPrev DECIMAL(18,2), ClCurr DECIMAL(18,2));
INSERT INTO @m VALUES
 (1, N'4LS20112', N'IRADV DX C3935i', N'RA3935', 654.00,  65164,  74955, 25393, 30710),
 (2, N'36A01664', N'IRADV DX 6855I',  N'RA6855', 984.00, 123135, 144336,     0,     0),
 (3, N'36A70005', N'IRADV DX 6855I',  N'RA6855',      0, 125785, 146935,     0,     0),
 (4, N'XYM01741', N'IRADC3530I',      '',             0,  77267,  86673,     0,     0);

DECLARE @pos INT, @serial NVARCHAR(60), @model NVARCHAR(60), @grp NVARCHAR(20),
        @rental DECIMAL(18,2), @bkPrev DECIMAL(18,2), @bkCurr DECIMAL(18,2),
        @clPrev DECIMAL(18,2), @clCurr DECIMAL(18,2), @ik BIGINT, @imk BIGINT;

DECLARE mc CURSOR LOCAL FAST_FORWARD FOR
  SELECT Pos, Serial, Model, Grp, Rental, BkPrev, BkCurr, ClPrev, ClCurr FROM @m ORDER BY Pos;
OPEN mc;
FETCH NEXT FROM mc INTO @pos, @serial, @model, @grp, @rental, @bkPrev, @bkCurr, @clPrev, @clCurr;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.zSCP2_Item
     (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
      MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
      ServiceStartDate, ServiceExpiryDate, LastModified)
    VALUES (@ck, N'DEMO-IKT-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), @model, @serial,
            @model + N' / IKTBN CHEMBONG', @pos, @grp, N'FLEET', '', '',
            'N', 'N', 'ONLINE', '2025-11-01', '2028-10-31', GETDATE());
    SET @ik = SCOPE_IDENTITY();

    IF @rental > 0
        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, @rental, '', 0, 0, 0,
                '2025-11-01', 36, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

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
        VALUES (@ik, N'CL', N'COLOR COPY + PRINT A4&A3', N'CL', '', 0, 0.30, '', 0, 0, @clPrev,
                NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
        SET @imk = SCOPE_IDENTITY();
        INSERT INTO dbo.zSCP2_MeterEntry
         (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
        VALUES (@imk, 2026, 9, @clCurr, '2026-09-30', 'MANUAL', 'N', GETDATE());
    END

    FETCH NEXT FROM mc INTO @pos, @serial, @model, @grp, @rental, @bkPrev, @bkCurr, @clPrev, @clCurr;
END
CLOSE mc;
DEALLOCATE mc;

INSERT INTO dbo.zSCP2_ContractRentalPrice
 (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
VALUES (@ck, 'R', N'RA3935', 654.00, 0, 0, '', '', GETDATE()),
       (@ck, 'R', N'RA6855', 984.00, 0, 0, '', '', GETDATE()),
       (@ck, 'M', N'FLEET',  0, 0.03, 0.30, '', '', GETDATE());

SELECT 'DEMO-IKT built: 4 machines, expect 4 lines and 5,079.54' AS Result;
GO

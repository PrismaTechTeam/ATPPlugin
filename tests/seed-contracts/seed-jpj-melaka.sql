-- DEMO-JPJ -- the JPJ MELAKA shape, rebuilt as a contract.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-jpj-melaka.sql
--
-- Taken from "Type Of BillingFormat/JPJ MELAKA_SUMTotalAmountFromBKCLbutNoRental.pdf", invoice
-- MR2607.0227. Twelve machines, one invoice, five lines:
--
--     01.RA-1 UNIT   HEAVY DUTY                1 @ 1,287.25 =  1,287.25
--     02.RA-5 UNIT   MEDIUM DUTY               5 @   655.50 =  3,277.50
--     03.RA-6 UNIT   LIGHT DUTY                6 @   476.90 =  2,861.40
--     201-BK C+ P    BK COPY + PRINT      80,720 @    0.0285 = 2,300.52
--     223-COLOR C+ P COLOR COPY + PRINT   10,643 @    0.2850 = 3,033.26
--                                              Net Total      12,759.93
--
-- Three duty classes on the rental side, the WHOLE FLEET on the meter side. Nothing lines up: the
-- copies of a heavy machine and a light one land on the same two lines, and the rental of the same
-- two machines lands on different ones.
--
-- THE LINE THAT PROVES THE ROUNDING RULE. Their worksheet adds the colour charges machine by
-- machine and reaches 3,033.29. The invoice prints 3,033.26, which is 10,643 x 0.285 -- the
-- quantity and price actually shown on the line. Three cents, and the invoice's own total follows
-- the line, not the worksheet: 12,759.93 against the worksheet's 12,759.96.
--
-- The heavy machine (28B01247) prints 32,074 black copies and no colour at all -- more than a third
-- of the fleet's black on its own, which is why it has a duty class and a rental to itself.
--
-- Re-runnable: an existing DEMO-JPJ is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-JPJ');
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
VALUES (N'DEMO-JPJ', '', N'3000-S0136',
        N'JPJ Melaka - rental in three duty classes, copies as one BK and one CL line for the whole fleet',
        '', 'Y', 'L', 'N', 'N', 'Y', 'A', 'A', 'G', 1, GETDATE(), '2024-12-01', '2026-11-30',
        'N', 'N', 'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

-- Duty decides the rental line. FLEET is the meter line -- one for black, one for colour, all twelve.
DECLARE @m TABLE (Pos INT, Serial NVARCHAR(60), Model NVARCHAR(60), Duty NVARCHAR(20),
                  Rental DECIMAL(18,2),
                  BkPrev DECIMAL(18,2), BkCurr DECIMAL(18,2),
                  ClPrev DECIMAL(18,2), ClCurr DECIMAL(18,2));
INSERT INTO @m VALUES
 ( 1, N'28B01247', N'iR-ADV DX 8986',   N'HEAVY',  1287.25, 532899, 564973,      0,     0),
 ( 2, N'4LS03017', N'IR ADV DX C3935I', N'MEDIUM',  655.50, 308980, 324998,  36174, 38779),
 ( 3, N'4LS03018', N'IR ADV DX C3935I', N'MEDIUM',  655.50, 193426, 199596,  52473, 55598),
 ( 4, N'4LS03019', N'IR ADV DX C3935I', N'MEDIUM',  655.50, 122360, 127223,  12137, 12963),
 ( 5, N'4LS03033', N'IR ADV DX C3935I', N'MEDIUM',  655.50, 150793, 153187,  37445, 39409),
 ( 6, N'4LS03010', N'IR ADV DX C3935I', N'MEDIUM',  655.50, 131513, 135518,  12160, 12449),
 ( 7, N'4MP02932', N'IR ADV DX C3926I', N'LIGHT',   476.90,  88654,  91263,   8528,  8697),
 ( 8, N'4MP02938', N'IR ADV DX C3926I', N'LIGHT',   476.90, 200890, 207821,   6741,  6998),
 ( 9, N'4MP02939', N'IR ADV DX C3926I', N'LIGHT',   476.90,  35554,  37126,  10884, 11532),
 (10, N'4MP02582', N'IR ADV DX C3926I', N'LIGHT',   476.90,  55650,  57676,   7196,  7401),
 (11, N'4MP02949', N'IR ADV DX C3926I', N'LIGHT',   476.90,  36246,  37519,   9235,  9516),
 (12, N'4MP02580', N'IR ADV DX C3926I', N'LIGHT',   476.90,  25032,  25817,  11976, 12250);

DECLARE @pos INT, @serial NVARCHAR(60), @model NVARCHAR(60), @duty NVARCHAR(20),
        @rental DECIMAL(18,2), @bkPrev DECIMAL(18,2), @bkCurr DECIMAL(18,2),
        @clPrev DECIMAL(18,2), @clCurr DECIMAL(18,2), @ik BIGINT, @imk BIGINT;

DECLARE mc CURSOR LOCAL FAST_FORWARD FOR
  SELECT Pos, Serial, Model, Duty, Rental, BkPrev, BkCurr, ClPrev, ClCurr FROM @m ORDER BY Pos;
OPEN mc;
FETCH NEXT FROM mc INTO @pos, @serial, @model, @duty, @rental, @bkPrev, @bkCurr, @clPrev, @clCurr;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.zSCP2_Item
     (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
      MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
      ServiceStartDate, ServiceExpiryDate, LastModified)
    VALUES (@ck, N'DEMO-JPJ-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), @model, @serial,
            @model + N' / JPJ MELAKA', @pos, @duty, N'FLEET', '', @duty + N' DUTY',
            'N', 'N', 'ONLINE', '2024-12-01', '2026-11-30', GETDATE());
    SET @ik = SCOPE_IDENTITY();

    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, @rental, '', 0, 0, 0,
            '2024-12-01', 24, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES (@ik, N'BK', N'BK COPY + PRINT A4&A3', N'BK', '', 0, 0.0285, '', 0, 0, @bkPrev,
            NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
    SET @imk = SCOPE_IDENTITY();
    INSERT INTO dbo.zSCP2_MeterEntry
     (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
    VALUES (@imk, 2026, 9, @bkCurr, '2026-09-30', 'MANUAL', 'N', GETDATE());

    -- The heavy machine has no colour counter at all.
    IF @clPrev > 0 OR @clCurr > 0
    BEGIN
        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES (@ik, N'CL', N'COLOR COPY + PRINT A4&A3', N'CL', '', 0, 0.2850, '', 0, 0, @clPrev,
                NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
        SET @imk = SCOPE_IDENTITY();
        INSERT INTO dbo.zSCP2_MeterEntry
         (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
        VALUES (@imk, 2026, 9, @clCurr, '2026-09-30', 'MANUAL', 'N', GETDATE());
    END

    FETCH NEXT FROM mc INTO @pos, @serial, @model, @duty, @rental, @bkPrev, @bkCurr, @clPrev, @clCurr;
END
CLOSE mc;
DEALLOCATE mc;

INSERT INTO dbo.zSCP2_ContractRentalPrice
 (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
VALUES (@ck, 'R', N'HEAVY',  1287.25, 0, 0, '', '', GETDATE()),
       (@ck, 'R', N'MEDIUM',  655.50, 0, 0, '', '', GETDATE()),
       (@ck, 'R', N'LIGHT',   476.90, 0, 0, '', '', GETDATE()),
       (@ck, 'M', N'FLEET',   0, 0.0285, 0.2850, '', '', GETDATE());

SELECT 'DEMO-JPJ built: 12 machines, expect 5 lines and 12,759.93' AS Result;
GO

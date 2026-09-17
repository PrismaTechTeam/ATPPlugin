-- DEMO-IPG -- the IPG KAMPUS TENGKU AMPUAN AFZAN shape, rebuilt as a contract.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-ipg-afzan.sql
--
-- Taken from "Type Of BillingFormat/INSTITUT PENDIDIKAN GURU KAMPUS TENGKU AMPUAN AFZAN...pdf",
-- invoice MR2607.1439. Six machines, one invoice, four lines:
--
--     01.RA-1 UNIT   IRADVC3930I  - RENTAL (12/36)    1 @ 237.50 =   237.50
--     02.RA-5 UNIT   IRADVDX4945I - RENTAL (12/36)    5 @ 266.00 = 1,330.00
--     03.MR.BK.      BK COPY                     32,085 @  0.0285 =  914.42
--     04.MR.CL.      COLOR COPY                   1,738 @  0.2850 =  495.33
--                                                Net Total         2,977.25
--
-- Same family as IKTBN CHEMBONG -- rental by model, copies as one line for everybody -- but the
-- rental is written the OTHER way: quantity 5 at 266.00 each, not quantity 1 at 1,330.00. Two
-- customers of the same shape who want the line to read differently; keeping both is the point.
--
-- ANOTHER LINE THAT ONLY ADDS UP ONE WAY. The six machines' black charges come to 914.41 one at a
-- time; the printed line is 32,085 x 0.0285 = 914.42. Their invoice prints 914.42, and its own
-- total (2,977.25) follows the line rather than the worksheet's 2,977.24.
--
--     4MD20044    5,869 ->   6,528      659 black + 1,738 colour  <- the only colour in the fleet
--     4NL02069   26,093 ->  28,057    1,964
--     4NL02178  156,527 -> 172,309   15,782
--     4NL02167   44,563 ->  49,780    5,217
--     4NL02176   47,364 ->  52,263    4,899
--     4NL02017   26,326 ->  29,890    3,564
--                                    ------
--                                    32,085   and 306,742 -> 338,827 on the printed line
--
-- Re-runnable: an existing DEMO-IPG is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-IPG');
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
VALUES (N'DEMO-IPG', '', N'3000-A0199',
        N'IPG Pahang - rental by model priced per unit, copies as one BK and one CL line',
        '', 'Y', 'L', 'Y', 'N', 'Y', 'A', 'A', 'G', 1, GETDATE(), '2025-10-01', '2028-09-30',
        'N', 'N', 'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

DECLARE @m TABLE (Pos INT, Serial NVARCHAR(60), Model NVARCHAR(60), Grp NVARCHAR(20),
                  Rental DECIMAL(18,2),
                  BkPrev DECIMAL(18,2), BkCurr DECIMAL(18,2),
                  ClPrev DECIMAL(18,2), ClCurr DECIMAL(18,2));
INSERT INTO @m VALUES
 (1, N'4MD20044', N'IR ADV DX C3930i', N'RA3930', 237.50,   5869,   6528, 11271, 13009),
 (2, N'4NL02069', N'IRADV DX 4945I',   N'RA4945', 266.00,  26093,  28057,     0,     0),
 (3, N'4NL02178', N'IRADV DX 4945I',   N'RA4945', 266.00, 156527, 172309,     0,     0),
 (4, N'4NL02167', N'IRADV DX 4945I',   N'RA4945', 266.00,  44563,  49780,     0,     0),
 (5, N'4NL02176', N'IRADV DX 4945I',   N'RA4945', 266.00,  47364,  52263,     0,     0),
 (6, N'4NL02017', N'IRADV DX 4945I',   N'RA4945', 266.00,  26326,  29890,     0,     0);

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
    VALUES (@ck, N'DEMO-IPG-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), @model, @serial,
            @model + N' / IPG PAHANG', @pos, @grp, N'FLEET', '', '',
            'N', 'N', 'ONLINE', '2025-10-01', '2028-09-30', GETDATE());
    SET @ik = SCOPE_IDENTITY();

    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, @rental, '', 0, 0, 0,
            '2025-10-01', 36, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

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

    FETCH NEXT FROM mc INTO @pos, @serial, @model, @grp, @rental, @bkPrev, @bkCurr, @clPrev, @clCurr;
END
CLOSE mc;
DEALLOCATE mc;

INSERT INTO dbo.zSCP2_ContractRentalPrice
 (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
VALUES (@ck, 'R', N'RA3930', 237.50, 0, 0, '', '', GETDATE()),
       (@ck, 'R', N'RA4945', 266.00, 0, 0, '', '', GETDATE()),
       (@ck, 'M', N'FLEET',  0, 0.0285, 0.2850, '', '', GETDATE());

SELECT 'DEMO-IPG built: 6 machines, expect 4 lines and 2,977.25' AS Result;
GO

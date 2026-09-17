-- DEMO-PON -- the HOSPITAL PONTIAN shape, rebuilt as a contract.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-pontian.sql
--
-- Taken from "Type Of BillingFormat/HOSPITAL PONTIAN.pdf", invoice MR2607.0357. Six machines, one
-- invoice, two lines:
--
--     109-BK C+ P      BK COPY + PRINT A4&A3    74,722 @ 0.0285 = 2,129.58
--     RA-5 UINT_EB2B   MONTHLY RENTAL (22/36)        5 @ 687.80 = 3,439.00
--                                              Net Total          5,568.58
--
-- Same shape as PERMAI LAMA on the surface -- one invoice, the fleet folded into one line per
-- charge -- but it carries two things no other demo contract has:
--
--  1. A 2% METER REBATE, and it is taken as COPIES, not as money off. The six machines used 76,244
--     between them; the invoice bills 74,722 and prints "Meter Rebate Qty (2%) : 1522" underneath.
--
--     The 1,522 is NOT 2% of 76,244 (that would be 1,524). Each machine's rebate is worked out on
--     its own usage and cut down to a whole copy, and the six are then added:
--
--         25,777 -> 515      5,746 -> 114     22,053 -> 441
--          5,272 -> 105      2,525 ->  50     14,871 -> 297     = 1,522
--
--     Round the total instead of the parts and the invoice is two copies out. This contract is the
--     one that would catch it.
--
--  2. SIX machines read, FIVE rented. The rental line's quantity is 5 while the meter line covers
--     all six -- XME03503 is a different model and carries no rental counter at all.
--
-- One more thing worth keeping: one machine has a colour counter that has not moved (42 to 42).
-- The invoice prints no colour line. A counter with no usage is not a line with a zero on it.
--
-- The rental start is 1/12/2024 so that a SEPTEMBER 2026 bill prints (22/36), which is what the
-- July invoice printed. The demo month is September; the number had to be moved to it, not the
-- other way round.
--
-- Re-runnable: an existing DEMO-PON is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-PON');
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

-- One invoice (RentalSeparateInvoice = 'N'), both sides merged across model ('A'). The line prints
-- the label only -- the serials go in the header block the way the real invoice does it -- but the
-- unit count stays on, because the customer's own item code says "RA-5 UINT".
INSERT INTO dbo.zSCP2_Contract
 (ContractNo, ContractTypeCode, DebtorCode, [Description], BillingFormatCode, UseNewLayout,
  MachineLineShows, ShowModelOnLine, ShowSerialOnLine, ShowUnitsOnLine,
  RentalLineMode, MeterLineMode, BillingMode, BillingDay, ContractDate,
  ServiceStartDate, ServiceExpiryDate, RentalSeparateInvoice, Inactive, CreatedBy, ModifiedBy,
  Created, Modified, LastModified)
VALUES (N'DEMO-PON', '', N'3000-A0090',
        N'Hospital Pontian - one invoice, 2% meter rebate taken as copies, 6 read / 5 rented',
        '', 'Y', 'L', 'N', 'N', 'Y', 'A', 'A', 'G', 1, GETDATE(), '2024-12-01', '2027-11-30',
        'N', 'N', 'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

-- ---------------------------------------------------------------- the six machines
--
-- Prev/Curr are the readings off the invoice: 24/6/2026 and 23/7/2026. HasRental says whether the
-- machine carries a rental counter -- XME03503 does not, which is why the rental line reads 5.
DECLARE @m TABLE (Pos INT, Serial NVARCHAR(60), Model NVARCHAR(60), Site NVARCHAR(80),
                  BkPrev DECIMAL(18,2), BkCurr DECIMAL(18,2),
                  ClPrev DECIMAL(18,2), ClCurr DECIMAL(18,2), HasRental CHAR(1));
INSERT INTO @m VALUES
 (1, N'3MN10250', N'iR-ADV DX 6860i', N'U.REKOD',        787432,  813209, 42, 42, 'Y'),
 (2, N'3MN10251', N'iR-ADV DX 6860i', N'U.KECEMASAN',    227991,  233737,  0,  0, 'Y'),
 (3, N'3MN10261', N'iR-ADV DX 6860i', N'U.FARMASI',      433871,  455924,  0,  0, 'Y'),
 (4, N'3MN10248', N'iR-ADV DX 6860i', N'U.MAKMAL',       141067,  146339,  0,  0, 'Y'),
 (5, N'3MN10269', N'iR-ADV DX 6860i', N'U.PENTADBIRAN',   66564,   69089,  0,  0, 'Y'),
 (6, N'XME03503', N'iR-ADV C5550i',   N'UNIT KUALITI',   353421,  368292,  0,  0, 'N');

DECLARE @pos INT, @serial NVARCHAR(60), @model NVARCHAR(60), @site NVARCHAR(80),
        @bkPrev DECIMAL(18,2), @bkCurr DECIMAL(18,2),
        @clPrev DECIMAL(18,2), @clCurr DECIMAL(18,2), @hasRental CHAR(1),
        @ik BIGINT, @imk BIGINT;

DECLARE mc CURSOR LOCAL FAST_FORWARD FOR
  SELECT Pos, Serial, Model, Site, BkPrev, BkCurr, ClPrev, ClCurr, HasRental FROM @m ORDER BY Pos;
OPEN mc;
FETCH NEXT FROM mc INTO @pos, @serial, @model, @site, @bkPrev, @bkCurr, @clPrev, @clCurr, @hasRental;
WHILE @@FETCH_STATUS = 0
BEGIN
    -- Every machine is in the same meter group, so the six fold into one BK line. Only the five
    -- rented ones are in the rental group; the sixth has nothing to fold.
    INSERT INTO dbo.zSCP2_Item
     (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
      MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
      ServiceStartDate, ServiceExpiryDate, LastModified)
    VALUES (@ck, N'DEMO-PON-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), @model, @serial,
            @model + N' / HOSPITAL PONTIAN ' + @site, @pos,
            CASE WHEN @hasRental = 'Y' THEN N'RA' ELSE '' END, N'FLEET', '', '',
            'N', 'N', 'ONLINE', '2024-12-01', '2027-11-30', GETDATE());
    SET @ik = SCOPE_IDENTITY();

    IF @hasRental = 'Y'
        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, 687.80, '', 0, 0, 0,
                '2024-12-01', 36, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

    -- 2% off the copies, per machine. RebateQtyInPercent is the whole point of this contract.
    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES (@ik, N'BK', N'BK COPY + PRINT A4&A3', N'BK', '', 0, 0.0285, '', 2, 0, @bkPrev,
            NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
    SET @imk = SCOPE_IDENTITY();
    INSERT INTO dbo.zSCP2_MeterEntry
     (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
    VALUES (@imk, 2026, 9, @bkCurr, '2026-09-30', 'MANUAL', 'N', GETDATE());

    -- The colour counter that never moves. It exists, it is read, and it prints nothing.
    IF @clPrev > 0
    BEGIN
        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES (@ik, N'CL', N'CL COPY + PRINT A4&A3', N'CL', '', 0, 0.2850, '', 2, 0, @clPrev,
                NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
        SET @imk = SCOPE_IDENTITY();
        INSERT INTO dbo.zSCP2_MeterEntry
         (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
        VALUES (@imk, 2026, 9, @clCurr, '2026-09-30', 'MANUAL', 'N', GETDATE());
    END

    FETCH NEXT FROM mc INTO @pos, @serial, @model, @site, @bkPrev, @bkCurr, @clPrev, @clCurr, @hasRental;
END
CLOSE mc;
DEALLOCATE mc;

-- The five rented machines share one agreed rental; the whole fleet shares one copy rate.
INSERT INTO dbo.zSCP2_ContractRentalPrice
 (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
VALUES (@ck, 'R', N'RA',    687.80, 0, 0, '', '', GETDATE()),
       (@ck, 'M', N'FLEET', 0, 0.0285, 0.2850, '', '', GETDATE());

SELECT 'DEMO-PON built: ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_Item WHERE ContractKey = @ck) AS VARCHAR) + ' machines, ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_ItemMeter m
               JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
              WHERE i.ContractKey = @ck) AS VARCHAR) + ' counters, expect 74,722 copies @ 0.0285 = 2,129.58'
       AS Result;
GO

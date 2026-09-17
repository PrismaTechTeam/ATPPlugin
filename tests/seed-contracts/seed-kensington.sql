-- DEMO-KEN -- the KENSINGTON GREEN SPECIALIST CENTRE shape, rebuilt as a contract.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-kensington.sql
--
-- Taken from "Type Of BillingFormat/KENSINGTON.pdf", invoice MR2607.0186. EIGHTEEN machines, one
-- invoice, three lines:
--
--     201-BK C+ P     BK COPY + PRINT A4&A3     45,959 @ 0.0245 = 1,126.00
--     223-COLOR C+ P  COLOR COPY + PRINT A4&A3   3,434 @ 0.3234 = 1,110.56
--     MIN 1764-12     MINIMUM RM 1764 OF COMMITTED MTH                0.00
--                                               Net Total          2,236.56
--
-- TWO THINGS THIS ONE IS FOR:
--
--  1. A COMMITTED MINIMUM, and the line prints even when it costs nothing. The customer promised
--     1,764.00 of printing a month; they printed 2,236.56 of it, so there is nothing to top up.
--     The line is still on the invoice -- it is the record that the promise was measured and met.
--     Delete it on a good month and the customer only ever sees the minimum in months they are
--     charged for it, which is the worst possible way to learn a term exists.
--
--  2. NO RENTAL AT ALL. Eighteen machines, not one rental line. The copies are the whole deal.
--
-- The rates are blended figures (0.0245 and 0.3234), not the usual round ones -- another reason the
-- printed line must be quantity x price: 3,434 x 0.3234 is 1,110.5556, and no per-machine sum of
-- roundings lands on the 1,110.56 their invoice prints.
--
-- Seventeen of the eighteen readings are off the worksheet. The last machine (2GS04044) is what the
-- printed totals say is missing -- 3,094 black and 509 colour -- so the fleet adds up to the
-- 45,959 and 3,434 on the invoice.
--
-- Re-runnable: an existing DEMO-KEN is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-KEN');
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
VALUES (N'DEMO-KEN', '', N'3000-K0011',
        N'Kensington Green - eighteen machines, no rental, one BK and one CL line, minimum RM1,764 a month',
        '', 'Y', 'L', 'N', 'N', 'N', 'A', 'A', 'G', 1, GETDATE(), '2025-01-01', '2027-12-31',
        'N', 'N', 'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

DECLARE @m TABLE (Pos INT, Serial NVARCHAR(60), Model NVARCHAR(60), Site NVARCHAR(80),
                  BkPrev DECIMAL(18,2), BkCurr DECIMAL(18,2),
                  ClPrev DECIMAL(18,2), ClCurr DECIMAL(18,2));
INSERT INTO @m VALUES
 ( 1, N'JWF54420', N'iR-ADV C5235', N'EMERGENCY DEPT',        278202, 280886, 34138, 34181),
 ( 2, N'JWF02526', N'iR-ADV C5235', N'L3 IVF',                 77737,  79370,  5871,  5978),
 ( 3, N'JWF69666', N'iR-ADV C5235', N'L3 OPERATION THEATRE',   58970,  59810,  4238,  4330),
 ( 4, N'JWF64879', N'iR-ADV C5235', N'L3 WARD',                78678,  79533, 22265, 22446),
 ( 5, N'JWF52552', N'iR-ADV C5235', N'L3 CSSD',                17943,  18144,  3088,  3107),
 ( 6, N'JWF52099', N'iR-ADV C5235', N'L1 OPD2',                71558,  72347,  5695,  5724),
 ( 7, N'JWF41255', N'iR-ADV C5235', N'L2 NURSERY',             47129,  48005, 10817, 10986),
 ( 8, N'JWF41877', N'iR-ADV C5235', N'L3 FACILITY',            54395,  55349,  8677,  8816),
 ( 9, N'JWF49995', N'iR-ADV C5235', N'LG IMAGING DEPT',        23078,  23640, 20532, 21272),
 (10, N'JWF46037', N'iR-ADV C5235', N'L1 ENDOSCOPY',           17205,  17468,  7981,  8029),
 (11, N'JWF86425', N'iR-ADV C5235', N'HSC DEPARTMENT',         27433,  28212,  2520,  2580),
 (12, N'XYM01450', N'iR-ADV C3530', N'KGSD W1A LV1',           56196,  61151,  6649,  7166),
 (13, N'WSR02066', N'iR-ADV C3530', N'L3 HDU',                210602, 228062,   810,   836),
 (14, N'WSR01600', N'iR-ADV C3530', N'BUSINESS OFFICE GF',     63223,  66533,  9841,  9914),
 (15, N'WSR01752', N'iR-ADV C3530', N'OPD',                    36117,  37731, 11024, 11624),
 (16, N'WSR04014', N'iR-ADV C3530', N'ADMIN',                  26002,  30260,   283,   336),
 (17, N'WSR01919', N'iR-ADV C3530', N'LG PHARMACY',             8532,   9364,   448,   477),
 (18, N'2GS04044', N'iR-ADV C3530', N'L2 HR OFFICE',           20000,  23094,  5000,  5509);

DECLARE @pos INT, @serial NVARCHAR(60), @model NVARCHAR(60), @site NVARCHAR(80),
        @bkPrev DECIMAL(18,2), @bkCurr DECIMAL(18,2),
        @clPrev DECIMAL(18,2), @clCurr DECIMAL(18,2), @ik BIGINT, @imk BIGINT, @first BIGINT = NULL;

DECLARE mc CURSOR LOCAL FAST_FORWARD FOR
  SELECT Pos, Serial, Model, Site, BkPrev, BkCurr, ClPrev, ClCurr FROM @m ORDER BY Pos;
OPEN mc;
FETCH NEXT FROM mc INTO @pos, @serial, @model, @site, @bkPrev, @bkCurr, @clPrev, @clCurr;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.zSCP2_Item
     (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
      MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
      ServiceStartDate, ServiceExpiryDate, LastModified)
    VALUES (@ck, N'DEMO-KEN-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), @model, @serial,
            @model + N' / KENSINGTON ' + @site, @pos, '', N'FLEET', '', '',
            'N', 'N', 'ONLINE', '2025-01-01', '2027-12-31', GETDATE());
    SET @ik = SCOPE_IDENTITY();
    IF @first IS NULL SET @first = @ik;

    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES (@ik, N'BK', N'BK COPY + PRINT A4&A3', N'BK', '', 0, 0.0245, '', 0, 0, @bkPrev,
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
    VALUES (@ik, N'CL', N'COLOR COPY + PRINT A4&A3', N'CL', '', 0, 0.3234, '', 0, 0, @clPrev,
            NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
    SET @imk = SCOPE_IDENTITY();
    INSERT INTO dbo.zSCP2_MeterEntry
     (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
    VALUES (@imk, 2026, 9, @clCurr, '2026-09-30', 'MANUAL', 'N', GETDATE());

    FETCH NEXT FROM mc INTO @pos, @serial, @model, @site, @bkPrev, @bkCurr, @clPrev, @clCurr;
END
CLOSE mc;
DEALLOCATE mc;

-- The committed minimum: one meter, scoped to the whole contract ('G'), measured against what the
-- fleet's copies came to. It tops up to 1,764.00 and charges nothing when they get there on their own.
INSERT INTO dbo.zSCP2_ItemMeter
 (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
  MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
  RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
  WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
VALUES (@first, N'COMMIT', N'MINIMUM RM 1764 OF COMMITTED MTH', N'COMMIT', '', 1764.00, 0, '', 0, 0, 0,
        NULL, 0, 'A', 0, 0, 100, 'BKCL', 0, 0, 'G', GETDATE());

INSERT INTO dbo.zSCP2_ContractRentalPrice
 (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
VALUES (@ck, 'M', N'FLEET', 0, 0.0245, 0.3234, '', '', GETDATE());

SELECT 'DEMO-KEN built: 18 machines, expect 3 lines and 2,236.56 with the minimum at 0.00' AS Result;
GO

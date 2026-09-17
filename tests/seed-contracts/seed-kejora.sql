-- DEMO-KJR -- the LEMBAGA KEMAJUAN JOHOR TENGGARA (KEJORA) shape, rebuilt as a contract.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-kejora.sql
--
-- Taken from "Type Of BillingFormat/LEMBAGA KEMAJUAN JOHOR TENGGARA (KEJORA).pdf" -- TWO invoices:
--
--   AMR2607.0031  rental   three lines, each a LUMP for its group      = 10,467.00
--                            RA-2 UNIT                     1 @ 1,900.00
--                            RA-7 UNIT  IRADVC5850I        1 @ 4,067.00
--                            RA-9 UNIT  IRADVC5840I        1 @ 4,500.00
--   AMR2607.0153  meters   THIRTY-SIX lines over 13 pages             = 22,121.38
--
-- The largest document set in the folder, and the one that shows both extremes at once: the rental
-- of eighteen machines fits on three lines, while their copies take thirty-six.
--
-- NOT EVERY MACHINE IS ON THE SAME COLOUR RATE. Sixteen are on 0.30; the two 35N machines are on
-- 0.25. Black is 0.03 throughout. So a fold "by colour" would have to price sixteen machines and
-- two machines differently on the same line -- which is exactly why this customer does not fold at
-- all, and why the rule is "merge what is charged alike" rather than "merge what is the same
-- colour".
--
--     black    44,306 copies @ 0.03            =  1,329.18
--     colour   16,588 copies @ 0.25 (two)      +  55,560 @ 0.30 (sixteen)  = 20,792.20
--                                                                            ---------
--                                                                            22,121.38
--
-- Total quantity on the invoice is 116,454 -- black and colour together, which is what the sum of
-- thirty-six lines comes to.
--
-- Re-runnable: an existing DEMO-KJR is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-KJR');
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
VALUES (N'DEMO-KJR', '', N'3000-L0003',
        N'KEJORA - eighteen machines: three lump rental lines on one invoice, thirty-six meter lines on another',
        '', 'Y', 'B', 'Y', 'Y', 'Y', 'A', 'S', 'G', 1, GETDATE(), '2025-01-01', '2027-12-31',
        'Y', 'N', 'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

-- Rental holds the LUMP for its group and sits on the group's first machine; the rest carry none.
DECLARE @m TABLE (Pos INT, Serial NVARCHAR(60), Model NVARCHAR(60), Site NVARCHAR(80),
                  Grp NVARCHAR(20), Rental DECIMAL(18,2), ClRate DECIMAL(18,6),
                  BkPrev DECIMAL(18,2), BkCurr DECIMAL(18,2),
                  ClPrev DECIMAL(18,2), ClCurr DECIMAL(18,2));
INSERT INTO @m VALUES
 ( 1, N'35N02498', N'IRADVC5860I', N'PEN. & PEN. SUMBER MANUSIA', N'RA2', 1900.00, 0.25,  88633,  95465, 123462, 131633),
 ( 2, N'35N02556', N'IRADVC5860I', N'KHIDMAT PENGURUSAN',         N'RA2',       0, 0.25,  70800,  77654,  83015,  91888),
 ( 3, N'2YB70267', N'IRADVC5850I', N'KEM.WIL. & PEMBANGUNAN',     N'RA7', 4067.00, 0.30,  65830,  71729,  97643, 107968),
 ( 4, N'2YB70268', N'IRADVC5850I', N'PEN. HARTANAH & FASILITI',   N'RA7',       0, 0.30,  40260,  43648,  48275,  51684),
 ( 5, N'2YB70261', N'IRADVC5850I', N'PERANCANGAN STRATEGIK',      N'RA7',       0, 0.30,  18597,  19756,  74685,  79248),
 ( 6, N'2YB70270', N'IRADVC5850I', N'PEJABAT PENGARAH',           N'RA7',       0, 0.30,  16401,  18708,  44764,  49071),
 ( 7, N'2YB70274', N'IRADVC5850I', N'UNDANG-UNDANG',              N'RA7',       0, 0.30,  23594,  25758,  43187,  47026),
 ( 8, N'2YB16783', N'IRADVC5850I', N'KEWANGAN',                   N'RA7',       0, 0.30,  19303,  21565,  42498,  46735),
 ( 9, N'2YB16805', N'IRADVC5850I', N'PEMBANGUNAN USAHAWAN',       N'RA7',       0, 0.30,  28112,  32213,  33829,  36566),
 (10, N'2YB19825', N'IRADVC5840I', N'KORPORAT',                   N'RA9', 4500.00, 0.30,  10913,  11490,  18148,  19038),
 (11, N'2YB19840', N'IRADVC5840I', N'PENTADBIRAN',                N'RA9',       0, 0.30,  10105,  10633,  27153,  33110),
 (12, N'2YN19779', N'IRADVC5840I', N'TEKNOLOGI MAKLUMAT',         N'RA9',       0, 0.30,   7448,   8678,  19330,  22417),
 (13, N'2YN19842', N'IRADVC5840I', N'AUDIT DALAM',                N'RA9',       0, 0.30,   8595,  10515,  13260,  14611),
 (14, N'2YN19852', N'IRADVC5840I', N'PEROLEHAN',                  N'RA9',       0, 0.30,  10093,  11618,  20354,  22127),
 (15, N'2YN19853', N'IRADVC5840I', N'AKAUN',                      N'RA9',       0, 0.30,  14003,  14300,  29866,  31166),
 (16, N'2YN19848', N'IRADVC5840I', N'PELANCONGAN',                N'RA9',       0, 0.30,   7384,   8830,  16994,  19322),
 (17, N'2YN19836', N'IRADVC5840I', N'PERINDUSTRIAN',              N'RA9',       0, 0.30,  14659,  16420,  20521,  25066),
 (18, N'2YN20436', N'IRADVC5840I', N'STOR',                       N'RA9',       0, 0.30,   2793,   2849,   3622,   4078);

DECLARE @pos INT, @serial NVARCHAR(60), @model NVARCHAR(60), @site NVARCHAR(80),
        @grp NVARCHAR(20), @rental DECIMAL(18,2), @clRate DECIMAL(18,6),
        @bkPrev DECIMAL(18,2), @bkCurr DECIMAL(18,2),
        @clPrev DECIMAL(18,2), @clCurr DECIMAL(18,2), @ik BIGINT, @imk BIGINT;

DECLARE mc CURSOR LOCAL FAST_FORWARD FOR
  SELECT Pos, Serial, Model, Site, Grp, Rental, ClRate, BkPrev, BkCurr, ClPrev, ClCurr
    FROM @m ORDER BY Pos;
OPEN mc;
FETCH NEXT FROM mc INTO @pos, @serial, @model, @site, @grp, @rental, @clRate,
                        @bkPrev, @bkCurr, @clPrev, @clCurr;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.zSCP2_Item
     (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
      MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
      ServiceStartDate, ServiceExpiryDate, LastModified)
    VALUES (@ck, N'DEMO-KJR-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), @model, @serial,
            N'KEJORA - ' + @site, @pos, @grp, '', '', '',
            'N', 'N', 'ONLINE', '2025-01-01', '2027-12-31', GETDATE());
    SET @ik = SCOPE_IDENTITY();

    IF @rental > 0
        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, @rental, '', 0, 0, 0,
                NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES (@ik, N'BK', N'BK COPY + PRINT', N'BK', @serial, 0, 0.03, '', 0, 0, @bkPrev,
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
    VALUES (@ik, N'CL', N'CL COPY + PRINT', N'CL', @serial, 0, @clRate, '', 0, 0, @clPrev,
            NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
    SET @imk = SCOPE_IDENTITY();
    INSERT INTO dbo.zSCP2_MeterEntry
     (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
    VALUES (@imk, 2026, 9, @clCurr, '2026-09-30', 'MANUAL', 'N', GETDATE());

    FETCH NEXT FROM mc INTO @pos, @serial, @model, @site, @grp, @rental, @clRate,
                            @bkPrev, @bkCurr, @clPrev, @clCurr;
END
CLOSE mc;
DEALLOCATE mc;

INSERT INTO dbo.zSCP2_ContractRentalPrice
 (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
VALUES (@ck, 'R', N'RA2', 1900.00, 0, 0, '', '', GETDATE()),
       (@ck, 'R', N'RA7', 4067.00, 0, 0, '', '', GETDATE()),
       (@ck, 'R', N'RA9', 4500.00, 0, 0, '', '', GETDATE());

SELECT 'DEMO-KJR built: 18 machines, expect 2 invoices -- rental 10,467.00 and 36 meter lines 22,121.38'
       AS Result;
GO

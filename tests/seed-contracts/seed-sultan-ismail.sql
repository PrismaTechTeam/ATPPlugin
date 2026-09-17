-- DEMO-HSI -- the HOSPITAL SULTAN ISMAIL shape, rebuilt as a contract.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-sultan-ismail.sql
--
-- Taken from "Type Of BillingFormat/HOSPITAL SULTAN ISMAIL.pdf", invoice MR2607.1347. Fourteen
-- machines, ONE invoice, four lines:
--
--     RENTAL HSI - 13 UNIT MEDIUM DUTY        13 @   441.75 =  5,742.75
--     RENTAL HSI - 1 UNIT HIGH HEAVY DUTY      1 @ 1,377.50 =  1,377.50
--     METER CHARGE HSI - BK COPY + PRINT  106,960 @   0.0285 = 3,048.36
--     METER CHARGE HSI - BK COPY + PRINT  212,804 @   0.0190 = 4,043.28
--                                             Net Total       14,211.89
--
-- What makes it worth keeping is lines 3 and 4. Their descriptions are IDENTICAL -- both read
-- "BK COPY + PRINT A4&A3" -- and they are two lines only because the rate differs, 0.0285 against
-- 0.019. Nothing on the paper explains the split. A fold that groups by description, or by model,
-- or by meter type, produces ONE line of 319,764 copies and a wrong invoice; the rule is
-- "merge what is charged alike".
--
-- The rental splits the same way but on a different axis: thirteen medium machines on one line and
-- the single heavy one on its own, because their monthly rents differ. So this contract carries two
-- groupings at once and they do NOT line up with each other by accident -- the heavy machine is
-- alone on both sides, but for two unrelated reasons.
--
-- Readings are the fourteen off the worksheet. They add up:
--
--     medium 13 machines   1,355,392 -> 1,462,352   =  106,960 copies
--     heavy   1 machine    2,601,479 -> 2,814,283   =  212,804 copies
--
-- One machine printing 212,804 while thirteen together print 106,960 is not a typo -- it is why
-- that machine is on 0.019 and has its own rental.
--
-- KNOWN DIFFERENCE: the customer's invoice writes "- 07/2026" on the rental lines where we write
-- "(15/24)". Their worksheet carries 15/24 too, so it is the printed template that differs, not the
-- figure. Ours is set to say 15/24 in September 2026, the demo month.
--
-- Re-runnable: an existing DEMO-HSI is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-HSI');
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

-- One invoice. Both sides merge across model ('A') -- the groups on each machine decide the rest,
-- and here they are DUTY on the rental side and RATE on the meter side.
INSERT INTO dbo.zSCP2_Contract
 (ContractNo, ContractTypeCode, DebtorCode, [Description], BillingFormatCode, UseNewLayout,
  MachineLineShows, ShowModelOnLine, ShowSerialOnLine, ShowUnitsOnLine,
  RentalLineMode, MeterLineMode, BillingMode, BillingDay, ContractDate,
  ServiceStartDate, ServiceExpiryDate, RentalSeparateInvoice, Inactive, CreatedBy, ModifiedBy,
  Created, Modified, LastModified)
VALUES (N'DEMO-HSI', '', N'3000-A0179',
        N'Hospital Sultan Ismail - one invoice, rental by duty, copies by RATE (two lines, same words)',
        '', 'Y', 'L', 'N', 'N', 'Y', 'A', 'A', 'G', 1, GETDATE(), '2025-07-01', '2027-06-30',
        'N', 'N', 'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

-- ---------------------------------------------------------------- the fourteen machines
--
-- Grp  which rental line and which meter line the machine lands on. MED and HEAVY are used on both
--      sides here, but that is this contract's arithmetic, not a rule.
DECLARE @m TABLE (Pos INT, Serial NVARCHAR(60), Model NVARCHAR(60), Site NVARCHAR(80),
                  Grp NVARCHAR(20), Rate DECIMAL(18,6), Rental DECIMAL(18,2),
                  BkPrev DECIMAL(18,2), BkCurr DECIMAL(18,2));
INSERT INTO @m VALUES
 ( 1, N'4PE01709', N'IRADV DX 4945I', N'STOR ALAT TULIS LV1',    N'MED',   0.0285,  441.75,  245278,  266033),
 ( 2, N'4PE01710', N'IRADV DX 4945I', N'JAB KERJA SOSIAL LV2',   N'MED',   0.0285,  441.75,  254089,  271620),
 ( 3, N'4PE01751', N'IRADV DX 4945I', N'JAB ONKOLOGI LV1',       N'MED',   0.0285,  441.75,   19718,   22692),
 ( 4, N'4PE01752', N'IRADV DX 4945I', N'JAB PATOLOGI LV4',       N'MED',   0.0285,  441.75,   51623,   56183),
 ( 5, N'4PE01758', N'IRADV DX 4945I', N'JAB FARMASI LV1',        N'MED',   0.0285,  441.75,   55938,   60499),
 ( 6, N'4PE01759', N'IRADV DX 4945I', N'UNIT HASIL LV3',         N'MED',   0.0285,  441.75,  105088,  114523),
 ( 7, N'4PE01760', N'IRADV DX 4945I', N'DEWAN BEDAH LV5',        N'MED',   0.0285,  441.75,  144456,  156414),
 ( 8, N'4PE01761', N'IRADV DX 4945I', N'JAB FARMASI LV3',        N'MED',   0.0285,  441.75,   47500,   54376),
 ( 9, N'4PE01762', N'IRADV DX 4945I', N'PEJ PAKAR LV6',          N'MED',   0.0285,  441.75,  152522,  161270),
 (10, N'4PE01764', N'IRADV DX 4945I', N'JAB REKOD PERUBATAN LV1',N'MED',   0.0285,  441.75,   14513,   15206),
 (11, N'4PE01839', N'IRADV DX 4945I', N'UNIT KECEMASAN LV1',     N'MED',   0.0285,  441.75,   79158,   85730),
 (12, N'4PE01866', N'IRADV DX 4945I', N'STOR ALAT TULIS LV1 (2)',N'MED',   0.0285,  441.75,  122232,  129973),
 (13, N'4PE01867', N'IRADV DX 4945I', N'PEJ PENTADBIRAN LV3',    N'MED',   0.0285,  441.75,   63277,   67833),
 (14, N'28B01545', N'iR-ADV DX 8905', N'LICENSE / COMBINE',      N'HEAVY', 0.0190, 1377.50, 2601479, 2814283);

DECLARE @pos INT, @serial NVARCHAR(60), @model NVARCHAR(60), @site NVARCHAR(80),
        @grp NVARCHAR(20), @rate DECIMAL(18,6), @rental DECIMAL(18,2),
        @bkPrev DECIMAL(18,2), @bkCurr DECIMAL(18,2), @ik BIGINT, @imk BIGINT;

DECLARE mc CURSOR LOCAL FAST_FORWARD FOR
  SELECT Pos, Serial, Model, Site, Grp, Rate, Rental, BkPrev, BkCurr FROM @m ORDER BY Pos;
OPEN mc;
FETCH NEXT FROM mc INTO @pos, @serial, @model, @site, @grp, @rate, @rental, @bkPrev, @bkCurr;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.zSCP2_Item
     (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
      MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
      ServiceStartDate, ServiceExpiryDate, LastModified)
    VALUES (@ck, N'DEMO-HSI-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), @model, @serial,
            @model + N' / HSI ' + @site, @pos, @grp, @grp, '',
            CASE WHEN @grp = N'MED' THEN N'MEDIUM DUTY' ELSE N'HIGH HEAVY DUTY' END,
            'N', 'N', 'ONLINE', '2025-07-01', '2027-06-30', GETDATE());
    SET @ik = SCOPE_IDENTITY();

    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, @rental, '', 0, 0, 0,
            '2025-07-01', 24, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES (@ik, N'BK', N'BK COPY + PRINT A4&A3', N'BK', '', 0, @rate, '', 0, 0, @bkPrev,
            NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
    SET @imk = SCOPE_IDENTITY();
    INSERT INTO dbo.zSCP2_MeterEntry
     (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
    VALUES (@imk, 2026, 9, @bkCurr, '2026-09-30', 'MANUAL', 'N', GETDATE());

    FETCH NEXT FROM mc INTO @pos, @serial, @model, @site, @grp, @rate, @rental, @bkPrev, @bkCurr;
END
CLOSE mc;
DEALLOCATE mc;

-- Two agreed rentals, two agreed copy rates. Same two codes on both sides, different meanings.
INSERT INTO dbo.zSCP2_ContractRentalPrice
 (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
VALUES (@ck, 'R', N'MED',    441.75, 0, 0, '', '', GETDATE()),
       (@ck, 'R', N'HEAVY', 1377.50, 0, 0, '', '', GETDATE()),
       (@ck, 'M', N'MED',   0, 0.0285, 0, '', '', GETDATE()),
       (@ck, 'M', N'HEAVY', 0, 0.0190, 0, '', '', GETDATE());

SELECT 'DEMO-HSI built: ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_Item WHERE ContractKey = @ck) AS VARCHAR) + ' machines, ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_ItemMeter m
               JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
              WHERE i.ContractKey = @ck) AS VARCHAR) + ' counters, expect 4 lines and 14,211.89'
       AS Result;
GO

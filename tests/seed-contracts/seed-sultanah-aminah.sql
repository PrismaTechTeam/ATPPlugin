-- DEMO-SA -- the HOSPITAL SULTANAH AMINAH shape. Matrix cell #6.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-sultanah-aminah.sql
--
-- From "Type Of BillingFormat/HOSPITAL SULTANAH AMINAH.pdf" -- two invoices off one contract,
-- MR2607.1216 and MR2607.1340, and the worksheet behind them.
--
--     A. rental on its own invoice      YES
--     B. rental grouped                 ALL ON ONE LINE
--     C. black and colour grouped       ALL ON ONE LINE
--
-- It is the cell nothing else in the demo book covers, and it is worth having for a second reason:
-- it is the clearest example of a FREE ALLOWANCE deciding whether a machine bills at all. Six
-- machines print; three of them bill nothing, because 6,000 copies a month are free and they did
-- not reach it.
--
--     machine   printed   free    net     at 0.019
--     .1          9,096   6,000   3,096      58.82
--     .2          3,683   6,000       0       0.00   <- under its allowance
--     .3          2,625   6,000       0       0.00   <- under its allowance
--     .4          1,842       0   1,842      35.00
--     .5          6,460   6,000     460       8.74
--     .6          3,294       0   3,294      62.59
--                                 -----     ------
--                                 8,692     165.15   ONE line on MR2607.1340
--
--     rental                    4 UNIT @ 475.00 = 1,900.00   ONE line on MR2607.1216
--
-- Two machines (.4 and .6) carry NO rental at all -- six machines on the contract, four of them
-- rented. The rental line's quantity is the number of RENTED machines, not the fleet.
--
-- Colour: none of them has a colour counter. The contract is black only, which is why the meter
-- invoice is a single line rather than two.
--
-- Re-runnable: an existing DEMO-SA is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-SA');
IF @ck IS NOT NULL
BEGIN
    DELETE e FROM dbo.zSCP2_MeterEntry e
      JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey
      JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey WHERE i.ContractKey = @ck;
    DELETE l FROM dbo.zSCP2_MeterReadingLog l
      JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = l.ItemMeterKey
      JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey WHERE i.ContractKey = @ck;
    DELETE t FROM dbo.zSCP_MeterTrans t
      JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = t.ServiceItemMeterTypeKey
      JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey WHERE i.ContractKey = @ck;
    DELETE m FROM dbo.zSCP2_ItemMeter m
      JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey WHERE i.ContractKey = @ck;
    DELETE FROM dbo.zSCP2_Item WHERE ContractKey = @ck;
    DELETE FROM dbo.zSCP2_ContractRentalPrice WHERE ContractKey = @ck;
    DELETE FROM dbo.zSCP2_Contract WHERE ContractKey = @ck;
END

-- RentalSeparateInvoice = 'Y' is the whole of axis A: the rentals leave the meter invoice and get
-- one of their own ("Rental- [...]"), which is what MR2607.1216 is.
INSERT INTO dbo.zSCP2_Contract
 (ContractNo, ContractTypeCode, DebtorCode, [Description], BillingFormatCode, UseNewLayout,
  MachineLineShows, RentalLineMode, MeterLineMode, BillingMode, BillingDay, ContractDate,
  ServiceStartDate, ServiceExpiryDate, RentalSeparateInvoice, Inactive, CreatedBy, ModifiedBy,
  Created, Modified, LastModified)
VALUES (N'DEMO-SA', '', N'3000-A0074',
        N'Sultanah Aminah - rental on its own invoice, whole fleet as ONE rental and ONE black line',
        '', 'Y', 'L', 'A', 'A', 'G', 1, GETDATE(), '2024-09-01', '2026-12-31', 'Y', 'N',
        'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

-- ---------------------------------------------------------------- the six machines
--
-- HasRental says which four are rented. Foc is the free allowance -- 6,000 a month on the four
-- that have it, and the reason .2 and .3 print thousands of copies and bill nothing.
DECLARE @m TABLE (Pos INT, Serial NVARCHAR(60), Model NVARCHAR(60), Site NVARCHAR(80),
                  HasRental BIT, Foc DECIMAL(18,2), BkPrev DECIMAL(18,2), BkCur DECIMAL(18,2));
INSERT INTO @m VALUES
 (1, N'4NL01863', N'IRADV DX 4945I', N'JAB PSIKIATRI & KESIHATAN',  1, 6000, 122258, 131354),
 (2, N'4NL01864', N'IRADV DX 4945I', N'ICU HYBRID',                 1, 6000,  46810,  50493),
 (3, N'4NL01865', N'IRADV DX 4945I', N'FARMASI PENGELUAR',          1, 6000,  30348,  32973),
 (4, N'UMW05916', N'iR-ADV 4551i',   N'UNIT PENTADBIRAN POLIKLINIK',0,    0,  20865,  22707),
 (5, N'4NL01860', N'IRADV DX 4945I', N'UNIT PENGURUSAN',            1, 6000, 101649, 108109),
 (6, N'YAM00870', N'iR-ADV 4551i',   N'UNIT FORENSIK',              0,    0,  46925,  50219);

DECLARE @pos INT, @serial NVARCHAR(60), @model NVARCHAR(60), @site NVARCHAR(80),
        @hasRent BIT, @foc DECIMAL(18,2), @bkPrev DECIMAL(18,2), @bkCur DECIMAL(18,2);
DECLARE @ik BIGINT, @imk BIGINT;
DECLARE mc CURSOR LOCAL FAST_FORWARD FOR
    SELECT Pos, Serial, Model, Site, HasRental, Foc, BkPrev, BkCur FROM @m ORDER BY Pos;
OPEN mc;
FETCH NEXT FROM mc INTO @pos, @serial, @model, @site, @hasRent, @foc, @bkPrev, @bkCur;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.zSCP2_Item
     (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
      MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
      ServiceStartDate, ServiceExpiryDate, LastModified)
    VALUES (@ck, N'DEMO-SA-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), @model, @serial,
            @model + N' / HSA - ' + @site, @pos,
            N'HSA',   -- one rental line for the lot
            N'HSA',   -- one black line for the lot
            '', '', 'N', 'N', 'ONLINE', '2024-09-01', '2026-12-31', GETDATE());
    SET @ik = SCOPE_IDENTITY();

    -- Four of the six are rented, at one agreed figure: 4 UNIT @ 475.00 = 1,900.00.
    IF @hasRent = 1
        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES
         (@ik, N'RENTAL', N'24MTH_MONTHLY RENTAL', N'RENTAL', '', 0, 475.00, '', 0, 0, 0,
          '2024-09-01', 24, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

    -- Black only. The free allowance rides on the counter, per machine, per month.
    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES
     (@ik, N'BK', N'BK COPY + PRINT A4&A3', N'BK', '', 0, 0.019, '', 0, @foc, @bkPrev,
      NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
    SET @imk = SCOPE_IDENTITY();
    INSERT INTO dbo.zSCP2_MeterEntry
     (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
    VALUES (@imk, 2026, 9, @bkCur, '2026-09-30', 'MANUAL', 'N', GETDATE());

    FETCH NEXT FROM mc INTO @pos, @serial, @model, @site, @hasRent, @foc, @bkPrev, @bkCur;
END
CLOSE mc;
DEALLOCATE mc;

-- One agreed rental for the fleet, and one agreed rate for the black line.
INSERT INTO dbo.zSCP2_ContractRentalPrice
 (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
VALUES (@ck, 'R', N'HSA', 475.00, 0, 0, '', '', GETDATE()),
       (@ck, 'M', N'HSA', 0, 0.019, 0, '', '', GETDATE());

SELECT 'DEMO-SA built: ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_Item WHERE ContractKey = @ck) AS VARCHAR) + ' machines, ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_ItemMeter m JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
              WHERE i.ContractKey = @ck AND m.MeterRole = 'RENTAL') AS VARCHAR) + ' rented, ' +
       'expect 2 invoices: rental 1,900.00 and black 8,692 @ 0.019 = 165.15' AS Result;
GO

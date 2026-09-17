-- DEMO-KKR -- the KOLEJ KOMUNITI ROMPIN shape. Matrix cell #8 plus the SeparateInvoice flag.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-rompin.sql
--
-- From "Type Of BillingFormat/KOLEJ KOMUNITI ROMPIN.pdf" -- THREE invoices off one contract:
--
--     AMR2607.0001   rental only        RA - 1 UNIT  @ 500.00 =   500.00
--                                       RA - 3 UNIT  @ 300.00 =   900.00
--                                                              --------
--                                                                1,400.00
--
--     AMR2607.0087   the four machines, one black line
--                    8,142 + 92 + 3,950 + 14,325 = 26,509 @ 0.03 = 795.27
--
--     AMR2607.0088   PERPUSTAKAAN, on its own
--                                          174 @ 0.03 =     5.22
--
-- Three settings, three different jobs, and this contract is where they are easiest to tell apart:
--
--     RentalSeparateInvoice   the rentals leave the meter invoice        -> 0001
--     MergeGroupCodeMeter     four machines share one black line         -> 0087
--     BillGroupCode           the fifth machine goes on a paper of its own -> 0088
--
-- The fifth machine is not merged with anything and is not rented -- a library copier the college
-- settles separately. Its bill group is what puts it on its own invoice; nothing else on this
-- contract carries one, so everything else stays together on 0087.
--
-- The rentals are grouped BY RATE, not by model: the C5850i rents at 500 and the three 4945i at
-- 300, so the rental invoice prints two lines. KENSINGTON has the same shape.
--
-- Re-runnable: an existing DEMO-KKR is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-KKR');
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

INSERT INTO dbo.zSCP2_Contract
 (ContractNo, ContractTypeCode, DebtorCode, [Description], BillingFormatCode, UseNewLayout,
  MachineLineShows, RentalLineMode, MeterLineMode, BillingMode, BillingDay, ContractDate,
  ServiceStartDate, ServiceExpiryDate, RentalSeparateInvoice, Inactive, CreatedBy, ModifiedBy,
  Created, Modified, LastModified)
VALUES (N'DEMO-KKR', '', N'3000-A0074',
        N'Kolej Komuniti Rompin - 3 invoices: rental apart, four machines merged, one on its own',
        '', 'Y', 'L', 'A', 'A', 'G', 1, GETDATE(), '2025-03-01', '2028-02-29', 'Y', 'N',
        'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

-- ---------------------------------------------------------------- the five machines
--
-- RentGrp is the rental line (two rates -> two lines). MtrGrp is the black line. BillGrp is the
-- INVOICE: blank keeps a machine on the contract's own paper, and PERPUS takes the fifth away.
DECLARE @m TABLE (Pos INT, Serial NVARCHAR(60), Model NVARCHAR(60), Site NVARCHAR(80),
                  RentGrp NVARCHAR(20), Rental DECIMAL(18,2), MtrGrp NVARCHAR(20),
                  BillGrp NVARCHAR(20), BkPrev DECIMAL(18,2), BkCur DECIMAL(18,2));
INSERT INTO @m VALUES
 (1, N'38F11138', N'IRADV DX C5850I', N'U.PENTADBIRAN', N'RENT-500', 500.00, N'KKR', '',        71113,  79255),
 (2, N'4NL01934', N'IRADV DX 4945I',  N'B.CETAK',       N'RENT-300', 300.00, N'KKR', '',        11798,  11890),
 (3, N'4NL01945', N'IRADV DX 4945I',  N'U.P.S.HAYAT',   N'RENT-300', 300.00, N'KKR', '',        59926,  63876),
 (4, N'4NL01992', N'IRADV DX 4945I',  N'B.PENSYARAH',   N'RENT-300', 300.00, N'KKR', '',       180361, 194686),
 (5, N'XYM06022', N'IRADC3530I',      N'PERPUSTAKAAN',  '',            0.00, '',     N'PERPUS',    409,    583);

DECLARE @pos INT, @serial NVARCHAR(60), @model NVARCHAR(60), @site NVARCHAR(80),
        @rentGrp NVARCHAR(20), @rental DECIMAL(18,2), @mtrGrp NVARCHAR(20),
        @billGrp NVARCHAR(20), @bkPrev DECIMAL(18,2), @bkCur DECIMAL(18,2);
DECLARE @ik BIGINT, @imk BIGINT;
DECLARE mc CURSOR LOCAL FAST_FORWARD FOR
    SELECT Pos, Serial, Model, Site, RentGrp, Rental, MtrGrp, BillGrp, BkPrev, BkCur
      FROM @m ORDER BY Pos;
OPEN mc;
FETCH NEXT FROM mc INTO @pos, @serial, @model, @site, @rentGrp, @rental, @mtrGrp, @billGrp, @bkPrev, @bkCur;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.zSCP2_Item
     (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
      MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
      ServiceStartDate, ServiceExpiryDate, LastModified)
    VALUES (@ck, N'DEMO-KKR-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), @model, @serial,
            @model + N' / KKR - ' + @site, @pos, @rentGrp, @mtrGrp, @billGrp, '',
            'N', 'N', 'ONLINE', '2025-03-01', '2028-02-29', GETDATE());
    SET @ik = SCOPE_IDENTITY();

    -- The library copier is not rented. Four rentals, two rates.
    IF @rental > 0
        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES
         (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, @rental, '', 0, 0, 0,
          '2025-03-01', 36, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES
     (@ik, N'BK', N'BK COPY + PRINT A4&A3', N'BK', '', 0, 0.03, '', 0, 0, @bkPrev,
      NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
    SET @imk = SCOPE_IDENTITY();
    INSERT INTO dbo.zSCP2_MeterEntry
     (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
    VALUES (@imk, 2026, 9, @bkCur, '2026-09-30', 'MANUAL', 'N', GETDATE());

    FETCH NEXT FROM mc INTO @pos, @serial, @model, @site, @rentGrp, @rental, @mtrGrp, @billGrp, @bkPrev, @bkCur;
END
CLOSE mc;
DEALLOCATE mc;

INSERT INTO dbo.zSCP2_ContractRentalPrice
 (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
VALUES (@ck, 'R', N'RENT-500', 500.00, 0, 0, '', '', GETDATE()),
       (@ck, 'R', N'RENT-300', 300.00, 0, 0, '', '', GETDATE()),
       (@ck, 'M', N'KKR',        0, 0.03, 0, '', '', GETDATE());

SELECT 'DEMO-KKR built: ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_Item WHERE ContractKey = @ck) AS VARCHAR) + ' machines, ' +
       'expect 3 invoices: rental 1,400.00 | merged black 795.27 | PERPUSTAKAAN 5.22' AS Result;
GO

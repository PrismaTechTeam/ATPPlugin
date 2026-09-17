-- DEMO-PL -- the HOSPITAL PERMAI LAMA shape, rebuilt as a contract.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-permai-lama.sql
--
-- Taken from "Type Of BillingFormat/HOSPITAL PERMAI LAMA.pdf", invoice MR2607.1338. Every number
-- below is off that invoice and its worksheet -- the serials, the readings, the rates, the rental.
--
-- It is the SIMPLEST of the eleven shapes, and worth having for exactly that reason: seven machines,
-- one model, and the whole fleet comes out as three lines on one invoice.
--
--     RA-7 UNIT   RENTAL          7 MTH  @ 334.40  =  2,340.80
--     201-BK C+ P BK COPY         32,852 @ 0.0285  =    936.28
--     223-COLOR   COLOR COPY       6,665 @ 0.304   =  2,026.16
--                                        Net Total =  5,303.24
--
-- Two details that are easy to miss and are the point of keeping it:
--
--   * The rental line's QUANTITY is the number of MACHINES (7), not months. The unit price is the
--     monthly rent of one machine.
--   * The printed meter reading -- 1,037,684 -- is not a reading any machine ever showed. It is the
--     seven added up. The same goes for the previous reading and therefore the usage.
--
-- Re-runnable: an existing DEMO-PL is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-PL');
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

-- One invoice (RentalSeparateInvoice = 'N'), and both line modes 'A' -- merge across model. There is
-- only one model here, so "across model" and "per model" would look the same; 'A' is what the deal
-- actually says, and the day an eighth machine of another model arrives it still bills as one line.
--
-- Dates: the invoice prints the rental as 31/36, so the term started 30 months before July 2026.
INSERT INTO dbo.zSCP2_Contract
 (ContractNo, ContractTypeCode, DebtorCode, [Description], BillingFormatCode, UseNewLayout,
  MachineLineShows, RentalLineMode, MeterLineMode, BillingMode, BillingDay, ContractDate,
  ServiceStartDate, ServiceExpiryDate, RentalSeparateInvoice, Inactive, CreatedBy, ModifiedBy,
  Created, Modified, LastModified)
VALUES (N'DEMO-PL', '', N'3000-A0074',
        N'Hospital Permai Lama - one invoice, whole fleet as one rental + one BK + one CL line',
        '', 'Y', 'L', 'A', 'A', 'G', 1, GETDATE(), '2024-01-01', '2026-12-31', 'N', 'N',
        'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

-- ---------------------------------------------------------------- the seven machines
--
-- All one model, all one merge group, so they fold into one line per charge. The branch each one
-- sits in is kept in the description -- it is how the hospital tells them apart, and it costs
-- nothing to carry.
DECLARE @m TABLE (Pos INT, Serial NVARCHAR(60), Site NVARCHAR(80),
                  BkPrev DECIMAL(18,2), ClPrev DECIMAL(18,2),
                  BkCurr DECIMAL(18,2), ClCurr DECIMAL(18,2));
INSERT INTO @m VALUES
 (1, N'4MD02040', N'KLINIK KESIHATAN',      177500, 14737, 183517, 15089),
 (2, N'4MD02032', N'BANGUNAN PENGURUSAN',   203445, 43563, 208667, 45007),
 (3, N'4MD02037', N'OPERASI / RISIKAN',      82080, 15530, 84857, 16294),
 (4, N'4MD02034', N'SIASATAN',              199724, 29188, 206975, 30604),
 (5, N'4MD02033', N'A & P / LESEN',         252579, 40411, 261593, 41970),
 (6, N'4MD02036', N'IKLAN',                  64491, 26971, 66376, 27926),
 (7, N'4MD02038', N'BANGUNAN SULTAN ISKANDAR', 25013, 1978, 25699, 2153);

DECLARE @pos INT, @serial NVARCHAR(60), @site NVARCHAR(80),
        @bkPrev DECIMAL(18,2), @clPrev DECIMAL(18,2),
        @bkCurr DECIMAL(18,2), @clCurr DECIMAL(18,2), @ik BIGINT, @imk BIGINT;
DECLARE mc CURSOR LOCAL FAST_FORWARD FOR SELECT Pos, Serial, Site, BkPrev, ClPrev, BkCurr, ClCurr FROM @m ORDER BY Pos;
OPEN mc;
FETCH NEXT FROM mc INTO @pos, @serial, @site, @bkPrev, @clPrev, @bkCurr, @clCurr;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.zSCP2_Item
     (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
      MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
      ServiceStartDate, ServiceExpiryDate, LastModified)
    VALUES (@ck, N'DEMO-PL-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), N'IR ADV DX C3930i', @serial,
            N'IR ADV DX C3930i / ' + @site, @pos, N'FLEET', N'FLEET', '', '', 'N', 'N', 'ONLINE',
            '2024-01-01', '2026-12-31', GETDATE());

    SET @ik = SCOPE_IDENTITY();

    -- The rental is 334.40 a month for ONE machine. Seven of them on one line is 2,340.80, which is
    -- what the invoice prints -- quantity 7, not quantity 1 of a lump sum.
    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES
     (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, 334.40, '', 0, 0, 0,
      '2024-01-01', 36, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE()),
     (@ik, N'BK', N'BLACK COPY', N'BK', '', 0, 0.0285, '', 0, 0, @bkPrev,
      NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE()),
     (@ik, N'CL', N'COLOUR COPY', N'CL', '', 0, 0.3040, '', 0, 0, @clPrev,
      NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

    -- The month the invoice covers. The readings are the ones printed on MR2607.1338; the
    -- opening above is the previous month off the same worksheet, so one month of usage is
    -- exactly what the customer was billed.
    INSERT INTO dbo.zSCP2_MeterEntry
     (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
    SELECT m.ItemMeterKey, 2026, 9,
           CASE m.MeterTypeCode WHEN N'BK' THEN @bkCurr ELSE @clCurr END,
           '2026-09-30', 'MANUAL', 'N', GETDATE()
      FROM dbo.zSCP2_ItemMeter m
     WHERE m.ItemKey = @ik AND m.MeterTypeCode IN (N'BK', N'CL');


    FETCH NEXT FROM mc INTO @pos, @serial, @site, @bkPrev, @clPrev, @bkCurr, @clCurr;
END
CLOSE mc;
DEALLOCATE mc;

-- The whole fleet is one rental line at one agreed price. Held on the contract, so a machine added
-- next month joins at the group's price instead of arriving with a number of its own.
INSERT INTO dbo.zSCP2_ContractRentalPrice
 (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
VALUES (@ck, 'R', N'FLEET', 334.40, 0, 0, '', '', GETDATE());

SELECT 'DEMO-PL built: ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_Item WHERE ContractKey = @ck) AS VARCHAR) + ' machines, ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_ItemMeter m
               JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
              WHERE i.ContractKey = @ck) AS VARCHAR) + ' counters' AS Result;
GO

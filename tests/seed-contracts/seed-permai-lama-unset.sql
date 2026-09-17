-- DEMO-PL2 -- the HOSPITAL PERMAI LAMA machines, with the billing setup NOT done.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-permai-lama-unset.sql
--
-- The same seven machines as DEMO-PL, off the same invoice (MR2607.1338) -- same serials, same
-- readings, same rates. What is deliberately NOT done is the billing setup:
--
--     no merge groups        every machine prints its own line
--     no line labels
--     no agreed line price   each machine bills its own rate
--     line modes = S         "one line per machine"
--
-- So it bills as 21 lines: seven rentals, seven black, seven colour. Correct money, unreadable
-- paper. DEMO-PL is the same contract with the setup done -- three lines, same total.
--
-- It exists to be set up in front of a camera. The point it makes is that merging changes what the
-- customer READS, not what the customer PAYS.
--
-- Re-runnable: an existing DEMO-PL2 is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-PL2');
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

-- Both line modes are 'S' -- one line per machine, which is what a contract nobody has set up does.
-- Merging them is the thing to be demonstrated, so it is not done here.
--
-- Dates: the invoice prints the rental as 31/36, so the term started 30 months before July 2026.
INSERT INTO dbo.zSCP2_Contract
 (ContractNo, ContractTypeCode, DebtorCode, [Description], BillingFormatCode, UseNewLayout,
  MachineLineShows, RentalLineMode, MeterLineMode, BillingMode, BillingDay, ContractDate,
  ServiceStartDate, ServiceExpiryDate, RentalSeparateInvoice, Inactive, CreatedBy, ModifiedBy,
  Created, Modified, LastModified)
VALUES (N'DEMO-PL2', '', N'3000-A0074',
        N'Hospital Permai Lama - BILLING SETUP NOT DONE (bills as 21 lines until it is)',
        '', 'Y', 'L', 'S', 'S', 'G', 1, GETDATE(), '2024-01-01', '2026-12-31', 'N', 'N',
        'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

-- ---------------------------------------------------------------- the seven machines
--
-- Same seven machines, but NO merge group and NO line label. Nothing tells the engine these belong
-- together, so each one prints on its own. That is the "before" this contract exists to show.
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
    VALUES (@ck, N'DEMO-PL2-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), N'IR ADV DX C3930i', @serial,
            N'IR ADV DX C3930i / ' + @site, @pos, '', '', '', '', 'N', 'N', 'ONLINE',
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

-- No agreed line price either. A price is agreed for a LINE, and there are no lines yet -- every
-- machine bills the rate on its own counter. Setting one is part of what gets demonstrated.

SELECT 'DEMO-PL2 built: ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_Item WHERE ContractKey = @ck) AS VARCHAR) + ' machines, ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_ItemMeter m
               JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
              WHERE i.ContractKey = @ck) AS VARCHAR) + ' counters' AS Result;
GO

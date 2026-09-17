-- DEMO-3G -- one contract, three branches, three invoices.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-three-groups.sql
--
-- Twenty-seven machines on ONE contract, nine to a branch. Each branch settles its own bill, so
-- each branch gets its own invoice -- and inside each invoice the nine machines fold onto three
-- lines: one rental, one black, one colour.
--
--     HQ       9 machines   rental  9 @ 300.00 = 2,700.00
--                           black   9,000 @ 0.030 =   270.00
--                           colour  1,800 @ 0.300 =   540.00      invoice  3,510.00
--
--     BRANCH   9 machines   rental  9 @ 250.00 = 2,250.00
--                           black  18,000 @ 0.025 =   450.00
--                           colour    900 @ 0.250 =   225.00      invoice  2,925.00
--
--     STORE    9 machines   rental  9 @ 200.00 = 1,800.00
--                           black   4,500 @ 0.020 =    90.00
--                           colour    450 @ 0.200 =    90.00      invoice  1,980.00
--
--                                                       contract  8,415.00
--
-- Three settings do three different jobs here, and the contract exists to keep them apart:
--
--   BillGroupCode         WHICH INVOICE a machine goes on.   Three codes -> three invoices.
--   MergeGroupCode        which machines share a RENTAL line.
--   MergeGroupCodeMeter   which machines share a BLACK / COLOUR line.
--
-- All three carry the same branch name, which is the ordinary case and the reason they are easy to
-- confuse. They are not the same thing: change only the bill group and you get three invoices of
-- nine lines each; change only the merge groups and you get one invoice of three lines.
--
-- Two models are mixed inside every branch on purpose. The lines fold by BRANCH, not by model --
-- with RentalLineMode / MeterLineMode = 'A' the model is not part of the question.
--
-- Every machine's usage is different (no two counters read alike) but each branch's totals are
-- round, so a merged line can be checked by eye against the numbers above.
--
-- Re-runnable: an existing DEMO-3G is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-3G');
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

-- One deal, billed in three. BillingMode 'G' = one invoice per contract; the bill groups on the
-- machines are what break that into three. 'S' here would give an invoice per MACHINE and the bill
-- groups would never come into it.
INSERT INTO dbo.zSCP2_Contract
 (ContractNo, ContractTypeCode, DebtorCode, [Description], BillingFormatCode, UseNewLayout,
  MachineLineShows, RentalLineMode, MeterLineMode, BillingMode, BillingDay, ContractDate,
  ServiceStartDate, ServiceExpiryDate, RentalSeparateInvoice, Inactive, CreatedBy, ModifiedBy,
  Created, Modified, LastModified)
VALUES (N'DEMO-3G', '', N'3000-A0074',
        N'Three branches, three invoices - 27 machines, 9 per branch, 3 lines each',
        '', 'Y', 'L', 'A', 'A', 'G', 1, GETDATE(), '2025-01-01', '2027-12-31', 'N', 'N',
        'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

-- ---------------------------------------------------------------- the three branches
DECLARE @g TABLE (GroupCode NVARCHAR(20), Site NVARCHAR(60), Rental DECIMAL(18,2),
                  BkRate DECIMAL(18,6), ClRate DECIMAL(18,6),
                  BkBase INT, BkStep INT, ClBase INT, ClStep INT, Ord INT);
INSERT INTO @g VALUES
 (N'HQ',     N'HEAD OFFICE',      300.00, 0.030, 0.300,  900, 20, 180, 4, 1),
 (N'BRANCH', N'BRANCH OFFICE',    250.00, 0.025, 0.250, 1900, 20,  90, 2, 2),
 (N'STORE',  N'STORE / WAREHOUSE',200.00, 0.020, 0.200,  400, 20,  40, 2, 3);

DECLARE @grp NVARCHAR(20), @site NVARCHAR(60), @rent DECIMAL(18,2),
        @bkRate DECIMAL(18,6), @clRate DECIMAL(18,6),
        @bkBase INT, @bkStep INT, @clBase INT, @clStep INT, @ord INT;
DECLARE @pos INT = 0, @p INT, @ik BIGINT, @imk BIGINT;
DECLARE @serial NVARCHAR(60), @model NVARCHAR(60);
DECLARE @bkOpen DECIMAL(18,2), @clOpen DECIMAL(18,2), @bkUse INT, @clUse INT;

DECLARE gc CURSOR LOCAL FAST_FORWARD FOR
    SELECT GroupCode, Site, Rental, BkRate, ClRate, BkBase, BkStep, ClBase, ClStep, Ord
      FROM @g ORDER BY Ord;
OPEN gc;
FETCH NEXT FROM gc INTO @grp, @site, @rent, @bkRate, @clRate, @bkBase, @bkStep, @clBase, @clStep, @ord;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @p = 1;
    WHILE @p <= 9
    BEGIN
        SET @pos = @pos + 1;
        SET @serial = N'3G' + RIGHT('00' + CAST(@ord AS VARCHAR), 2) + N'-' +
                      RIGHT('000' + CAST(@pos AS VARCHAR), 3);
        -- Two models in every branch. The lines still fold by branch, which is the point.
        SET @model = CASE WHEN @p % 2 = 1 THEN N'IR ADV DX C3930i' ELSE N'IR ADV DX C5850i' END;

        INSERT INTO dbo.zSCP2_Item
         (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
          MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
          ServiceStartDate, ServiceExpiryDate, LastModified)
        VALUES (@ck, N'DEMO-3G-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), @model, @serial,
                @model + N' / ' + @site, @pos,
                @grp,   -- one rental line per branch
                @grp,   -- one black line and one colour line per branch
                @grp,   -- and its own invoice
                '', 'N', 'N', 'ONLINE', '2025-01-01', '2027-12-31', GETDATE());
        SET @ik = SCOPE_IDENTITY();

        -- Openings: every counter stands somewhere different, the way a real fleet does.
        SET @bkOpen = 100000 + @pos * 1000;
        SET @clOpen =   5000 + @pos *  100;
        -- Usage: different on every machine, but the nine add up to the round branch total.
        SET @bkUse = @bkBase + @bkStep * @p;
        SET @clUse = @clBase + @clStep * @p;

        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES
         (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, @rent, '', 0, 0, 0,
          '2025-01-01', 36, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES
         (@ik, N'BK', N'BLACK COPY', N'BK', '', 0, @bkRate, '', 0, 0, @bkOpen,
          NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
        SET @imk = SCOPE_IDENTITY();
        INSERT INTO dbo.zSCP2_MeterEntry
         (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
        VALUES (@imk, 2026, 9, @bkOpen + @bkUse, '2026-09-30', 'MANUAL', 'N', GETDATE());

        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES
         (@ik, N'CL', N'COLOUR COPY', N'CL', '', 0, @clRate, '', 0, 0, @clOpen,
          NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
        SET @imk = SCOPE_IDENTITY();
        INSERT INTO dbo.zSCP2_MeterEntry
         (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
        VALUES (@imk, 2026, 9, @clOpen + @clUse, '2026-09-30', 'MANUAL', 'N', GETDATE());

        SET @p = @p + 1;
    END

    -- The branch's agreed prices, held on the CONTRACT rather than on nine machines. A tenth machine
    -- moved into this branch next month joins at the branch's price instead of arriving with a
    -- number of its own.
    INSERT INTO dbo.zSCP2_ContractRentalPrice
     (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
    VALUES (@ck, 'R', @grp, @rent, 0, 0, '', '', GETDATE()),
           (@ck, 'M', @grp, 0, @bkRate, @clRate, '', '', GETDATE());

    FETCH NEXT FROM gc INTO @grp, @site, @rent, @bkRate, @clRate, @bkBase, @bkStep, @clBase, @clStep, @ord;
END
CLOSE gc;
DEALLOCATE gc;

SELECT 'DEMO-3G built: ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_Item WHERE ContractKey = @ck) AS VARCHAR) + ' machines, ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_ItemMeter m
               JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
              WHERE i.ContractKey = @ck) AS VARCHAR) + ' counters, ' +
       CAST((SELECT COUNT(DISTINCT BillGroupCode) FROM dbo.zSCP2_Item WHERE ContractKey = @ck) AS VARCHAR) +
       ' bill groups, ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_MeterEntry e
               JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey
               JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
              WHERE i.ContractKey = @ck) AS VARCHAR) + ' readings staged for 9/2026' AS Result;
GO

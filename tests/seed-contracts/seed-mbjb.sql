-- DEMO-MBJB -- the MAJLIS BANDARAYA JOHOR BAHRU shape. Matrix cell #4, at full size.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-mbjb.sql
--
-- From "Type Of BillingFormat/MBJB.pdf", invoice AMR2607.0147 -- ONE invoice, 52 machines, eleven
-- lines:
--
--     RA -2 UNIT   HEAVY DUTY "105 cpm"          2 @ 955.00 =  1,910.00
--     RA -3 UNIT   HEAVY DUTY "75 cpm"           3 @ 650.00 =  1,950.00
--     RA -36 UNIT  MEDIUM HEAVY DUTY "55 cpm"   36 @ 540.00 = 19,440.00
--     RA -11 UNIT  MEDIUM HEAVY DUTY "45 cpm"   11 @ 435.00 =  4,785.00
--                                                 TOTAL RENTAL  28,085.00
--
--     BK  8105i     69,393 @ 0.020 =  1,387.86
--     BK  C5170     23,465 @ 0.020 =    469.30
--     CL  C5170      9,076 @ 0.280 =  2,541.28
--     BK  C5160    193,975 @ 0.020 =  3,879.50
--     CL  C5160     89,900 @ 0.280 = 25,172.00
--     BK  C5150     19,552 @ 0.020 =    391.04
--     CL  C5150     15,518 @ 0.280 =  4,345.04
--                                     TOTAL METER  38,186.02
--                                                  ---------
--                                                  66,271.02   (the invoice adds 6% SST on top)
--
-- Worth having for three things nothing else in the demo book has:
--
--   * SIZE. Fifty-two machines folding to eleven lines. Anything that folds per machine here
--     produces a hundred-line invoice, and it is obvious at a glance which happened.
--
--   * A class with NO COLOUR. The two 8105i print black only, so that class contributes one meter
--     line, not two. Seven meter lines from four classes, not eight.
--
--   * PROOF THE MATRIX IS WRONG ABOUT IT. BILLING-MATRIX.md files MBJB under "BK & CL 每台一组"
--     (one line per machine). The invoice says otherwise: 52 machines, 7 meter lines, grouped by
--     duty class exactly like Pasir Gudang. It is cell #4, not #12 -- and #12 has no customer.
--
-- Every machine's readings differ; each class's readings and usage add up to the printed figures
-- exactly, so a merged line can be checked against the invoice above.
--
-- Re-runnable: an existing DEMO-MBJB is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-MBJB');
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

-- One invoice for everything: rental and meters on the same paper.
INSERT INTO dbo.zSCP2_Contract
 (ContractNo, ContractTypeCode, DebtorCode, [Description], BillingFormatCode, UseNewLayout,
  MachineLineShows, RentalLineMode, MeterLineMode, BillingMode, BillingDay, ContractDate,
  ServiceStartDate, ServiceExpiryDate, RentalSeparateInvoice, Inactive, CreatedBy, ModifiedBy,
  Created, Modified, LastModified)
VALUES (N'DEMO-MBJB', '', N'3000-A0074',
        N'MBJB - 52 machines, one invoice, four duty classes folding to eleven lines',
        '', 'Y', 'L', 'A', 'A', 'G', 1, GETDATE(), '2024-08-01', '2027-07-31', 'N', 'N',
        'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

-- ---------------------------------------------------------------- the four duty classes
DECLARE @c TABLE (Ord INT, Code NVARCHAR(20), Model NVARCHAR(60), Label NVARCHAR(60), Units INT,
                  Rental DECIMAL(18,2),
                  BkPrev BIGINT, BkUse BIGINT, ClPrev BIGINT, ClUse BIGINT);
INSERT INTO @c VALUES
 (1, N'CLS-105', N'imageFORCE 8105i', N'HEAVY DUTY "105 cpm"',         2, 955.00,  358512,  69393,      0,     0),
 (2, N'CLS-75',  N'imageFORCE C5170', N'HEAVY DUTY "75 cpm"',          3, 650.00,  118471,  23465,  62333,  9076),
 (3, N'CLS-55',  N'imageFORCE C5160', N'MEDIUM HEAVY DUTY "55 cpm"',  36, 540.00, 1029478, 193975, 529787, 89900),
 (4, N'CLS-45',  N'imageFORCE C5150', N'MEDIUM HEAVY DUTY "45 cpm"',  11, 435.00,  126012,  19552,  55704, 15518);

DECLARE @ord INT, @code NVARCHAR(20), @model NVARCHAR(60), @label NVARCHAR(60), @units INT,
        @rental DECIMAL(18,2), @bkPrev BIGINT, @bkUse BIGINT, @clPrev BIGINT, @clUse BIGINT;
DECLARE @i INT, @pos INT = 0, @ik BIGINT, @imk BIGINT;
DECLARE @pBk BIGINT, @uBk BIGINT, @pCl BIGINT, @uCl BIGINT;
DECLARE @doneBkP BIGINT, @doneBkU BIGINT, @doneClP BIGINT, @doneClU BIGINT;

DECLARE cc CURSOR LOCAL FAST_FORWARD FOR
    SELECT Ord, Code, Model, Label, Units, Rental, BkPrev, BkUse, ClPrev, ClUse FROM @c ORDER BY Ord;
OPEN cc;
FETCH NEXT FROM cc INTO @ord, @code, @model, @label, @units, @rental, @bkPrev, @bkUse, @clPrev, @clUse;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @i = 1;
    SET @doneBkP = 0; SET @doneBkU = 0; SET @doneClP = 0; SET @doneClU = 0;
    WHILE @i <= @units
    BEGIN
        SET @pos = @pos + 1;

        -- Spread the class's readings over its machines. Every machine differs; the LAST one takes
        -- whatever is left, so the class still adds up to the figure on the invoice exactly.
        IF @i < @units
        BEGIN
            SET @pBk = (@bkPrev / @units) + @i * 7;
            SET @uBk = (@bkUse  / @units) + @i * 3;
            SET @pCl = CASE WHEN @clPrev = 0 THEN 0 ELSE (@clPrev / @units) + @i * 7 END;
            SET @uCl = CASE WHEN @clUse  = 0 THEN 0 ELSE (@clUse  / @units) + @i * 3 END;
        END
        ELSE
        BEGIN
            SET @pBk = @bkPrev - @doneBkP;
            SET @uBk = @bkUse  - @doneBkU;
            SET @pCl = CASE WHEN @clPrev = 0 THEN 0 ELSE @clPrev - @doneClP END;
            SET @uCl = CASE WHEN @clUse  = 0 THEN 0 ELSE @clUse  - @doneClU END;
        END
        SET @doneBkP = @doneBkP + @pBk; SET @doneBkU = @doneBkU + @uBk;
        SET @doneClP = @doneClP + @pCl; SET @doneClU = @doneClU + @uCl;

        INSERT INTO dbo.zSCP2_Item
         (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
          MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
          ServiceStartDate, ServiceExpiryDate, LastModified)
        VALUES (@ck, N'DEMO-MBJB-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), @model,
                N'MBJB' + RIGHT('000' + CAST(@pos AS VARCHAR), 3),
                @model + N' / ' + @label, @pos,
                @code,   -- one rental line per duty class
                @code,   -- one black (and one colour) line per duty class
                '',      -- one invoice for the lot
                @label,  -- the words the invoice prints beside the charge
                'N', 'N', 'ONLINE', '2024-08-01', '2027-07-31', GETDATE());
        SET @ik = SCOPE_IDENTITY();

        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES
         (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, @rental, '', 0, 0, 0,
          '2024-08-01', 36, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES
         (@ik, N'BK', N'BK COPY+PRINT A4&A3', N'BK', '', 0, 0.020, '', 0, 0, @pBk,
          NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
        SET @imk = SCOPE_IDENTITY();
        INSERT INTO dbo.zSCP2_MeterEntry
         (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
        VALUES (@imk, 2026, 9, @pBk + @uBk, '2026-09-30', 'MANUAL', 'N', GETDATE());

        -- The 105 cpm machines have no colour counter at all, which is why that class prints one
        -- meter line and the other three print two.
        IF @clPrev > 0
        BEGIN
            INSERT INTO dbo.zSCP2_ItemMeter
             (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
              MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
              RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
              WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
            VALUES
             (@ik, N'CL', N'CL COPY+PRINT A4&A3', N'CL', '', 0, 0.280, '', 0, 0, @pCl,
              NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
            SET @imk = SCOPE_IDENTITY();
            INSERT INTO dbo.zSCP2_MeterEntry
             (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
            VALUES (@imk, 2026, 9, @pCl + @uCl, '2026-09-30', 'MANUAL', 'N', GETDATE());
        END

        SET @i = @i + 1;
    END

    INSERT INTO dbo.zSCP2_ContractRentalPrice
     (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
    VALUES (@ck, 'R', @code, @rental, 0, 0, '', '', GETDATE()),
           (@ck, 'M', @code, 0, 0.020, CASE WHEN @clPrev > 0 THEN 0.280 ELSE 0 END, '', '', GETDATE());

    FETCH NEXT FROM cc INTO @ord, @code, @model, @label, @units, @rental, @bkPrev, @bkUse, @clPrev, @clUse;
END
CLOSE cc;
DEALLOCATE cc;

SELECT 'DEMO-MBJB built: ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_Item WHERE ContractKey = @ck) AS VARCHAR) + ' machines, ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_ItemMeter m JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
              WHERE i.ContractKey = @ck) AS VARCHAR) + ' counters. ' +
       'Expect 1 invoice, 11 lines, 66,271.02' AS Result;
GO

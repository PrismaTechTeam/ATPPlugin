-- DEMO-TGK -- the HOSPITAL TANGKAK shape, rebuilt as a contract.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-tangkak.sql
--
-- Taken from "Type Of BillingFormat/HOSPITAL TANGKAK_ByBranchOrLocationSplit.pdf". FIVE machines,
-- NINE invoices -- the most documents any customer in the folder gets:
--
--   MR2607.1219  rental  3MB70006   769.50        MR2607.1413  meters  3MB70006     0.00
--   MR2607.1220  rental  3MB70008   769.50        MR2607.1414  meters  3MB70008     0.00
--   MR2607.1221  rental  4WE00869   769.50        MR2607.1415  meters  4WE00869     0.00
--   MR2607.1222  rental  3MB10304 1,406.00        MR2607.1416  meters  2GS03078    61.62
--                                                 MR2607.1417  meters  3MB10304   475.81
--
-- Every machine bills on its own paper AND the rental bills apart from the copies. Four machines
-- are rented, five print -- so it is 4 + 5 = 9, not 5 x 2 = 10. A system that doubles blindly
-- issues an empty rental invoice for 2GS03078.
--
-- THREE OF THE NINE COME TO 0.00 AND ARE STILL ISSUED. Those machines are allowed 5,000 free black
-- copies a month and printed 3,502, 4,018 and 1,783. The customer gets an invoice that says the
-- meter moved and the allowance covered it -- which is the only way they can check it.
--
-- The two that do cost money also carry a 3% rebate, taken after the free copies:
--
--   2GS03078   1,940 black, no allowance      less 3% (58)  = 1,882 @ 0.0285 =  53.64
--                 28 colour                                 =    28 @ 0.2850 =   7.98
--   3MB10304  10,623 black less 5,000 free = 5,623 less 168 = 5,455 @ 0.0285 = 155.47
--              1,658 colour less  500 free = 1,158 less  34 = 1,124 @ 0.2850 = 320.34
--
-- Re-runnable: an existing DEMO-TGK is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-TGK');
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

-- BillingMode 'S' = one invoice per machine. RentalSeparateInvoice 'Y' = and the rental on its own.
-- The two settings compose: 4 rentals + 5 meters = 9 documents.
INSERT INTO dbo.zSCP2_Contract
 (ContractNo, ContractTypeCode, DebtorCode, [Description], BillingFormatCode, UseNewLayout,
  MachineLineShows, ShowModelOnLine, ShowSerialOnLine, ShowUnitsOnLine,
  RentalLineMode, MeterLineMode, BillingMode, BillingDay, ContractDate,
  ServiceStartDate, ServiceExpiryDate, RentalSeparateInvoice, Inactive, CreatedBy, ModifiedBy,
  Created, Modified, LastModified)
VALUES (N'DEMO-TGK', '', N'3000-A0213',
        N'Hospital Tangkak - one invoice per machine AND the rental apart: nine documents from five machines',
        '', 'Y', 'B', 'Y', 'Y', 'N', 'A', 'S', 'S', 1, GETDATE(), '2025-11-01', '2028-10-31',
        'Y', 'N', 'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

DECLARE @m TABLE (Pos INT, Serial NVARCHAR(60), Model NVARCHAR(60), Site NVARCHAR(80),
                  Rental DECIMAL(18,2), BkRate DECIMAL(18,6), BkFoc DECIMAL(18,2), BkReb DECIMAL(18,2),
                  BkPrev DECIMAL(18,2), BkCurr DECIMAL(18,2),
                  ClFoc DECIMAL(18,2), ClPrev DECIMAL(18,2), ClCurr DECIMAL(18,2));
INSERT INTO @m VALUES
 (1, N'3MB70006', N'iR-ADV DX 6870i', N'PATOLOGI 1',    769.50, 0.0290, 5000, 0,  14829,  18331, 0,     0,     0),
 (2, N'3MB70008', N'iR-ADV DX 6870i', N'PATOLOGI 2',    769.50, 0.0290, 5000, 0,  29975,  33993, 0,     0,     0),
 (3, N'4WE00869', N'iR-ADV DX 6870i', N'PADANG LALANG', 769.50, 0.0290, 5000, 0,  14800,  16583, 0,     0,     0),
 (4, N'3MB10304', N'iR-ADV DX 6870i', N'HOSPITAL',     1406.00, 0.0285, 5000, 3, 101028, 111651, 500, 10136, 11794),
 (5, N'2GS03078', N'IRADVC3530',      N'WAD',                0, 0.0285,    0, 3,  30280,  32220, 0,   4955,  4983);

DECLARE @pos INT, @serial NVARCHAR(60), @model NVARCHAR(60), @site NVARCHAR(80),
        @rental DECIMAL(18,2), @bkRate DECIMAL(18,6), @bkFoc DECIMAL(18,2), @bkReb DECIMAL(18,2),
        @bkPrev DECIMAL(18,2), @bkCurr DECIMAL(18,2),
        @clFoc DECIMAL(18,2), @clPrev DECIMAL(18,2), @clCurr DECIMAL(18,2), @ik BIGINT, @imk BIGINT;

DECLARE mc CURSOR LOCAL FAST_FORWARD FOR
  SELECT Pos, Serial, Model, Site, Rental, BkRate, BkFoc, BkReb, BkPrev, BkCurr, ClFoc, ClPrev, ClCurr
    FROM @m ORDER BY Pos;
OPEN mc;
FETCH NEXT FROM mc INTO @pos, @serial, @model, @site, @rental, @bkRate, @bkFoc, @bkReb,
                        @bkPrev, @bkCurr, @clFoc, @clPrev, @clCurr;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.zSCP2_Item
     (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
      MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
      ServiceStartDate, ServiceExpiryDate, LastModified)
    VALUES (@ck, N'DEMO-TGK-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), @model, @serial,
            @model + N' / HOSPITAL TANGKAK ' + @site, @pos, '', '', '', '',
            'N', 'N', 'ONLINE', '2025-11-01', '2028-10-31', GETDATE());
    SET @ik = SCOPE_IDENTITY();

    IF @rental > 0
        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, @rental, '', 0, 0, 0,
                '2025-11-01', 36, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
      RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
      WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
    VALUES (@ik, N'BK', N'BK COPY + PRINT A4&A3', N'BK', '', 0, @bkRate, '', @bkReb, @bkFoc, @bkPrev,
            NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
    SET @imk = SCOPE_IDENTITY();
    INSERT INTO dbo.zSCP2_MeterEntry
     (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
    VALUES (@imk, 2026, 9, @bkCurr, '2026-09-30', 'MANUAL', 'N', GETDATE());

    IF @clPrev > 0 OR @clCurr > 0
    BEGIN
        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
          RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
          WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
        VALUES (@ik, N'CL', N'COLOR COPY + PRINT A4&A3', N'CL', '', 0, 0.2850, '', @bkReb, @clFoc, @clPrev,
                NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
        SET @imk = SCOPE_IDENTITY();
        INSERT INTO dbo.zSCP2_MeterEntry
         (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
        VALUES (@imk, 2026, 9, @clCurr, '2026-09-30', 'MANUAL', 'N', GETDATE());
    END

    FETCH NEXT FROM mc INTO @pos, @serial, @model, @site, @rental, @bkRate, @bkFoc, @bkReb,
                            @bkPrev, @bkCurr, @clFoc, @clPrev, @clCurr;
END
CLOSE mc;
DEALLOCATE mc;

SELECT 'DEMO-TGK built: 5 machines, expect 9 invoices -- 3,714.50 rental and 537.43 meters' AS Result;
GO

-- DEMO-SYN -- the SYNTURN (M) shape, rebuilt as a contract.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-synturn.sql
--
-- Taken from "Type Of BillingFormat/SYNTURN.pdf", invoice MR2607.0192. Two machines, one invoice,
-- two lines:
--
--     109-BK C+ P   BK COPY + PRINT A4&A3   22,853 @ 0.05   = 1,142.65
--     RA-60MTH      MONTHLY RENTAL (49/60)       1 @ 1,399.00 = 1,399.00
--                                                Total          2,541.65
--                                                SST 6%            83.94   <- on the rental only
--                                                Net Total      2,625.59
--
-- What makes this one worth keeping: BLACK AND COLOUR ARE THE SAME PRICE HERE. The deal is 0.05 a
-- click whatever colour it was, with 20,000 clicks free a month, and the invoice prints ONE line.
-- Their worksheet adds the black and colour counters of both machines together to get there:
--
--     JMK04072   black 23,588 -> 26,528     colour 297,627 -> 325,671
--     JMK07419   black  1,672 ->  2,454     colour  47,839 ->  58,926
--                                           -----------------------
--                total        370,726 -> 413,579  =  42,853 clicks
--                less the free allowance                -20,000
--                                                        22,853  @ 0.05 = 1,142.65
--
-- So each machine is modelled with ONE counter carrying its total clicks, which is what the deal
-- actually counts. Splitting it into black and colour would print two lines at one price and say
-- something about the deal that is not true.
--
-- The 20,000 free is split 10,000 and 10,000 -- the invoice only ever shows the pair's total, and
-- the money is the same either way.
--
-- The rental is ONE amount for both machines: quantity 1 at 1,399.00, so the counter sits on the
-- first machine and speaks for both.
--
-- Re-runnable: an existing DEMO-SYN is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-SYN');
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
VALUES (N'DEMO-SYN', '', N'3000-S0033',
        N'Synturn - one price for every click whatever colour, 20,000 free a month, one rental for the pair',
        '', 'Y', 'L', 'N', 'N', 'N', 'A', 'A', 'G', 1, GETDATE(), '2022-09-01', '2027-08-31',
        'N', 'N', 'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

DECLARE @ik BIGINT, @imk BIGINT;

-- 1  iR-ADV C5255 -- carries the rental for both
INSERT INTO dbo.zSCP2_Item
 (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
  MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
  ServiceStartDate, ServiceExpiryDate, LastModified)
VALUES (@ck, N'DEMO-SYN-001', N'iR-ADV C5255', N'JMK04072',
        N'iR-ADV C5255 / SYNTURN (M)', 1, N'PAIR', N'PAIR', '', '',
        'N', 'N', 'ONLINE', '2022-09-01', '2027-08-31', GETDATE());
SET @ik = SCOPE_IDENTITY();

INSERT INTO dbo.zSCP2_ItemMeter
 (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
  MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
  RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
  WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
VALUES (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, 1399.00, '', 0, 0, 0,
        '2022-09-01', 60, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

INSERT INTO dbo.zSCP2_ItemMeter
 (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
  MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
  RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
  WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
VALUES (@ik, N'BK', N'BK COPY + PRINT A4&A3', N'BK', '', 0, 0.05, '', 0, 10000, 321215,
        NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
SET @imk = SCOPE_IDENTITY();
INSERT INTO dbo.zSCP2_MeterEntry
 (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
VALUES (@imk, 2026, 9, 352199, '2026-09-30', 'MANUAL', 'N', GETDATE());

-- 2  the second machine, no rental of its own
INSERT INTO dbo.zSCP2_Item
 (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
  MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
  ServiceStartDate, ServiceExpiryDate, LastModified)
VALUES (@ck, N'DEMO-SYN-002', N'iR-ADV C5255', N'JMK07419',
        N'iR-ADV C5255 / SYNTURN (M)', 2, N'PAIR', N'PAIR', '', '',
        'N', 'N', 'ONLINE', '2022-09-01', '2027-08-31', GETDATE());
SET @ik = SCOPE_IDENTITY();

INSERT INTO dbo.zSCP2_ItemMeter
 (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
  MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
  RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
  WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
VALUES (@ik, N'BK', N'BK COPY + PRINT A4&A3', N'BK', '', 0, 0.05, '', 0, 10000, 49511,
        NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
SET @imk = SCOPE_IDENTITY();
INSERT INTO dbo.zSCP2_MeterEntry
 (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
VALUES (@imk, 2026, 9, 61380, '2026-09-30', 'MANUAL', 'N', GETDATE());

INSERT INTO dbo.zSCP2_ContractRentalPrice
 (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
VALUES (@ck, 'R', N'PAIR', 1399.00, 0, 0, '', '', GETDATE()),
       (@ck, 'M', N'PAIR', 0, 0.05, 0, '', '', GETDATE());

SELECT 'DEMO-SYN built: 2 machines, expect 2,541.65 before SST -- 22,853 clicks @ 0.05 plus 1,399.00 rental'
       AS Result;
GO

-- DEMO-SGL -- the SINGLE ADVERTISING shape, rebuilt as a contract.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-single.sql
--
-- Taken from "Type Of BillingFormat/SINGLE.pdf", invoice MR2607.1250. Two machines, one invoice,
-- three lines:
--
--     201-BK C+ P     BK COPY + PRINT A4&A3       (nothing to pay)
--     223-COLOR C+ P  COLOR COPY + PRINT A4&A3   110 @ 0.40 =    44.00
--     RA-60MTH        MONTHLY RENTAL (16/60)       1 @ 250.00 = 250.00
--                                                  Total          294.00
--                                                  SST 6%          15.00   <- on the rental only
--                                                  Net Total      309.00
--
-- ("SINGLE" is the customer's name -- Single Advertising & Trading -- not "one machine".)
--
-- Two things here that no earlier demo contract has:
--
--  1. FREE COPIES A MONTH. The two machines are allowed 600 and 400 black copies; between them they
--     printed 557. So the black line costs NOTHING and STILL PRINTS, with the allowance shown:
--     "Meter FOC Qty : 1000". A line the customer owes nothing on is not a line to hide -- they are
--     entitled to see the meter moved and the allowance covered it.
--
--  2. The rental is ONE amount for the pair -- quantity 1 MTH at 250.00, not 2 units at 125. So the
--     rental counter sits on the first machine and speaks for both. That is a different arrangement
--     from PERMAI LAMA, where quantity IS the machine count.
--
-- The colour line is the one with money on it, and it is the sum of two machines that printed very
-- differently: 9 copies and 101.
--
-- Re-runnable: an existing DEMO-SGL is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-SGL');
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
VALUES (N'DEMO-SGL', '', N'3000-S0027',
        N'Single Advertising - two machines on one invoice, free copies each month, one rental for the pair',
        '', 'Y', 'L', 'N', 'N', 'N', 'A', 'A', 'G', 1, GETDATE(), '2025-06-01', '2030-05-31',
        'N', 'N', 'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

DECLARE @ik BIGINT, @imk BIGINT;

-- 1  IR ADV DX C3922I -- carries the rental for both machines, and 600 free black copies
INSERT INTO dbo.zSCP2_Item
 (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
  MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
  ServiceStartDate, ServiceExpiryDate, LastModified)
VALUES (@ck, N'DEMO-SGL-001', N'IR ADV DX C3922I', N'4MU08481',
        N'IR ADV DX C3922I / SINGLE ADVERTISING', 1, N'PAIR', N'PAIR', '', '',
        'N', 'N', 'ONLINE', '2025-06-01', '2030-05-31', GETDATE());
SET @ik = SCOPE_IDENTITY();

INSERT INTO dbo.zSCP2_ItemMeter
 (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
  MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
  RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
  WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
VALUES (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, 250.00, '', 0, 0, 0,
        '2025-06-01', 60, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

INSERT INTO dbo.zSCP2_ItemMeter
 (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
  MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
  RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
  WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
VALUES (@ik, N'BK', N'BK COPY + PRINT A4&A3', N'BK', '', 0, 0.03, '', 0, 600, 6158,
        NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
SET @imk = SCOPE_IDENTITY();
INSERT INTO dbo.zSCP2_MeterEntry
 (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
VALUES (@imk, 2026, 9, 6647, '2026-09-30', 'MANUAL', 'N', GETDATE());

INSERT INTO dbo.zSCP2_ItemMeter
 (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
  MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
  RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
  WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
VALUES (@ik, N'CL', N'COLOR COPY + PRINT A4&A3', N'CL', '', 0, 0.40, '', 0, 0, 220,
        NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
SET @imk = SCOPE_IDENTITY();
INSERT INTO dbo.zSCP2_MeterEntry
 (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
VALUES (@imk, 2026, 9, 229, '2026-09-30', 'MANUAL', 'N', GETDATE());

-- 2  iR-ADV C3525i -- no rental of its own; 400 free black copies
INSERT INTO dbo.zSCP2_Item
 (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
  MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
  ServiceStartDate, ServiceExpiryDate, LastModified)
VALUES (@ck, N'DEMO-SGL-002', N'iR-ADV C3525i', N'2GF01415',
        N'iR-ADV C3525i / SINGLE ADVERTISING', 2, N'PAIR', N'PAIR', '', '',
        'N', 'N', 'ONLINE', '2025-06-01', '2030-05-31', GETDATE());
SET @ik = SCOPE_IDENTITY();

INSERT INTO dbo.zSCP2_ItemMeter
 (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
  MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
  RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
  WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
VALUES (@ik, N'BK', N'BK COPY + PRINT A4&A3', N'BK', '', 0, 0.03, '', 0, 400, 1982,
        NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
SET @imk = SCOPE_IDENTITY();
INSERT INTO dbo.zSCP2_MeterEntry
 (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
VALUES (@imk, 2026, 9, 2050, '2026-09-30', 'MANUAL', 'N', GETDATE());

INSERT INTO dbo.zSCP2_ItemMeter
 (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
  MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths,
  RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope,
  WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified)
VALUES (@ik, N'CL', N'COLOR COPY + PRINT A4&A3', N'CL', '', 0, 0.40, '', 0, 0, 2878,
        NULL, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
SET @imk = SCOPE_IDENTITY();
INSERT INTO dbo.zSCP2_MeterEntry
 (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
VALUES (@imk, 2026, 9, 2979, '2026-09-30', 'MANUAL', 'N', GETDATE());

INSERT INTO dbo.zSCP2_ContractRentalPrice
 (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
VALUES (@ck, 'R', N'PAIR', 250.00, 0, 0, '', '', GETDATE()),
       (@ck, 'M', N'PAIR', 0, 0.03, 0.40, '', '', GETDATE());

SELECT 'DEMO-SGL built: 2 machines, expect 294.00 before SST -- black 0.00 (1,000 free), colour 44.00, rental 250.00'
       AS Result;
GO

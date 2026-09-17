-- DEMO-PG -- the HOSPITAL PASIR GUDANG shape, rebuilt as a contract.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-pasir-gudang.sql
--
-- Taken from "Type Of BillingFormat/HOSPITAL PASIR GUDANG.pdf" (invoices MR2607.1106 and .1107) and
-- the analysis of it in BILLING-MATRIX.md. Two things make this contract worth having:
--
--  1. TWO invoices -- the copies on one, the rental on another, and the rental one is entirely FOC
--     with a net total of 0.00. A zero invoice that still gets issued.
--
--  2. The copy lines are grouped by RATE, NOT BY MODEL. Six machines of four models come out as two
--     black lines, because five of them are charged 0.0285 and one is charged 0.019. Grouping by
--     model would print four lines and be wrong. This is the contract that proves "merge by model"
--     is not the same rule as "merge what is charged alike".
--
-- The readings below are the real ones off the invoice: the five medium machines total 517,491
-- before and 570,351 after, exactly as printed on line 02.MR.BK.
--
-- Re-runnable: an existing DEMO-PG is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-PG');
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

-- RentalSeparateInvoice = 'Y' is what makes it two invoices.
-- Both line modes are 'A' (merge across model); the hand-made groups on each machine then decide
-- WHICH machines share a line, and a group always beats the mode.
INSERT INTO dbo.zSCP2_Contract
 (ContractNo, ContractTypeCode, DebtorCode, [Description], BillingFormatCode, UseNewLayout,
  MachineLineShows, RentalLineMode, MeterLineMode, BillingMode, BillingDay, ContractDate,
  ServiceStartDate, ServiceExpiryDate, RentalSeparateInvoice, Inactive, CreatedBy, ModifiedBy,
  Created, Modified, LastModified)
VALUES (N'DEMO-PG', '', N'3000-A0005',
        N'Hospital Pasir Gudang - 2 invoices, copies grouped by RATE not model, rental free for 12 months',
        '', 'Y', 'L', 'A', 'A', 'G', 1, GETDATE(), '2026-01-01', '2028-12-31', 'Y', 'N',
        'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

DECLARE @ik BIGINT;

-- ---------------------------------------------------------------- the six machines
--
--  MergeGroupCodeMeter  which copy line it lands on   -- by RATE
--  MergeGroupCode       which rental line it lands on -- 1 unit / 1 unit / 3 units
--  LineGroupCode        the words printed on the line
--
-- Machine 6 carries no rental meter at all: the invoice bills five rentals, not six.

-- 1  iR-ADV C5550i -- the only machine with colour
INSERT INTO dbo.zSCP2_Item
 (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
  MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
  ServiceStartDate, ServiceExpiryDate, LastModified)
VALUES (@ck, N'DEMO-PG-001', N'iR-ADV C5550i', N'PG-0001', N'iR-ADV C5550i / PG-0001', 1,
        N'RA3', N'MEDIUM', '', N'MEDIUM DUTY', 'N', 'N', 'ONLINE', '2026-01-01', '2028-12-31', GETDATE());
SET @ik = SCOPE_IDENTITY();
INSERT INTO dbo.zSCP2_ItemMeter
 (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
  MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalMonths, RentalBasis,
  WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope, WaivePartialThreshold,
  WaivePartialAmount, CommitScope, LastModified)
VALUES (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, 300.00,      '', 0, 0, 0,       0, 'A', 0,0,100,'S',0,0,'S', GETDATE()),
       (@ik, N'BK',     N'BLACK COPY',     N'BK',     '', 0, 0.0285, '', 0, 0, 120000,  0, 'A', 0,0,100,'S',0,0,'S', GETDATE()),
       (@ik, N'CL',     N'COLOUR COPY',    N'CL',     '', 0, 0.2850, '', 0, 0, 40000,   0, 'A', 0,0,100,'S',0,0,'S', GETDATE());

-- 2  iR-ADV 4545i
INSERT INTO dbo.zSCP2_Item
 (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
  MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
  ServiceStartDate, ServiceExpiryDate, LastModified)
VALUES (@ck, N'DEMO-PG-002', N'iR-ADV 4545i', N'PG-0002', N'iR-ADV 4545i / PG-0002', 2,
        N'RA3', N'MEDIUM', '', N'MEDIUM DUTY', 'N', 'N', 'ONLINE', '2026-01-01', '2028-12-31', GETDATE());
SET @ik = SCOPE_IDENTITY();
INSERT INTO dbo.zSCP2_ItemMeter
 (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
  MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalMonths, RentalBasis,
  WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope, WaivePartialThreshold,
  WaivePartialAmount, CommitScope, LastModified)
VALUES (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, 300.00,      '', 0, 0, 0,      0, 'A', 0,0,100,'S',0,0,'S', GETDATE()),
       (@ik, N'BK',     N'BLACK COPY',     N'BK',     '', 0, 0.0285, '', 0, 0, 95000,  0, 'A', 0,0,100,'S',0,0,'S', GETDATE());

-- 3  iR-ADV 4545i
INSERT INTO dbo.zSCP2_Item
 (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
  MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
  ServiceStartDate, ServiceExpiryDate, LastModified)
VALUES (@ck, N'DEMO-PG-003', N'iR-ADV 4545i', N'PG-0003', N'iR-ADV 4545i / PG-0003', 3,
        N'RA3', N'MEDIUM', '', N'MEDIUM DUTY', 'N', 'N', 'ONLINE', '2026-01-01', '2028-12-31', GETDATE());
SET @ik = SCOPE_IDENTITY();
INSERT INTO dbo.zSCP2_ItemMeter
 (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
  MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalMonths, RentalBasis,
  WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope, WaivePartialThreshold,
  WaivePartialAmount, CommitScope, LastModified)
VALUES (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, 300.00,      '', 0, 0, 0,      0, 'A', 0,0,100,'S',0,0,'S', GETDATE()),
       (@ik, N'BK',     N'BLACK COPY',     N'BK',     '', 0, 0.0285, '', 0, 0, 88000,  0, 'A', 0,0,100,'S',0,0,'S', GETDATE());

-- 4  iR-ADV 4545i -- its own rental line (1 unit)
INSERT INTO dbo.zSCP2_Item
 (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
  MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
  ServiceStartDate, ServiceExpiryDate, LastModified)
VALUES (@ck, N'DEMO-PG-004', N'iR-ADV 4545i', N'PG-0004', N'iR-ADV 4545i / PG-0004', 4,
        N'RA1', N'MEDIUM', '', N'MEDIUM DUTY', 'N', 'N', 'ONLINE', '2026-01-01', '2028-12-31', GETDATE());
SET @ik = SCOPE_IDENTITY();
INSERT INTO dbo.zSCP2_ItemMeter
 (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
  MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalMonths, RentalBasis,
  WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope, WaivePartialThreshold,
  WaivePartialAmount, CommitScope, LastModified)
VALUES (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, 300.00,      '', 0, 0, 0,       0, 'A', 0,0,100,'S',0,0,'S', GETDATE()),
       (@ik, N'BK',     N'BLACK COPY',     N'BK',     '', 0, 0.0285, '', 0, 0, 130491,  0, 'A', 0,0,100,'S',0,0,'S', GETDATE());

-- 5  iR-ADV C4535i -- its own rental line (1 unit)
INSERT INTO dbo.zSCP2_Item
 (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
  MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
  ServiceStartDate, ServiceExpiryDate, LastModified)
VALUES (@ck, N'DEMO-PG-005', N'iR-ADV C4535i', N'PG-0005', N'iR-ADV C4535i / PG-0005', 5,
        N'RA2', N'MEDIUM', '', N'MEDIUM DUTY', 'N', 'N', 'ONLINE', '2026-01-01', '2028-12-31', GETDATE());
SET @ik = SCOPE_IDENTITY();
INSERT INTO dbo.zSCP2_ItemMeter
 (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
  MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalMonths, RentalBasis,
  WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope, WaivePartialThreshold,
  WaivePartialAmount, CommitScope, LastModified)
VALUES (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, 300.00,      '', 0, 0, 0,      0, 'A', 0,0,100,'S',0,0,'S', GETDATE()),
       (@ik, N'BK',     N'BLACK COPY',     N'BK',     '', 0, 0.0285, '', 0, 0, 84000,  0, 'A', 0,0,100,'S',0,0,'S', GETDATE());

-- 6  iR-ADV 8505 -- heavy duty, its own rate, and NO RENTAL
INSERT INTO dbo.zSCP2_Item
 (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
  MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
  ServiceStartDate, ServiceExpiryDate, LastModified)
VALUES (@ck, N'DEMO-PG-006', N'iR-ADV 8505', N'PG-0006', N'iR-ADV 8505 / PG-0006', 6,
        '', N'HEAVY', '', N'HEAVY DUTY', 'N', 'N', 'ONLINE', '2026-01-01', '2028-12-31', GETDATE());
SET @ik = SCOPE_IDENTITY();
INSERT INTO dbo.zSCP2_ItemMeter
 (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
  MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalMonths, RentalBasis,
  WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope, WaivePartialThreshold,
  WaivePartialAmount, CommitScope, LastModified)
VALUES (@ik, N'BK', N'BLACK COPY', N'BK', '', 0, 0.0190, '', 0, 0, 900000, 0, 'A', 0,0,100,'S',0,0,'S', GETDATE());

-- ---------------------------------------------------------------- the rental, and why it is free
--
-- The rental is 300.00 a machine -- a real figure, not a zero. What makes the invoice come to 0.00
-- is the deal on top of it: FREE FOR THE FIRST 12 MONTHS, which lives in seed-pasir-gudang-foc.sql
-- and is set through Meters & Pricing the way a person would set it.
--
--     RA3  three machines  3 x 300.00      free this month
--     RA1  one machine         300.00      free this month
--     RA2  one machine         300.00      free this month
--                                          Net Total  0.00
--
-- FREE IS NOT WAIVED. In the free window the rental is simply not charged -- three lines, nothing
-- to pay -- which is what MR2607.1107 prints (three lines reading FOC). It is NOT billed at 300
-- and credited back on a contra line: that is a different deal that happens to reach the same
-- total, and the customer can see which one they signed.
--
-- The only difference left is the word: their layout writes "FOC" in the amount column where
-- ours writes 0.00. That is the AutoCount report layout, not the figures.
INSERT INTO dbo.zSCP2_ContractRentalPrice
 (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
VALUES (@ck, 'R', N'RA1', 300, 0, 0, '', '', GETDATE()),
       (@ck, 'R', N'RA2', 300, 0, 0, '', '', GETDATE()),
       (@ck, 'R', N'RA3', 300, 0, 0, '', '', GETDATE());

-- ---------------------------------------------------------------- the month being billed
--
-- Each reading is the opening above plus exactly the usage MR2607.1106/.1107 billed, so one month
-- of this contract reproduces the invoice to the cent:
--
--     five medium machines   52,860 @ 0.0285 = 1,506.51
--     the heavy one          60,249 @ 0.0190 = 1,144.73
--     the one colour counter  5,693 @ 0.2850 = 1,622.51
--                                              4,273.75
--
-- The four colour counters that read nothing get no reading at all -- they are on the machines,
-- and they print nothing, which is what the invoice shows.
DECLARE @r TABLE (ItemNo NVARCHAR(50), MeterType NVARCHAR(20), Curr DECIMAL(18,2));
INSERT INTO @r VALUES
 (N'DEMO-PG-001', N'BK', 148643), (N'DEMO-PG-001', N'CL', 45693),
 (N'DEMO-PG-002', N'BK', 105040),
 (N'DEMO-PG-003', N'BK',  92049),
 (N'DEMO-PG-004', N'BK', 137662),
 (N'DEMO-PG-005', N'BK',  86957),
 (N'DEMO-PG-006', N'BK', 960249);

INSERT INTO dbo.zSCP2_MeterEntry
 (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
SELECT m.ItemMeterKey, 2026, 9, r.Curr, '2026-09-30', 'MANUAL', 'N', GETDATE()
  FROM @r r
  JOIN dbo.zSCP2_Item i ON i.ContractKey = @ck AND i.ServiceItemNo = r.ItemNo
  JOIN dbo.zSCP2_ItemMeter m ON m.ItemKey = i.ItemKey AND m.MeterTypeCode = r.MeterType;

SELECT 'DEMO-PG built: ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_Item WHERE ContractKey = @ck) AS VARCHAR) + ' machines, ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_ItemMeter m
               JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
              WHERE i.ContractKey = @ck) AS VARCHAR) + ' counters' AS Result;
GO

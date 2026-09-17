-- DEMO-DUTY -- what a Line label is, and what it is not.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-duty-labels.sql
--
-- A duty label is the trade's word for how hard a machine is built to work, with its speed beside
-- it. The customer's own invoices use four:
--
--     LIGHT DUTY                    a desk machine
--     MEDIUM DUTY "30-50 ppm"       an office floor
--     MEDIUM HEAVY DUTY "57 ppm"    a busy department
--     HEAVY DUTY "65 ppm"           a print room
--
-- It is printed beside the charge and NOTHING else. It does not decide which machines share a
-- line, and it does not decide which invoice they go on. The contract screen used to claim
-- otherwise -- "machines sharing a label also share a line" -- and nothing in the engine has ever
-- keyed off it. This contract is built to show both halves of that at once:
--
--   FLEET-A   four machines, THREE different labels, on ONE line.
--             Different labels do not split a line. The line prints all three.
--
--   FLEET-B   three machines, all MEDIUM HEAVY DUTY "57 ppm" -- the SAME label one of the
--             FLEET-A machines carries -- on a line of their own.
--             The same label does not merge them. The merge group did that, and it says B.
--
--   the ninth  one machine in no group at all: its own line, its own label.
--
-- What it bills, one invoice, nine lines:
--
--     FLEET-A  rental  4 @ 450.00 = 1,800.00   MEDIUM DUTY "30-50 ppm",
--                                              MEDIUM HEAVY DUTY "57 ppm", HEAVY DUTY "65 ppm"
--              black   15,000 @ 0.0250 =  375.00
--              colour   2,600 @ 0.2500 =  650.00
--     FLEET-B  rental  3 @ 380.00 = 1,140.00   MEDIUM HEAVY DUTY "57 ppm"
--              black   18,000 @ 0.0220 =  396.00
--              colour   3,000 @ 0.2200 =  660.00
--     solo     rental  1 @ 220.00 =   220.00   LIGHT DUTY
--              black    2,000 @ 0.0300 =   60.00
--              colour     400 @ 0.3000 =  120.00
--                                     ---------
--                                        5,421.00
--
-- Re-runnable: an existing DEMO-DUTY is removed and rebuilt.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'DEMO-DUTY');
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

-- One invoice for the lot: no bill groups, so nothing here splits the paper. Everything you see
-- comes from the merge groups and the labels.
INSERT INTO dbo.zSCP2_Contract
 (ContractNo, ContractTypeCode, DebtorCode, [Description], BillingFormatCode, UseNewLayout,
  MachineLineShows, RentalLineMode, MeterLineMode, BillingMode, BillingDay, ContractDate,
  ServiceStartDate, ServiceExpiryDate, RentalSeparateInvoice, Inactive, CreatedBy, ModifiedBy,
  Created, Modified, LastModified)
VALUES (N'DEMO-DUTY', '', N'3000-A0074',
        N'Duty labels - three labels on one line, one label on two lines',
        '', 'Y', 'L', 'A', 'A', 'G', 1, GETDATE(), '2025-01-01', '2027-12-31', 'N', 'N',
        'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

-- ---------------------------------------------------------------- the machines
--
-- Grp is the merge group -- the thing that actually decides the line. Label is the word printed
-- beside the charge. Read the two columns side by side: FLEET-A holds three labels, and
-- MEDIUM HEAVY DUTY "57 ppm" appears in BOTH groups.
DECLARE @m TABLE (Pos INT, Serial NVARCHAR(60), Model NVARCHAR(60), Grp NVARCHAR(20),
                  Label NVARCHAR(60), Rental DECIMAL(18,2),
                  BkRate DECIMAL(18,6), ClRate DECIMAL(18,6),
                  BkUse INT, ClUse INT);
INSERT INTO @m VALUES
 (1, N'DUTY-001', N'iR-ADV DX 4735i',  N'FLEET-A', N'MEDIUM DUTY "30-50 ppm"',    450.00, 0.0250, 0.2500, 3000,  500),
 (2, N'DUTY-002', N'iR-ADV DX 4745i',  N'FLEET-A', N'MEDIUM DUTY "30-50 ppm"',    450.00, 0.0250, 0.2500, 3500,  600),
 (3, N'DUTY-003', N'iR-ADV DX 6860i',  N'FLEET-A', N'MEDIUM HEAVY DUTY "57 ppm"', 450.00, 0.0250, 0.2500, 4000,  700),
 (4, N'DUTY-004', N'iR-ADV DX 6765i',  N'FLEET-A', N'HEAVY DUTY "65 ppm"',        450.00, 0.0250, 0.2500, 4500,  800),
 (5, N'DUTY-005', N'iR-ADV DX 6860i',  N'FLEET-B', N'MEDIUM HEAVY DUTY "57 ppm"', 380.00, 0.0220, 0.2200, 5000,  900),
 (6, N'DUTY-006', N'iR-ADV DX 6860i',  N'FLEET-B', N'MEDIUM HEAVY DUTY "57 ppm"', 380.00, 0.0220, 0.2200, 6000, 1000),
 (7, N'DUTY-007', N'iR-ADV DX 6860i',  N'FLEET-B', N'MEDIUM HEAVY DUTY "57 ppm"', 380.00, 0.0220, 0.2200, 7000, 1100),
 (8, N'DUTY-008', N'iR-ADV DX C3830i', '',         N'LIGHT DUTY',                 220.00, 0.0300, 0.3000, 2000,  400);

DECLARE @pos INT, @serial NVARCHAR(60), @model NVARCHAR(60), @grp NVARCHAR(20),
        @label NVARCHAR(60), @rent DECIMAL(18,2),
        @bkRate DECIMAL(18,6), @clRate DECIMAL(18,6), @bkUse INT, @clUse INT;
DECLARE @ik BIGINT, @imk BIGINT, @bkOpen DECIMAL(18,2), @clOpen DECIMAL(18,2);

DECLARE mc CURSOR LOCAL FAST_FORWARD FOR
    SELECT Pos, Serial, Model, Grp, Label, Rental, BkRate, ClRate, BkUse, ClUse FROM @m ORDER BY Pos;
OPEN mc;
FETCH NEXT FROM mc INTO @pos, @serial, @model, @grp, @label, @rent, @bkRate, @clRate, @bkUse, @clUse;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.zSCP2_Item
     (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode,
      MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode,
      ServiceStartDate, ServiceExpiryDate, LastModified)
    VALUES (@ck, N'DEMO-DUTY-' + RIGHT('000' + CAST(@pos AS VARCHAR), 3), @model, @serial,
            @model + N' / ' + @label, @pos,
            @grp,   -- the merge group decides the line
            @grp,
            '',     -- no bill group: one invoice
            @label, -- the label is only printed
            'N', 'N', 'ONLINE', '2025-01-01', '2027-12-31', GETDATE());
    SET @ik = SCOPE_IDENTITY();

    SET @bkOpen = 200000 + @pos * 1000;
    SET @clOpen =   8000 + @pos *  100;

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

    FETCH NEXT FROM mc INTO @pos, @serial, @model, @grp, @label, @rent, @bkRate, @clRate, @bkUse, @clUse;
END
CLOSE mc;
DEALLOCATE mc;

-- The agreed price of each line. The ninth machine is in no group, so there is no line to agree a
-- price for -- it bills the rate on its own counter, which is what an ungrouped machine does.
INSERT INTO dbo.zSCP2_ContractRentalPrice
 (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
VALUES (@ck, 'R', N'FLEET-A', 450.00, 0, 0, '', '', GETDATE()),
       (@ck, 'M', N'FLEET-A', 0, 0.0250, 0.2500, '', '', GETDATE()),
       (@ck, 'R', N'FLEET-B', 380.00, 0, 0, '', '', GETDATE()),
       (@ck, 'M', N'FLEET-B', 0, 0.0220, 0.2200, '', '', GETDATE());

SELECT 'DEMO-DUTY built: ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_Item WHERE ContractKey = @ck) AS VARCHAR) + ' machines, ' +
       CAST((SELECT COUNT(DISTINCT LineGroupCode) FROM dbo.zSCP2_Item
              WHERE ContractKey = @ck AND ISNULL(LineGroupCode,'') <> '') AS VARCHAR) + ' labels, ' +
       CAST((SELECT COUNT(DISTINCT MergeGroupCode) FROM dbo.zSCP2_Item
              WHERE ContractKey = @ck AND ISNULL(MergeGroupCode,'') <> '') AS VARCHAR) + ' merge groups' AS Result;
GO

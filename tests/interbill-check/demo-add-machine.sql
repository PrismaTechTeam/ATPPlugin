-- Puts one more machine on the PARENT's contract, which the subsidiary has not taken.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ASNDUMMY -I -i tests\interbill-check\demo-add-machine.sql
--
-- This is the "they changed something" half of the story, staged so it can be shown on demand: run
-- it, then Refresh the Inter-Billing screen and the machine appears under "2. What they changed" as
-- something to take or to leave.
--
-- A different MODEL on purpose. The three seeded machines are all iR-ADV C3560i, so a fourth of the
-- same model is easy to miss on screen; a C5850i is obviously new.
--
-- Re-runnable, and run.ps1 removes it again -- the harness resets the parent to its seeded three
-- before every run, because the counts it checks are written down.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'HQ-2026-001');
DECLARE @ik BIGINT;

IF @ck IS NULL
BEGIN
    RAISERROR('HQ-2026-001 is not in this book -- run seed-parent-book.sql first.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM dbo.zSCP2_Item WHERE SerialNumber = N'HQA-005')
BEGIN
    INSERT INTO dbo.zSCP2_Item
     (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos,
      MergeGroupCode, MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem,
      MachineMode, ServiceStartDate, ServiceExpiryDate, LastModified)
    VALUES (@ck, N'HQ-2026-001-005', N'iR-ADV C5850i', N'HQA-005',
            N'iR-ADV C5850i / HQA-005', 5, '', '', '', '', 'N', 'N', 'ONLINE',
            '2026-01-01', '2028-12-31', GETDATE());
    SET @ik = SCOPE_IDENTITY();

    -- The parent's own prices, which is exactly what will NOT cross when the subsidiary takes it.
    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalMonths, RentalBasis,
      WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope, WaivePartialThreshold,
      WaivePartialAmount, CommitScope, LastModified)
    VALUES
     (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, 520.00, '', 0, 0, 0,     0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE()),
     (@ik, N'BK',     N'BLACK COPY',     N'BK',     '', 0, 0.0170, '', 0, 0, 41000, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE()),
     (@ik, N'CL',     N'COLOUR COPY',    N'CL',     '', 0, 0.1600, '', 0, 0, 12500, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

    -- ...and the reading they took for it, so it can be billed the moment it is taken and priced.
    INSERT INTO dbo.zSCP2_MeterEntry
     (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
    SELECT m.ItemMeterKey, 2026, 9,
           m.InitialReading + CASE WHEN m.MeterTypeCode = 'BK' THEN 6200 ELSE 900 END,
           '2026-09-28', 'ONLINE', 'N', GETDATE()
      FROM dbo.zSCP2_ItemMeter m
     WHERE m.ItemKey = @ik AND m.MeterTypeCode IN ('BK', 'CL');
END

SELECT 'HQA-005 added -- the parent contract now has ' +
       CAST((SELECT COUNT(*) FROM dbo.zSCP2_Item WHERE ContractKey = @ck) AS VARCHAR) +
       ' machines' AS Result;

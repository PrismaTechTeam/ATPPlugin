-- HQ's side of the Inter-Billing board demo, for the PARENT book (AED_ASNDUMMY).
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ASNDUMMY -I -i tests\interbill-board\seed-board-hq.sql
--
-- Run tests\interbill-check\seed-parent-book.sql first: it makes HQ-2026-001 (3 machines, September
-- readings). This file adds one HQ contract for each other state the board shows:
--
--   HQ-2026-002   4 machines, Rental BK CL, Sep readings      taken, then HQ adds HQB-005  -> Changed at HQ
--   HQ-2026-003   2 machines, Rental BK, HQ bills BK at 0      taken with BK at 0.0000       -> Unpriced
--   HQ-2026-004   4 machines                                   not taken                     -> Not taken
--   HQ-2026-005   1 machine, ended 31 Aug                      not taken                     -> Ended (hidden)
--   HQ-2026-006   2 machines, only HQF-001 read in September   taken                         -> Waiting HQ reading
--   HQ-2026-007   3 machines, Rental BK CL, all read            taken                         -> Ready
--
-- HQ-2026-001 (from seed-parent-book.sql) is left Not taken: AED_ATPTEST still holds a local contract
-- with that number from the 7 Sep fixture, so taking it there would clash on the number.
--
-- Rerunnable: each contract is removed with its machines, counters and readings before it is put back.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.zSCP2_BookIdentity') IS NULL OR OBJECT_ID('dbo.zSCP2_MeterEntry') IS NULL
BEGIN
    RAISERROR('This book has no Service Contract tables. Install the plug-in and open the book once.', 16, 1);
    SET NOEXEC ON;
END
GO

DECLARE @c TABLE (No NVARCHAR(30), Descr NVARCHAR(100), Machines INT, Prefix NVARCHAR(10), Model NVARCHAR(40),
                  HasCl CHAR(1), BkRate DECIMAL(18,6), Rental DECIMAL(18,2), Expiry DATE, Inactive CHAR(1), ReadUpTo INT);
INSERT INTO @c VALUES
 (N'HQ-2026-002', N'Kolej fleet',          4, N'HQB', N'iR-ADV C3530i', 'Y', 0.0200, 420.00, '2028-12-31', 'N', 4),
 (N'HQ-2026-003', N'Two mono units',       2, N'HQC', N'iR 2645i',      'N', 0.0000, 380.00, '2028-12-31', 'N', 2),
 (N'HQ-2026-004', N'Kastam fleet',         4, N'HQD', N'iR-ADV C3560i', 'Y', 0.0180, 380.00, '2028-12-31', 'N', 4),
 (N'HQ-2026-005', N'Returned unit',        1, N'HQE', N'iR 2625i',      'N', 0.0200, 300.00, '2026-08-31', 'N', 1),
 (N'HQ-2026-006', N'JPJ pair',             2, N'HQF', N'iR-ADV C5840i', 'Y', 0.0190, 450.00, '2028-12-31', 'N', 1),
 (N'HQ-2026-007', N'Hospital fleet',       3, N'HQG', N'iR-ADV C3560i', 'Y', 0.0180, 380.00, '2028-12-31', 'N', 3);

DECLARE @no NVARCHAR(30), @descr NVARCHAR(100), @n INT, @prefix NVARCHAR(10), @model NVARCHAR(40), @hasCl CHAR(1),
        @bk DECIMAL(18,6), @rent DECIMAL(18,2), @exp DATE, @inact CHAR(1), @readUpTo INT;
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT No, Descr, Machines, Prefix, Model, HasCl, BkRate, Rental, Expiry, Inactive, ReadUpTo FROM @c;
OPEN cur;
FETCH NEXT FROM cur INTO @no, @descr, @n, @prefix, @model, @hasCl, @bk, @rent, @exp, @inact, @readUpTo;
WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = @no);
    IF @ck IS NOT NULL
    BEGIN
        DELETE e FROM dbo.zSCP2_MeterEntry e JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey
          JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey WHERE i.ContractKey = @ck;
        DELETE m FROM dbo.zSCP2_ItemMeter m JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey WHERE i.ContractKey = @ck;
        DELETE FROM dbo.zSCP2_Item WHERE ContractKey = @ck;
        DELETE FROM dbo.zSCP2_Contract WHERE ContractKey = @ck;
    END

    INSERT INTO dbo.zSCP2_Contract
     (ContractNo, ContractTypeCode, DebtorCode, [Description], BillingFormatCode, UseNewLayout,
      MachineLineShows, RentalLineMode, MeterLineMode, BillingMode, BillingDay, ContractDate,
      ServiceStartDate, ServiceExpiryDate, RentalSeparateInvoice, Inactive, CreatedBy, ModifiedBy,
      Created, Modified, LastModified)
    VALUES (@no, '', N'3000-SUB01', @descr, '', 'Y', 'B', 'S', 'S', 'G', 1, GETDATE(),
            '2026-01-01', @exp, 'N', @inact, 'ADMIN', 'ADMIN', GETDATE(), GETDATE(), GETDATE());
    SET @ck = SCOPE_IDENTITY();

    DECLARE @i INT = 1;
    WHILE @i <= @n
    BEGIN
        DECLARE @sn NVARCHAR(60) = @prefix + N'-' + RIGHT('000' + CAST(@i AS VARCHAR), 3);
        INSERT INTO dbo.zSCP2_Item
         (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos,
          MergeGroupCode, MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem,
          MachineMode, ServiceStartDate, ServiceExpiryDate, LastModified)
        VALUES (@ck, @no + N'-' + RIGHT('000' + CAST(@i AS VARCHAR), 3), @model, @sn, @model + N' / ' + @sn, @i,
                '', '', '', '', 'N', 'N', 'ONLINE', '2026-01-01', @exp, GETDATE());
        DECLARE @ik BIGINT = SCOPE_IDENTITY();

        INSERT INTO dbo.zSCP2_ItemMeter
         (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
          MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalMonths, RentalBasis,
          WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope, WaivePartialThreshold,
          WaivePartialAmount, CommitScope, LastModified)
        VALUES
         (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, @rent, '', 0, 0, 0, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE()),
         (@ik, N'BK', N'BLACK COPY', N'BK', '', 0, @bk, '', 0, 0, 50000 + @i * 7301, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());
        IF @hasCl = 'Y'
            INSERT INTO dbo.zSCP2_ItemMeter
             (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
              MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalMonths, RentalBasis,
              WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope, WaivePartialThreshold,
              WaivePartialAmount, CommitScope, LastModified)
            VALUES (@ik, N'CL', N'COLOUR COPY', N'CL', '', 0, 0.2000, '', 0, 0, 9000 + @i * 811, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

        -- HQ's September readings: fetched by PUMS for most, keyed at HQ for the third machine.
        IF @i <= @readUpTo
            INSERT INTO dbo.zSCP2_MeterEntry (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
            SELECT m.ItemMeterKey, 2026, 9,
                   m.InitialReading + CASE WHEN m.MeterTypeCode = 'BK' THEN 4200 + @i * 610 ELSE 380 + @i * 45 END,
                   CASE WHEN @i = 3 THEN '2026-09-12 16:20' ELSE '2026-09-13 09:05' END,
                   CASE WHEN @i = 3 THEN 'MANUAL' ELSE 'ONLINE' END, 'N', GETDATE()
              FROM dbo.zSCP2_ItemMeter m WHERE m.ItemKey = @ik AND m.MeterTypeCode IN ('BK','CL');
        SET @i = @i + 1;
    END
    FETCH NEXT FROM cur INTO @no, @descr, @n, @prefix, @model, @hasCl, @bk, @rent, @exp, @inact, @readUpTo;
END
CLOSE cur; DEALLOCATE cur;

-- HQ-2026-007 was already billed at HQ in August. A contract taken mid-life must start from HQ's
-- August reading, not from the install reading: September copies = September - August.
INSERT INTO dbo.zSCP2_MeterEntry (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
SELECT m.ItemMeterKey, 2026, 8, m.InitialReading + CASE WHEN m.MeterTypeCode = 'BK' THEN 1500 ELSE 150 END,
       '2026-08-13 09:05', 'ONLINE', 'N', GETDATE()
  FROM dbo.zSCP2_ItemMeter m JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey
 WHERE c.ContractNo = N'HQ-2026-007' AND m.MeterTypeCode IN ('BK','CL');

SELECT c.ContractNo, machines = (SELECT COUNT(*) FROM dbo.zSCP2_Item i WHERE i.ContractKey = c.ContractKey),
       readings = (SELECT COUNT(*) FROM dbo.zSCP2_MeterEntry e JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey
                    JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey WHERE i.ContractKey = c.ContractKey AND e.PeriodYear = 2026 AND e.PeriodMonth = 9)
  FROM dbo.zSCP2_Contract c WHERE c.ContractNo LIKE N'HQ-2026-%' ORDER BY c.ContractNo;
GO
SET NOEXEC OFF;
GO

-- Puts a parent-company contract into a REAL account book, so inter-billing can be tested against
-- the thing it will actually run against.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d <the new book> -i tests\interbill-check\seed-parent-book.sql
--
-- BEFORE running this the book must be a real one:
--   1. created in AutoCount (Manage Account Book -> create),
--   2. with this plug-in installed and the book OPENED ONCE, so ScpMigrations has run.
--
-- That second step is the whole point of using a real book. It is what creates zSCP2_BookIdentity
-- (the identity the connection test reads back), zSCP2_MeterEntry, and the standard RENTAL / BK / CL
-- meter types. Nothing here creates any of them: if this script fails because they are missing, the
-- honest answer is that the plug-in has not been loaded there yet.
--
-- Contrast with make-book-a.sql, which hand-builds a 9-table stand-in. That one is quick and proves
-- the module reads nothing it has not declared. It does NOT prove the module works against an
-- account book, because it is not one.

-- QUOTED_IDENTIFIER must be ON: this book carries FILTERED indexes (the inter-billing link table
-- has one, and so do the BK/CL uniques on zSCP2_ItemMeter), and SQL Server refuses any INSERT or
-- DELETE against such a table when the session has it off -- which is sqlcmd's default.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

-- Refuse loudly rather than half-seeding a book that is not ready.
IF OBJECT_ID('dbo.zSCP2_BookIdentity') IS NULL
   OR OBJECT_ID('dbo.zSCP2_MeterEntry') IS NULL
   OR OBJECT_ID('dbo.zSCP2_Contract') IS NULL
BEGIN
    RAISERROR('This book has no Service Contract tables. Install the plug-in and open the book once, then run this again.', 16, 1);
    SET NOEXEC ON;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.zSCP_MeterType WHERE MeterTypeCode IN ('RENTAL','BK','CL'))
BEGIN
    RAISERROR('The standard RENTAL / BK / CL meter types are missing. Open the book once with the plug-in installed so its seed runs, then run this again.', 16, 1);
    SET NOEXEC ON;
END
GO

-- The company this parent book bills: the subsidiary. On a real parent contract the customer IS the
-- other company, which is exactly why the subsidiary has to choose its own customer when it takes it.
--
-- A debtor IS a general ledger account in AutoCount -- dbo.Debtor.AccNo has a foreign key to
-- dbo.GLMast -- so the account comes first or the customer cannot exist.
IF NOT EXISTS (SELECT 1 FROM dbo.GLMast WHERE AccNo = N'3000-SUB01')
INSERT INTO dbo.GLMast (AccNo, [Description], AccType, CurrencyCode, Guid)
VALUES (N'3000-SUB01', N'THE SUBSIDIARY SDN BHD', 'CA', 'MYR', NEWID());

IF NOT EXISTS (SELECT 1 FROM dbo.Debtor WHERE AccNo = N'3000-SUB01')
INSERT INTO dbo.Debtor
 (AccNo, CompanyName, Address1, Address2, Attention, Phone1, Fax1, DisplayTerm, CurrencyCode,
  AllowExceedCreditLimit, DiscountPercent, LastModified, LastModifiedUserID, CreatedTimeStamp,
  CreatedUserID, HasBonusPoint, IsGroupCompany, IsActive, LastUpdate, InclusiveTax, RoundingMethod,
  Guid, SGEInvoicePeppolFormat)
VALUES (N'3000-SUB01', N'THE SUBSIDIARY SDN BHD', N'', N'', N'', N'', N'', 'C.O.D.', 'MYR',
        'T', 0, GETDATE(), 'ADMIN', GETDATE(), 'ADMIN', 'F', 'F', 'T', 0, 'F', 0, NEWID(), '');
GO

-- One contract, three identical machines, a rental and two usage counters each. Small on purpose:
-- every number the run checks is written down in Program.cs, so a bigger fixture would only make a
-- failure harder to read.
--
-- Re-runnable: an existing HQ-2026-001 is emptied and rebuilt, so the harness always starts from the
-- same three machines however many times it has added a fourth.
DECLARE @ck BIGINT = (SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'HQ-2026-001');

IF @ck IS NOT NULL
BEGIN
    DELETE e FROM dbo.zSCP2_MeterEntry e
      JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey
      JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
     WHERE i.ContractKey = @ck;
    DELETE m FROM dbo.zSCP2_ItemMeter m
      JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
     WHERE i.ContractKey = @ck;
    DELETE FROM dbo.zSCP2_Item WHERE ContractKey = @ck;
    DELETE FROM dbo.zSCP2_Contract WHERE ContractKey = @ck;
END

INSERT INTO dbo.zSCP2_Contract
 (ContractNo, ContractTypeCode, DebtorCode, [Description], BillingFormatCode, UseNewLayout,
  MachineLineShows, RentalLineMode, MeterLineMode, BillingMode, BillingDay, ContractDate,
  ServiceStartDate, ServiceExpiryDate, RentalSeparateInvoice, Inactive, CreatedBy, ModifiedBy,
  Created, Modified, LastModified)
-- BillingMode 'S' -- one invoice per machine -- and billing day 7, both on purpose. The subsidiary
-- must start from THESE, not from a default the module made up, and a fixture that used the module's
-- old defaults would let that bug back in without the run noticing.
VALUES (N'HQ-2026-001', '', N'3000-SUB01', N'Fleet placed with the subsidiary', '', 'Y', 'L',
        'S', 'S', 'S', 7, GETDATE(), '2026-01-01', '2028-12-31', 'N', 'N', 'ADMIN', 'ADMIN',
        GETDATE(), GETDATE(), GETDATE());
SET @ck = SCOPE_IDENTITY();

DECLARE @i INT = 1;
DECLARE @ik BIGINT;
DECLARE @sn NVARCHAR(60);
WHILE @i <= 3
BEGIN
    SET @sn = N'HQA-' + RIGHT('000' + CAST(@i AS VARCHAR), 3);
    INSERT INTO dbo.zSCP2_Item
     (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos,
      MergeGroupCode, MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem,
      MachineMode, ServiceStartDate, ServiceExpiryDate, LastModified)
    VALUES (@ck, N'HQ-2026-001-' + RIGHT('000' + CAST(@i AS VARCHAR), 3), N'iR-ADV C3560i', @sn,
            N'iR-ADV C3560i / ' + @sn, @i, N'FLEET', N'FLEET', '', '', 'N', 'N', 'ONLINE',
            '2026-01-01', '2028-12-31', GETDATE());
    SET @ik = SCOPE_IDENTITY();

    -- THEIR prices. These are exactly what must NOT cross when the subsidiary takes this contract:
    -- what a copy is worth to the end customer is a different agreement with a different company.
    INSERT INTO dbo.zSCP2_ItemMeter
     (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate,
      MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalMonths, RentalBasis,
      WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope, WaivePartialThreshold,
      WaivePartialAmount, CommitScope, LastModified)
    VALUES
     (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, 380.00, '', 0, 0, 0,                  0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE()),
     (@ik, N'BK',     N'BLACK COPY',     N'BK',     '', 0, 0.0180, '', 0, 0, 100000 + @i * 1000,  0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE()),
     (@ik, N'CL',     N'COLOUR COPY',    N'CL',     '', 0, 0.1800, '', 0, 0,  30000 + @i * 500,   0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE());

    SET @i = @i + 1;
END

-- The readings they took: September 2026, usage counters only. A rental has nothing to read.
INSERT INTO dbo.zSCP2_MeterEntry
 (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified)
SELECT m.ItemMeterKey, 2026, 9,
       m.InitialReading + CASE WHEN m.MeterTypeCode = 'BK' THEN 8400 ELSE 1250 END,
       '2026-09-28', 'ONLINE', 'N', GETDATE()
  FROM dbo.zSCP2_ItemMeter m
  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
 WHERE i.ContractKey = @ck AND m.MeterTypeCode IN ('BK', 'CL');

-- The three machines are ONE line, at one agreed figure. Both halves matter to the take: the
-- grouping says which machines fold together, and this row says what the folded line costs.
-- Bringing the first across without the second gives the subsidiary a fleet line priced off
-- whichever machine the engine happens to read first.
INSERT INTO dbo.zSCP2_ContractRentalPrice
 (ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified)
VALUES (@ck, 'R', N'FLEET', 380.00, 0, 0, '', '', GETDATE());

SELECT 'seeded: contract ' + CAST(@ck AS VARCHAR) +
       ', machines ' + CAST((SELECT COUNT(*) FROM dbo.zSCP2_Item WHERE ContractKey = @ck) AS VARCHAR) +
       ', counters ' + CAST((SELECT COUNT(*) FROM dbo.zSCP2_ItemMeter m
                               JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
                              WHERE i.ContractKey = @ck) AS VARCHAR) +
       ', readings ' + CAST((SELECT COUNT(*) FROM dbo.zSCP2_MeterEntry e
                               JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey
                               JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
                              WHERE i.ContractKey = @ck) AS VARCHAR) +
       ', book id ' + CAST((SELECT TOP 1 BookId FROM dbo.zSCP2_BookIdentity) AS VARCHAR(40)) AS Result;
GO

SET NOEXEC OFF;
GO

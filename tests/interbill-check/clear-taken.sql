-- Empties a BILLING book of everything it took from another one.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d <the billing book> -I -i tests\interbill-check\clear-taken.sql
--
-- Removes the contracts taken through inter-billing, their machines, counters, readings and billing
-- log, and every link. Leaves this book's OWN contracts alone -- the scope is "has a CONTRACT
-- link", so a contract nobody took is never in it.
--
-- What it deliberately does NOT remove:
--
--   The CONNECTION (zSCP2_InterBillBook). It is a setting, not test data: a server, a database, a
--   user, an encrypted password and a margin, all typed by hand. Clearing the contracts so the
--   next take starts clean is routine; making somebody re-type their connection each time is not,
--   and this script deleted it once already. Point the Setup screen at another book to change it.
--
--   Meter types created during a take. They are reference data of this book now, other contracts
--   may already name them, and dropping them would be the one destructive thing in here.
--
-- -I matters: this book carries filtered indexes, and SQL Server refuses a DELETE against such a
-- table when QUOTED_IDENTIFIER is off -- which is sqlcmd's default.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

-- Everything this book took from the other one, and the connection it took it through.
-- Scoped to contracts that ARE inter-billed: a contract of this book's own is never touched.
DECLARE @ck TABLE (ContractKey BIGINT PRIMARY KEY);
INSERT INTO @ck SELECT DISTINCT LocalKey FROM dbo.zSCP2_InterBillLink
 WHERE EntityType = 'CONTRACT' AND LocalKey > 0;

SELECT 'about to remove ' + CAST((SELECT COUNT(*) FROM @ck) AS VARCHAR) + ' contract(s)' AS Step;

DELETE e FROM dbo.zSCP2_MeterEntry e
  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey
  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
 WHERE i.ContractKey IN (SELECT ContractKey FROM @ck);

DELETE t FROM dbo.zSCP_MeterTrans t
  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = t.ServiceItemMeterTypeKey
  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
 WHERE i.ContractKey IN (SELECT ContractKey FROM @ck);

-- The billing log is per COUNTER, so it goes before the counters do.
DELETE l FROM dbo.zSCP2_MeterReadingLog l
  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = l.ItemMeterKey
  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
 WHERE i.ContractKey IN (SELECT ContractKey FROM @ck);

DELETE m FROM dbo.zSCP2_ItemMeter m
  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
 WHERE i.ContractKey IN (SELECT ContractKey FROM @ck);

DELETE c FROM dbo.zSCP2_ItemCode c
  JOIN dbo.zSCP2_Item i ON i.ItemKey = c.ItemKey
 WHERE i.ContractKey IN (SELECT ContractKey FROM @ck);

-- The agreed price of a LINE. It comes across with the grouping now, so it goes back out with
-- it -- left behind, it is a figure filed under a group name in a contract that no longer
-- exists, waiting for the day somebody types that name again.
DELETE FROM dbo.zSCP2_ContractRentalPrice WHERE ContractKey IN (SELECT ContractKey FROM @ck);

DELETE FROM dbo.zSCP2_Item     WHERE ContractKey IN (SELECT ContractKey FROM @ck);
DELETE FROM dbo.zSCP2_Contract WHERE ContractKey IN (SELECT ContractKey FROM @ck);

DELETE FROM dbo.zSCP2_InterBillLink;
-- zSCP2_InterBillBook is NOT touched -- see the note at the top.

-- Billing-log rows whose counter is already gone. zSCP2_MeterReadingLog has no cascade, so
-- every earlier clear left its rows behind and the book slowly filled with the log of
-- counters nobody can name any more.
DELETE FROM dbo.zSCP2_MeterReadingLog
 WHERE ItemMeterKey NOT IN (SELECT ItemMeterKey FROM dbo.zSCP2_ItemMeter);

-- and prices whose contract went before this script learned to take them.
DELETE FROM dbo.zSCP2_ContractRentalPrice
 WHERE ContractKey NOT IN (SELECT ContractKey FROM dbo.zSCP2_Contract);

SELECT 'contracts left    : ' + CAST(COUNT(*) AS VARCHAR) AS Result FROM dbo.zSCP2_Contract
UNION ALL SELECT 'line prices left  : ' + CAST(COUNT(*) AS VARCHAR) FROM dbo.zSCP2_ContractRentalPrice
UNION ALL SELECT 'links left        : ' + CAST(COUNT(*) AS VARCHAR) FROM dbo.zSCP2_InterBillLink
UNION ALL SELECT 'connections KEPT  : ' + CAST(COUNT(*) AS VARCHAR) FROM dbo.zSCP2_InterBillBook;

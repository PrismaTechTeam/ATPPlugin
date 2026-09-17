-- Removes the numbered demo contracts, DEMO-01 to DEMO-99.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\drop-demo-numbered.sql
--
-- Those are the shape contracts -- one per billing arrangement, invented figures -- built by
-- tests\seed-contracts\run.ps1 while the engine was being written. The customer-shaped ones
-- (DEMO-MBJB, DEMO-PG, DEMO-KJR and the rest) are rebuilt from the real invoices in this folder and
-- are NOT touched: the pattern here matches two digits and nothing else.
--
-- It REFUSES to delete a contract that has been billed. A contract whose readings carry an invoice
-- key, or whose meters have a baseline in zSCP_MeterTrans, is the other half of a document somebody
-- can still open; deleting it leaves an invoice pointing at nothing.
--
-- Re-runnable, and reversible: run.ps1 builds them again.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID('tempdb..#go') IS NOT NULL DROP TABLE #go;
SELECT c.ContractKey, c.ContractNo
  INTO #go
  FROM dbo.zSCP2_Contract c
 WHERE c.ContractNo LIKE 'DEMO-[0-9][0-9]';

DECLARE @billed INT =
    (SELECT COUNT(*) FROM dbo.zSCP2_MeterEntry e
       JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey
       JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
       JOIN #go g ON g.ContractKey = i.ContractKey
      WHERE e.InvoicedDocKey IS NOT NULL)
  + (SELECT COUNT(*) FROM dbo.zSCP_MeterTrans t
       JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = t.ServiceItemMeterTypeKey
       JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
       JOIN #go g ON g.ContractKey = i.ContractKey);

IF @billed > 0
BEGIN
    SELECT 'REFUSED: ' + CAST(@billed AS VARCHAR) +
           ' reading(s) here have been billed. Delete the invoices first.' AS Result;
    RETURN;
END

DECLARE @n INT = (SELECT COUNT(*) FROM #go);
DECLARE @m INT = (SELECT COUNT(*) FROM dbo.zSCP2_Item i JOIN #go g ON g.ContractKey = i.ContractKey);

DELETE e FROM dbo.zSCP2_MeterEntry e
  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey
  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
  JOIN #go g ON g.ContractKey = i.ContractKey;
DELETE m FROM dbo.zSCP2_ItemMeter m
  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
  JOIN #go g ON g.ContractKey = i.ContractKey;
DELETE i FROM dbo.zSCP2_Item i JOIN #go g ON g.ContractKey = i.ContractKey;
DELETE p FROM dbo.zSCP2_ContractRentalPrice p JOIN #go g ON g.ContractKey = p.ContractKey;
DELETE c FROM dbo.zSCP2_Contract c JOIN #go g ON g.ContractKey = c.ContractKey;

SELECT 'removed ' + CAST(@n AS VARCHAR) + ' numbered demo contract(s), ' +
       CAST(@m AS VARCHAR) + ' machines. run.ps1 builds them again.' AS Result;
GO

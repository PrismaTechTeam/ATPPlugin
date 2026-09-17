-- The three stock items an invoice line needs: RENTAL, BK, CL.
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d <the book> -i tests\interbill-check\seed-charge-items.sql
--
-- Every meter type points at an AutoCount item code (zSCP_MeterType.ACItemCode), and that is what
-- goes on the invoice line. A book without those three items can hold contracts and readings but
-- cannot produce an invoice, which is a confusing way to fail: the contract looks complete and
-- Generate quietly produces nothing.
--
-- They are copied from AED_ATPTEST rather than hand-written, because dbo.Item carries thirty-odd
-- columns and nine foreign keys, and a hand-written INSERT would be a guess at which of them the
-- installed AutoCount version insists on. Copying takes whatever that version actually stores.
--
-- Prices are NOT copied and there is nothing to copy: these items carry no price. What a copy costs
-- is on the contract, per machine or per printed line -- never on the item.

SET NOCOUNT ON;
GO

IF DB_ID('AED_ATPTEST') IS NULL
BEGIN
    RAISERROR('AED_ATPTEST is not on this server -- nothing to copy the charge items from.', 16, 1);
    SET NOEXEC ON;
END
GO

-- dbo.Item.DocKey is NOT NULL and has no default: AutoCount allocates it in code, not in the
-- database (there is no sequence and no counter table anywhere in the book). For a fixture the
-- honest thing is to take the next number after what this book already has and say so -- these are
-- three master records in a dummy book, not documents anybody will reconcile.
DECLARE @dk BIGINT = ISNULL((SELECT MAX(DocKey) FROM dbo.Item), 0);

INSERT INTO dbo.Item (ItemCode, DocKey, [Description], Desc2, StockControl, HasSerialNo, HasBatchNo,
                      DutyRate, CostingMethod, SalesUOM, PurchaseUOM, ReportUOM, BaseUOM,
                      LastModified, LastModifiedUserID, CreatedTimeStamp, CreatedUserID,
                      IsActive, LastUpdate, HasPromoter, Discontinued, BackOrderControl,
                      Guid, MustGenerateEInvoice)
SELECT s.ItemCode,
       @dk + ROW_NUMBER() OVER (ORDER BY s.ItemCode),
       s.[Description], s.Desc2, s.StockControl, s.HasSerialNo, s.HasBatchNo,
       s.DutyRate, s.CostingMethod, s.SalesUOM, s.PurchaseUOM, s.ReportUOM, s.BaseUOM,
       GETDATE(), 'ADMIN', GETDATE(), 'ADMIN',
       s.IsActive, 0, s.HasPromoter, s.Discontinued, s.BackOrderControl,
       NEWID(), s.MustGenerateEInvoice
  FROM AED_ATPTEST.dbo.Item s
 WHERE s.ItemCode IN ('RENTAL', 'BK', 'CL')
   AND NOT EXISTS (SELECT 1 FROM dbo.Item d WHERE d.ItemCode = s.ItemCode);
GO

-- An item with no unit of measure cannot go on a document line.
INSERT INTO dbo.ItemUOM (ItemCode, UOM, Rate, MinSalePrice, MaxSalePrice, MinPurchasePrice,
                         MaxPurchasePrice, LastUpdate, Guid, AutoCalcPrice, AutoCalcPrice2,
                         AutoCalcPrice3, AutoCalcPrice4, AutoCalcPrice5, AutoCalcPrice6,
                         AutoCalcMinSalePrice, AutoCalcMaxSalePrice)
SELECT s.ItemCode, s.UOM, s.Rate, s.MinSalePrice, s.MaxSalePrice, s.MinPurchasePrice,
       s.MaxPurchasePrice, 0, NEWID(), s.AutoCalcPrice, s.AutoCalcPrice2,
       s.AutoCalcPrice3, s.AutoCalcPrice4, s.AutoCalcPrice5, s.AutoCalcPrice6,
       s.AutoCalcMinSalePrice, s.AutoCalcMaxSalePrice
  FROM AED_ATPTEST.dbo.ItemUOM s
 WHERE s.ItemCode IN ('RENTAL', 'BK', 'CL')
   AND NOT EXISTS (SELECT 1 FROM dbo.ItemUOM d
                    WHERE d.ItemCode = s.ItemCode AND d.UOM = s.UOM);
GO

SELECT 'charge items: ' +
       CAST((SELECT COUNT(*) FROM dbo.Item WHERE ItemCode IN ('RENTAL','BK','CL')) AS VARCHAR) +
       ' items, ' +
       CAST((SELECT COUNT(*) FROM dbo.ItemUOM WHERE ItemCode IN ('RENTAL','BK','CL')) AS VARCHAR) +
       ' units of measure' AS Result;
GO

SET NOEXEC OFF;
GO

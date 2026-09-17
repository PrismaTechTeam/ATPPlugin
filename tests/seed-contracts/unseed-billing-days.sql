-- 把 seed-billing-days.sql 摆的开单日收回去：八张合约回到各自 seed 的原样（Billing due day 1、没有 Rental invoice day）。
-- RentalSeparateInvoice 不动 —— 测试摆盘用的就是原值。机器、读数、价钱一律没碰过。
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\unseed-billing-days.sql
--
-- 可以重跑。

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

UPDATE c
   SET c.BillingDay = 1,
       c.RentalBillingDay = 0,
       c.LastModified = GETDATE()
  FROM dbo.zSCP2_Contract c
 WHERE c.ContractNo IN (N'DEMO-PG', N'DEMO-SA', N'DEMO-MRA', N'DEMO-KJR',
                        N'DEMO-KST', N'DEMO-TGK', N'DEMO-PL', N'DEMO-F19');
PRINT 'restored: ' + CAST(@@ROWCOUNT AS varchar);

SELECT c.ContractNo, c.BillingDay AS MeterDay, c.RentalBillingDay AS RentalDay, c.RentalSeparateInvoice AS Separate
  FROM dbo.zSCP2_Contract c
 WHERE c.ContractNo IN (N'DEMO-PG', N'DEMO-SA', N'DEMO-MRA', N'DEMO-KJR',
                        N'DEMO-KST', N'DEMO-TGK', N'DEMO-PL', N'DEMO-F19')
 ORDER BY c.ContractNo;
GO

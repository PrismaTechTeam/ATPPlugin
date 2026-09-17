-- 开单日的测试摆盘：租金一天、表数另一天。
--
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\seed-billing-days.sql
--
-- 只动合约上的两个栏位（Billing due day / Rental invoice day），机器、读数、价钱一律不碰 ——
-- 那二十张对照合约的金额还是要对得上 PDF。
--
-- 摆成这样是为了把每一种情况都撞一次：
--
--   DEMO-PG    表数 7  租金 1   分单     两天差最远，最好看
--   DEMO-SA    表数 12 租金 1   分单     跟 PG 同一天开租金 —— 1 号那天会看到两张合约
--   DEMO-MRA   表数 10 租金 3   分单
--   DEMO-KJR   表数 20 租金 5   分单
--   DEMO-KST   表数 25 租金 5   分单     跟 KJR 同一天
--   DEMO-TGK   表数 15 租金 2   分单 + 一台机一张单
--   DEMO-PL    表数 18 租金 4   没分单   <- 租金那天要被忽略，两边都算 18
--   DEMO-F19   表数 22 租金 0   分单     <- 0 = 跟表数同一天，两边都算 22
--
-- 最后两张是反例，专门用来证明规则没有乱套：一张没分单（同一张纸不可能两个日子），
-- 一张填 0（等于没设）。
--
-- 可以重跑。

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @d TABLE (ContractNo NVARCHAR(50), MeterDay INT, RentalDay INT, Sep CHAR(1));
INSERT INTO @d VALUES
 (N'DEMO-PG',   7, 1, 'Y'),
 (N'DEMO-SA',  12, 1, 'Y'),
 (N'DEMO-MRA', 10, 3, 'Y'),
 (N'DEMO-KJR', 20, 5, 'Y'),
 (N'DEMO-KST', 25, 5, 'Y'),
 (N'DEMO-TGK', 15, 2, 'Y'),
 (N'DEMO-PL',  18, 4, 'N'),
 (N'DEMO-F19', 22, 0, 'Y');

UPDATE c
   SET c.BillingDay = d.MeterDay,
       c.RentalBillingDay = d.RentalDay,
       c.RentalSeparateInvoice = d.Sep,
       c.LastModified = GETDATE()
  FROM dbo.zSCP2_Contract c
  JOIN @d d ON d.ContractNo = c.ContractNo;

-- 机器自己的日子（BillingDayOverride）会盖过合约的。这里全部清掉，免得一台机偷偷跑去别天，
-- 测的时候找不到它。
UPDATE i SET i.BillingDayOverride = NULL
  FROM dbo.zSCP2_Item i
  JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey
  JOIN @d d ON d.ContractNo = c.ContractNo
 WHERE i.BillingDayOverride IS NOT NULL;

SELECT c.ContractNo,
       c.BillingDay            AS MeterDay,
       c.RentalBillingDay      AS RentalDay,
       c.RentalSeparateInvoice AS Separate,
       (SELECT COUNT(*) FROM dbo.zSCP2_ItemMeter m
          JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
         WHERE i.ContractKey = c.ContractKey
           AND (UPPER(ISNULL(m.MeterRole,'')) IN ('RENTAL','WAIVE'))) AS RentalRows,
       (SELECT COUNT(*) FROM dbo.zSCP2_ItemMeter m
          JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
         WHERE i.ContractKey = c.ContractKey
           AND UPPER(ISNULL(m.MeterRole,'')) IN ('BK','CL')) AS MeterRows
  FROM dbo.zSCP2_Contract c
  JOIN @d d ON d.ContractNo = c.ContractNo
 ORDER BY c.RentalBillingDay, c.BillingDay;
GO

-- DEMO-KST 测试摆盘：有几台机有九月读数、有几台没有（2026-09-11）。
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\kst-remove-some-readings.sql
-- 只删 zSCP2_MeterEntry 的 2026/9 staged 读数、只删还没开单的；上期读数（zSCP_MeterTrans）和 7、8 月一律不碰。
--   整台没读数：KST-005 / 006 / 007（彩色机）、KST-015 / 016（黑白机）、KST-024（没租金那台）
--   只有一半：  KST-002 留 BK，删 CL
-- 放回去：kst-restore-readings.sql。可以重跑。
SET NOCOUNT ON;
DELETE e
  FROM dbo.zSCP2_MeterEntry e
  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey
  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
  JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey
 WHERE c.ContractNo = N'DEMO-KST' AND e.PeriodYear = 2026 AND e.PeriodMonth = 9 AND e.InvoicedDocKey IS NULL
   AND ( i.ServiceItemNo IN (N'DEMO-KST-005', N'DEMO-KST-006', N'DEMO-KST-007', N'DEMO-KST-015', N'DEMO-KST-016', N'DEMO-KST-024')
      OR (i.ServiceItemNo = N'DEMO-KST-002' AND m.MeterRole = N'CL') );
PRINT 'deleted: ' + CAST(@@ROWCOUNT AS varchar);

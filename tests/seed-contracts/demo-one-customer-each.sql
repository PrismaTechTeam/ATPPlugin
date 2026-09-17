-- Demo 摆盘：一家客户一张合约（2026-09-12）。
--   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ATPTEST -I -i tests\seed-contracts\demo-one-customer-each.sql
--
-- 之前七张 DEMO 合约共用 3000-A0074，另外五个不同的 code 都叫 ARENA STABIL，画面上看起来像同一家。
-- 这里做两件事：
--   1. 共用 3000-A0074 的合约分到六个没人用的 debtor（没有合约、没有发票）；DEMO-SA 留在 A0074。
--   2. 每张 DEMO 合约的 debtor 改名成它对照的真实客户（billing-format-check.md 那份）。
-- DEMO-PG 留在 3000-A0005：它九月的租金单 MR2609.0831 已经开在这个 debtor 下面，不搬。
-- 机器、读数、价钱、发票一律没碰。放回去：demo-one-customer-each-restore.sql。可以重跑。
SET NOCOUNT ON;

DECLARE @m TABLE (ContractNo NVARCHAR(50), AccNo NVARCHAR(20), CompanyName NVARCHAR(120));
INSERT INTO @m VALUES
 (N'DEMO-PG',   '3000-A0005', N'HOSPITAL PASIR GUDANG'),
 (N'DEMO-SA',   '3000-A0074', N'HOSPITAL SULTANAH AMINAH'),
 (N'DEMO-3G',   '3000-A0087', N'ARENA STABIL SDN. BHD.'),
 (N'DEMO-DUTY', '3000-A0014', N'DUTY LABEL (DEMO) SDN BHD'),
 (N'DEMO-KKR',  '3000-A0023', N'KOLEJ KOMUNITI ROMPIN'),
 (N'DEMO-MBJB', '3000-A0051', N'MAJLIS BANDARAYA JOHOR BAHRU'),
 (N'DEMO-PL',   '3000-A0071', N'HOSPITAL PERMAI LAMA'),
 (N'DEMO-PL2',  '3000-A0073', N'HOSPITAL PERMAI LAMA (BILLING SETUP NOT DONE)'),
 (N'DEMO-PON',  '3000-A0090', N'HOSPITAL PONTIAN'),
 (N'DEMO-HSI',  '3000-A0179', N'HOSPITAL SULTAN ISMAIL'),
 (N'DEMO-IPG',  '3000-A0199', N'IPG KAMPUS TENGKU AMPUAN AFZAN'),
 (N'DEMO-TGK',  '3000-A0213', N'HOSPITAL TANGKAK'),
 (N'DEMO-F19',  '3000-F0019', N'FASTROCOM (MALAYSIA) SDN BHD'),
 (N'DEMO-IKT',  '3000-I0001', N'IKTBN CHEMBONG'),
 (N'DEMO-KST',  '3000-J0026', N'JABATAN KASTAM DIRAJA MALAYSIA TG KUPANG'),
 (N'DEMO-J56',  '3000-J0056', N'JABATAN PERLINDUNGAN HIDUPAN LIAR'),
 (N'DEMO-KEN',  '3000-K0011', N'KENSINGTON GREEN SPECIALIST CENTRE'),
 (N'DEMO-KJR',  '3000-L0003', N'LEMBAGA KEMAJUAN JOHOR TENGGARA (KEJORA)'),
 (N'DEMO-MRA',  '3000-M0001', N'MAJLIS AMANAH RAKYAT (MARA) JOHOR'),
 (N'DEMO-PPM',  '3000-P0013', N'PUSPEN MUAR'),
 (N'DEMO-SGL',  '3000-S0027', N'SINGLE ADVERTISING & TRADING SDN BHD'),
 (N'DEMO-SYN',  '3000-S0033', N'SYNTURN (M) SDN BHD'),
 (N'DEMO-JPJ',  '3000-S0136', N'JPJ MELAKA');

UPDATE c SET c.DebtorCode = m.AccNo, c.LastModified = GETDATE()
  FROM dbo.zSCP2_Contract c JOIN @m m ON m.ContractNo = c.ContractNo
 WHERE c.DebtorCode <> m.AccNo;
PRINT 'contracts moved: ' + CAST(@@ROWCOUNT AS varchar);

UPDATE d SET d.CompanyName = m.CompanyName
  FROM dbo.Debtor d JOIN @m m ON m.AccNo = d.AccNo
 WHERE d.CompanyName <> m.CompanyName;
PRINT 'debtors renamed: ' + CAST(@@ROWCOUNT AS varchar);

SELECT c.ContractNo, c.DebtorCode, LEFT(d.CompanyName,44) AS Customer,
       demoShares = (SELECT COUNT(*) FROM dbo.zSCP2_Contract c2 WHERE c2.DebtorCode = c.DebtorCode AND c2.ContractNo LIKE 'DEMO-%') - 1
  FROM dbo.zSCP2_Contract c JOIN dbo.Debtor d ON d.AccNo = c.DebtorCode
 WHERE c.ContractNo LIKE 'DEMO-%' ORDER BY d.CompanyName;

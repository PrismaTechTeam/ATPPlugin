SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v18: the first month this book bills a contract.
--
-- A contract is billed one month after another, from its start to its end: a 36-month contract is
-- 36 months, and the next invoice is always the first month that has neither an invoice nor a
-- recorded skip. Nobody picks the month, so nobody can pick the wrong one and leave a month unbilled.
--
-- That only works if the sequence knows where this book's part of it begins. A contract taken over
-- from another book in its ninth month was billed for months one to eight by that book; a contract
-- brought over from an older system was billed there. BillFromPeriod says where to start, as
-- YYYYMM (202609 = September 2026). Months before it count as billed elsewhere.
--
-- NULL means the contract's own start month. Taking a contract from another book writes the month
-- it was taken. It is stored, not worked out from the link's TakenAt, because TakenAt moves every
-- time a change from the other book is accepted -- and the start of billing must not move with it.

IF COL_LENGTH('dbo.zSCP2_Contract', 'BillFromPeriod') IS NULL
    ALTER TABLE dbo.zSCP2_Contract ADD BillFromPeriod INT NULL;
GO

-- Contracts taken before this column existed: the earliest link on the contract is the take (the
-- contract's own link may have been re-stamped by an accepted change; its machines' links often not).
UPDATE c
   SET c.BillFromPeriod = YEAR(t.TakenAt) * 100 + MONTH(t.TakenAt)
  FROM dbo.zSCP2_Contract c
  JOIN (SELECT l.RootContractKey, MIN(l.TakenAt) AS TakenAt
          FROM dbo.zSCP2_InterBillLink l
         WHERE l.LocalKey > 0 AND l.TakenAt IS NOT NULL
         GROUP BY l.RootContractKey) t ON t.RootContractKey = c.ContractKey
 WHERE c.BillFromPeriod IS NULL
   AND EXISTS (SELECT 1 FROM dbo.zSCP2_InterBillLink k
                WHERE k.EntityType = 'CONTRACT' AND k.LocalKey = c.ContractKey);
GO

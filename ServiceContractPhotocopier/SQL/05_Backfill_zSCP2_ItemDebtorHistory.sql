-- Seed the OPENING ownership period for any item that has no history row yet, so the
-- "Debtors Ownership History" tab shows the item's current owner instead of a blank grid.
-- History is otherwise only written when the debtor CHANGES on save, which means every
-- pre-existing item would show nothing. Idempotent: the NOT EXISTS guard makes re-runs a no-op.
SET NOCOUNT ON;

IF OBJECT_ID('dbo.zSCP2_ItemDebtorHistory','U') IS NOT NULL
   AND OBJECT_ID('dbo.zSCP2_Item','U') IS NOT NULL
   AND OBJECT_ID('dbo.zSCP2_Contract','U') IS NOT NULL
BEGIN
    INSERT INTO dbo.zSCP2_ItemDebtorHistory
        (ItemKey, ServiceItemNo, DebtorCode, GradeCode, ContractNo, StartDate, EndDate, Remark, LastModified)
    SELECT
        i.ItemKey,
        ISNULL(i.ServiceItemNo, ''),
        c.DebtorCode,
        ISNULL(i.GradeCode, ''),
        ISNULL(c.ContractNo, ''),
        COALESCE(c.ServiceStartDate, c.ContractDate, CAST(GETDATE() AS DATE)),
        NULL,                       -- still the open (current) period
        'Opening ownership',
        GETDATE()
    FROM dbo.zSCP2_Item i
    INNER JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey
    WHERE ISNULL(c.DebtorCode, '') <> ''
      AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_ItemDebtorHistory h WHERE h.ItemKey = i.ItemKey);
END
GO

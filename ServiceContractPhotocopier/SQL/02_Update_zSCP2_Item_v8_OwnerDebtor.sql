-- Ownership can be a CONTRACT (ContractKey set; owner = that contract's debtor) or a CUSTOMER
-- directly (ContractKey NULL; owner = OwnerDebtorCode). Effective owner everywhere is
-- COALESCE(contract debtor, OwnerDebtorCode). Idempotent.
SET NOCOUNT ON;

IF COL_LENGTH('dbo.zSCP2_Item','OwnerDebtorCode') IS NULL
    ALTER TABLE dbo.zSCP2_Item ADD [OwnerDebtorCode] NVARCHAR(20) NOT NULL CONSTRAINT DF_zSCP2I_OwnerDebtor DEFAULT('');
GO

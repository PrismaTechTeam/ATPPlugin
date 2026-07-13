-- Contract-less service items: a service item may exist WITHOUT a contract (ContractKey NULL) and be
-- attached to one later via the contract editor's "+" (attach) button; "-" detaches it (survives as
-- contract-less). Idempotent. The FK to zSCP2_Contract stays (NULL is exempt from FK checks).
SET NOCOUNT ON;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.zSCP2_Item')
           AND name = 'ContractKey' AND is_nullable = 0)
    ALTER TABLE dbo.zSCP2_Item ALTER COLUMN [ContractKey] BIGINT NULL;
GO

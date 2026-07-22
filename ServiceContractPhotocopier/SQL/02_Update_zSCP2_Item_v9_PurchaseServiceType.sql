-- v9: per-item Purchase Date (migrated from master serviceitem.purchasedate) and a per-item
-- Service Type (its own value list zSCP_LK_ServiceType, seeded from the Contract Type list).
-- Both are item-own fields (not contract-inherited). Idempotent.
SET NOCOUNT ON;

IF COL_LENGTH('dbo.zSCP2_Item','PurchaseDate') IS NULL
    ALTER TABLE dbo.zSCP2_Item ADD [PurchaseDate] DATE NULL;
GO

IF COL_LENGTH('dbo.zSCP2_Item','ServiceTypeCode') IS NULL
    ALTER TABLE dbo.zSCP2_Item ADD [ServiceTypeCode] NVARCHAR(20) NOT NULL CONSTRAINT DF_zSCP2I_ServiceType DEFAULT('');
GO

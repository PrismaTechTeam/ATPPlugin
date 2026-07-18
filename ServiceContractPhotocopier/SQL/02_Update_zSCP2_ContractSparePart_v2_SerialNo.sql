-- Serial No on provided items (zSCP2_ContractSparePart): each provided machine/accessory line can
-- carry the serial it was delivered with, picked from AutoCount's ItemSerialNo. Idempotent.
SET NOCOUNT ON;

IF COL_LENGTH('dbo.zSCP2_ContractSparePart','SerialNumber') IS NULL
    ALTER TABLE dbo.zSCP2_ContractSparePart ADD [SerialNumber] NVARCHAR(100) NOT NULL CONSTRAINT DF_zSCP2CSP_SerialNumber DEFAULT('');
GO

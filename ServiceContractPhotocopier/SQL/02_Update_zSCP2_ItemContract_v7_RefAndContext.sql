-- Reference No on both Contract and Item; plus the contract-context fields on the Service Item so its
-- header matches the contract (minus the billing checkboxes + Contract Value). All idempotent.
SET NOCOUNT ON;

-- Reference No on the contract
IF COL_LENGTH('dbo.zSCP2_Contract','ReferenceNo') IS NULL
    ALTER TABLE dbo.zSCP2_Contract ADD [ReferenceNo] NVARCHAR(80) NOT NULL CONSTRAINT DF_zSCP2C_ReferenceNo DEFAULT('');

-- Item: reference + contract-context fields (ServiceExpiryDate already exists on zSCP2_Item)
IF COL_LENGTH('dbo.zSCP2_Item','ReferenceNo')      IS NULL ALTER TABLE dbo.zSCP2_Item ADD [ReferenceNo]      NVARCHAR(80)  NOT NULL CONSTRAINT DF_zSCP2I_ReferenceNo DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','ContractTypeCode') IS NULL ALTER TABLE dbo.zSCP2_Item ADD [ContractTypeCode] NVARCHAR(20)  NOT NULL CONSTRAINT DF_zSCP2I_CType DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','StaffCode')        IS NULL ALTER TABLE dbo.zSCP2_Item ADD [StaffCode]        NVARCHAR(20)  NOT NULL CONSTRAINT DF_zSCP2I_Staff DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','ServiceStartDate') IS NULL ALTER TABLE dbo.zSCP2_Item ADD [ServiceStartDate] DATE          NULL;
IF COL_LENGTH('dbo.zSCP2_Item','Address1')         IS NULL ALTER TABLE dbo.zSCP2_Item ADD [Address1]         NVARCHAR(300) NOT NULL CONSTRAINT DF_zSCP2I_Address1 DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','Attention')        IS NULL ALTER TABLE dbo.zSCP2_Item ADD [Attention]        NVARCHAR(100) NOT NULL CONSTRAINT DF_zSCP2I_Attention DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','Phone')            IS NULL ALTER TABLE dbo.zSCP2_Item ADD [Phone]            NVARCHAR(40)  NOT NULL CONSTRAINT DF_zSCP2I_Phone DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','TermCode')         IS NULL ALTER TABLE dbo.zSCP2_Item ADD [TermCode]         NVARCHAR(40)  NOT NULL CONSTRAINT DF_zSCP2I_Term DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','AreaCode')         IS NULL ALTER TABLE dbo.zSCP2_Item ADD [AreaCode]         NVARCHAR(40)  NOT NULL CONSTRAINT DF_zSCP2I_Area DEFAULT('');
GO

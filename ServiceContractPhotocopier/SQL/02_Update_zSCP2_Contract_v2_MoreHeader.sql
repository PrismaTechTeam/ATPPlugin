-- More Header tab fields (Image #103): extra contact fields + a Delivery Address block.
-- Idempotent — each column guarded by COL_LENGTH so it is safe on every plugin load.
SET NOCOUNT ON;

IF COL_LENGTH('dbo.zSCP2_Contract','City')            IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [City]            NVARCHAR(60)  NOT NULL CONSTRAINT DF_zSCP2C_City DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','PostalCode')      IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [PostalCode]      NVARCHAR(20)  NOT NULL CONSTRAINT DF_zSCP2C_PostalCode DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','State')           IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [State]           NVARCHAR(60)  NOT NULL CONSTRAINT DF_zSCP2C_State DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','Country')         IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [Country]         NVARCHAR(60)  NOT NULL CONSTRAINT DF_zSCP2C_Country DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','Fax')             IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [Fax]             NVARCHAR(40)  NOT NULL CONSTRAINT DF_zSCP2C_Fax DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','Ref1')            IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [Ref1]            NVARCHAR(80)  NOT NULL CONSTRAINT DF_zSCP2C_Ref1 DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','Ref2')            IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [Ref2]            NVARCHAR(80)  NOT NULL CONSTRAINT DF_zSCP2C_Ref2 DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','Ref3')            IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [Ref3]            NVARCHAR(80)  NOT NULL CONSTRAINT DF_zSCP2C_Ref3 DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','Ref4')            IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [Ref4]            NVARCHAR(80)  NOT NULL CONSTRAINT DF_zSCP2C_Ref4 DEFAULT('');

IF COL_LENGTH('dbo.zSCP2_Contract','DelBranchCode')   IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [DelBranchCode]   NVARCHAR(40)  NOT NULL CONSTRAINT DF_zSCP2C_DelBranchCode DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','DelBranchName')   IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [DelBranchName]   NVARCHAR(100) NOT NULL CONSTRAINT DF_zSCP2C_DelBranchName DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','DelAddress')      IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [DelAddress]      NVARCHAR(300) NOT NULL CONSTRAINT DF_zSCP2C_DelAddress DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','DelCity')         IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [DelCity]         NVARCHAR(60)  NOT NULL CONSTRAINT DF_zSCP2C_DelCity DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','DelPostalCode')   IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [DelPostalCode]   NVARCHAR(20)  NOT NULL CONSTRAINT DF_zSCP2C_DelPostalCode DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','DelState')        IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [DelState]        NVARCHAR(60)  NOT NULL CONSTRAINT DF_zSCP2C_DelState DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','DelCountry')      IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [DelCountry]      NVARCHAR(60)  NOT NULL CONSTRAINT DF_zSCP2C_DelCountry DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','DelPhone')        IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [DelPhone]        NVARCHAR(40)  NOT NULL CONSTRAINT DF_zSCP2C_DelPhone DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','DelFax')          IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [DelFax]          NVARCHAR(40)  NOT NULL CONSTRAINT DF_zSCP2C_DelFax DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','DelEmail')        IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [DelEmail]        NVARCHAR(100) NOT NULL CONSTRAINT DF_zSCP2C_DelEmail DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','DelContactPerson')IS NULL ALTER TABLE dbo.zSCP2_Contract ADD [DelContactPerson] NVARCHAR(100) NOT NULL CONSTRAINT DF_zSCP2C_DelContact DEFAULT('');
GO

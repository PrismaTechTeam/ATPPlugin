-- Service Item overhaul: header Item Code + Grade, More Header block, Note/Remarks, and Preventive
-- Maintenance fields. All idempotent (COL_LENGTH guards). One file for all phases.
SET NOCOUNT ON;

-- Header additions
IF COL_LENGTH('dbo.zSCP2_Item','ItemCode')  IS NULL ALTER TABLE dbo.zSCP2_Item ADD [ItemCode]  NVARCHAR(40) NOT NULL CONSTRAINT DF_zSCP2I_ItemCode  DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','GradeCode') IS NULL ALTER TABLE dbo.zSCP2_Item ADD [GradeCode] NVARCHAR(20) NOT NULL CONSTRAINT DF_zSCP2I_GradeCode DEFAULT('');

-- More Header (mirror the contract's set)
IF COL_LENGTH('dbo.zSCP2_Item','City')            IS NULL ALTER TABLE dbo.zSCP2_Item ADD [City]            NVARCHAR(60)  NOT NULL CONSTRAINT DF_zSCP2I_City DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','PostalCode')      IS NULL ALTER TABLE dbo.zSCP2_Item ADD [PostalCode]      NVARCHAR(20)  NOT NULL CONSTRAINT DF_zSCP2I_PostalCode DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','State')           IS NULL ALTER TABLE dbo.zSCP2_Item ADD [State]           NVARCHAR(60)  NOT NULL CONSTRAINT DF_zSCP2I_State DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','Country')         IS NULL ALTER TABLE dbo.zSCP2_Item ADD [Country]         NVARCHAR(60)  NOT NULL CONSTRAINT DF_zSCP2I_Country DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','Fax')             IS NULL ALTER TABLE dbo.zSCP2_Item ADD [Fax]             NVARCHAR(40)  NOT NULL CONSTRAINT DF_zSCP2I_Fax DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','Ref1')            IS NULL ALTER TABLE dbo.zSCP2_Item ADD [Ref1]            NVARCHAR(80)  NOT NULL CONSTRAINT DF_zSCP2I_Ref1 DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','Ref2')            IS NULL ALTER TABLE dbo.zSCP2_Item ADD [Ref2]            NVARCHAR(80)  NOT NULL CONSTRAINT DF_zSCP2I_Ref2 DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','Ref3')            IS NULL ALTER TABLE dbo.zSCP2_Item ADD [Ref3]            NVARCHAR(80)  NOT NULL CONSTRAINT DF_zSCP2I_Ref3 DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','Ref4')            IS NULL ALTER TABLE dbo.zSCP2_Item ADD [Ref4]            NVARCHAR(80)  NOT NULL CONSTRAINT DF_zSCP2I_Ref4 DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','DelBranchCode')   IS NULL ALTER TABLE dbo.zSCP2_Item ADD [DelBranchCode]   NVARCHAR(40)  NOT NULL CONSTRAINT DF_zSCP2I_DelBranchCode DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','DelBranchName')   IS NULL ALTER TABLE dbo.zSCP2_Item ADD [DelBranchName]   NVARCHAR(100) NOT NULL CONSTRAINT DF_zSCP2I_DelBranchName DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','DelAddress')      IS NULL ALTER TABLE dbo.zSCP2_Item ADD [DelAddress]      NVARCHAR(300) NOT NULL CONSTRAINT DF_zSCP2I_DelAddress DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','DelCity')         IS NULL ALTER TABLE dbo.zSCP2_Item ADD [DelCity]         NVARCHAR(60)  NOT NULL CONSTRAINT DF_zSCP2I_DelCity DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','DelPostalCode')   IS NULL ALTER TABLE dbo.zSCP2_Item ADD [DelPostalCode]   NVARCHAR(20)  NOT NULL CONSTRAINT DF_zSCP2I_DelPostalCode DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','DelState')        IS NULL ALTER TABLE dbo.zSCP2_Item ADD [DelState]        NVARCHAR(60)  NOT NULL CONSTRAINT DF_zSCP2I_DelState DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','DelCountry')      IS NULL ALTER TABLE dbo.zSCP2_Item ADD [DelCountry]      NVARCHAR(60)  NOT NULL CONSTRAINT DF_zSCP2I_DelCountry DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','DelPhone')        IS NULL ALTER TABLE dbo.zSCP2_Item ADD [DelPhone]        NVARCHAR(40)  NOT NULL CONSTRAINT DF_zSCP2I_DelPhone DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','DelFax')          IS NULL ALTER TABLE dbo.zSCP2_Item ADD [DelFax]          NVARCHAR(40)  NOT NULL CONSTRAINT DF_zSCP2I_DelFax DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','DelEmail')        IS NULL ALTER TABLE dbo.zSCP2_Item ADD [DelEmail]        NVARCHAR(100) NOT NULL CONSTRAINT DF_zSCP2I_DelEmail DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','DelContactPerson')IS NULL ALTER TABLE dbo.zSCP2_Item ADD [DelContactPerson] NVARCHAR(100) NOT NULL CONSTRAINT DF_zSCP2I_DelContact DEFAULT('');

-- Note / Remarks
IF COL_LENGTH('dbo.zSCP2_Item','Note')    IS NULL ALTER TABLE dbo.zSCP2_Item ADD [Note]    NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.zSCP2_Item','Remark1') IS NULL ALTER TABLE dbo.zSCP2_Item ADD [Remark1] NVARCHAR(200) NOT NULL CONSTRAINT DF_zSCP2I_Remark1 DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','Remark2') IS NULL ALTER TABLE dbo.zSCP2_Item ADD [Remark2] NVARCHAR(200) NOT NULL CONSTRAINT DF_zSCP2I_Remark2 DEFAULT('');

-- Preventive Maintenance
IF COL_LENGTH('dbo.zSCP2_Item','PMActive')          IS NULL ALTER TABLE dbo.zSCP2_Item ADD [PMActive]          CHAR(1)      NOT NULL CONSTRAINT DF_zSCP2I_PMActive DEFAULT('N');
IF COL_LENGTH('dbo.zSCP2_Item','PMIntervalType')    IS NULL ALTER TABLE dbo.zSCP2_Item ADD [PMIntervalType]    NVARCHAR(20) NOT NULL CONSTRAINT DF_zSCP2I_PMIntervalType DEFAULT('NONE');
IF COL_LENGTH('dbo.zSCP2_Item','PMIntervalValue')   IS NULL ALTER TABLE dbo.zSCP2_Item ADD [PMIntervalValue]   INT          NOT NULL CONSTRAINT DF_zSCP2I_PMIntervalValue DEFAULT(0);
IF COL_LENGTH('dbo.zSCP2_Item','PMStartDate')       IS NULL ALTER TABLE dbo.zSCP2_Item ADD [PMStartDate]       DATE         NULL;
IF COL_LENGTH('dbo.zSCP2_Item','PMLastServiceDate') IS NULL ALTER TABLE dbo.zSCP2_Item ADD [PMLastServiceDate] DATE         NULL;
IF COL_LENGTH('dbo.zSCP2_Item','PMNextServiceDate') IS NULL ALTER TABLE dbo.zSCP2_Item ADD [PMNextServiceDate] DATE         NULL;
IF COL_LENGTH('dbo.zSCP2_Item','PMDept')            IS NULL ALTER TABLE dbo.zSCP2_Item ADD [PMDept]            NVARCHAR(40) NOT NULL CONSTRAINT DF_zSCP2I_PMDept DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','PMJob')             IS NULL ALTER TABLE dbo.zSCP2_Item ADD [PMJob]             NVARCHAR(40) NOT NULL CONSTRAINT DF_zSCP2I_PMJob DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Item','PMLocation')        IS NULL ALTER TABLE dbo.zSCP2_Item ADD [PMLocation]        NVARCHAR(60) NOT NULL CONSTRAINT DF_zSCP2I_PMLocation DEFAULT('');
GO

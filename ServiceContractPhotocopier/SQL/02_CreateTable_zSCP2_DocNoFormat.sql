SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Plugin-owned Document Numbering Format (mirrors AutoCount's Document Numbering Format Maintenance
-- idea, scoped to the plugin's own documents). FormatString uses AutoCount's convention: literal text
-- plus ONE <000...> placeholder whose width = number of zeros, e.g. 'SC-<000000>' -> SC-000123.
-- Consumed by ScpDocNo.Next(); maintained in "Document Numbering Format" under Service & Contract.
CREATE TABLE [dbo].[zSCP2_DocNoFormat](
    [DocType]      NVARCHAR(10)  NOT NULL PRIMARY KEY,
    [Description]  NVARCHAR(60)  NOT NULL CONSTRAINT DF_zSCP2_DocNoFormat_Desc DEFAULT(''),
    [FormatString] NVARCHAR(40)  NOT NULL,
    [NextNumber]   INT           NOT NULL CONSTRAINT DF_zSCP2_DocNoFormat_Next DEFAULT(1),
    [LastModified] DATETIME      NOT NULL CONSTRAINT DF_zSCP2_DocNoFormat_LM DEFAULT(GETDATE())
);
GO

-- Seed: continue from the highest existing number so new documents never collide with legacy data.
INSERT INTO [dbo].[zSCP2_DocNoFormat] (DocType, Description, FormatString, NextNumber)
SELECT 'SC', 'Service Contract', 'SC-<000000>',
       ISNULL((SELECT MAX(TRY_CAST(SUBSTRING(ContractNo,4,20) AS INT))
               FROM [dbo].[zSCP2_Contract] WHERE ContractNo LIKE 'SC-%'), 0) + 1;
GO
INSERT INTO [dbo].[zSCP2_DocNoFormat] (DocType, Description, FormatString, NextNumber)
SELECT 'SI', 'Service Item', 'SI-<000000>',
       ISNULL((SELECT MAX(TRY_CAST(SUBSTRING(ServiceItemNo,4,20) AS INT))
               FROM [dbo].[zSCP2_Item] WHERE ServiceItemNo LIKE 'SI-%'), 0) + 1;
GO

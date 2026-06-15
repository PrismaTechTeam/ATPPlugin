SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Combined Service Contract module v2 — contract header.
-- One customer (DebtorCode) per contract; many machines (zSCP2_Item) under it.
-- BillingDay = default day-of-month (1..31) used by the Meter Reading Integration billing run.
-- BillingMode: 'G' = one grouped invoice for the whole contract; 'S' = separate invoice per item.
CREATE TABLE [dbo].[zSCP2_Contract](
	[ContractKey]       [bigint] IDENTITY(1,1) NOT NULL,
	[ContractNo]        [nvarchar](50)  NOT NULL,
	[ContractTypeCode]  [nvarchar](20)  NOT NULL DEFAULT(''),
	[DebtorCode]        [nvarchar](12)  NOT NULL DEFAULT(''),
	[ContractDate]      [date] NULL,
	[ServiceStartDate]  [date] NULL,
	[ServiceExpiryDate] [date] NULL,
	[ContractValue]     [decimal](20,2) NOT NULL DEFAULT(0),
	[BillingDay]        [tinyint]       NOT NULL DEFAULT(1),
	[BillingMode]       [char](1)       NOT NULL DEFAULT('G'),
	[Address1]          [nvarchar](200) NOT NULL DEFAULT(''),
	[Address2]          [nvarchar](200) NOT NULL DEFAULT(''),
	[Address3]          [nvarchar](200) NOT NULL DEFAULT(''),
	[Address4]          [nvarchar](200) NOT NULL DEFAULT(''),
	[Attention]         [nvarchar](200) NOT NULL DEFAULT(''),
	[Phone]             [nvarchar](30)  NOT NULL DEFAULT(''),
	[TermCode]          [nvarchar](30)  NOT NULL DEFAULT(''),
	[AreaCode]          [nvarchar](12)  NOT NULL DEFAULT(''),
	[StaffCode]         [nvarchar](20)  NOT NULL DEFAULT(''),
	[Description]       [nvarchar](200) NOT NULL DEFAULT(''),
	[Remark1]           [nvarchar](100) NOT NULL DEFAULT(''),
	[Remark2]           [nvarchar](100) NOT NULL DEFAULT(''),
	[Note]              [nvarchar](max) NULL,
	[Inactive]          [char](1)       NOT NULL DEFAULT('N'),
	[Created]           [datetime2](0) NULL,
	[Modified]          [datetime2](0) NULL,
	[CreatedBy]         [nvarchar](20)  NOT NULL DEFAULT(''),
	[ModifiedBy]        [nvarchar](20)  NOT NULL DEFAULT(''),
	[LastModified]      [datetime2](0)  NOT NULL DEFAULT(GETDATE()),
 CONSTRAINT [PK_zSCP2_Contract]       PRIMARY KEY CLUSTERED ([ContractKey] ASC),
 CONSTRAINT [UQ_zSCP2_Contract_No]    UNIQUE NONCLUSTERED ([ContractNo] ASC),
 CONSTRAINT [CK_zSCP2_Contract_Mode]  CHECK ([BillingMode] IN ('G','S')),
 CONSTRAINT [CK_zSCP2_Contract_BDay]  CHECK ([BillingDay] BETWEEN 1 AND 31),
 CONSTRAINT [CK_zSCP2_Contract_Inact] CHECK ([Inactive] IN ('Y','N'))
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
CREATE INDEX [IX_zSCP2_Contract_Debtor]     ON [dbo].[zSCP2_Contract]([DebtorCode])
GO
CREATE INDEX [IX_zSCP2_Contract_BillingDay] ON [dbo].[zSCP2_Contract]([BillingDay])
GO

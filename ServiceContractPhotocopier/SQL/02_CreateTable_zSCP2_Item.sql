SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Combined Service Contract module v2 — a SERVICE ITEM line under a contract (NOT a machine).
-- Identified by ServiceItemNo. Has one SerialNumber (the metered unit, matched to the PUMS meter API)
-- and BK/CL meter config (zSCP2_ItemMeter). The AutoCount item codes it provides are listed in
-- the child table zSCP2_ItemCode. BillingDayOverride NULL => inherit the contract's BillingDay.
CREATE TABLE [dbo].[zSCP2_Item](
	[ItemKey]            [bigint] IDENTITY(1,1) NOT NULL,
	[ContractKey]        [bigint]       NOT NULL,
	[ServiceItemNo]      [nvarchar](50) NOT NULL,
	[SerialNumber]       [nvarchar](60) NOT NULL DEFAULT(''),
	[Description]        [nvarchar](200) NOT NULL DEFAULT(''),
	[BillingDayOverride] [tinyint] NULL,
	[DepartmentCode]     [nvarchar](30) NOT NULL DEFAULT(''),
	[JobCode]            [nvarchar](30) NOT NULL DEFAULT(''),
	[StockLocationCode]  [nvarchar](30) NOT NULL DEFAULT(''),
	[Pos]                [int]          NOT NULL DEFAULT(0),
	[Inactive]           [char](1)      NOT NULL DEFAULT('N'),
	[LastModified]       [datetime2](0) NOT NULL DEFAULT(GETDATE()),
 CONSTRAINT [PK_zSCP2_Item]          PRIMARY KEY CLUSTERED ([ItemKey] ASC),
 CONSTRAINT [UQ_zSCP2_Item_No]       UNIQUE NONCLUSTERED ([ServiceItemNo] ASC),
 CONSTRAINT [FK_zSCP2_Item_Contract] FOREIGN KEY ([ContractKey])
       REFERENCES [dbo].[zSCP2_Contract]([ContractKey]) ON DELETE CASCADE,
 CONSTRAINT [CK_zSCP2_Item_BDay]     CHECK ([BillingDayOverride] IS NULL OR [BillingDayOverride] BETWEEN 1 AND 31),
 CONSTRAINT [CK_zSCP2_Item_Inact]    CHECK ([Inactive] IN ('Y','N'))
) ON [PRIMARY]
GO
CREATE INDEX [IX_zSCP2_Item_Contract] ON [dbo].[zSCP2_Item]([ContractKey])
GO
CREATE INDEX [IX_zSCP2_Item_Serial]   ON [dbo].[zSCP2_Item]([SerialNumber])
GO

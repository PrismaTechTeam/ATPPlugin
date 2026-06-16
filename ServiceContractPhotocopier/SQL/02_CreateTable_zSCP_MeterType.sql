SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[zSCP_MeterType](
	[MeterTypeKey]         [bigint] IDENTITY(1,1) NOT NULL,
	[MeterTypeCode]        [nvarchar](20) NOT NULL,
	[Description]          [nvarchar](200) NOT NULL DEFAULT(''),
	[StockCode]            [nvarchar](30) NOT NULL DEFAULT(''),
	[MinimumCharges]       [decimal](20,2) NOT NULL DEFAULT(0),
	[ChargesRate]          [decimal](20,6) NOT NULL DEFAULT(0),
	[MeterMultiPriceCode]  [nvarchar](20) NOT NULL DEFAULT(''),
	[RebateQtyInPercent]   [decimal](20,2) NOT NULL DEFAULT(0),
	[FOCQty]               [decimal](20,2) NOT NULL DEFAULT(0),
	[ACItemCode]           [nvarchar](30) NOT NULL DEFAULT(''),
	[Inactive]             [char](1) NOT NULL DEFAULT('N'),
	[LastModified]         [datetime2](0) NOT NULL DEFAULT(GETDATE()),
 CONSTRAINT [PK_zSCP_MeterType] PRIMARY KEY CLUSTERED ([MeterTypeKey] ASC),
 CONSTRAINT [UQ_zSCP_MeterType_Code] UNIQUE NONCLUSTERED ([MeterTypeCode] ASC),
 CONSTRAINT [CK_zSCP_MeterType_Inactive] CHECK ([Inactive] IN ('Y','N'))
 -- Note: MeterMultiPriceCode intentionally NOT a hard FK — empty string is allowed as "no multi-price tier",
 -- and app-level validation via ScpValidationHelper handles the lookup check. This matches how MariaDB
 -- v8_atp_main uses the column (soft reference via varchar without FK constraint).
) ON [PRIMARY]
GO
CREATE INDEX [IX_zSCP_MeterType_StockCode] ON [dbo].[zSCP_MeterType]([StockCode])
GO

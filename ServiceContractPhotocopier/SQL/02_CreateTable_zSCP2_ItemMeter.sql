SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Combined Service Contract module v2 — per-machine meter configuration.
-- MeterRole tags which meter is Black ('BK') vs Colour ('CL') so the API's TotalBK/TotalCL
-- map deterministically (never inferred from the meter code text). 'NA' = neither (untagged).
-- Per-machine override of rate/min/FOC/rebate/multi-price; InitialReading seeds the first usage calc.
CREATE TABLE [dbo].[zSCP2_ItemMeter](
	[ItemMeterKey]        [bigint] IDENTITY(1,1) NOT NULL,
	[ItemKey]             [bigint]       NOT NULL,
	[MeterTypeCode]       [nvarchar](20) NOT NULL,
	[MeterRole]           [char](2)      NOT NULL DEFAULT('NA'),
	[MinimumCharges]      [decimal](20,2) NOT NULL DEFAULT(0),
	[ChargesRate]         [decimal](20,6) NOT NULL DEFAULT(0),
	[MeterMultiPriceCode] [nvarchar](20) NOT NULL DEFAULT(''),
	[RebateQtyInPercent]  [decimal](20,2) NOT NULL DEFAULT(0),
	[FOCQty]              [decimal](20,2) NOT NULL DEFAULT(0),
	[InitialReading]      [decimal](20,2) NOT NULL DEFAULT(0),
	[LastModified]        [datetime2](0) NOT NULL DEFAULT(GETDATE()),
 CONSTRAINT [PK_zSCP2_ItemMeter]           PRIMARY KEY CLUSTERED ([ItemMeterKey] ASC),
 CONSTRAINT [UQ_zSCP2_ItemMeter]           UNIQUE NONCLUSTERED ([ItemKey] ASC, [MeterTypeCode] ASC),
 CONSTRAINT [FK_zSCP2_ItemMeter_Item]      FOREIGN KEY ([ItemKey])
       REFERENCES [dbo].[zSCP2_Item]([ItemKey]) ON DELETE CASCADE,
 CONSTRAINT [FK_zSCP2_ItemMeter_MeterType] FOREIGN KEY ([MeterTypeCode])
       REFERENCES [dbo].[zSCP_MeterType]([MeterTypeCode]),
 CONSTRAINT [CK_zSCP2_ItemMeter_Role]      CHECK ([MeterRole] IN ('BK','CL','NA'))
) ON [PRIMARY]
GO
-- At most one BK and one CL per machine (NA rows unlimited).
CREATE UNIQUE INDEX [UX_zSCP2_ItemMeter_BK] ON [dbo].[zSCP2_ItemMeter]([ItemKey]) WHERE [MeterRole] = 'BK'
GO
CREATE UNIQUE INDEX [UX_zSCP2_ItemMeter_CL] ON [dbo].[zSCP2_ItemMeter]([ItemKey]) WHERE [MeterRole] = 'CL'
GO

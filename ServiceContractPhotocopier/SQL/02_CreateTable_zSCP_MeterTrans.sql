SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[zSCP_MeterTrans](
	[MeterTransKey]           [bigint] IDENTITY(1,1) NOT NULL,
	[ServiceItemMeterTypeKey] [bigint] NOT NULL,
	[ServiceItemKey]          [bigint] NOT NULL,
	[MeterTypeCode]           [nvarchar](20) NOT NULL,
	[MeterTransDate]          [datetime2](0) NOT NULL,
	[MeterTransReading]       [decimal](20,2) NOT NULL DEFAULT(0),
	[SalesInvoiceDocKey]      [bigint] NULL,
	[Remark]                  [nvarchar](500) NOT NULL DEFAULT(''),
	[LastModified]            [datetime2](0) NOT NULL DEFAULT(GETDATE()),
 CONSTRAINT [PK_zSCP_MeterTrans] PRIMARY KEY CLUSTERED ([MeterTransKey] ASC),
 CONSTRAINT [FK_zSCP_MeterTrans_zSCP_ServiceItemMeterType] FOREIGN KEY ([ServiceItemMeterTypeKey]) REFERENCES [dbo].[zSCP_ServiceItemMeterType]([ServiceItemMeterTypeKey]) ON DELETE CASCADE
) ON [PRIMARY]
GO
CREATE INDEX [IX_zSCP_MeterTrans_Date] ON [dbo].[zSCP_MeterTrans]([MeterTransDate])
GO
CREATE INDEX [IX_zSCP_MeterTrans_ServiceItem] ON [dbo].[zSCP_MeterTrans]([ServiceItemKey])
GO
CREATE INDEX [IX_zSCP_MeterTrans_SIMT] ON [dbo].[zSCP_MeterTrans]([ServiceItemMeterTypeKey], [MeterTransDate] DESC, [MeterTransKey] DESC) INCLUDE ([MeterTransReading])
GO

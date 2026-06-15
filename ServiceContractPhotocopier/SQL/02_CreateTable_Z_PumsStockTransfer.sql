SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- PUMS integration: raw Stock Transfer payloads received from PUMS (Webhook 2 -> /api/stocktransfer).
-- Mirror of the ATPApi schema INCLUDING the v1.1.0 columns
-- (GeneratedDocNo / CompletedBy / CompletedAt / From/ToLocationOverride), so a fresh install is
-- complete and 02_UpdateTable_Z_PumsStockTransfer_v1.1.0.sql is not needed on new books.
CREATE TABLE [dbo].[Z_PumsStockTransfer](
	[AutoKey]              [bigint] IDENTITY(1,1) NOT NULL,
	[RequestId]            [nvarchar](50)  NOT NULL,
	[DocumentDateTime]     [datetime2](7)  NOT NULL,
	[Technician]           [nvarchar](100) NULL,
	[Part]                 [nvarchar](255) NULL,
	[Qty]                  [decimal](18,4) NULL,
	[TransferType]         [nvarchar](20)  NULL,
	[Unit]                 [nvarchar](20)  NULL,
	[Approval]             [nvarchar](50)  NULL,
	[Status]               [varchar](10)   NOT NULL,
	[ReceivedAt]           [datetime2](7)  NOT NULL DEFAULT(sysutcdatetime()),
	[RawJson]              [nvarchar](max) NULL,
	[GeneratedDocNo]       [nvarchar](50)  NULL,
	[CompletedBy]          [nvarchar](50)  NULL,
	[CompletedAt]          [datetime2](7)  NULL,
	[FromLocationOverride] [nvarchar](50)  NULL,
	[ToLocationOverride]   [nvarchar](50)  NULL,
 CONSTRAINT [PK_Z_PumsStockTransfer] PRIMARY KEY CLUSTERED ([AutoKey] ASC)
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
CREATE INDEX [IX_PumsStockTransfer_Id] ON [dbo].[Z_PumsStockTransfer]([RequestId])
GO

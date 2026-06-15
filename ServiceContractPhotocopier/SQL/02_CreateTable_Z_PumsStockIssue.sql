SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- PUMS integration: raw Stock Issue payloads received from PUMS (Webhook 1 -> /api/stockissue).
-- Mirror of the ATPApi schema INCLUDING the v1.1.0 columns
-- (GeneratedDocNo / CompletedBy / CompletedAt / LocationOverride), so a fresh install is complete
-- and 02_UpdateTable_Z_PumsStockIssue_v1.1.0.sql is not needed on new books.
CREATE TABLE [dbo].[Z_PumsStockIssue](
	[AutoKey]          [bigint] IDENTITY(1,1) NOT NULL,
	[StockIssueId]     [nvarchar](50)  NOT NULL,
	[IssueDateTime]    [datetime2](7)  NOT NULL,
	[StockIssueNo]     [nvarchar](50)  NULL,
	[ReferenceNo]      [nvarchar](50)  NULL,
	[Description]      [nvarchar](255) NULL,
	[Department]       [nvarchar](50)  NULL,
	[Job]              [nvarchar](50)  NULL,
	[Technician]       [nvarchar](100) NULL,
	[Location]         [nvarchar](50)  NULL,
	[ItemCode]         [nvarchar](50)  NULL,
	[Quantity]         [decimal](18,4) NULL,
	[UOM]              [nvarchar](20)  NULL,
	[Status]           [varchar](10)   NOT NULL,
	[ReceivedAt]       [datetime2](7)  NOT NULL DEFAULT(sysutcdatetime()),
	[RawJson]          [nvarchar](max) NULL,
	[GeneratedDocNo]   [nvarchar](50)  NULL,
	[CompletedBy]      [nvarchar](50)  NULL,
	[CompletedAt]      [datetime2](7)  NULL,
	[LocationOverride] [nvarchar](50)  NULL,
 CONSTRAINT [PK_Z_PumsStockIssue] PRIMARY KEY CLUSTERED ([AutoKey] ASC)
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
CREATE INDEX [IX_PumsStockIssue_Id] ON [dbo].[Z_PumsStockIssue]([StockIssueId])
GO

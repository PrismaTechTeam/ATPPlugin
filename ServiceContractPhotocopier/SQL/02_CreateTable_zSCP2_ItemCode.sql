SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Combined Service Contract module v2 — the AutoCount item codes a service item provides
-- (e.g. printer model A). One service item -> many item codes. SerialNumber here is the
-- AutoCount item's serial (informational, per provided item).
CREATE TABLE [dbo].[zSCP2_ItemCode](
	[ItemCodeKey]  [bigint] IDENTITY(1,1) NOT NULL,
	[ItemKey]      [bigint]       NOT NULL,
	[ItemCode]     [nvarchar](40) NOT NULL DEFAULT(''),
	[Description]  [nvarchar](200) NOT NULL DEFAULT(''),
	[Qty]          [decimal](20,2) NOT NULL DEFAULT(1),
	[SerialNumber] [nvarchar](60) NOT NULL DEFAULT(''),
	[Pos]          [int]          NOT NULL DEFAULT(0),
	[LastModified] [datetime2](0) NOT NULL DEFAULT(GETDATE()),
 CONSTRAINT [PK_zSCP2_ItemCode]      PRIMARY KEY CLUSTERED ([ItemCodeKey] ASC),
 CONSTRAINT [FK_zSCP2_ItemCode_Item] FOREIGN KEY ([ItemKey])
       REFERENCES [dbo].[zSCP2_Item]([ItemKey]) ON DELETE CASCADE
) ON [PRIMARY]
GO
CREATE INDEX [IX_zSCP2_ItemCode_Item] ON [dbo].[zSCP2_ItemCode]([ItemKey])
GO

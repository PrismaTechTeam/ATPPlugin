SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- PUMS integration: concurrency guard that serializes Stock Request -> AutoCount document generation.
-- Previously created lazily by PumsTaskLock; now provisioned on plugin load.
CREATE TABLE [dbo].[Z_PumsTaskLock](
	[LockKey]  [nvarchar](50)  NOT NULL,
	[LockedBy] [nvarchar](200) NOT NULL,
	[Machine]  [nvarchar](100) NULL,
	[LockedAt] [datetime2](7)  NOT NULL,
 CONSTRAINT [PK_Z_PumsTaskLock] PRIMARY KEY CLUSTERED ([LockKey] ASC)
) ON [PRIMARY]
GO

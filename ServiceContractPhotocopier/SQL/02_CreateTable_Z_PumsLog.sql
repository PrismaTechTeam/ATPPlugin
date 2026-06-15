SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- PUMS integration: audit log of webhook calls (request/response payloads, results).
-- Mirror of the schema created by the ATPApi webhook service (ATPApi\AutoCount\DbMigration.cs).
CREATE TABLE [dbo].[Z_PumsLog](
	[LogKey]      [bigint] IDENTITY(1,1) NOT NULL,
	[LogType]     [varchar](20)   NOT NULL,
	[Source]      [varchar](50)   NOT NULL,
	[ReferenceId] [nvarchar](100) NULL,
	[Message]     [nvarchar](max) NULL,
	[Payload]     [nvarchar](max) NULL,
	[Response]    [nvarchar](max) NULL,
	[LoggedAt]    [datetime2](7)  NOT NULL DEFAULT(sysutcdatetime()),
	[LoggedBy]    [nvarchar](50)  NULL,
 CONSTRAINT [PK_Z_PumsLog] PRIMARY KEY CLUSTERED ([LogKey] ASC)
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
CREATE INDEX [IX_PumsLog_LoggedAt] ON [dbo].[Z_PumsLog]([LoggedAt] DESC)
GO

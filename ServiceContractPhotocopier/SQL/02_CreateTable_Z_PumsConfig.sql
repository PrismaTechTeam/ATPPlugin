SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- PUMS integration: key/value plugin settings (DEFAULT_FROM_LOCATION, FLAG_CONTROL, etc.)
-- Previously created lazily by PumsConfig.EnsureTable(); now provisioned on plugin load.
CREATE TABLE [dbo].[Z_PumsConfig](
	[ConfigKey]   [nvarchar](50)  NOT NULL,
	[ConfigValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_Z_PumsConfig] PRIMARY KEY CLUSTERED ([ConfigKey] ASC)
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

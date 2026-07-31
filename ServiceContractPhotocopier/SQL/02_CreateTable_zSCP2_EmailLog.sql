SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Bulk Email send history (demo feedback 28/07 #9b). One row per invoice per send: written by
-- the Bulk Email Invoice screen AFTER AutoCount's Batch Mail dialog actually queued the mail
-- (detected via new dbo.Mail rows). Drives the "Emailed" column / "not yet emailed" checklist
-- and the contract Billing History tab's reminder banner. No FK — the log outlives documents.
CREATE TABLE [dbo].[zSCP2_EmailLog](
	[EmailLogKey]  [bigint] IDENTITY(1,1) NOT NULL,
	[DocKey]       [bigint] NOT NULL,
	[DocNo]        [nvarchar](30)  NOT NULL CONSTRAINT [DF_zSCP2_EmailLog_DocNo] DEFAULT(''),
	[DebtorCode]   [nvarchar](20)  NOT NULL CONSTRAINT [DF_zSCP2_EmailLog_Debtor] DEFAULT(''),
	[Email]        [nvarchar](200) NOT NULL CONSTRAINT [DF_zSCP2_EmailLog_Email] DEFAULT(''),
	[SentAt]       [datetime] NOT NULL CONSTRAINT [DF_zSCP2_EmailLog_SentAt] DEFAULT(GETDATE()),
	[SentBy]       [nvarchar](50)  NOT NULL CONSTRAINT [DF_zSCP2_EmailLog_SentBy] DEFAULT(''),
 CONSTRAINT [PK_zSCP2_EmailLog] PRIMARY KEY CLUSTERED ([EmailLogKey] ASC)
) ON [PRIMARY]
GO
CREATE INDEX [IX_zSCP2_EmailLog_DocKey] ON [dbo].[zSCP2_EmailLog]([DocKey])
GO
CREATE INDEX [IX_zSCP2_EmailLog_Debtor] ON [dbo].[zSCP2_EmailLog]([DebtorCode])
GO

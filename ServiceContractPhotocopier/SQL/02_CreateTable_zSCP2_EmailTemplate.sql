SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Bulk Email templates (user request 01/08): named, user-maintained versions instead of one
-- hardcoded wording. Exactly one row is the DEFAULT — the Bulk Email run uses it. Style:
-- 'PLAIN' sends the text as typed; 'STYLED' wraps it in the professional HTML frame at send.
CREATE TABLE [dbo].[zSCP2_EmailTemplate](
	[TemplateKey]  [bigint] IDENTITY(1,1) NOT NULL,
	[Name]         [nvarchar](50)  NOT NULL,
	[Subject]      [nvarchar](200) NOT NULL CONSTRAINT [DF_zSCP2_EmailTemplate_Subject] DEFAULT(''),
	[Body]         [nvarchar](max) NOT NULL CONSTRAINT [DF_zSCP2_EmailTemplate_Body] DEFAULT(''),
	[Style]        [varchar](10)   NOT NULL CONSTRAINT [DF_zSCP2_EmailTemplate_Style] DEFAULT('PLAIN'),
	[IsDefault]    [char](1)       NOT NULL CONSTRAINT [DF_zSCP2_EmailTemplate_IsDefault] DEFAULT('N'),
	[LastModified] [datetime]      NOT NULL CONSTRAINT [DF_zSCP2_EmailTemplate_Modified] DEFAULT(GETDATE()),
 CONSTRAINT [PK_zSCP2_EmailTemplate] PRIMARY KEY CLUSTERED ([TemplateKey] ASC),
 CONSTRAINT [UQ_zSCP2_EmailTemplate_Name] UNIQUE NONCLUSTERED ([Name] ASC)
) ON [PRIMARY]
GO
-- Seed the "Standard" default. If the single-template era stored wording in Z_PumsConfig,
-- migrate it so nobody's edited text is lost; otherwise use the stock wording.
DECLARE @subj NVARCHAR(200) = N'Invoice {DocNos}';
DECLARE @body NVARCHAR(MAX) = N'Dear {CompanyName},' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
	N'Please find attached your invoice(s): {DocNos}.' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) + N'Thank you.';
DECLARE @style VARCHAR(10) = 'PLAIN';
IF OBJECT_ID('dbo.Z_PumsConfig') IS NOT NULL
BEGIN
	SELECT @subj  = ISNULL(NULLIF((SELECT ConfigValue FROM dbo.Z_PumsConfig WHERE ConfigKey = 'BULKMAIL_SUBJECT'), N''), @subj);
	SELECT @body  = ISNULL(NULLIF((SELECT ConfigValue FROM dbo.Z_PumsConfig WHERE ConfigKey = 'BULKMAIL_BODY'), N''), @body);
	SELECT @style = ISNULL(NULLIF((SELECT CAST(ConfigValue AS VARCHAR(10)) FROM dbo.Z_PumsConfig WHERE ConfigKey = 'BULKMAIL_STYLE'), ''), @style);
END
INSERT INTO dbo.zSCP2_EmailTemplate ([Name], [Subject], [Body], [Style], [IsDefault])
VALUES (N'Standard', @subj, @body, @style, 'Y');
GO

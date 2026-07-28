SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Field-level change audit for contracts and their service items / meters. Append-only; NO foreign
-- keys on purpose (the audit outlives its masters — zSCP2_MeterReadingLog precedent). One SAVE =
-- one ChangeSetId so the viewer can group a save's rows. ChangeSource: CONTRACT (header save diff),
-- STRATEGY-APPLY (legacy pushes from the retired Apply-to-Meters step), RENTAL (Rental Maintenance), OWNERSHIP (change owner).
CREATE TABLE [dbo].[zSCP2_ContractAudit](
	[AuditKey]     [bigint] IDENTITY(1,1) NOT NULL,
	[ContractKey]  [bigint]           NOT NULL,
	[ContractNo]   [nvarchar](50)     NOT NULL DEFAULT(''),
	[ItemKey]      [bigint]           NULL,
	[ItemMeterKey] [bigint]           NULL,
	[ChangeSetId]  [uniqueidentifier] NOT NULL,
	[ChangeSource] [nvarchar](20)     NOT NULL DEFAULT(''),
	[FieldName]    [nvarchar](60)     NOT NULL,
	[OldValue]     [nvarchar](max)    NULL,
	[NewValue]     [nvarchar](max)    NULL,
	[ChangedAt]    [datetime]         NOT NULL CONSTRAINT [DF_zSCP2CA_At] DEFAULT(GETDATE()),
	[ChangedBy]    [nvarchar](60)     NOT NULL CONSTRAINT [DF_zSCP2CA_By] DEFAULT(''),
 CONSTRAINT [PK_zSCP2_ContractAudit] PRIMARY KEY CLUSTERED ([AuditKey] ASC)
) ON [PRIMARY]
GO
CREATE INDEX [IX_zSCP2_ContractAudit_Contract] ON [dbo].[zSCP2_ContractAudit]([ContractKey], [ChangedAt])
GO
CREATE INDEX [IX_zSCP2_ContractAudit_Field] ON [dbo].[zSCP2_ContractAudit]([ContractKey], [FieldName], [ChangedAt])
GO

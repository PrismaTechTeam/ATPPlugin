SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Staging of the CURRENT meter reading per meter, per billing period (year+month), before it is
-- invoiced. Holds manual key-ins and accepted API values so they survive restarts and so a later
-- Fetch can detect "this was keyed manually but the API now has a value" conflicts.
--   Source : MANUAL  = typed by the user
--            ONLINE  = accepted from API 1
--            OFFLINE = accepted from API 2
-- One staged reading per (ItemMeterKey, PeriodYear, PeriodMonth). Cleared/repointed independently
-- of zSCP_MeterTrans, which is only written when an invoice is actually generated.
CREATE TABLE [dbo].[zSCP2_MeterEntry](
	[EntryKey]        [bigint] IDENTITY(1,1) NOT NULL,
	[ItemMeterKey]    [bigint] NOT NULL,
	[PeriodYear]      [int] NOT NULL,
	[PeriodMonth]     [int] NOT NULL,
	[CurrentReading]  [decimal](20,2) NOT NULL DEFAULT(0),
	[ReadingDate]     [datetime2](0) NULL,
	[Source]          [char](8) NOT NULL DEFAULT('MANUAL'),
	[Invoiced]        [char](1) NOT NULL DEFAULT('N'),
	[LastModified]    [datetime2](0) NOT NULL DEFAULT(GETDATE()),
 CONSTRAINT [PK_zSCP2_MeterEntry] PRIMARY KEY CLUSTERED ([EntryKey] ASC),
 CONSTRAINT [UQ_zSCP2_MeterEntry_Period] UNIQUE NONCLUSTERED ([ItemMeterKey] ASC, [PeriodYear] ASC, [PeriodMonth] ASC),
 CONSTRAINT [FK_zSCP2_MeterEntry_zSCP2_ItemMeter] FOREIGN KEY ([ItemMeterKey]) REFERENCES [dbo].[zSCP2_ItemMeter]([ItemMeterKey]) ON DELETE CASCADE,
 CONSTRAINT [CK_zSCP2_MeterEntry_Source] CHECK ([Source] IN ('MANUAL','ONLINE','OFFLINE')),
 CONSTRAINT [CK_zSCP2_MeterEntry_Invoiced] CHECK ([Invoiced] IN ('Y','N'))
) ON [PRIMARY]
GO
CREATE INDEX [IX_zSCP2_MeterEntry_Period] ON [dbo].[zSCP2_MeterEntry]([PeriodYear], [PeriodMonth])
GO

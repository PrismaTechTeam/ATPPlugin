-- Per-INVOICE contract snapshot. Written inside the same transaction that stamps the meter
-- readings when an invoice is generated: the contract's strategy rules + billing flags AS THEY
-- WERE at that moment, serialized to text. Editing the strategy later can never rewrite the
-- history of an already-generated invoice — this row proves what the deal was.
-- (Per-meter pricing as billed — rate/FOC/rebate/min/charge — is already immutably recorded in
-- zSCP2_MeterReadingLog; this table adds the RULE set + header flags.)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_ContractSnapshot]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[zSCP2_ContractSnapshot](
        [SnapshotKey]   [bigint] IDENTITY(1,1) NOT NULL,
        [ContractKey]   [bigint]        NOT NULL,
        [ContractNo]    [nvarchar](40)  NOT NULL DEFAULT(''),
        [InvoiceDocKey] [bigint]        NULL,
        [InvoiceDocNo]  [nvarchar](40)  NOT NULL DEFAULT(''),
        [PeriodYear]    [int]           NOT NULL DEFAULT(0),
        [PeriodMonth]   [int]           NOT NULL DEFAULT(0),
        [SnapshotText]  [nvarchar](max) NULL,
        [CreatedAt]     [datetime]      NOT NULL DEFAULT(GETDATE()),
        [CreatedBy]     [nvarchar](60)  NOT NULL DEFAULT(''),
     CONSTRAINT [PK_zSCP2_ContractSnapshot] PRIMARY KEY CLUSTERED ([SnapshotKey] ASC)
    );
    CREATE INDEX [IX_zSCP2_ContractSnapshot_Contract] ON [dbo].[zSCP2_ContractSnapshot]([ContractKey], [PeriodYear], [PeriodMonth]);
    CREATE INDEX [IX_zSCP2_ContractSnapshot_Doc] ON [dbo].[zSCP2_ContractSnapshot]([InvoiceDocKey]);
END

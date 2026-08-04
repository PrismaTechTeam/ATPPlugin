SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v3: Demo 28/07 #10 - Sales CN reading correction linkage.
-- Correction rows keep SalesInvoiceDocKey NULL (invisible to the invoice reconcile) and carry the
-- CN's DocKey + DocNo so the CN reconcile can roll them back when the CN is deleted/cancelled.
-- CNDocNo is denormalized ON PURPOSE: after a hard delete of the CN, dbo.CN no longer holds the
-- DocNo, and the 'CN-DELETED' audit row must still name the document.
-- CorrectedInvoiceDocKey = the invoice the CN corrected: when THAT invoice is later deleted and
-- the month re-billed, the stale override rolls back too (otherwise it would shadow the new bill).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.zSCP_MeterTrans') AND name = 'CNDocKey')
    ALTER TABLE [dbo].[zSCP_MeterTrans] ADD [CNDocKey] [bigint] NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.zSCP_MeterTrans') AND name = 'CNDocNo')
    ALTER TABLE [dbo].[zSCP_MeterTrans] ADD [CNDocNo] [nvarchar](30) NOT NULL CONSTRAINT [DF_zSCP_MeterTrans_CNDocNo] DEFAULT('');
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.zSCP_MeterTrans') AND name = 'CorrectedInvoiceDocKey')
    ALTER TABLE [dbo].[zSCP_MeterTrans] ADD [CorrectedInvoiceDocKey] [bigint] NULL;
GO
-- Plain (non-filtered) index: a filtered index would force strict SET options onto EVERY writer
-- of this table; the early v3 draft created it filtered - swap it out if present.
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_zSCP_MeterTrans_CN' AND object_id = OBJECT_ID('dbo.zSCP_MeterTrans') AND has_filter = 1)
    DROP INDEX [IX_zSCP_MeterTrans_CN] ON [dbo].[zSCP_MeterTrans];
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_zSCP_MeterTrans_CN' AND object_id = OBJECT_ID('dbo.zSCP_MeterTrans'))
    CREATE INDEX [IX_zSCP_MeterTrans_CN] ON [dbo].[zSCP_MeterTrans]([CNDocKey]);
GO

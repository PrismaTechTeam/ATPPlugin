SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Billing-period guard (B-1): once an invoice is generated for a meter + period, the staging row is
-- stamped with the invoice. Generate skips stamped rows, so the same period can never be billed
-- twice by accident. Idempotent: safe on every plugin load and on fresh DBs.
IF COL_LENGTH('dbo.zSCP2_MeterEntry','InvoicedDocKey') IS NULL
    ALTER TABLE [dbo].[zSCP2_MeterEntry] ADD [InvoicedDocKey] BIGINT NULL;
GO
IF COL_LENGTH('dbo.zSCP2_MeterEntry','InvoicedDocNo') IS NULL
    ALTER TABLE [dbo].[zSCP2_MeterEntry] ADD [InvoicedDocNo] NVARCHAR(30) NULL;
GO
IF COL_LENGTH('dbo.zSCP2_MeterEntry','InvoicedAt') IS NULL
    ALTER TABLE [dbo].[zSCP2_MeterEntry] ADD [InvoicedAt] DATETIME NULL;
GO

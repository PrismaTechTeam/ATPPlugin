SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v12: customer feedback 07/08 — the month-end bulk email must carry the "Summary sales invoice
-- meter listing" (Appendix A) alongside the invoice and the SOA. Same shape as the SOA pair:
--   GenerateMeterListing   'Y' = this customer receives the listing each cycle, 'N' (default) = no
--   MeterListingReportName  the AutoCount report design to print it with; empty = the default layout
IF COL_LENGTH('dbo.zSCP2_Contract', 'GenerateMeterListing') IS NULL
    ALTER TABLE dbo.zSCP2_Contract ADD GenerateMeterListing CHAR(1) NOT NULL CONSTRAINT DF_zSCP2_Contract_GenerateMeterListing DEFAULT('N');
GO

IF COL_LENGTH('dbo.zSCP2_Contract', 'MeterListingReportName') IS NULL
    ALTER TABLE dbo.zSCP2_Contract ADD MeterListingReportName NVARCHAR(100) NULL;
GO

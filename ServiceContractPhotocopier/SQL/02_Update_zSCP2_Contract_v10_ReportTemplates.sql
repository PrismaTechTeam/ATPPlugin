SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v10: per-contract report templates (demo feedback 28/07 #9c). A 60-month contract agrees its
-- paperwork ONCE — including which invoice layout the customer receives — so the contract header
-- stores it and Bulk Email picks the right design per invoice automatically:
--   InvoiceReportName = AutoCount "Invoice Document" report design name ('' = the default layout)
--   GenerateSOA       = 'Y' -> this contract's debtor should receive a Statement of Account
--   SOAReportName     = AutoCount "Debtor Statement" report design name ('' = default)
-- Report designs are referenced BY NAME (AutoCount has no separate code) — renaming a design in
-- the Report Designer orphans the reference; the UI flags it and billing falls back to default.
IF COL_LENGTH('dbo.zSCP2_Contract', 'InvoiceReportName') IS NULL
    ALTER TABLE dbo.zSCP2_Contract ADD InvoiceReportName NVARCHAR(100) NOT NULL CONSTRAINT DF_zSCP2_Contract_InvRpt DEFAULT('');
GO
IF COL_LENGTH('dbo.zSCP2_Contract', 'GenerateSOA') IS NULL
    ALTER TABLE dbo.zSCP2_Contract ADD GenerateSOA CHAR(1) NOT NULL CONSTRAINT DF_zSCP2_Contract_GenSOA DEFAULT('N');
GO
IF COL_LENGTH('dbo.zSCP2_Contract', 'SOAReportName') IS NULL
    ALTER TABLE dbo.zSCP2_Contract ADD SOAReportName NVARCHAR(100) NOT NULL CONSTRAINT DF_zSCP2_Contract_SOARpt DEFAULT('');
GO

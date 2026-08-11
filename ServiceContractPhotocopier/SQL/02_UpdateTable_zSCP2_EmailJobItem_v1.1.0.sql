SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v1.1.0: record WHAT was sent, not just that it was. The send history knew the customer, the
-- invoices and the outcome, but not the wording or the report design used — so "what did this
-- customer actually receive?" could not be answered after the fact, and the Send Progress screen
-- had two columns it could only leave blank.
--
-- Both are stored as NAMES, not keys, on purpose: a template renamed or deleted next month must not
-- rewrite what last month's email says it used. This is a record of an event, not a live reference.
IF COL_LENGTH('dbo.zSCP2_EmailJobItem', 'EmailTemplateName') IS NULL
    ALTER TABLE dbo.zSCP2_EmailJobItem ADD EmailTemplateName NVARCHAR(200) NULL;
GO

IF COL_LENGTH('dbo.zSCP2_EmailJobItem', 'InvoiceLayoutName') IS NULL
    ALTER TABLE dbo.zSCP2_EmailJobItem ADD InvoiceLayoutName NVARCHAR(200) NULL;
GO

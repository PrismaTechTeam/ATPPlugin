SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v1.2.0: the send can now carry more than the invoice — a contract can ask for the Statement of
-- Account and/or the Summary sales invoice meter listing (Appendix A) to go out with it. The history
-- has to record WHICH of those a customer actually received, or "we sent your statement" becomes
-- unanswerable a month later.
--
-- Free text ("SOA", "Meter listing", "SOA + Meter listing"), not flags: it is a record of what was
-- attached that day, and it must stay readable even after the settings behind it change.
IF COL_LENGTH('dbo.zSCP2_EmailJobItem', 'ExtraDocs') IS NULL
    ALTER TABLE dbo.zSCP2_EmailJobItem ADD ExtraDocs NVARCHAR(200) NULL;
GO

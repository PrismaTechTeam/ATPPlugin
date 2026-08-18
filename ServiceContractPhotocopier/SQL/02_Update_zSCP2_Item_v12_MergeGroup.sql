SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v12: which machines print as ONE line, when the contract merges lines.
--
-- The three line modes answer the question in general -- merge everything, merge by model, do not
-- merge -- and none of them can say "these two models together, that one apart". Real contracts do:
-- a C5335 and a C5665 on one rental line of 2 UNIT, and four C1234 on a line of their own.
--
-- This is that: a name the user gives a set of machines. Where it is set it REPLACES the mode's own
-- bucket, so under "merge by model" it merges models together, and under "merge, ignoring model"
-- it splits a group off the single line. It does not turn merging on -- a contract set to one line
-- per machine stays one line per machine.
--
-- Three keys on a machine now, three different verbs. Do not confuse them:
--
--   BillGroupCode   which INVOICE this machine lands on            (v10, "Own invoice")
--   LineGroupCode   the WORD its line prints ("HEAVY DUTY")        (v11, a description, never a split)
--   MergeGroupCode  which LINE it prints on                        (here)
--
-- '' = inert: the machine follows the contract's line mode exactly as before. An existing book is
-- unaffected until someone groups something.

IF COL_LENGTH('dbo.zSCP2_Item', 'MergeGroupCode') IS NULL
    ALTER TABLE dbo.zSCP2_Item ADD MergeGroupCode NVARCHAR(40) NOT NULL
        CONSTRAINT DF_zSCP2_Item_MergeGroupCode DEFAULT('');
GO

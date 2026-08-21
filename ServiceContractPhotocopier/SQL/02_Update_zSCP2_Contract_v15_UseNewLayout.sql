SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v15: the new rules get a switch of their own, instead of riding on a name.
--
-- Until now "does this contract carry a BillingFormatCode" decided two unrelated things: how lines
-- merge, AND which arithmetic the money uses (rebate deducted as copies, cents rounded away from
-- zero, a 0.00 line still printed). That is why ticking a box on the contract screen -- which
-- cleared the code -- silently sent a contract back to the old money. A label was steering the
-- arithmetic.
--
-- UseNewLayout says it directly. Backfilled 'Y' for every contract that has a format today, so
-- nothing changes for them, and 'N' everywhere else, so the 3,080 imported contracts keep billing
-- exactly as they do now until somebody moves them over one at a time. That per-contract move is
-- also the parallel run: switch one customer, compare against what Master Accounting issued.

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_Contract]') AND name = 'UseNewLayout')
BEGIN
    ALTER TABLE [dbo].[zSCP2_Contract]
        ADD [UseNewLayout] CHAR(1) NOT NULL
            CONSTRAINT [DF_zSCP2_Contract_UseNewLayout] DEFAULT 'N';

    EXEC(N'UPDATE [dbo].[zSCP2_Contract] SET [UseNewLayout] = ''Y''
           WHERE ISNULL([BillingFormatCode], '''') <> ''''');
END
GO

-- The machine line's wording used to live on the format row. A contract that no longer names a
-- format still has to print something, so it carries its own answer: 'B' model + label, 'M' model
-- only, 'L' label only. Backfilled from whatever format the contract was on.
IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_Contract]') AND name = 'MachineLineShows')
BEGIN
    ALTER TABLE [dbo].[zSCP2_Contract]
        ADD [MachineLineShows] CHAR(1) NOT NULL
            CONSTRAINT [DF_zSCP2_Contract_MachineLineShows] DEFAULT 'B';

    EXEC(N'UPDATE c SET c.[MachineLineShows] = ISNULL(f.[MachineLineShows], ''B'')
             FROM [dbo].[zSCP2_Contract] c
             JOIN [dbo].[zSCP2_BillingFormat] f ON f.FormatCode = c.BillingFormatCode');
END
GO

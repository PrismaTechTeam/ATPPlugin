SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v2: the margin this book adds to the other book's prices.
--
-- The first version of taking a contract brought no price across at all, on the reasoning that what a
-- copy is worth to the end customer is a different agreement with a different company. That reasoning
-- is right and the conclusion was wrong: a fifty-machine contract then needed fifty prices retyped
-- before it could bill anything, and nobody was going to do that.
--
-- A margin settles it without the system inventing a price. The parent charges 1.30; this company
-- works on 30%; the price is 1.69. That figure is not guessed -- it is what somebody decided when
-- they typed 30, applied to a number the other company decided. Everything stays editable
-- afterwards: these are ordinary prices on an ordinary contract from the moment they land.
--
-- Held on the connection because it is a standing arrangement between the two companies, not a
-- property of one contract. The taking screen offers it as the starting figure and lets it be
-- changed for the contract in hand.
--
-- Zero is meaningful and is the default: bill at exactly what they charge.

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_InterBillBook]')
                  AND name = 'MarginPercent')
BEGIN
    ALTER TABLE [dbo].[zSCP2_InterBillBook]
        ADD [MarginPercent] DECIMAL(9,4) NOT NULL
            CONSTRAINT [DF_zSCP2_IBB_Margin] DEFAULT (0);
END
GO

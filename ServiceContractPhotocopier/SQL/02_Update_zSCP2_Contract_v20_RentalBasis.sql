SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v20: when a contract bills its rental (feedback ATP-10).
--
--   RentalBasis = 'A'  ACCRUAL -- each bill carries that month's copies and that month's rental.
--                      The rule every contract had before; the default.
--   RentalBasis = 'P'  PREPAYMENT -- the rental is collected a month ahead: the month before the
--                      contract starts bills the first month's rental alone, every bill after it
--                      carries that month's copies and NEXT month's rental, and the last month
--                      bills copies only. A contract from 1 Oct 2026, 36 x 300.00: 30 Sep bills
--                      "(1/36) OCT 2026" 300.00; 30 Oct bills October's copies + "(2/36) NOV 2026".
--
-- Per contract; every existing contract is 'A' and bills exactly as before. The machines' own
-- zSCP2_ItemMeter.RentalBasis (v4; only the retired Rental Maintenance / Rental Assign screens,
-- both off the menu, ever wrote 'P', and no book seen has one) is NOT promoted: one machine would
-- flip a whole contract and move its other machines' rent a month. The contract screen keeps the
-- machines in step with the contract from now on; the contract decides.
--
-- Idempotent: guarded on the column; the constraint only when missing.

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_Contract]') AND name = 'RentalBasis')
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    ALTER TABLE [dbo].[zSCP2_Contract]
        ADD [RentalBasis] CHAR(1) NOT NULL
            CONSTRAINT [DF_zSCP2_Contract_RentalBasis] DEFAULT ('A');

    COMMIT TRANSACTION;
    SET XACT_ABORT OFF;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                WHERE name = 'CK_zSCP2_Contract_RentalBasis'
                  AND parent_object_id = OBJECT_ID(N'[dbo].[zSCP2_Contract]'))
BEGIN
    ALTER TABLE [dbo].[zSCP2_Contract] WITH CHECK
        ADD CONSTRAINT [CK_zSCP2_Contract_RentalBasis] CHECK ([RentalBasis] IN ('A','P'));
END
GO

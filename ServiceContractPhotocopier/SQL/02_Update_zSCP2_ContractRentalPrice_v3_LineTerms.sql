SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v3: a line carries more than a rental price.
--
-- The table held one figure per group -- the rental. But a printed line is one Qty x Unit Price for
-- whatever it charges, so a merged black line needs an agreed rate the same way a merged rental
-- does, and for the same reason: machines on different rates cannot share a row, because the row
-- has one price cell and there is no honest way to put two numbers in it.
--
-- The minimum and the waive are terms on the line rather than prices: bill at least X, and forgive
-- Y of the rental once the line's black and colour reach Z. They live here because they are agreed
-- for the line, not carried by any one machine on it.
--
-- Side ('R' rental, 'M' copies) joins the key because the two sides group independently now -- a
-- name can mean one set of machines on the rental and another on the copies.

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_ContractRentalPrice]') AND name = 'Side')
BEGIN
    ALTER TABLE [dbo].[zSCP2_ContractRentalPrice] DROP CONSTRAINT [PK_zSCP2_ContractRentalPrice];

    ALTER TABLE [dbo].[zSCP2_ContractRentalPrice]
        ADD [Side] CHAR(1) NOT NULL CONSTRAINT [DF_zSCP2_ContractRentalPrice_Side] DEFAULT 'R';

    ALTER TABLE [dbo].[zSCP2_ContractRentalPrice]
        ADD CONSTRAINT [PK_zSCP2_ContractRentalPrice] PRIMARY KEY CLUSTERED ([ContractKey], [Side], [GroupCode]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_ContractRentalPrice]') AND name = 'BkPrice')
BEGIN
    ALTER TABLE [dbo].[zSCP2_ContractRentalPrice] ADD
        [BkPrice]   DECIMAL(18,6) NOT NULL CONSTRAINT [DF_zSCP2_CRP_BkPrice]   DEFAULT 0,
        [ClPrice]   DECIMAL(18,6) NOT NULL CONSTRAINT [DF_zSCP2_CRP_ClPrice]   DEFAULT 0,
        [MinCharge] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_zSCP2_CRP_MinCharge] DEFAULT 0,
        [LadderBk]  NVARCHAR(20)  NOT NULL CONSTRAINT [DF_zSCP2_CRP_LadderBk]  DEFAULT '',
        [LadderCl]  NVARCHAR(20)  NOT NULL CONSTRAINT [DF_zSCP2_CRP_LadderCl]  DEFAULT '',
        [WaiveAt]   DECIMAL(18,2) NOT NULL CONSTRAINT [DF_zSCP2_CRP_WaiveAt]   DEFAULT 0,
        [WaiveAmt]  DECIMAL(18,2) NOT NULL CONSTRAINT [DF_zSCP2_CRP_WaiveAmt]  DEFAULT 0;
END
GO

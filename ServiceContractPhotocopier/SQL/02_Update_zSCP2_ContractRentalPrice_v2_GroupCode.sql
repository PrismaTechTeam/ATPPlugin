SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v2: the price is keyed by the LINE, not by the model.
--
-- v1 keyed on ModelCode because "merge by model" was the only way a rental line could hold several
-- machines. Machines can now be grouped by hand (zSCP2_Item.MergeGroupCode), so a line is not always
-- a model -- it can be "C5335 + C5665" under a name the user chose. GroupCode holds whichever it is:
--
--   ''            every machine on the contract (the merge ignores model, nothing grouped by hand)
--   <model>       one model, under "merge by model"
--   #<name>       a hand-made group; the # keeps a group name from colliding with a model code
--
-- Rename, not drop: a book that already priced its groups by model keeps those prices, and they
-- still answer for the same lines.

IF COL_LENGTH('dbo.zSCP2_ContractRentalPrice', 'ModelCode') IS NOT NULL
   AND COL_LENGTH('dbo.zSCP2_ContractRentalPrice', 'GroupCode') IS NULL
BEGIN
    EXEC sp_rename 'dbo.zSCP2_ContractRentalPrice.ModelCode', 'GroupCode', 'COLUMN';
END
GO

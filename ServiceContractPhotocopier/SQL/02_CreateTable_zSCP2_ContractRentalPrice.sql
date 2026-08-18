-- The rental price of a GROUP, when a contract merges its rental lines.
--
-- "Merge by model" prints one line per model -- "3 UNIT ... MONTHLY RENTAL (1/36)" -- and that
-- line has ONE price. Where that price lives decides who owns it. Left on the machines, the
-- printed rate is only whatever the members happen to agree on, and a machine added next month
-- arrives at its own number. Put here, the group owns it: every rental meter in the group bills
-- at this price, and a new machine joins at the group's price without anyone retyping it.
--
-- GroupCode is the line the price belongs to:
--   ''            every machine on the contract (the merge ignores model, nothing grouped by hand)
--   <model>       one model, under 'merge by model'
--   #<name>       a hand-made group of machines (zSCP2_Item.MergeGroupCode); the # keeps a group
--                 name from colliding with a model code
-- 'no merge' has no groups at all -- each machine's own meter is its price, as before.
--
-- No row = no override. A contract that has never opened the dialog bills exactly as it did.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'zSCP2_ContractRentalPrice')
BEGIN
    CREATE TABLE [dbo].[zSCP2_ContractRentalPrice](
        [ContractKey]  bigint        NOT NULL,
        [GroupCode]    nvarchar(60)  NOT NULL,          -- '' = every machine on the contract
        [UnitPrice]    decimal(18,6) NOT NULL CONSTRAINT DF_zSCP2_CRP_UnitPrice DEFAULT(0),
        [LastModified] datetime2     NOT NULL CONSTRAINT DF_zSCP2_CRP_LastModified DEFAULT(GETDATE()),
        CONSTRAINT [PK_zSCP2_ContractRentalPrice] PRIMARY KEY CLUSTERED ([ContractKey], [GroupCode])
    );
END

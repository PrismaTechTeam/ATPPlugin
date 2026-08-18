SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v8: WHOSE print charges a committed minimum is measured against.
--
-- The meter row already carries WaiveScope, which says WHICH charges count -- black only, colour
-- only, or both. This says whose. They are different questions and both are needed: "RM 3,000 a
-- month across these six machines, colour only" needs an answer to each.
--
--   'S'  this machine                     -- what the engine has always done, so the default changes nothing
--   'G'  this machine's merge group       -- zSCP2_Item.MergeGroupCode (v12)
--   'C'  the whole contract
--
-- A pooled minimum is expressed by putting ONE committed meter on ONE machine of the group and
-- setting 'G'. The other machines carry none. That prints one line, topping up against the group's
-- charges -- which is the honest shape. Six meters at RM 3,000 would bill six top-ups.
--
-- Because of that, two group-scope minimums on one group would both top up against the same pool and
-- bill it twice. The contract screen refuses to save that; there is no DB constraint because the
-- group a machine belongs to lives on a different table.

IF COL_LENGTH('dbo.zSCP2_ItemMeter', 'CommitScope') IS NULL
    ALTER TABLE dbo.zSCP2_ItemMeter ADD CommitScope CHAR(1) NOT NULL
        CONSTRAINT DF_zSCP2_ItemMeter_CommitScope DEFAULT('S');
GO

-- v6: per-contract FOC/Rebate RESET period (super-flexible allowance refresh).
-- The FOC free-copy allowance (meter FOCQty, and the multi-price ladder's 0.00 first band) refreshes every
-- reset period. Default = Monthly (unchanged behaviour). Finer periods ACCRUE within a billing run:
--   effective FOC = base FOC x (number of reset periods in the billed span).
--   e.g. FOC 1000, reset Weekly, billed for a ~30-day month -> ~4 weeks -> 4000 free that month.
--   FOCResetUnit: 'M' = monthly (default, = 1 per monthly bill), 'W' = weekly, 'D' = every FOCResetN days.
--   FOCResetN: the N for 'D' (ignored for M/W). Idempotent.
IF COL_LENGTH('dbo.zSCP2_Contract', 'FOCResetUnit') IS NULL
    ALTER TABLE [dbo].[zSCP2_Contract] ADD [FOCResetUnit] char(1) NOT NULL
        CONSTRAINT DF_zSCP2_Contract_FOCResetUnit DEFAULT('M');
GO
IF COL_LENGTH('dbo.zSCP2_Contract', 'FOCResetN') IS NULL
    ALTER TABLE [dbo].[zSCP2_Contract] ADD [FOCResetN] int NOT NULL
        CONSTRAINT DF_zSCP2_Contract_FOCResetN DEFAULT(0);
GO

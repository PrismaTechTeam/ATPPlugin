SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v4: a group's ladder can be the group's OWN bands, not only a code.
--
-- v3 added LadderBk / LadderCl as NVARCHAR(20) -- room for a code from the master price list and
-- nothing else. That was enough while a group could only point at a ladder somebody had already
-- built. It is not enough now: a group agrees its own bands the way a single meter does, and those
-- travel as text -- "5000|0.030;10000|0.025;100000000|0.020" is 37 characters and a real deal.
--
-- Twenty was already tight for a code, too. 'BK +P - 0.028 FOC300' is exactly twenty.

IF EXISTS (SELECT 1 FROM sys.columns
            WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_ContractRentalPrice]')
              AND name = 'LadderBk' AND max_length < 800)
BEGIN
    ALTER TABLE [dbo].[zSCP2_ContractRentalPrice] ALTER COLUMN [LadderBk] NVARCHAR(400) NOT NULL;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns
            WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_ContractRentalPrice]')
              AND name = 'LadderCl' AND max_length < 800)
BEGIN
    ALTER TABLE [dbo].[zSCP2_ContractRentalPrice] ALTER COLUMN [LadderCl] NVARCHAR(400) NOT NULL;
END
GO

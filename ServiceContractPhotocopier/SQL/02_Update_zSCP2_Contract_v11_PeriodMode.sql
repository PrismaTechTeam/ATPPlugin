SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v11: Demo 28/07 #16 "billing period follows contract date" — 'Y' = invoice DISPLAY dates follow
-- the contract cycle (start day .. start day + 1 month - 1 day), 'N' (default) = actual reading
-- dates as before. Display-only: meter stamps always keep the real audit dates.
IF COL_LENGTH('dbo.zSCP2_Contract', 'PeriodFollowContract') IS NULL
    ALTER TABLE dbo.zSCP2_Contract ADD PeriodFollowContract CHAR(1) NOT NULL CONSTRAINT DF_zSCP2_Contract_PeriodFollowContract DEFAULT('N');
GO

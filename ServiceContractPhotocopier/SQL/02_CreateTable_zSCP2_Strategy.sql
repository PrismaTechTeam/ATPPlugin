SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Strategy Maintenance master: one row per marketing/billing strategy, attachable to a contract by
-- soft code (zSCP2_Contract.StrategyCode, no FK — MeterMultiPriceCode precedent). One header table;
-- every kind's parameters are scalars, so no line table.
-- StrategyType kinds (from the customer's Marketing Strategy workbook):
--   WAIVE-TARGET  : waive rental when the month's meter charges reach TargetAmount
--                   (PartialPct > 0 = reaching PartialPct% of target waives PartialPct% of rental)
--   RENTAL-FREE-N : first FreeMonths rental periods free (applied to rental meters' FOCQty countdown)
--   COMMIT-MIN    : committed minimum print charges (CommitAmount enforced via MinimumCharges floor)
--   FOC-REBATE    : FocCopies free copies + RebatePct rebate; NetBilling='Y' bills
--                   NET = (usage - FOC) x (1 - rebate%), 'N' keeps legacy raw-usage billing
--   INITIAL-METER : initial/estimate meter policy (registered; estimates keyed via manual entry)
--   LIMIT         : FOC cap — LimitScope 'S' per machine / 'G' pooled group (master .C convention)
CREATE TABLE [dbo].[zSCP2_Strategy](
	[StrategyKey]    [bigint] IDENTITY(1,1) NOT NULL,
	[StrategyCode]   [nvarchar](20)  NOT NULL,
	[Description]    [nvarchar](200) NOT NULL DEFAULT(''),
	[StrategyType]   [nvarchar](20)  NOT NULL DEFAULT(''),
	[TargetAmount]   [decimal](20,2) NOT NULL DEFAULT(0),
	[PartialPct]     [decimal](20,2) NOT NULL DEFAULT(0),
	[FreeMonths]     [int]           NOT NULL DEFAULT(0),
	[CommitAmount]   [decimal](20,2) NOT NULL DEFAULT(0),
	[FocCopies]      [decimal](20,2) NOT NULL DEFAULT(0),
	[RebatePct]      [decimal](20,2) NOT NULL DEFAULT(0),
	[NetBilling]     [char](1)       NOT NULL DEFAULT('N'),
	[LimitScope]     [char](1)       NOT NULL DEFAULT('S'),
	[LimitQty]       [decimal](20,2) NOT NULL DEFAULT(0),
	[Remark]         [nvarchar](200) NOT NULL DEFAULT(''),
	[Inactive]       [char](1)       NOT NULL DEFAULT('N'),
	[Created]        [datetime2](0)  NULL,
	[CreatedBy]      [nvarchar](60)  NOT NULL DEFAULT(''),
	[Modified]       [datetime2](0)  NULL,
	[ModifiedBy]     [nvarchar](60)  NOT NULL DEFAULT(''),
	[LastModified]   [datetime2](0)  NOT NULL DEFAULT(GETDATE()),
 CONSTRAINT [PK_zSCP2_Strategy] PRIMARY KEY CLUSTERED ([StrategyKey] ASC),
 CONSTRAINT [UQ_zSCP2_Strategy_Code] UNIQUE NONCLUSTERED ([StrategyCode] ASC),
 CONSTRAINT [CK_zSCP2_Strategy_Type] CHECK ([StrategyType] IN ('','WAIVE-TARGET','RENTAL-FREE-N','COMMIT-MIN','FOC-REBATE','INITIAL-METER','LIMIT')),
 CONSTRAINT [CK_zSCP2_Strategy_Net] CHECK ([NetBilling] IN ('Y','N')),
 CONSTRAINT [CK_zSCP2_Strategy_Scope] CHECK ([LimitScope] IN ('S','G')),
 CONSTRAINT [CK_zSCP2_Strategy_Inactive] CHECK ([Inactive] IN ('Y','N'))
) ON [PRIMARY]
GO

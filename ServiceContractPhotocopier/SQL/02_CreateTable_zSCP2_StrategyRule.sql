SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Strategy RULE lines (the composable "builder"): a strategy is a header (zSCP2_Strategy) plus any
-- number of rule lines here. Each line is ONE rule primitive (RuleKind) applied to a Scope of meters,
-- with its own parameters. A single strategy can mix different kinds AND repeat a kind (e.g. FOC+Rebate
-- once for BK, once for CL). The billing pipeline iterates these lines LIVE at Generate in
-- Seq order. The header's legacy StrategyType/param columns are now vestigial (kept for compatibility;
-- new saves write StrategyType='' and carry everything here).
--
-- RuleKind primitives (the engine's vocabulary — a strategy is freely composed from them):
--   WAIVE-TARGET  : waive rental when the period's meter charges reach TargetAmount
--                   (PartialPct > 0 = reaching PartialPct% of target waives PartialPct% of rental)
--   RENTAL-FREE-N : first FreeMonths rental periods free (rental meters' FOCQty countdown)
--   COMMIT-MIN    : committed minimum print charge (CommitAmount as the MinimumCharges floor)
--   FOC-REBATE    : FocCopies free copies + RebatePct rebate; Scope BK/CL targets the usage role;
--                   NetBilling='Y' bills NET=(usage-FOC)x(1-rebate%), 'N' keeps raw-usage billing
--   INITIAL-METER : initial/estimate meter policy (registered; estimates keyed via manual entry)
--   LIMIT         : FOC cap — LimitScope 'S' per machine / 'G' pooled group (master .C convention)
-- Scope: '' = all meters / 'BK' / 'CL' = usage role / 'RENTAL' = flat/rental meters.
CREATE TABLE [dbo].[zSCP2_StrategyRule](
	[RuleKey]        [bigint] IDENTITY(1,1) NOT NULL,
	[StrategyKey]    [bigint]        NOT NULL,
	[Seq]            [int]           NOT NULL DEFAULT(0),
	[RuleKind]       [nvarchar](20)  NOT NULL DEFAULT(''),
	[Scope]          [nvarchar](10)  NOT NULL DEFAULT(''),
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
	[LastModified]   [datetime2](0)  NOT NULL DEFAULT(GETDATE()),
 CONSTRAINT [PK_zSCP2_StrategyRule] PRIMARY KEY CLUSTERED ([RuleKey] ASC),
 CONSTRAINT [FK_zSCP2_StrategyRule_Strategy] FOREIGN KEY ([StrategyKey])
     REFERENCES [dbo].[zSCP2_Strategy]([StrategyKey]) ON DELETE CASCADE,
 CONSTRAINT [CK_zSCP2_StrategyRule_Kind] CHECK ([RuleKind] IN
     ('','WAIVE-TARGET','RENTAL-FREE-N','COMMIT-MIN','FOC-REBATE','INITIAL-METER','LIMIT')),
 CONSTRAINT [CK_zSCP2_StrategyRule_Net] CHECK ([NetBilling] IN ('Y','N')),
 CONSTRAINT [CK_zSCP2_StrategyRule_Scope2] CHECK ([LimitScope] IN ('S','G'))
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_zSCP2_StrategyRule_Strategy]
    ON [dbo].[zSCP2_StrategyRule]([StrategyKey] ASC, [Seq] ASC);
GO

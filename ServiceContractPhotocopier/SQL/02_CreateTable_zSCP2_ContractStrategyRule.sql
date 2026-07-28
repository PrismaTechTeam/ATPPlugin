SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- The contract's OWN copy of strategy rules ("template -> instance"): picking a Strategy code on a
-- contract COPIES that master template's rule lines (zSCP2_StrategyRule) into here, keyed by ContractKey.
-- Afterwards the contract edits ITS rules freely (add/delete/reorder/params) WITHOUT touching the master
-- template. The Meter Reading billing pipeline reads THESE rows LIVE at Generate (the old
-- "Apply Strategy to Meters" push step is retired), not the
-- master. Mirrors zSCP2_StrategyRule's columns plus:
--   ContractKey     : owning contract (FK + ON DELETE CASCADE — rules die with the contract).
--   ServiceItemKeys : comma-separated zSCP2_Item.ItemKey list the rule applies to; '' (empty) = ALL
--                     service items in the contract. Per-rule binding (UI: "Applied for all Service
--                     Items" checkbox + a multi-tick Service Item checked-combo).
--   SeededFromCode  : which master template this contract's rules were copied from (reference only).
CREATE TABLE [dbo].[zSCP2_ContractStrategyRule](
	[ContractRuleKey] [bigint] IDENTITY(1,1) NOT NULL,
	[ContractKey]     [bigint]        NOT NULL,
	[ServiceItemKeys] [nvarchar](1000) NOT NULL DEFAULT(''),
	[Seq]             [int]           NOT NULL DEFAULT(0),
	[RuleKind]        [nvarchar](20)  NOT NULL DEFAULT(''),
	[Scope]           [nvarchar](10)  NOT NULL DEFAULT(''),
	[TargetAmount]    [decimal](20,2) NOT NULL DEFAULT(0),
	[PartialPct]      [decimal](20,2) NOT NULL DEFAULT(0),
	[FreeMonths]      [int]           NOT NULL DEFAULT(0),
	[CommitAmount]    [decimal](20,2) NOT NULL DEFAULT(0),
	[FocCopies]       [decimal](20,2) NOT NULL DEFAULT(0),
	[RebatePct]       [decimal](20,2) NOT NULL DEFAULT(0),
	[NetBilling]      [char](1)       NOT NULL DEFAULT('N'),
	[LimitScope]      [char](1)       NOT NULL DEFAULT('S'),
	[LimitQty]        [decimal](20,2) NOT NULL DEFAULT(0),
	[Remark]          [nvarchar](200) NOT NULL DEFAULT(''),
	[SeededFromCode]  [nvarchar](20)  NOT NULL DEFAULT(''),
	[LastModified]    [datetime2](0)  NOT NULL DEFAULT(GETDATE()),
 CONSTRAINT [PK_zSCP2_ContractStrategyRule] PRIMARY KEY CLUSTERED ([ContractRuleKey] ASC),
 CONSTRAINT [FK_zSCP2_ContractStrategyRule_Contract] FOREIGN KEY ([ContractKey])
     REFERENCES [dbo].[zSCP2_Contract]([ContractKey]) ON DELETE CASCADE,
 CONSTRAINT [CK_zSCP2_ContractStrategyRule_Kind] CHECK ([RuleKind] IN
     ('','WAIVE-TARGET','RENTAL-FREE-N','COMMIT-MIN','FOC-REBATE','INITIAL-METER','LIMIT')),
 CONSTRAINT [CK_zSCP2_ContractStrategyRule_Net] CHECK ([NetBilling] IN ('Y','N')),
 CONSTRAINT [CK_zSCP2_ContractStrategyRule_Scope2] CHECK ([LimitScope] IN ('S','G'))
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_zSCP2_ContractStrategyRule_Contract]
    ON [dbo].[zSCP2_ContractStrategyRule]([ContractKey] ASC, [Seq] ASC);
GO

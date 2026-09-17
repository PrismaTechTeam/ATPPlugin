SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- A month of a contract that is deliberately not billed.
--
-- The billing sequence stops at the first month with no invoice, and keeps stopping there until it
-- is billed. That is the point -- a month cannot quietly go missing -- but some months really are
-- not billed: the machine was away for repair, the customer was given a free month, the contract
-- was suspended. Skipping records that decision, with who made it and why, and the sequence moves on.
--
-- A skip is never deleted. Undoing one stamps UndoneAt / UndoneBy, so the history of what was
-- skipped and put back stays readable; only a skip that is not undone counts.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_ContractPeriodSkip]'))
BEGIN
    CREATE TABLE [dbo].[zSCP2_ContractPeriodSkip] (
        [SkipKey]      BIGINT         IDENTITY(1,1) NOT NULL,
        [ContractKey]  BIGINT         NOT NULL,
        [PeriodYear]   INT            NOT NULL,
        [PeriodMonth]  INT            NOT NULL,
        [Reason]       NVARCHAR(200)  NOT NULL CONSTRAINT [DF_zSCP2_ContractPeriodSkip_Reason] DEFAULT (N''),
        [SkippedBy]    NVARCHAR(50)   NOT NULL CONSTRAINT [DF_zSCP2_ContractPeriodSkip_By] DEFAULT (N''),
        [SkippedAt]    DATETIME       NOT NULL CONSTRAINT [DF_zSCP2_ContractPeriodSkip_At] DEFAULT (GETDATE()),
        [UndoneBy]     NVARCHAR(50)   NULL,
        [UndoneAt]     DATETIME       NULL,
        CONSTRAINT [PK_zSCP2_ContractPeriodSkip] PRIMARY KEY CLUSTERED ([SkipKey]),
        CONSTRAINT [FK_zSCP2_ContractPeriodSkip_Contract] FOREIGN KEY ([ContractKey])
            REFERENCES [dbo].[zSCP2_Contract]([ContractKey]) ON DELETE CASCADE,
        CONSTRAINT [CK_zSCP2_ContractPeriodSkip_Month] CHECK ([PeriodMonth] BETWEEN 1 AND 12)
    );
END
GO

-- One live skip per contract and month.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_zSCP2_ContractPeriodSkip_Live'
                AND object_id = OBJECT_ID(N'[dbo].[zSCP2_ContractPeriodSkip]'))
    CREATE UNIQUE INDEX [UX_zSCP2_ContractPeriodSkip_Live]
        ON [dbo].[zSCP2_ContractPeriodSkip] ([ContractKey], [PeriodYear], [PeriodMonth])
        WHERE [UndoneAt] IS NULL;
GO

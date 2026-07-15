SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Debtor ownership history for the v2 service item (zSCP2_Item). A row per customer-ownership period.
-- The legacy zSCP_ServiceItemDebtorHistory FKs to the v1 zSCP_ServiceItem and CANNOT hold v2 ItemKeys,
-- so v2 gets its own table keyed to zSCP2_Item. Grade/ContractNo are captured at record time (history).
CREATE TABLE [dbo].[zSCP2_ItemDebtorHistory](
    [HistoryKey]    [bigint] IDENTITY(1,1) NOT NULL,
    [ItemKey]       [bigint] NOT NULL,
    [ServiceItemNo] [nvarchar](40)  NOT NULL CONSTRAINT DF_zSCP2IDH_ItemNo DEFAULT(''),
    [DebtorCode]    [nvarchar](40)  NOT NULL CONSTRAINT DF_zSCP2IDH_Debtor DEFAULT(''),
    [GradeCode]     [nvarchar](20)  NOT NULL CONSTRAINT DF_zSCP2IDH_Grade DEFAULT(''),
    [ContractNo]    [nvarchar](30)  NOT NULL CONSTRAINT DF_zSCP2IDH_ContractNo DEFAULT(''),
    [StartDate]     [date]          NULL,
    [EndDate]       [date]          NULL,
    [Remark]        [nvarchar](200) NOT NULL CONSTRAINT DF_zSCP2IDH_Remark DEFAULT(''),
    [LastModified]  [datetime]      NOT NULL CONSTRAINT DF_zSCP2IDH_LM DEFAULT(GETDATE()),
 CONSTRAINT [PK_zSCP2_ItemDebtorHistory] PRIMARY KEY CLUSTERED ([HistoryKey] ASC),
 CONSTRAINT [FK_zSCP2_ItemDebtorHistory_Item] FOREIGN KEY ([ItemKey])
     REFERENCES [dbo].[zSCP2_Item]([ItemKey]) ON DELETE CASCADE
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_zSCP2_ItemDebtorHistory_Item] ON [dbo].[zSCP2_ItemDebtorHistory]([ItemKey], [StartDate]);
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Spare Parts / Services Provided under a contract (Image #101). One row per line item.
-- ItemKey links a line to a specific service item (CSSI): item-bound lines are shown read-only on the
-- CONTRACT (cannot be deleted there) but editable on the SERVICE ITEM editor. ContractKey-only lines
-- (ItemKey NULL) are contract-level lines the user adds directly on the contract.
-- Pos drives the Move Up / Move Down ordering.
CREATE TABLE [dbo].[zSCP2_ContractSparePart](
    [SparePartKey]  [bigint] IDENTITY(1,1) NOT NULL,
    [ContractKey]   [bigint] NOT NULL,
    [ItemKey]       [bigint] NULL,
    [ItemCode]      [nvarchar](40)  NOT NULL CONSTRAINT DF_zSCP2_SP_ItemCode DEFAULT(''),
    [Description]   [nvarchar](200) NOT NULL CONSTRAINT DF_zSCP2_SP_Desc DEFAULT(''),
    [Unlimited]     [char](1)       NOT NULL CONSTRAINT DF_zSCP2_SP_Unlimited DEFAULT('N'),
    [UOM]           [nvarchar](20)  NOT NULL CONSTRAINT DF_zSCP2_SP_UOM DEFAULT(''),
    [Quantity]      [decimal](18,4) NOT NULL CONSTRAINT DF_zSCP2_SP_Qty DEFAULT(0),
    [Discount]      [nvarchar](30)  NOT NULL CONSTRAINT DF_zSCP2_SP_Disc DEFAULT(''),
    [UnitPrice]     [decimal](18,4) NOT NULL CONSTRAINT DF_zSCP2_SP_Price DEFAULT(0),
    [TaxType]       [nvarchar](20)  NOT NULL CONSTRAINT DF_zSCP2_SP_TaxType DEFAULT(''),
    [TaxInclusive]  [char](1)       NOT NULL CONSTRAINT DF_zSCP2_SP_TaxInc DEFAULT('N'),
    [TaxRate]       [decimal](9,4)  NOT NULL CONSTRAINT DF_zSCP2_SP_TaxRate DEFAULT(0),
    [Pos]           [int]           NOT NULL CONSTRAINT DF_zSCP2_SP_Pos DEFAULT(0),
    [LastModified]  [datetime]      NOT NULL CONSTRAINT DF_zSCP2_SP_LM DEFAULT(GETDATE()),
 CONSTRAINT [PK_zSCP2_ContractSparePart] PRIMARY KEY CLUSTERED ([SparePartKey] ASC),
 CONSTRAINT [FK_zSCP2_ContractSparePart_Contract] FOREIGN KEY ([ContractKey])
     REFERENCES [dbo].[zSCP2_Contract]([ContractKey]) ON DELETE CASCADE
 -- No FK on ItemKey: zSCP2_Item already cascades from zSCP2_Contract, so a second cascade path here
 -- ("multiple cascade paths" error) is illegal. ItemKey is a plain nullable link, managed in code;
 -- item-bound spare parts are deleted/repointed by the app when their service item changes.
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_zSCP2_ContractSparePart_Contract] ON [dbo].[zSCP2_ContractSparePart]([ContractKey]);
GO
CREATE NONCLUSTERED INDEX [IX_zSCP2_ContractSparePart_Item] ON [dbo].[zSCP2_ContractSparePart]([ItemKey]);
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v10: Demo 28/07 #6 "Group Deal split billing" — machines of ONE contract carry a Bill Group
-- code; at generate, same contract + same code share ONE invoice ('' = not grouped, legacy path).
IF COL_LENGTH('dbo.zSCP2_Item', 'BillGroupCode') IS NULL
    ALTER TABLE dbo.zSCP2_Item ADD BillGroupCode NVARCHAR(20) NOT NULL CONSTRAINT DF_zSCP2_Item_BillGroupCode DEFAULT('');
GO

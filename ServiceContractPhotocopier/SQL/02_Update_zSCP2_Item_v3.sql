SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Evolve an existing zSCP2_Item (created before the "service item is not a machine" correction):
-- rename ItemNo -> ServiceItemNo, drop the obsolete MachineName / StockCode columns (and their
-- auto-named DEFAULT constraints first). Idempotent: safe on every plugin load and on fresh DBs.
IF COL_LENGTH('dbo.zSCP2_Item','ItemNo') IS NOT NULL AND COL_LENGTH('dbo.zSCP2_Item','ServiceItemNo') IS NULL
    EXEC sp_rename 'dbo.zSCP2_Item.ItemNo', 'ServiceItemNo', 'COLUMN';
GO
DECLARE @c nvarchar(256);
SELECT @c = dc.name FROM sys.default_constraints dc
    JOIN sys.columns col ON col.object_id = dc.parent_object_id AND col.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.zSCP2_Item') AND col.name = 'MachineName';
IF @c IS NOT NULL EXEC('ALTER TABLE [dbo].[zSCP2_Item] DROP CONSTRAINT [' + @c + ']');
IF COL_LENGTH('dbo.zSCP2_Item','MachineName') IS NOT NULL
    ALTER TABLE [dbo].[zSCP2_Item] DROP COLUMN [MachineName];
GO
DECLARE @c2 nvarchar(256);
SELECT @c2 = dc.name FROM sys.default_constraints dc
    JOIN sys.columns col ON col.object_id = dc.parent_object_id AND col.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.zSCP2_Item') AND col.name = 'StockCode';
IF @c2 IS NOT NULL EXEC('ALTER TABLE [dbo].[zSCP2_Item] DROP CONSTRAINT [' + @c2 + ']');
IF COL_LENGTH('dbo.zSCP2_Item','StockCode') IS NOT NULL
    ALTER TABLE [dbo].[zSCP2_Item] DROP COLUMN [StockCode];
GO

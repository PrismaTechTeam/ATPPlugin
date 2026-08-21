SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v14: black and colour get a grouping column of their own.
--
-- MergeGroupCode was doing both jobs -- which machines share a rental line, and which share a BK+CL
-- line -- because the two used to be told apart by the format's two line modes instead. With the
-- modes gone from the screen, one column cannot answer two questions: the commonest deal in this
-- business is one agreed rental across the fleet while every machine still bills its own copies,
-- and that needs the machines grouped on one side and apart on the other.
--
-- MergeGroupCode keeps the rental. MergeGroupCodeMeter takes the copies, backfilled from it so
-- nothing that is grouped today comes apart.

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_Item]') AND name = 'MergeGroupCodeMeter')
BEGIN
    ALTER TABLE [dbo].[zSCP2_Item]
        ADD [MergeGroupCodeMeter] NVARCHAR(40) NOT NULL
            CONSTRAINT [DF_zSCP2_Item_MergeGroupCodeMeter] DEFAULT '';

    EXEC(N'UPDATE [dbo].[zSCP2_Item]
              SET [MergeGroupCodeMeter] = [MergeGroupCode]
            WHERE ISNULL([MergeGroupCode], '''') <> ''''');
END
GO

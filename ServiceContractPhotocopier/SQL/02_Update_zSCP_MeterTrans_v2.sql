SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Repoint zSCP_MeterTrans from the old zSCP_ServiceItemMeterType to the new zSCP2_ItemMeter.
-- ServiceItemMeterTypeKey now stores zSCP2_ItemMeter.ItemMeterKey; ServiceItemKey stores zSCP2_Item.ItemKey.
-- Idempotent: after the first run the old FK is gone and the new FK exists, so re-runs are no-ops.
-- TRUNCATE is safe here — meter-reading history is throwaway test data during this transition.
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_zSCP_MeterTrans_zSCP_ServiceItemMeterType')
BEGIN
    ALTER TABLE [dbo].[zSCP_MeterTrans] DROP CONSTRAINT [FK_zSCP_MeterTrans_zSCP_ServiceItemMeterType];
    TRUNCATE TABLE [dbo].[zSCP_MeterTrans];
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_zSCP_MeterTrans_zSCP2_ItemMeter')
   AND EXISTS (SELECT 1 FROM sys.tables WHERE name = 'zSCP2_ItemMeter')
   AND EXISTS (SELECT 1 FROM sys.tables WHERE name = 'zSCP_MeterTrans')
BEGIN
    ALTER TABLE [dbo].[zSCP_MeterTrans] WITH CHECK
        ADD CONSTRAINT [FK_zSCP_MeterTrans_zSCP2_ItemMeter]
        FOREIGN KEY ([ServiceItemMeterTypeKey])
        REFERENCES [dbo].[zSCP2_ItemMeter]([ItemMeterKey]) ON DELETE CASCADE;
END
GO

-- Per-METER multi-price tier override. When rows exist for a meter they REPLACE the master
-- scheme's ladder (zSCP_MeterMultiPriceItem) for that meter only:
--   * meter picked scheme X and modified its tiers  -> rows here + MeterMultiPriceCode = X   ("X (Modified)")
--   * meter built a fully custom ladder             -> rows here + MeterMultiPriceCode = ''  ("(Custom)")
--   * meter uses scheme X as-is                     -> NO rows here, MeterMultiPriceCode = X
-- Billing keys these ladders as '#<ItemMeterKey>' (ScpMultiPrice.LoadLadders).
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_ItemMeterPrice]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[zSCP2_ItemMeterPrice](
        [ItemMeterPriceKey] [bigint] IDENTITY(1,1) NOT NULL,
        [ItemMeterKey]      [bigint]        NOT NULL,
        [MeterReading]      [decimal](18,2) NOT NULL DEFAULT(0),
        [UnitPrice]         [decimal](18,6) NOT NULL DEFAULT(0),
     CONSTRAINT [PK_zSCP2_ItemMeterPrice] PRIMARY KEY CLUSTERED ([ItemMeterPriceKey] ASC),
     CONSTRAINT [FK_zSCP2_ItemMeterPrice_ItemMeter] FOREIGN KEY ([ItemMeterKey])
        REFERENCES [dbo].[zSCP2_ItemMeter]([ItemMeterKey]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_zSCP2_ItemMeterPrice_Meter] ON [dbo].[zSCP2_ItemMeterPrice]([ItemMeterKey]);
END

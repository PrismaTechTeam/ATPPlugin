-- The months of a contract come off newest first, or not at all.
--
-- September's copies are the difference between September's reading and the one August billed.
-- Delete August's invoice and September is left measuring from a reading nobody was charged for,
-- so the same copies sit on two invoices. The plugin refuses this in its own delete; this trigger
-- makes the same refusal hold inside AutoCount's own Invoice screen, where the plugin is not asked.
--
-- A later month counts only while its invoice is still in the book, so deleting October and then
-- September in the right order goes through untouched.

IF OBJECT_ID(N'[dbo].[TR_zSCP2_IV_NoDeleteEarlierBilledMonth]', N'TR') IS NOT NULL
    DROP TRIGGER [dbo].[TR_zSCP2_IV_NoDeleteEarlierBilledMonth]
GO

CREATE TRIGGER [dbo].[TR_zSCP2_IV_NoDeleteEarlierBilledMonth]
ON [dbo].[IV]
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM deleted) RETURN;

    -- An account book without the Service & Contract tables has nothing to protect.
    IF OBJECT_ID(N'dbo.zSCP2_MeterEntry', N'U') IS NULL RETURN;
    IF OBJECT_ID(N'dbo.zSCP2_ItemMeter',  N'U') IS NULL RETURN;
    IF OBJECT_ID(N'dbo.zSCP2_Item',       N'U') IS NULL RETURN;
    IF OBJECT_ID(N'dbo.zSCP2_Contract',   N'U') IS NULL RETURN;

    DECLARE @msg NVARCHAR(500);

    ;WITH going AS (
        SELECT DISTINCT
               d.DocNo,
               i.ContractKey,
               e.PeriodYear * 100 + e.PeriodMonth AS P
          FROM deleted d
          JOIN dbo.zSCP2_MeterEntry e ON e.InvoicedDocKey = d.DocKey
          JOIN dbo.zSCP2_ItemMeter  m ON m.ItemMeterKey   = e.ItemMeterKey
          JOIN dbo.zSCP2_Item       i ON i.ItemKey        = m.ItemKey
    )
    SELECT TOP 1 @msg =
           N'Invoice ' + RTRIM(going.DocNo) + N' cannot be deleted: ' + RTRIM(c.ContractNo) +
           N' is already invoiced for ' +
           LEFT(DATENAME(MONTH, DATEADD(MONTH, later.PeriodMonth - 1, '2000-01-01')), 3) + N' ' +
           CONVERT(NVARCHAR(4), later.PeriodYear) +
           N' on ' + RTRIM(ISNULL(later.InvoicedDocNo, '')) +
           N'. That invoice was billed from the reading this one charged for. ' +
           N'Delete the later month first, then this one.'
      FROM going
      JOIN dbo.zSCP2_Item       i2    ON i2.ContractKey    = going.ContractKey
      JOIN dbo.zSCP2_ItemMeter  m2    ON m2.ItemKey        = i2.ItemKey
      JOIN dbo.zSCP2_MeterEntry later  ON later.ItemMeterKey = m2.ItemMeterKey
                                     AND later.PeriodYear * 100 + later.PeriodMonth > going.P
                                     AND later.InvoicedDocKey IS NOT NULL
      JOIN dbo.zSCP2_Contract   c     ON c.ContractKey     = going.ContractKey
     WHERE EXISTS (SELECT 1 FROM dbo.IV iv WHERE iv.DocKey = later.InvoicedDocKey)
     ORDER BY later.PeriodYear, later.PeriodMonth;

    IF @msg IS NOT NULL
    BEGIN
        RAISERROR(@msg, 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END
GO

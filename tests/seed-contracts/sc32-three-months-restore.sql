SET NOCOUNT ON;
GO

-- Undo sc32-three-months.sql: takes the readings, prices and opening readings back off
-- SC 000000032 (AED_ATPTEST). A month already invoiced is left alone -- an invoice must keep the
-- reading it billed.

DECLARE @ck BIGINT = (SELECT ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'SC 000000032');
IF @ck IS NULL BEGIN RAISERROR('SC 000000032 not found in this book.', 16, 1); RETURN; END

DELETE e
  FROM dbo.zSCP2_MeterEntry e
  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey
  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
 WHERE i.ContractKey = @ck
   AND e.PeriodYear = 2026 AND e.PeriodMonth IN (8, 9, 10)
   AND e.InvoicedDocKey IS NULL AND ISNULL(e.InvoicedDocNo, '') = '';

UPDATE m
   SET m.ChargesRate = 0, m.InitialReading = 0, m.LastModified = GETDATE()
  FROM dbo.zSCP2_ItemMeter m
  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
 WHERE i.ContractKey = @ck
   AND UPPER(ISNULL(m.MeterRole,'')) IN ('RENTAL','BK','CL');

SELECT COUNT(*) AS ReadingsLeft
  FROM dbo.zSCP2_MeterEntry e
  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey
  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
 WHERE i.ContractKey = @ck;
GO

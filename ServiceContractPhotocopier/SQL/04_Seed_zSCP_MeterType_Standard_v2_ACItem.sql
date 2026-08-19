SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v2: point the five standard meter types at the five charge items.
--
-- An invoice line is posted under a stock item, and the meter type is what names it. The standard
-- types were seeded with that blank, because only the account book knows which item rent and copies
-- are billed under -- so the Item column on the invoice came out empty.
--
-- The book's own convention for these is group S001, no stock control, type R001 for a rental and
-- M001 for a meter reading; the old 446 types follow it exactly (01.MR.BK.2JC10897 and the rest).
-- Under the new rules the rate lives on the machine's meter, so five items serve every contract
-- instead of one per customer per machine.
--
-- Only fills a BLANK. A book that already pointed these somewhere keeps its own answer, and the
-- update is skipped entirely if the item does not exist -- naming an item that is not there would
-- put an unpostable code on every line.

UPDATE mt
   SET mt.ACItemCode = mt.MeterTypeCode,
       mt.StockCode  = CASE WHEN ISNULL(mt.StockCode,'') = '' THEN mt.MeterTypeCode ELSE mt.StockCode END,
       mt.LastModified = GETDATE()
  FROM [dbo].[zSCP_MeterType] mt
  JOIN [dbo].[Item] i ON i.ItemCode = mt.MeterTypeCode
 WHERE mt.MeterTypeCode IN (N'RENTAL', N'BK', N'CL', N'COMMIT', N'WAIVE')
   AND ISNULL(mt.ACItemCode,'') = '';
GO

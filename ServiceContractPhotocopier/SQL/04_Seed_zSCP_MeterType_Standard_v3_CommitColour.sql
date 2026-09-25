SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v3: a second committed-minimum type, for a colour minimum that sits beside a black one.
--
-- Feedback ATP-4: a machine can be held to a black minimum AND a colour minimum at once (black
-- copies at least RM 200, colour at least RM 100), each topped up on its own. Both are COMMIT
-- meters on the same machine, and zSCP2_ItemMeter allows one meter per type per machine
-- (UQ_zSCP2_ItemMeter_Machine), so the colour one needs its own type. What it counts is still
-- the meter's WaiveScope -- the type only keeps the two rows apart.
--
-- Posted under the same stock item as COMMIT, whatever this book set that to: a minimum is a
-- minimum on the invoice, black or colour. Insert-if-missing, and a blank item is filled from
-- COMMIT's later, so setting COMMIT in Meter Type maintenance carries over.

IF NOT EXISTS (SELECT 1 FROM [dbo].[zSCP_MeterType] WHERE MeterTypeCode = N'COMMIT-CL')
    INSERT INTO [dbo].[zSCP_MeterType]
        (MeterTypeCode, [Description], StockCode, MeterMultiPriceCode, MinimumCharges, ChargesRate,
         RebateQtyInPercent, FOCQty, Inactive, LastModified, ACItemCode, IsFlatCharge, IsRentalWaive, DefaultRole)
    SELECT N'COMMIT-CL', N'MINIMUM COMMITTED COLOUR PRINT CHARGES',
           ISNULL(c.StockCode, N''), N'', 0.00, 0.000000, 0.00, 0.00, 'N', GETDATE(),
           ISNULL(c.ACItemCode, N''), 'Y', 'N', 'COMMIT'
      FROM (SELECT 1 AS One) x
      LEFT JOIN [dbo].[zSCP_MeterType] c ON c.MeterTypeCode = N'COMMIT';
GO

UPDATE cl
   SET cl.ACItemCode = c.ACItemCode,
       cl.StockCode  = CASE WHEN ISNULL(cl.StockCode,'') = '' THEN c.StockCode ELSE cl.StockCode END,
       cl.LastModified = GETDATE()
  FROM [dbo].[zSCP_MeterType] cl
  JOIN [dbo].[zSCP_MeterType] c ON c.MeterTypeCode = N'COMMIT'
 WHERE cl.MeterTypeCode = N'COMMIT-CL'
   AND ISNULL(cl.ACItemCode,'') = ''
   AND ISNULL(c.ACItemCode,'') <> '';
GO

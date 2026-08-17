-- The three meter types the NEW way needs: one rental, one black, one colour.
--
-- The old book has 446 of them because Master Accounting could only vary a charge by
-- inventing a meter type, so every machine, every customer and every rate got its own
-- (01.MR.BK.2XP12674, 01.RA-4 APM, ...). Under the billing-format engine the rate lives
-- on the machine's meter and the wording comes from the format, so a meter type is only
-- three things now: is this rent, black copies, or colour copies.
--
-- Rates stay 0 on purpose. These are kinds, not prices -- the price is whatever the
-- machine's meter says. ACItemCode is blank because only the account book knows which
-- stock item rent and copies are billed under; set it once in Meter Type maintenance.
--
-- Insert-if-missing: a book that already has them keeps its own edits, and re-running the
-- migration never duplicates or resets them.

IF NOT EXISTS (SELECT 1 FROM [dbo].[zSCP_MeterType] WHERE MeterTypeCode = N'RENTAL')
    INSERT INTO [dbo].[zSCP_MeterType]
        (MeterTypeCode, [Description], StockCode, MeterMultiPriceCode, MinimumCharges, ChargesRate,
         RebateQtyInPercent, FOCQty, Inactive, LastModified, ACItemCode, IsFlatCharge, IsRentalWaive, DefaultRole)
    VALUES
        (N'RENTAL', N'MONTHLY RENTAL', N'', N'', 0.00, 0.000000, 0.00, 0.00, 'N', GETDATE(), N'', 'Y', 'N', 'RENTAL');

IF NOT EXISTS (SELECT 1 FROM [dbo].[zSCP_MeterType] WHERE MeterTypeCode = N'BK')
    INSERT INTO [dbo].[zSCP_MeterType]
        (MeterTypeCode, [Description], StockCode, MeterMultiPriceCode, MinimumCharges, ChargesRate,
         RebateQtyInPercent, FOCQty, Inactive, LastModified, ACItemCode, IsFlatCharge, IsRentalWaive, DefaultRole)
    VALUES
        (N'BK', N'BLACK COPY + PRINT A4 & A3', N'', N'', 0.00, 0.000000, 0.00, 0.00, 'N', GETDATE(), N'', 'N', 'N', 'BK');

IF NOT EXISTS (SELECT 1 FROM [dbo].[zSCP_MeterType] WHERE MeterTypeCode = N'CL')
    INSERT INTO [dbo].[zSCP_MeterType]
        (MeterTypeCode, [Description], StockCode, MeterMultiPriceCode, MinimumCharges, ChargesRate,
         RebateQtyInPercent, FOCQty, Inactive, LastModified, ACItemCode, IsFlatCharge, IsRentalWaive, DefaultRole)
    VALUES
        (N'CL', N'COLOUR COPY + PRINT A4 & A3', N'', N'', 0.00, 0.000000, 0.00, 0.00, 'N', GETDATE(), N'', 'N', 'N', 'CL');

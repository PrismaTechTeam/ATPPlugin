-- The five meter types the NEW way needs: rent, black, colour, committed minimum, waive.
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
-- COMMIT and WAIVE carry no amount either. A committed minimum is per machine (RM 3,000 here,
-- RM 800 there) and a waive is per deal, so both live on the machine's meter row -- which is exactly
-- why the old book grew forty-odd MIN 300-12MTH / MIN 1500-60MTH types and a (W) type per term.
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

IF NOT EXISTS (SELECT 1 FROM [dbo].[zSCP_MeterType] WHERE MeterTypeCode = N'COMMIT')
    INSERT INTO [dbo].[zSCP_MeterType]
        (MeterTypeCode, [Description], StockCode, MeterMultiPriceCode, MinimumCharges, ChargesRate,
         RebateQtyInPercent, FOCQty, Inactive, LastModified, ACItemCode, IsFlatCharge, IsRentalWaive, DefaultRole)
    VALUES
        (N'COMMIT', N'MINIMUM COMMITTED PRINT CHARGES', N'', N'', 0.00, 0.000000, 0.00, 0.00, 'N', GETDATE(), N'', 'Y', 'N', 'COMMIT');

IF NOT EXISTS (SELECT 1 FROM [dbo].[zSCP_MeterType] WHERE MeterTypeCode = N'WAIVE')
    INSERT INTO [dbo].[zSCP_MeterType]
        (MeterTypeCode, [Description], StockCode, MeterMultiPriceCode, MinimumCharges, ChargesRate,
         RebateQtyInPercent, FOCQty, Inactive, LastModified, ACItemCode, IsFlatCharge, IsRentalWaive, DefaultRole)
    VALUES
        (N'WAIVE', N'RENTAL WAIVE', N'', N'', 0.00, 0.000000, 0.00, 0.00, 'N', GETDATE(), N'', 'Y', 'Y', 'WAIVE');

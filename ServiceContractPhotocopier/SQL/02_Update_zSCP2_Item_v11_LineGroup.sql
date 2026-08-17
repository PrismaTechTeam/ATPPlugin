SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v11: the word printed on a machine's invoice line, and the bucket it groups into.
--
-- Not to be confused with BillGroupCode (v10), which is next to it and answers a different
-- question. Two keys, two verbs:
--
--   BillGroupCode  which INVOICE this machine lands on   (v10, "Own invoice")
--   LineGroupCode  which LINE, and what that line says   (here, "Line label")
--
-- The customer's invoices carry HEAVY DUTY / MEDIUM DUTY / LIGHT DUTY on the line text, and today
-- that word lives inside the item code (01.MR.BK.HOSPITAL PG ... HEAVY DUTY) because Master
-- Accounting had nowhere else to put it -- which is why one meter type exists per customer per
-- duty. Here it is a field.
--
-- Role-scoped on purpose, because the two roles disagree. Rental lines always key on it. Meter
-- lines key on it only when the contract's MeterLineMode is 'M'; under 'A' it is a label the line
-- prints when its members agree, and nothing more. JPJ is the proof: its machines are tagged HEAVY
-- (1), MEDIUM (5) and LIGHT (6) and its rental prints as three lines, but all twelve share a BK
-- rate of 0.0285 and print as ONE BK line of 80,720. Keying meter lines on the label unconditionally
-- would split that into three and no longer match the issued invoice.
--
-- '' = inert. An existing book is unaffected until someone tags a machine.

IF COL_LENGTH('dbo.zSCP2_Item', 'LineGroupCode') IS NULL
    ALTER TABLE dbo.zSCP2_Item ADD LineGroupCode NVARCHAR(20) NOT NULL
        CONSTRAINT DF_zSCP2_Item_LineGroupCode DEFAULT('');
GO

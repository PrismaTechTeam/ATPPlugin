SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- The 11 formats the 22 July-2026 invoices actually use, named so nobody has to think in modes.
--
-- ASCII only, deliberately. This file is run two ways -- as an embedded resource by the plugin
-- (UTF-8) and by hand through sqlcmd (which reads the ANSI codepage) -- and a middle dot or an
-- em dash comes out of the second path as mojibake that then survives, because the insert below is
-- insert-if-missing and will not correct a row that already exists. Plain hyphens cannot break.
--
-- Insert-if-missing: a format the user has renamed is never overwritten, and a deleted one returns
-- on the next plugin load -- set Inactive='Y' instead of deleting.
--
-- Customers listed per row are where the format was observed; they are documentation, not data.

;WITH seed(FormatCode, FormatName, InvoiceSplit, RentalLineMode, MeterLineMode, Remark) AS (
    SELECT * FROM (VALUES
    -- ---- one invoice for everything -------------------------------------------------------
    ('1INV-ALL',        N'1 invoice, rental 1 line, BK+CL 1 line each',
                        'ONE', 'A', 'A', N'SULTAN ISMAIL, SINGLE, SYNTURN, PERMAI LAMA, PONTIAN'),
    ('1INV-RMODEL',     N'1 invoice, rental per model, BK+CL 1 line each',
                        'ONE', 'M', 'A', N'JPJ MELAKA, PUSPEN MUAR, IKTBN CHEMBONG'),
    ('1INV-MMACHINE',   N'1 invoice, rental 1 line, BK+CL per machine',
                        'ONE', 'A', 'S', N'3000-J0056 (meter)'),
    ('1INV-BYMODEL',    N'1 invoice, rental per model, BK+CL per model',
                        'ONE', 'M', 'M', N'MBJB: 52 machines print as 4 rental + 7 meter lines'),
    -- ---- rental on its own invoice --------------------------------------------------------
    ('2INV-ALL',        N'2 invoices, rental 1 line, BK+CL 1 line each',
                        'RS',  'A', 'A', N'SULTANAH AMINAH'),
    ('2INV-RMODEL',     N'2 invoices, rental per model, BK+CL 1 line each',
                        'RS',  'M', 'A', N'PASIR GUDANG, ROMPIN, IPG TENGKU AMPUAN AFZAN'),
    ('2INV-MMACHINE',   N'2 invoices, rental 1 line, BK+CL per machine',
                        'RS',  'A', 'S', N'3000-F0019'),
    ('2INV-RMODEL-MM',  N'2 invoices, rental per model, BK+CL per machine',
                        'RS',  'M', 'S', N'KEJORA, MARA'),
    ('2INV-EACH',       N'2 invoices, rental per machine, BK+CL per machine',
                        'RS',  'S', 'S', N'JABATAN KASTAM: the most itemised layout'),
    ('2INV-RMACHINE',   N'2 invoices, rental per machine, BK+CL 1 line each',
                        'RS',  'S', 'A', N'KENSINGTON'),
    -- ---- one set of invoices per machine --------------------------------------------------
    ('PERMACHINE',      N'One rental + one meter invoice per machine',
                        'PMS', 'S', 'S', N'TANGKAK: 5 machines produced 9 invoices in July 2026')
    ) v(FormatCode, FormatName, InvoiceSplit, RentalLineMode, MeterLineMode, Remark)
)
INSERT INTO dbo.zSCP2_BillingFormat (FormatCode, FormatName, InvoiceSplit, RentalLineMode, MeterLineMode, Remark)
SELECT s.FormatCode, s.FormatName, s.InvoiceSplit, s.RentalLineMode, s.MeterLineMode, s.Remark
FROM seed s
WHERE NOT EXISTS (SELECT 1 FROM dbo.zSCP2_BillingFormat f WHERE f.FormatCode = s.FormatCode);
GO

-- One-time repair for books seeded before the ASCII rule above, where a hand-run through sqlcmd
-- left a mangled byte in the name. Only touches rows still carrying one, so a rename survives.
UPDATE dbo.zSCP2_BillingFormat
   SET FormatName = REPLACE(REPLACE(FormatName, NCHAR(31179), ','), '  ', ' '),
       Remark     = REPLACE(REPLACE(Remark,     NCHAR(31179), ':'), '  ', ' ')
 WHERE FormatName LIKE N'%' + NCHAR(31179) + N'%' OR Remark LIKE N'%' + NCHAR(31179) + N'%';
GO

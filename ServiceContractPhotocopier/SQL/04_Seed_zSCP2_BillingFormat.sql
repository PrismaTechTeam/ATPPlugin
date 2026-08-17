SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- The 11 formats the 22 July-2026 invoices actually use, named so nobody has to think in modes.
-- Insert-if-missing: a format edited by the user is never overwritten, and a format deleted by the
-- user stays deleted only until the next plugin load -- rename it or set Inactive='Y' instead.
--
-- Customers listed per row are the ones observed on that format; they are documentation, not data.
-- A contract is attached to a format by 05_Backfill_zSCP2_Contract_BillingFormats.sql or by hand.

;WITH seed(FormatCode, FormatName, InvoiceSplit, RentalLineMode, MeterLineMode, Remark) AS (
    SELECT * FROM (VALUES
    -- ---- one invoice for everything -------------------------------------------------------
    ('1INV-ALL',        N'1 invoice · rental 1 line · BK+CL 1 line each',
                        'ONE', 'A', 'A', N'SULTAN ISMAIL, SINGLE, SYNTURN, PERMAI LAMA, PONTIAN'),
    ('1INV-RMODEL',     N'1 invoice · rental per model · BK+CL 1 line each',
                        'ONE', 'M', 'A', N'JPJ MELAKA, PUSPEN MUAR, IKTBN CHEMBONG'),
    ('1INV-MMACHINE',   N'1 invoice · rental 1 line · BK+CL per machine',
                        'ONE', 'A', 'S', N'3000-J0056 (meter)'),
    ('1INV-BYMODEL',    N'1 invoice · rental per model · BK+CL per model',
                        'ONE', 'M', 'M', N'MBJB — 52 machines print as 4 rental + 7 meter lines'),
    -- ---- rental on its own invoice --------------------------------------------------------
    ('2INV-ALL',        N'2 invoices · rental 1 line · BK+CL 1 line each',
                        'RS',  'A', 'A', N'SULTANAH AMINAH'),
    ('2INV-RMODEL',     N'2 invoices · rental per model · BK+CL 1 line each',
                        'RS',  'M', 'A', N'PASIR GUDANG, ROMPIN, IPG TENGKU AMPUAN AFZAN'),
    ('2INV-MMACHINE',   N'2 invoices · rental 1 line · BK+CL per machine',
                        'RS',  'A', 'S', N'3000-F0019'),
    ('2INV-RMODEL-MM',  N'2 invoices · rental per model · BK+CL per machine',
                        'RS',  'M', 'S', N'KEJORA, MARA'),
    ('2INV-EACH',       N'2 invoices · rental per machine · BK+CL per machine',
                        'RS',  'S', 'S', N'JABATAN KASTAM — the most itemised layout'),
    ('2INV-RMACHINE',   N'2 invoices · rental per machine · BK+CL 1 line each',
                        'RS',  'S', 'A', N'KENSINGTON'),
    -- ---- one set of invoices per machine --------------------------------------------------
    ('PERMACHINE',      N'One rental + one meter invoice per machine',
                        'PMS', 'S', 'S', N'TANGKAK — 5 machines produced 9 invoices in July 2026')
    ) v(FormatCode, FormatName, InvoiceSplit, RentalLineMode, MeterLineMode, Remark)
)
INSERT INTO dbo.zSCP2_BillingFormat (FormatCode, FormatName, InvoiceSplit, RentalLineMode, MeterLineMode, Remark)
SELECT s.FormatCode, s.FormatName, s.InvoiceSplit, s.RentalLineMode, s.MeterLineMode, s.Remark
FROM seed s
WHERE NOT EXISTS (SELECT 1 FROM dbo.zSCP2_BillingFormat f WHERE f.FormatCode = s.FormatCode);
GO

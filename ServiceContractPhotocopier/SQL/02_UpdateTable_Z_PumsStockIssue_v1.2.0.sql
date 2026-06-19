-- v1.2.0 — capture the machine serial that PUMS sends under the JSON key "Serial Number"
-- on the Stock Issue webhook. Idempotent: safe to run on existing books.
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Z_PumsStockIssue' AND COLUMN_NAME = 'SerialNumber'
)
    ALTER TABLE Z_PumsStockIssue ADD SerialNumber NVARCHAR(50) NULL;

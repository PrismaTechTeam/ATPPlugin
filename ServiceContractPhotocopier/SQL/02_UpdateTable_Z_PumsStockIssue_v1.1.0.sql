-- v1.1.0 — add operator-set LocationOverride column so the user can pick a
-- Stock Issue line Location from a dropdown on the Stock Request Task grid.
-- Idempotent: safe to run on existing books.
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Z_PumsStockIssue' AND COLUMN_NAME = 'LocationOverride'
)
    ALTER TABLE Z_PumsStockIssue ADD LocationOverride NVARCHAR(50) NULL;

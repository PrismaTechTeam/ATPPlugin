-- v1.1.0 — add operator-set FromLocationOverride / ToLocationOverride columns so
-- the user can pick Stock Transfer From / To Location values from a dropdown on
-- the Stock Request Task grid before generating the AutoCount ST document.
-- Idempotent: safe to run on existing books.
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Z_PumsStockTransfer' AND COLUMN_NAME = 'FromLocationOverride'
)
    ALTER TABLE Z_PumsStockTransfer ADD FromLocationOverride NVARCHAR(50) NULL;

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Z_PumsStockTransfer' AND COLUMN_NAME = 'ToLocationOverride'
)
    ALTER TABLE Z_PumsStockTransfer ADD ToLocationOverride NVARCHAR(50) NULL;

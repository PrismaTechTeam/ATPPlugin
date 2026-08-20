SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v13: the line label needs room for the labels this business actually prints.
--
-- LineGroupCode was NVARCHAR(20). The book's own invoices carry things like
--   MEDIUM DUTY "30-50 ppm" - IRADX4935I     (38 characters, 38 lines)
--   MEDIUM HEAVY DUTY ''45cpm''              (27 characters)
--   HEAVY DUTY ''105cpm''                    (21 characters)
-- so anything beyond a bare "HEAVY DUTY" was refused outright -- SQL Server errors on truncation
-- rather than silently cutting, which at least meant nobody shipped a half-word to a customer, but
-- it also meant the field could not hold what it exists to hold.
--
-- 60 covers every label seen in the legacy book with room to spare. Widening only: no existing value
-- can fail to fit, and the label is a DESCRIPTION -- it is never part of a fold key, never indexed,
-- and never compared -- so nothing downstream depends on its length.

IF EXISTS (SELECT 1 FROM sys.columns
            WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_Item]')
              AND name = 'LineGroupCode'
              AND max_length < 120)          -- max_length is bytes: NVARCHAR(60) = 120
BEGIN
    ALTER TABLE [dbo].[zSCP2_Item] ALTER COLUMN [LineGroupCode] NVARCHAR(60) NULL;
END
GO

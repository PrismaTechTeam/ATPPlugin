SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Who this account book IS, permanently.
--
-- Inter-billing links rows across two books: a contract in the subsidiary points back at the one in
-- the parent, an invoice points at the invoice it came from, a counter says which book owns it. Every
-- one of those links needs to name the OTHER book, and it has to keep naming it correctly a decade
-- later.
--
-- Neither the database name nor the company name can do that job. A book restored from a backup gets
-- whatever database name the person typed; a company changes its registered name; two books on the
-- same server can be called the same thing in different instances. A GUID issued once and never
-- reissued is the only identity that survives all of it.
--
-- One row, for ever. The plugin writes it on first load and then only ever reads it.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_BookIdentity]'))
BEGIN
    CREATE TABLE [dbo].[zSCP2_BookIdentity] (
        [OnlyRow]    BIT              NOT NULL CONSTRAINT [DF_zSCP2_BookIdentity_OnlyRow] DEFAULT 1,
        [BookId]     UNIQUEIDENTIFIER NOT NULL,
        [CreatedAt]  DATETIME         NOT NULL CONSTRAINT [DF_zSCP2_BookIdentity_CreatedAt] DEFAULT GETDATE(),
        CONSTRAINT [PK_zSCP2_BookIdentity] PRIMARY KEY CLUSTERED ([OnlyRow]),
        -- One row means one row. Without this the table could grow a second identity and every link
        -- written afterwards would point at whichever one was read first.
        CONSTRAINT [CK_zSCP2_BookIdentity_OnlyRow] CHECK ([OnlyRow] = 1)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[zSCP2_BookIdentity])
BEGIN
    INSERT INTO [dbo].[zSCP2_BookIdentity] ([OnlyRow], [BookId]) VALUES (1, NEWID());
END
GO

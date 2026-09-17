SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- The OTHER account book, as this one reaches it.
--
-- Inter-billing runs between two books of the same owner: the parent holds the machines and takes
-- the readings, the subsidiary holds the end customer and issues the bill. The subsidiary PULLS --
-- it is the only side that knows which contracts it wants and which debtor to put them on -- so it
-- is the subsidiary that stores a connection, and the connection is READ ONLY in use. Nothing this
-- module does ever writes into the other book.
--
-- One row per book reachable from here. There is normally one; there is no rule against several,
-- and a group with two parents would need them.
--
-- The password is stored encrypted (see ScpSecret) and never shown back on screen. Both books
-- belong to the same owner, so this is a convenience store and not a trust boundary -- it exists so
-- nobody has to retype a server password every month, not to withstand an attacker who already has
-- the database.
--
-- RemoteBookId is learned, never typed: a successful test reads the other book's own permanent
-- identity out of zSCP2_BookIdentity and keeps it here. Every link row afterwards names that GUID,
-- so if somebody later repoints this row at a different database the links do not silently follow.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_InterBillBook]'))
BEGIN
    CREATE TABLE [dbo].[zSCP2_InterBillBook] (
        [BookKey]        INT              IDENTITY(1,1) NOT NULL,
        -- What the user calls it: "HQ", "Prisma Sdn Bhd (parent)". Shown everywhere the module
        -- names a source, because a database name means nothing to the person choosing.
        [Alias]          NVARCHAR(60)     NOT NULL,
        [ServerName]     NVARCHAR(200)    NOT NULL,
        [DatabaseName]   NVARCHAR(200)    NOT NULL,
        [WindowsAuth]    CHAR(1)          NOT NULL CONSTRAINT [DF_zSCP2_IBB_WinAuth]  DEFAULT ('N'),
        [DbUser]         NVARCHAR(100)    NOT NULL CONSTRAINT [DF_zSCP2_IBB_DbUser]   DEFAULT (''),
        [DbPasswordEnc]  NVARCHAR(500)    NOT NULL CONSTRAINT [DF_zSCP2_IBB_Pwd]      DEFAULT (''),
        -- Learned on a successful test. NULL means "never reached it yet" -- and until it is known,
        -- nothing can be linked, because a link with no source book is a link to nowhere.
        [RemoteBookId]   UNIQUEIDENTIFIER NULL,
        [RemoteCompany]  NVARCHAR(200)    NOT NULL CONSTRAINT [DF_zSCP2_IBB_Company]  DEFAULT (''),
        [Inactive]       CHAR(1)          NOT NULL CONSTRAINT [DF_zSCP2_IBB_Inactive] DEFAULT ('N'),
        [LastTestedAt]   DATETIME         NULL,
        [LastTestResult] NVARCHAR(400)    NOT NULL CONSTRAINT [DF_zSCP2_IBB_Result]   DEFAULT (''),
        [CreatedAt]      DATETIME         NOT NULL CONSTRAINT [DF_zSCP2_IBB_Created]  DEFAULT (GETDATE()),
        [LastModified]   DATETIME         NOT NULL CONSTRAINT [DF_zSCP2_IBB_Modified] DEFAULT (GETDATE()),
        CONSTRAINT [PK_zSCP2_InterBillBook] PRIMARY KEY CLUSTERED ([BookKey])
    );

    CREATE UNIQUE INDEX [UX_zSCP2_InterBillBook_Alias] ON [dbo].[zSCP2_InterBillBook] ([Alias]);
END
GO

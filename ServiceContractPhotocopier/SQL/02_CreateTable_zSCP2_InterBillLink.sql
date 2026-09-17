SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- What in this book came from the other book, and what it looked like when it was taken.
--
-- One row per linked thing. The link is the whole module: three of its rules do nothing without it.
--
--   "a machine was added over there"        -> in the other book, no link row here
--   "that counter may not be deleted"       -> this counter HAS a link row
--   "the invoice this came from was cancelled" -> the invoice's link row, told
--
-- The rule the design hangs on is that this is NOT a copy. The contract here is this book's own
-- contract, with this book's debtor, this book's prices, this book's bill groups; it merely shares
-- the other book's machines and readings. So the link stores the minimum that makes the two rows
-- findable from each other, and nothing else.
--
-- SourceKey holds the other book's KEY -- ContractKey, ItemKey, ItemMeterKey, DocKey -- and never
-- its NUMBER. Contract numbers and document numbers are edited, reused and duplicated across books;
-- a surrogate key cannot move. SourceRef keeps the number too, but only so a human reading this
-- table can tell what they are looking at. Nothing matches on it.
--
-- TakenSnapshot is what the other book said at the moment it was taken -- 'RENT=450.00;BK=0.0250;
-- CL=0.2500;ACTIVE=Y'. Without it there is no way to tell "they changed this" from "it was always
-- like that", and the "changed since you took it" list cannot exist. With it the list is a set
-- comparison and nothing more.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[zSCP2_InterBillLink]'))
BEGIN
    CREATE TABLE [dbo].[zSCP2_InterBillLink] (
        [LinkKey]         BIGINT           IDENTITY(1,1) NOT NULL,
        -- CONTRACT / ITEM / METER / INVOICE. Four kinds, one table: they are asked the same three
        -- questions and splitting them would only mean writing the same query four times.
        [EntityType]      VARCHAR(10)      NOT NULL,
        -- The row here that this link belongs to. ZERO is legitimate and means one thing: the other
        -- book has something this book was OFFERED and turned down. Nothing local exists, and the
        -- row is here so the offer is not made again every month.
        [LocalKey]        BIGINT           NOT NULL,
        -- The contract in THIS book that every link belongs to; a contract's own link points at
        -- itself. It is what scopes "show me everything taken for this deal" to one query.
        [RootContractKey] BIGINT           NOT NULL,
        [SourceBookId]    UNIQUEIDENTIFIER NOT NULL,
        [SourceKey]       BIGINT           NOT NULL,
        [SourceRef]       NVARCHAR(60)     NOT NULL CONSTRAINT [DF_zSCP2_IBL_Ref]      DEFAULT (''),
        [TakenAt]         DATETIME         NOT NULL CONSTRAINT [DF_zSCP2_IBL_TakenAt]  DEFAULT (GETDATE()),
        [TakenSnapshot]   NVARCHAR(MAX)    NOT NULL CONSTRAINT [DF_zSCP2_IBL_Snap]     DEFAULT (''),
        -- What this book has been TOLD about the source since. LIVE, GONE (withdrawn over there),
        -- CANCELLED (an invoice cancelled over there). Being told is as far as it goes: what to do
        -- about it is this book's decision, because this book's document is a real document to a
        -- real customer and may already be paid.
        -- ...and DECLINED, which goes with LocalKey = 0: offered and refused.
        [Status]          VARCHAR(10)      NOT NULL CONSTRAINT [DF_zSCP2_IBL_Status]   DEFAULT ('LIVE'),
        [NoticeAt]        DATETIME         NULL,
        [NoticeText]      NVARCHAR(400)    NOT NULL CONSTRAINT [DF_zSCP2_IBL_Notice]   DEFAULT (''),
        CONSTRAINT [PK_zSCP2_InterBillLink] PRIMARY KEY CLUSTERED ([LinkKey])
    );

    -- A row in this book has at most ONE source. This is the index the "may not be deleted" check
    -- hits on every counter, every save, so it has to be the fast one.
    --
    -- Filtered past zero, because zero is not a row: it is the marker for something offered and
    -- turned down, and there can be many of those.
    CREATE UNIQUE INDEX [UX_zSCP2_InterBillLink_Local]
        ON [dbo].[zSCP2_InterBillLink] ([EntityType], [LocalKey]) WHERE [LocalKey] > 0;

    -- Deliberately NOT unique the other way. Two contracts here may legitimately take from the same
    -- contract over there -- one parent contract whose machines were sold on to two end customers is
    -- a real arrangement, and a unique constraint would forbid it for no reason.
    CREATE INDEX [IX_zSCP2_InterBillLink_Source]
        ON [dbo].[zSCP2_InterBillLink] ([SourceBookId], [EntityType], [SourceKey]);

    CREATE INDEX [IX_zSCP2_InterBillLink_Root]
        ON [dbo].[zSCP2_InterBillLink] ([RootContractKey]);
END
GO

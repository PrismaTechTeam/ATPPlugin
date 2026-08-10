SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* Bulk email as a JOB rather than a modal dialog (customer 10/08).
   The job outlives the form: closing the window must not kill a send that is
   half way through a customer list. */
IF OBJECT_ID('dbo.zSCP2_EmailJob') IS NULL
CREATE TABLE dbo.zSCP2_EmailJob (
    JobKey        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_zSCP2_EmailJob PRIMARY KEY,
    CreatedAt     DATETIME      NOT NULL CONSTRAINT DF_zSCP2_EmailJob_CreatedAt DEFAULT (GETDATE()),
    CreatedBy     NVARCHAR(60)  NULL,
    MachineName   NVARCHAR(80)  NULL,          -- who started it, for the "another user is sending" message
    Status        VARCHAR(12)   NOT NULL CONSTRAINT DF_zSCP2_EmailJob_Status DEFAULT ('RUNNING'),
                                               -- RUNNING / DONE / ABORTED
    TotalCount    INT           NOT NULL CONSTRAINT DF_zSCP2_EmailJob_Total DEFAULT (0),
    SuccessCount  INT           NOT NULL CONSTRAINT DF_zSCP2_EmailJob_Success DEFAULT (0),
    FailedCount   INT           NOT NULL CONSTRAINT DF_zSCP2_EmailJob_Failed DEFAULT (0),
    SkippedCount  INT           NOT NULL CONSTRAINT DF_zSCP2_EmailJob_Skipped DEFAULT (0),
    FinishedAt    DATETIME      NULL,
    LastHeartbeat DATETIME      NULL           -- a job whose heartbeat went stale is presumed dead
);
GO

/* One row per RECIPIENT (a customer's whole invoice set is one email), so the
   progress list reads the way the confirmation screen does. */
IF OBJECT_ID('dbo.zSCP2_EmailJobItem') IS NULL
CREATE TABLE dbo.zSCP2_EmailJobItem (
    ItemKey     BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_zSCP2_EmailJobItem PRIMARY KEY,
    JobKey      BIGINT        NOT NULL,
    DebtorCode  NVARCHAR(30)  NULL,
    DebtorName  NVARCHAR(200) NULL,
    Email       NVARCHAR(200) NULL,
    DocNos      NVARCHAR(900) NULL,            -- the invoice numbers in this one email
    DocKeys     NVARCHAR(900) NULL,            -- comma list, so the log rows can be written on success
    Status      VARCHAR(12)   NOT NULL CONSTRAINT DF_zSCP2_EmailJobItem_Status DEFAULT ('PENDING'),
                                               -- PENDING / SENT / FAILED / SKIPPED
    ErrorMsg    NVARCHAR(500) NULL,
    SentAt      DATETIME      NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_zSCP2_EmailJobItem_Job')
    CREATE INDEX IX_zSCP2_EmailJobItem_Job ON dbo.zSCP2_EmailJobItem (JobKey, ItemKey);
GO

/* Distributed lock, one row per INVOICE. Taking the lock is an INSERT: the
   primary key does the arbitration, so two users on two PCs cannot both grab the
   same invoice no matter how the timing falls. Released on completion; a stale
   row (owner died mid-run) is taken over after the timeout, otherwise a crash
   would block that invoice forever. */
IF OBJECT_ID('dbo.zSCP2_EmailLock') IS NULL
CREATE TABLE dbo.zSCP2_EmailLock (
    DocKey      BIGINT        NOT NULL CONSTRAINT PK_zSCP2_EmailLock PRIMARY KEY,
    JobKey      BIGINT        NULL,
    LockedBy    NVARCHAR(60)  NULL,
    MachineName NVARCHAR(80)  NULL,
    LockedAt    DATETIME      NOT NULL CONSTRAINT DF_zSCP2_EmailLock_LockedAt DEFAULT (GETDATE())
);
GO

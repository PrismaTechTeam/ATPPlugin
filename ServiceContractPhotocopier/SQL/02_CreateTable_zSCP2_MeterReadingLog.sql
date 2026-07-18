-- APPEND-ONLY meter reading audit log: every reading event (manual key-in, API fetch accepted,
-- invoice billed, invoice deleted, reading cleared) is a new row. Rows are NEVER updated or deleted
-- by the application, so the full history (previous, previous-previous, ...) is always reconstructable
-- even after invoices are deleted or machines are removed (no FK on purpose — audit outlives masters).
IF OBJECT_ID('dbo.zSCP2_MeterReadingLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.zSCP2_MeterReadingLog (
        LogKey        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_zSCP2_MeterReadingLog PRIMARY KEY,
        ItemMeterKey  BIGINT        NOT NULL,
        PeriodYear    INT           NOT NULL CONSTRAINT DF_zSCP2MRL_Year DEFAULT(0),
        PeriodMonth   INT           NOT NULL CONSTRAINT DF_zSCP2MRL_Month DEFAULT(0),
        Reading       DECIMAL(18,2) NOT NULL CONSTRAINT DF_zSCP2MRL_Reading DEFAULT(0),
        ReadingDate   DATETIME      NULL,
        Source        NVARCHAR(20)  NOT NULL CONSTRAINT DF_zSCP2MRL_Source DEFAULT(''),   -- MANUAL/ONLINE/OFFLINE/INVOICE/INVOICE-DELETED/CLEARED
        DocNo         NVARCHAR(40)  NOT NULL CONSTRAINT DF_zSCP2MRL_DocNo DEFAULT(''),
        CreatedAt     DATETIME      NOT NULL CONSTRAINT DF_zSCP2MRL_CreatedAt DEFAULT(GETDATE()),
        CreatedBy     NVARCHAR(60)  NOT NULL CONSTRAINT DF_zSCP2MRL_CreatedBy DEFAULT('')
    );
    CREATE INDEX IX_zSCP2_MeterReadingLog_Meter ON dbo.zSCP2_MeterReadingLog(ItemMeterKey, CreatedAt);
END
GO

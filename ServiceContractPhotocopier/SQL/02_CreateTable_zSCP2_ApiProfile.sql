-- API connection profiles for the Meter Reading integration (Plugin Option > API tab).
-- One row per environment (Production / Local Mock / staging / ...). Switching the active profile
-- copies its values into Z_PumsConfig (METER_API_BASE_URL / METER_API_KEY / METER_API_MODE /
-- METER_API_TIMEOUT_MS), so the API client code reads config exactly as before.
IF OBJECT_ID('dbo.zSCP2_ApiProfile', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.zSCP2_ApiProfile (
        ProfileName  NVARCHAR(50)  NOT NULL CONSTRAINT PK_zSCP2_ApiProfile PRIMARY KEY,
        BaseUrl      NVARCHAR(200) NOT NULL CONSTRAINT DF_zSCP2AP_Url DEFAULT(''),
        Token        NVARCHAR(200) NOT NULL CONSTRAINT DF_zSCP2AP_Token DEFAULT(''),
        Mode         NVARCHAR(10)  NOT NULL CONSTRAINT DF_zSCP2AP_Mode DEFAULT('LIVE'),   -- MOCK / LIVE
        TimeoutMs    INT           NOT NULL CONSTRAINT DF_zSCP2AP_Timeout DEFAULT(15000),
        LastModified DATETIME      NOT NULL CONSTRAINT DF_zSCP2AP_LM DEFAULT(GETDATE())
    );
    INSERT INTO dbo.zSCP2_ApiProfile (ProfileName, BaseUrl, Token, Mode, TimeoutMs) VALUES
        (N'Production', N'https://atgroup.asia', N'', N'LIVE', 15000),
        (N'Local Mock', N'http://localhost:8090', N'', N'MOCK', 15000);
END
GO

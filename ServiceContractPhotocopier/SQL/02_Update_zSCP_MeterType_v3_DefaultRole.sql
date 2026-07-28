SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v3: per-type DEFAULT ROLE (user decision 2026-07-27). Picking a meter type on a machine
-- auto-fills the row's Role from here, so a rental type reads RENTAL, a waive type WAIVE and a
-- committed-minimum type COMMIT at a glance (BK/CL/NA are also valid). '' = infer from the name.
IF COL_LENGTH('dbo.zSCP_MeterType', 'DefaultRole') IS NULL
    ALTER TABLE dbo.zSCP_MeterType ADD DefaultRole VARCHAR(10) NOT NULL CONSTRAINT DF_zSCP_MeterType_DefaultRole DEFAULT('');
GO
UPDATE dbo.zSCP_MeterType SET DefaultRole = 'WAIVE'
WHERE DefaultRole = '' AND ISNULL(IsRentalWaive,'N') = 'Y';
GO
UPDATE dbo.zSCP_MeterType SET DefaultRole = 'COMMIT'
WHERE DefaultRole = '' AND (MeterTypeCode LIKE 'MIN %' OR MeterTypeCode LIKE 'MIN-%' OR MeterTypeCode LIKE 'MIN.%' OR MeterTypeCode = 'MIN');
GO
UPDATE dbo.zSCP_MeterType SET DefaultRole = 'RENTAL'
WHERE DefaultRole = '' AND ISNULL(IsFlatCharge,'N') = 'Y';
GO

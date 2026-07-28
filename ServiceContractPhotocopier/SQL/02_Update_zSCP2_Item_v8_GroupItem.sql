SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v8: GROUP item flag (user decision 2026-07-27 — the master's ".C" combined-machine concept,
-- now engine-driven). A contract can carry ONE invisible "group machine" whose meters charge
-- against the WHOLE fleet: a group MIN tops up to the SUM of every machine's scoped BK/CL
-- charges, a group WAIVE fires on the fleet total, a group RENTAL bills one rent for all.
-- Group items are hidden from the machine grid and the Maintain Service Item list; they show in
-- Meter Reading Integration like any machine (their flat lines must be billable).
-- Legacy migrated ".C" items are NOT backfilled — their combined readings are keyed manually.
IF COL_LENGTH('dbo.zSCP2_Item', 'IsGroupItem') IS NULL
    ALTER TABLE dbo.zSCP2_Item ADD IsGroupItem CHAR(1) NOT NULL CONSTRAINT DF_zSCP2_Item_IsGroupItem DEFAULT('N');
GO

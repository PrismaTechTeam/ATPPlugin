-- Multi-machine CSSI: a meter can belong to a specific provided unit (Item Provided row), identified
-- by that unit's serial. MachineSerialNo = '' means the meter belongs to the CSSI's own header machine
-- (classic single-machine mode). zSCP2_Item.MultiMachine = the editor checkbox state.
-- The meter identity therefore widens from (ItemKey, MeterTypeCode) to
-- (ItemKey, MeterTypeCode, MachineSerialNo) — two machines under one CSSI may use the same meter type.
-- All idempotent.
SET NOCOUNT ON;

IF COL_LENGTH('dbo.zSCP2_ItemMeter','MachineSerialNo') IS NULL
    ALTER TABLE dbo.zSCP2_ItemMeter ADD [MachineSerialNo] NVARCHAR(100) NOT NULL CONSTRAINT DF_zSCP2IM_MachineSerial DEFAULT('');

IF COL_LENGTH('dbo.zSCP2_Item','MultiMachine') IS NULL
    ALTER TABLE dbo.zSCP2_Item ADD [MultiMachine] CHAR(1) NOT NULL CONSTRAINT DF_zSCP2I_MultiMachine DEFAULT('N');
GO

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_zSCP2_ItemMeter' AND parent_object_id = OBJECT_ID('dbo.zSCP2_ItemMeter'))
    ALTER TABLE dbo.zSCP2_ItemMeter DROP CONSTRAINT UQ_zSCP2_ItemMeter;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_zSCP2_ItemMeter_Machine' AND object_id = OBJECT_ID('dbo.zSCP2_ItemMeter'))
    CREATE UNIQUE INDEX UQ_zSCP2_ItemMeter_Machine ON dbo.zSCP2_ItemMeter(ItemKey, MeterTypeCode, MachineSerialNo);
GO

-- The per-role one-BK/one-CL filtered indexes must also widen to per MACHINE (ItemKey alone would
-- reject a second machine's black meter under the same CSSI). Old single-column shape is detected by
-- its key column count and rebuilt.
IF EXISTS (SELECT 1 FROM sys.indexes i
           WHERE i.name = 'UX_zSCP2_ItemMeter_BK' AND i.object_id = OBJECT_ID('dbo.zSCP2_ItemMeter')
             AND (SELECT COUNT(*) FROM sys.index_columns ic
                  WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0) = 1)
    DROP INDEX [UX_zSCP2_ItemMeter_BK] ON dbo.zSCP2_ItemMeter;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_zSCP2_ItemMeter_BK' AND object_id = OBJECT_ID('dbo.zSCP2_ItemMeter'))
    CREATE UNIQUE INDEX [UX_zSCP2_ItemMeter_BK] ON dbo.zSCP2_ItemMeter([ItemKey], [MachineSerialNo]) WHERE [MeterRole] = 'BK';

IF EXISTS (SELECT 1 FROM sys.indexes i
           WHERE i.name = 'UX_zSCP2_ItemMeter_CL' AND i.object_id = OBJECT_ID('dbo.zSCP2_ItemMeter')
             AND (SELECT COUNT(*) FROM sys.index_columns ic
                  WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0) = 1)
    DROP INDEX [UX_zSCP2_ItemMeter_CL] ON dbo.zSCP2_ItemMeter;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_zSCP2_ItemMeter_CL' AND object_id = OBJECT_ID('dbo.zSCP2_ItemMeter'))
    CREATE UNIQUE INDEX [UX_zSCP2_ItemMeter_CL] ON dbo.zSCP2_ItemMeter([ItemKey], [MachineSerialNo]) WHERE [MeterRole] = 'CL';
GO

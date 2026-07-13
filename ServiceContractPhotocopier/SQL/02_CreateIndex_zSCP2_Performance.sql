SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Performance indexes for the Maintain Service Contract / Maintain Service Item lists and the
-- Meter Reading load. Idempotent (guarded by sys.indexes) so it is safe to run on every plugin
-- load: a first install gets them, an existing book gets any that are still missing.
-- Registered via RunDDL in ScpMigrations_Cls.

-- 1) Service Item list: the BK/CL meter join (bk/cl.ItemKey = i.ItemKey AND MeterRole = 'BK'/'CL').
--    Covers the join key AND outputs MeterTypeCode, so the 3k-row list needs no key lookups.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_zSCP2_ItemMeter_RoleCover'
              AND object_id = OBJECT_ID('dbo.zSCP2_ItemMeter'))
    CREATE NONCLUSTERED INDEX [IX_zSCP2_ItemMeter_RoleCover]
        ON [dbo].[zSCP2_ItemMeter]([ItemKey], [MeterRole]) INCLUDE ([MeterTypeCode]);
GO

-- 2) Service Item list ordering + covering: the list is ORDER BY ServiceItemNo and reads several
--    item columns. This lets the sort come straight from the index with no lookup per row.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_zSCP2_Item_ListCover'
              AND object_id = OBJECT_ID('dbo.zSCP2_Item'))
    CREATE NONCLUSTERED INDEX [IX_zSCP2_Item_ListCover]
        ON [dbo].[zSCP2_Item]([ServiceItemNo])
        INCLUDE ([ContractKey], [SerialNumber], [BillingDayOverride], [ServiceExpiryDate], [Inactive]);
GO

-- 3) Contract list view (zvSCP2_ContractList): ItemCount subquery counts items per contract.
--    IX_zSCP2_Item_Contract already covers this; ensure it exists on older books just in case.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_zSCP2_Item_Contract'
              AND object_id = OBJECT_ID('dbo.zSCP2_Item'))
    CREATE NONCLUSTERED INDEX [IX_zSCP2_Item_Contract]
        ON [dbo].[zSCP2_Item]([ContractKey]);
GO

-- 4) Contract list ordering: the list is ORDER BY ContractNo. UQ_zSCP2_Contract_No already covers
--    the key; add the columns the list reads so the grid load is a single ordered scan.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_zSCP2_Contract_ListCover'
              AND object_id = OBJECT_ID('dbo.zSCP2_Contract'))
    CREATE NONCLUSTERED INDEX [IX_zSCP2_Contract_ListCover]
        ON [dbo].[zSCP2_Contract]([ContractNo])
        INCLUDE ([DebtorCode], [ContractDate], [ServiceStartDate], [ServiceExpiryDate],
                 [ContractValue], [BillingDay], [BillingMode], [Inactive]);
GO

-- 5) Meter Reading: the "last reading before period" lookup scans zSCP_MeterTrans by service item
--    and date. IX_zSCP_MeterTrans_SIMT already covers the meter-type path; add a service-item +
--    date covering index for the by-item path used when prefilling the grid.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_zSCP_MeterTrans_ItemDate'
              AND object_id = OBJECT_ID('dbo.zSCP_MeterTrans'))
    CREATE NONCLUSTERED INDEX [IX_zSCP_MeterTrans_ItemDate]
        ON [dbo].[zSCP_MeterTrans]([ServiceItemKey], [MeterTransDate] DESC)
        INCLUDE ([MeterTransReading]);
GO

-- 6) Meter staging prefill: reads all staged rows for a billing period (filter by year+month) and
--    needs each row's reading + invoiced stamp. Cover the period-path so prefill is one seek + no
--    lookups. (UQ_zSCP2_MeterEntry_Period seeks by ItemMeterKey; this one serves the period scan.)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_zSCP2_MeterEntry_PeriodCover'
              AND object_id = OBJECT_ID('dbo.zSCP2_MeterEntry'))
    CREATE NONCLUSTERED INDEX [IX_zSCP2_MeterEntry_PeriodCover]
        ON [dbo].[zSCP2_MeterEntry]([PeriodYear], [PeriodMonth])
        INCLUDE ([ItemMeterKey], [CurrentReading], [ReadingDate], [Source], [InvoicedDocNo], [InvoicedAt]);
GO

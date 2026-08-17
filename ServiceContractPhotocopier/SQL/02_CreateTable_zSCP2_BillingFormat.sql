SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Billing Format — the named preset a contract points at, instead of every contract carrying its
-- own copy of the same three answers.
--
-- The 22 July-2026 invoices look like 21 bespoke arrangements but are three independent choices:
--   InvoiceSplit    how many invoices          ONE / RS (rental separate) / PM / PMS
--   RentalLineMode  how rental lines group     A across model / M same model / S per machine
--   MeterLineMode   how BK & CL lines group    A / M / S
-- 19 combinations are reachable; 11 are in use. 04_Seed_zSCP2_BillingFormat.sql loads those 11.
--
-- Picking a format on a contract COPIES its values into the contract's own columns (BillingMode,
-- RentalSeparateInvoice, RentalLineMode, MeterLineMode). The engine only ever reads the contract,
-- so nothing in the billing path has to know this table exists, and a contract can still be edited
-- away from its format without dragging every sibling with it.
CREATE TABLE [dbo].[zSCP2_BillingFormat](
	[BillingFormatKey]   [bigint] IDENTITY(1,1) NOT NULL,
	[FormatCode]         [nvarchar](20)  NOT NULL,
	[FormatName]         [nvarchar](100) NOT NULL DEFAULT(''),
	-- ONE = one invoice for everything
	-- RS  = rental on its own invoice, meters on another   (BillingMode G + RentalSeparateInvoice Y)
	-- PM  = one invoice per machine, rental and meters together          (BillingMode S)
	-- PMS = one invoice per machine, rental separate from meters         (BillingMode S + RS)
	[InvoiceSplit]       [varchar](3)    NOT NULL DEFAULT('ONE'),
	[RentalLineMode]     [char](1)       NOT NULL DEFAULT('A'),
	[MeterLineMode]      [char](1)       NOT NULL DEFAULT('S'),
	-- What a merged BK/CL line prints under itself. NULL = follow the company default.
	--   S = summed readings + the serial list (what every issued invoice does today)
	--   R = one row per machine
	[ReadingText]        [char](1)       NULL,
	-- NULL = company default template. Tokens are resolved at invoice time.
	[RentalDescTemplate] [nvarchar](max) NULL,
	[MeterDescTemplate]  [nvarchar](max) NULL,
	[Remark]             [nvarchar](200) NOT NULL DEFAULT(''),
	[Inactive]           [char](1)       NOT NULL DEFAULT('N'),
	[LastModified]       [datetime2](0)  NOT NULL DEFAULT(GETDATE()),
 CONSTRAINT [PK_zSCP2_BillingFormat]        PRIMARY KEY CLUSTERED ([BillingFormatKey] ASC),
 CONSTRAINT [UQ_zSCP2_BillingFormat_Code]   UNIQUE NONCLUSTERED ([FormatCode] ASC),
 CONSTRAINT [CK_zSCP2_BillingFormat_Split]  CHECK ([InvoiceSplit] IN ('ONE','RS','PM','PMS')),
 CONSTRAINT [CK_zSCP2_BillingFormat_Rental] CHECK ([RentalLineMode] IN ('A','M','S')),
 CONSTRAINT [CK_zSCP2_BillingFormat_Meter]  CHECK ([MeterLineMode] IN ('A','M','S')),
 CONSTRAINT [CK_zSCP2_BillingFormat_Read]   CHECK ([ReadingText] IS NULL OR [ReadingText] IN ('S','R')),
 CONSTRAINT [CK_zSCP2_BillingFormat_Inact]  CHECK ([Inactive] IN ('Y','N'))
) ON [PRIMARY]
GO

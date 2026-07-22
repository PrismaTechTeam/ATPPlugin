SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Only insert defaults if the target table is empty so re-running is safe.

IF NOT EXISTS (SELECT 1 FROM [dbo].[zSCP_LK_ServiceStatus])
BEGIN
	INSERT INTO [dbo].[zSCP_LK_ServiceStatus] ([ServiceStatusCode], [Description]) VALUES
		(N'OPEN',        N'Open / Not yet attended'),
		(N'IN-PROGRESS', N'In progress'),
		(N'PENDING',     N'Pending customer / parts'),
		(N'CLOSED',      N'Closed'),
		(N'CANCELLED',   N'Cancelled');
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[zSCP_LK_ServiceSeverity])
BEGIN
	INSERT INTO [dbo].[zSCP_LK_ServiceSeverity] ([ServiceSeverityCode], [Description]) VALUES
		(N'LOW',      N'Low'),
		(N'MEDIUM',   N'Medium'),
		(N'HIGH',     N'High'),
		(N'CRITICAL', N'Critical');
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[zSCP_LK_AppointmentPriority])
BEGIN
	INSERT INTO [dbo].[zSCP_LK_AppointmentPriority] ([AppointmentPriorityCode], [Description]) VALUES
		(N'LOW',    N'Low'),
		(N'NORMAL', N'Normal'),
		(N'HIGH',   N'High'),
		(N'URGENT', N'Urgent');
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[zSCP_LK_AppointmentType])
BEGIN
	INSERT INTO [dbo].[zSCP_LK_AppointmentType] ([AppointmentTypeCode], [Description]) VALUES
		(N'SERVICE',    N'Service appointment'),
		(N'DELIVERY',   N'Delivery appointment'),
		(N'COLLECTION', N'Collection appointment'),
		(N'MEETING',    N'Meeting');
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[zSCP_LK_ServiceContractType])
BEGIN
	INSERT INTO [dbo].[zSCP_LK_ServiceContractType] ([ServiceContractTypeCode], [Description]) VALUES
		(N'RA',    N'Rental of machine'),
		(N'MR',    N'Meter reading'),
		(N'RA+MR', N'Rental + meter reading');
END
GO

-- Service Type (per service item) mirrors the Contract Type list. Cloned from Contract Type so the
-- two stay identical on a fresh install; maintained independently afterwards (General Setup -> Service Type).
INSERT INTO [dbo].[zSCP_LK_ServiceType] ([ServiceTypeCode], [Description], [Inactive], [LastModified])
SELECT ct.[ServiceContractTypeCode], ct.[Description], ct.[Inactive], GETDATE()
FROM [dbo].[zSCP_LK_ServiceContractType] ct
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[zSCP_LK_ServiceType] st WHERE st.[ServiceTypeCode] = ct.[ServiceContractTypeCode]);
GO

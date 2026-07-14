SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- List view for the combined Service Contract module v2 (zSCP2_ContractLst_Form).
CREATE VIEW [dbo].[zvSCP2_ContractList]
AS
SELECT
	c.ContractKey,
	c.ContractNo,
	c.ContractTypeCode,
	c.DebtorCode,
	ISNULL(d.CompanyName, '') AS DebtorName,
	c.ContractDate,
	c.ServiceStartDate,
	c.ServiceExpiryDate,
	c.ContractValue,
	c.BillingDay,
	CASE WHEN ISNULL(c.BillOnMonthEnd,'N') = 'Y' THEN 'Y' ELSE 'N' END AS BillOnMonthEnd,
	c.BillingMode,
	ISNULL(c.StaffCode, '')  AS Agent,
	ISNULL(c.AreaCode, '')   AS Area,
	ISNULL(c.Description, '') AS Description,
	c.Inactive,
	(SELECT COUNT(*) FROM dbo.zSCP2_Item i WHERE i.ContractKey = c.ContractKey) AS ItemCount
FROM dbo.zSCP2_Contract c
LEFT JOIN dbo.Debtor d ON d.AccNo = c.DebtorCode
GO

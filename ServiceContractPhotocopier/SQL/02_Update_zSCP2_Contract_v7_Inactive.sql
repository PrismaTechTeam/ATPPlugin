-- Inactive metadata: WHEN the contract was deactivated and WHY. Set when the Inactive checkbox
-- is turned on (date = that day, reason prompted from the operator); both cleared when the
-- contract is reactivated. Display-only for billing (billing keys off Inactive itself).
IF COL_LENGTH('dbo.zSCP2_Contract', 'InactiveDate') IS NULL
    ALTER TABLE [dbo].[zSCP2_Contract] ADD [InactiveDate] [date] NULL;
IF COL_LENGTH('dbo.zSCP2_Contract', 'InactiveReason') IS NULL
    ALTER TABLE [dbo].[zSCP2_Contract] ADD [InactiveReason] [nvarchar](200) NULL;

-- Contract-level Department + Project (AutoCount native Dept / Project masters). Idempotent.
SET NOCOUNT ON;
IF COL_LENGTH('dbo.zSCP2_Contract','DeptNo') IS NULL
    ALTER TABLE dbo.zSCP2_Contract ADD [DeptNo] NVARCHAR(40) NOT NULL CONSTRAINT DF_zSCP2C_DeptNo DEFAULT('');
IF COL_LENGTH('dbo.zSCP2_Contract','ProjNo') IS NULL
    ALTER TABLE dbo.zSCP2_Contract ADD [ProjNo] NVARCHAR(40) NOT NULL CONSTRAINT DF_zSCP2C_ProjNo DEFAULT('');
GO

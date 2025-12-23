-- Step 1: Add ID column with IDENTITY (auto-increment)
ALTER TABLE [dbo].[keplero_compare]
ADD [Id] INT IDENTITY(1,1) NOT NULL;
GO

-- Step 2: Add Primary Key constraint
ALTER TABLE [dbo].[keplero_compare]
ADD CONSTRAINT PK_keplero_compare PRIMARY KEY CLUSTERED ([Id]);
GO

-- Step 3: Verify the table structure
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'keplero_compare'
ORDER BY ORDINAL_POSITION;
GO

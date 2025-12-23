-- Create table with primary key
CREATE TABLE [dbo].[keplero_compare](
	[Id] [int] IDENTITY(1,1) PRIMARY KEY NOT NULL,  -- Auto-increment primary key
	[ItemId] [int] NULL,
	[Protocollo] [nvarchar](250) NULL,
	[Coda] [nvarchar](100) NULL,
	[StatoPratica] [nvarchar](250) NULL,
	[Stato] [nvarchar](250) NULL,
	[Esito] [nvarchar](250) NULL,
	[DataEsito] [datetime] NULL,
	[StatoPratica_Keplero] [nvarchar](100) NOT NULL,
	[Riga] [int] NULL
) ON [PRIMARY]
GO

-- Copy data from original table (if exists)
-- INSERT INTO [dbo].[keplero_compare] (ItemId, Protocollo, Coda, StatoPratica, Stato, Esito, DataEsito, StatoPratica_Keplero, Riga)
-- SELECT ItemId, Protocollo, Coda, StatoPratica, Stato, Esito, DataEsito, StatoPratica_Keplero, Riga
-- FROM [dbo].[YourOriginalTableName]
-- GO

-- Create indexes for better performance
CREATE INDEX IX_keplero_compare_ItemId ON [dbo].[keplero_compare](ItemId)
CREATE INDEX IX_keplero_compare_Protocollo ON [dbo].[keplero_compare](Protocollo)
CREATE INDEX IX_keplero_compare_DataEsito ON [dbo].[keplero_compare](DataEsito)
GO

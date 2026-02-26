IF OBJECT_ID('dbo.ScanTransactions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ScanTransactions (
      Id uniqueidentifier NOT NULL PRIMARY KEY,
      ScanAt datetime2 NOT NULL,
      Plant nvarchar(10) NOT NULL,
      QrRawText nvarchar(max) NOT NULL,
      PartNum nvarchar(50) NULL,
      LotNum nvarchar(100) NULL,
      ExpireDate date NULL,
      ReceiveDate date NULL,
      QtyLabel decimal(18,3) NOT NULL,
      UomLabel nvarchar(20) NULL,
      QtyOnHand decimal(18,3) NULL,
      ValidationStatus nvarchar(10) NOT NULL,
      ValidationMessage nvarchar(255) NOT NULL,
      EpicorResponseJson nvarchar(max) NULL
    );
    CREATE INDEX IX_ScanTransactions_ScanAt ON dbo.ScanTransactions(ScanAt DESC);
END
GO

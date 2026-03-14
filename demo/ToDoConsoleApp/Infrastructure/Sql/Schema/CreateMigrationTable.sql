-- ====================================================
-- Migration Metadata Table
-- ====================================================
IF NOT EXISTS (
    SELECT * FROM sys.tables WHERE name = 'Migrations'
)
BEGIN
CREATE TABLE dbo.Migrations (
    Id NVARCHAR(200) NOT NULL PRIMARY KEY,
    AppliedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Hash NVARCHAR(64) NULL -- optional: store hash of script contents
);
END

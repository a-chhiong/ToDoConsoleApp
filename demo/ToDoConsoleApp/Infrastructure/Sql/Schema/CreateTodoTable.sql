-- ====================================================
-- Create Todos Table
-- ====================================================
-- This script creates the base schema for the Todo application
-- Run this once during initial database setup

IF NOT EXISTS (
   SELECT * FROM sys.objects 
   WHERE object_id = OBJECT_ID(N'[dbo].[Todos]') AND type in (N'U')
)
BEGIN
    PRINT 'Creating Todos table...';
    
    CREATE TABLE [dbo].[Todos]
    (
        [Id] INT PRIMARY KEY IDENTITY(1,1),
        [Title] NVARCHAR(255) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [IsCompleted] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CompletedAt] DATETIME2 NULL
    );

    -- Create index on CreatedAt for better query performance
    CREATE NONCLUSTERED INDEX [IX_Todos_CreatedAt] 
    ON [dbo].[Todos]([CreatedAt] DESC);

    -- Create index on IsCompleted for filtering queries
    CREATE NONCLUSTERED INDEX [IX_Todos_IsCompleted] 
    ON [dbo].[Todos]([IsCompleted])
    INCLUDE ([Title], [CreatedAt]);

    PRINT 'Todos table created successfully.';
END
ELSE
BEGIN
    PRINT 'Todos table already exists.';
END
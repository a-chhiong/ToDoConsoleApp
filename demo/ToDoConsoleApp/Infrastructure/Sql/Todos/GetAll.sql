-- Retrieve all Todo items ordered by creation date (newest first)

SELECT 
    [Id],
    [Title],
    [Description],
    [IsCompleted],
    [CreatedAt],
    [CompletedAt]
FROM [dbo].[Todos]
ORDER BY [CreatedAt] ASC;
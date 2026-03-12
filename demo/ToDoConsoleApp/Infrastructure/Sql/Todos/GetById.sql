-- Retrieve a specific Todo item by ID

SELECT 
    [Id],
    [Title],
    [Description],
    [IsCompleted],
    [CreatedAt],
    [CompletedAt]
FROM [dbo].[Todos]
WHERE [Id] = @Id;
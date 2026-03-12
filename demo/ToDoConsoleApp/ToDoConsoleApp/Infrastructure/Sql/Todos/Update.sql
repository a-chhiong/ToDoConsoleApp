-- Update an existing Todo item

UPDATE [dbo].[Todos]
SET
    [Title] = @Title,
    [Description] = @Description,
    [IsCompleted] = @IsCompleted,
    [CompletedAt] = @CompletedAt
WHERE [Id] = @Id;
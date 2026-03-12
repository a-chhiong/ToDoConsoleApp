-- Delete a Todo item by ID

DELETE FROM [dbo].[Todos]
WHERE [Id] = @Id;
-- Insert a new Todo item
-- Returns the ID of the newly created item

INSERT INTO [dbo].[Todos] (
    [Title],
    [Description],
    [IsCompleted],
    [CreatedAt]
)
VALUES (
    @Title,
    @Description,
    @IsCompleted,
    @CreatedAt
);

SELECT CAST(SCOPE_IDENTITY() AS INT);
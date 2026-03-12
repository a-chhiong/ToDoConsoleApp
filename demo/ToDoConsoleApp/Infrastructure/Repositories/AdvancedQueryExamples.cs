using System.Data;
using Dapper;
using ToDoConsoleApp.Domain.Entities;

namespace ToDoConsoleApp.Infrastructure.Repositories;

/// <summary>
/// Demonstrates advanced Dapper.SqlBuilder usage patterns.
/// These examples show best practices for complex dynamic queries.
/// </summary>
public class AdvancedQueryExamples
{
    private readonly IDbConnection _connection;

    public AdvancedQueryExamples(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Example 1: Complex filtering with multiple optional WHERE clauses.
    /// Shows how SqlBuilder automatically joins WHERE conditions with AND.
    /// </summary>
    public async Task<IEnumerable<TodoItem>> GetComplexFilteredAsync(
        string? titleFilter = null,
        bool? isCompleted = null,
        DateTime? createdAfter = null,
        DateTime? createdBefore = null)
    {
        var builder = new SqlBuilder();
        var template = builder.AddTemplate(
            @"SELECT [Id], [Title], [Description], [IsCompleted], [CreatedAt], [CompletedAt]
              FROM [dbo].[Todos]
              /**where**/
              /**orderby**/");

        // Dynamically add WHERE clauses - each adds an AND automatically
        if (!string.IsNullOrEmpty(titleFilter))
            builder.Where("[Title] LIKE @TitleFilter", new { TitleFilter = $"%{titleFilter}%" });

        if (isCompleted.HasValue)
            builder.Where("[IsCompleted] = @IsCompleted", new { IsCompleted = isCompleted.Value });

        if (createdAfter.HasValue)
            builder.Where("[CreatedAt] >= @CreatedAfter", new { CreatedAfter = createdAfter.Value });

        if (createdBefore.HasValue)
            builder.Where("[CreatedAt] <= @CreatedBefore", new { CreatedBefore = createdBefore.Value });

        builder.OrderBy("[CreatedAt] DESC");

        return await _connection.QueryAsync<TodoItem>(
            template.RawSql,
            template.Parameters);
    }

    /// <summary>
    /// Example 2: Multiple query templates from same builder.
    /// Useful when you need COUNT and data in separate queries.
    /// </summary>
    public async Task<(IEnumerable<TodoItem> Data, int TotalCount)> GetPagedWithCountAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null)
    {
        var builder = new SqlBuilder();

        // Template for counting - simpler query
        var countTemplate = builder.AddTemplate(
            @"SELECT COUNT(*) FROM [dbo].[Todos]
              /**where**/");

        // Template for paging - uses ROW_NUMBER for pagination
        var dataTemplate = builder.AddTemplate(
            @"SELECT X.* FROM (
                SELECT [Id], [Title], [Description], [IsCompleted], [CreatedAt], [CompletedAt],
                       ROW_NUMBER() OVER (/**orderby**/) AS RowNumber
                FROM [dbo].[Todos]
                /**where**/
              ) AS X
              WHERE RowNumber BETWEEN @StartRow AND @EndRow",
            new { StartRow = (pageNumber - 1) * pageSize + 1, EndRow = pageNumber * pageSize });

        // Add shared WHERE clause
        if (!string.IsNullOrEmpty(searchTerm))
            builder.Where("[Title] LIKE @SearchTerm", new { SearchTerm = $"%{searchTerm}%" });

        // Add shared ORDER BY
        builder.OrderBy("[CreatedAt] DESC");

        // Execute both queries
        var count = await _connection.ExecuteScalarAsync<int>(countTemplate.RawSql, countTemplate.Parameters);
        var data = await _connection.QueryAsync<TodoItem>(dataTemplate.RawSql, dataTemplate.Parameters);

        return (data, count);
    }

    /// <summary>
    /// Example 3: UPDATE with SqlBuilder - dynamic SET clauses.
    /// Shows Set() method for UPDATE statements.
    /// </summary>
    public async Task<int> UpdateSelectiveAsync(int id, Dictionary<string, object> updates)
    {
        var builder = new SqlBuilder();
        var template = builder.AddTemplate(
            @"UPDATE [dbo].[Todos]
              /**set**/
              WHERE [Id] = @Id",
            new { Id = id });

        // Dynamically add SET clauses for only provided fields
        if (updates.ContainsKey("Title"))
            builder.Set("[Title] = @Title", new { Title = updates["Title"] });

        if (updates.ContainsKey("Description"))
            builder.Set("[Description] = @Description", new { Description = updates["Description"] });

        if (updates.ContainsKey("IsCompleted"))
            builder.Set("[IsCompleted] = @IsCompleted", new { IsCompleted = updates["IsCompleted"] });

        return await _connection.ExecuteAsync(template.RawSql, template.Parameters);
    }

    /// <summary>
    /// Example 4: UNION queries with SqlBuilder.
    /// Shows Intersect() for combining result sets.
    /// </summary>
    public async Task<IEnumerable<TodoItem>> GetCompletedOrOldAsync(int daysOld = 30)
    {
        var builder = new SqlBuilder();
        var template = builder.AddTemplate(
            @"SELECT [Id], [Title], [Description], [IsCompleted], [CreatedAt], [CompletedAt]
              FROM [dbo].[Todos]
              WHERE [IsCompleted] = 1
              /**intersect**/");

        builder.Intersect(
            @"SELECT [Id], [Title], [Description], [IsCompleted], [CreatedAt], [CompletedAt]
              FROM [dbo].[Todos]
              WHERE [CreatedAt] < @OldDate",
            new { OldDate = DateTime.UtcNow.AddDays(-daysOld) });

        return await _connection.QueryAsync<TodoItem>(template.RawSql, template.Parameters);
    }

    /// <summary>
    /// Example 5: JOIN with SqlBuilder.
    /// Advanced example showing JOIN operations (requires joining with another table).
    /// </summary>
    public async Task<IEnumerable<dynamic>> GetWithJoinAsync(string? categoryFilter = null)
    {
        var builder = new SqlBuilder();
        var template = builder.AddTemplate(
            @"SELECT t.[Id], t.[Title], t.[Description], t.[IsCompleted], t.[CreatedAt]
              FROM [dbo].[Todos] t
              /**innerjoin**/
              /**where**/
              /**orderby**/");

        // Example JOIN (requires additional table in your schema)
        builder.InnerJoin(
            @"INNER JOIN [dbo].[Categories] c ON t.CategoryId = c.Id");

        if (!string.IsNullOrEmpty(categoryFilter))
            builder.Where("c.[Name] = @CategoryName", new { CategoryName = categoryFilter });

        builder.OrderBy("t.[CreatedAt] DESC");

        return await _connection.QueryAsync(template.RawSql, template.Parameters);
    }

    /// <summary>
    /// Example 6: GROUP BY and HAVING with SqlBuilder.
    /// Shows aggregation queries with optional filtering.
    /// </summary>
    public async Task<IEnumerable<dynamic>> GetStatusStatisticsAsync(bool? filterCompleted = null)
    {
        var builder = new SqlBuilder();
        var template = builder.AddTemplate(
            @"SELECT [IsCompleted], COUNT(*) as [Count], MAX([CreatedAt]) as [LatestCreated]
              FROM [dbo].[Todos]
              /**where**/
              /**groupby**/
              /**having**/");

        if (filterCompleted.HasValue)
            builder.Where("[IsCompleted] = @FilterCompleted", new { FilterCompleted = filterCompleted.Value });

        builder.GroupBy("[IsCompleted]");
        builder.Having("COUNT(*) > @MinCount", new { MinCount = 1 });

        return await _connection.QueryAsync(template.RawSql, template.Parameters);
    }

    /// <summary>
    /// Example 7: OR conditions using OrWhere.
    /// Shows how to create OR conditions instead of AND.
    /// </summary>
    public async Task<IEnumerable<TodoItem>> GetUrgentOrCompletedAsync(int urgentThresholdDays = 7)
    {
        var builder = new SqlBuilder();
        var template = builder.AddTemplate(
            @"SELECT [Id], [Title], [Description], [IsCompleted], [CreatedAt], [CompletedAt]
              FROM [dbo].[Todos]
              /**where**/
              /**orderby**/");

        // First WHERE clause (AND)
        builder.Where("[IsCompleted] = 0");

        // OR clause - creates (... OR ...) grouping
        builder.OrWhere(
            @"[IsCompleted] = 1 OR [CreatedAt] < @UrgentDate",
            new { UrgentDate = DateTime.UtcNow.AddDays(-urgentThresholdDays) });

        builder.OrderBy("[CreatedAt] DESC");

        return await _connection.QueryAsync<TodoItem>(template.RawSql, template.Parameters);
    }

    /// <summary>
    /// Example 8: Complex SELECT with dynamic column selection.
    /// Shows Select() method for building SELECT lists dynamically.
    /// </summary>
    public async Task<IEnumerable<dynamic>> SelectColumnsAsync(bool includeDescription = true, bool includeDates = true)
    {
        var builder = new SqlBuilder();
        var template = builder.AddTemplate(
            @"SELECT /**select**/ FROM [dbo].[Todos]
              WHERE [IsCompleted] = 0
              /**orderby**/");

        // Base columns
        builder.Select("[Id], [Title], [IsCompleted]");

        // Optional columns
        if (includeDescription)
            builder.Select("[Description]");

        if (includeDates)
            builder.Select("[CreatedAt], [CompletedAt]");

        builder.OrderBy("[CreatedAt] DESC");

        return await _connection.QueryAsync(template.RawSql, template.Parameters);
    }
}
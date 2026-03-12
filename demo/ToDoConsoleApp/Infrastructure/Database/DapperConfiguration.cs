using Dapper;
using ToDoConsoleApp.Domain.Entities;

namespace ToDoConsoleApp.Infrastructure.Database;

/// <summary>
/// Centralizes all Dapper type mappings and configurations.
/// Should be called once during application startup.
/// </summary>
public static class DapperConfiguration
{
    public static void Configure()
    {
        // Set global command timeout
        SqlMapper.Settings.CommandTimeout = 30;

        // Configure type mapping for TodoItem
        var todoTypeMap = new CustomPropertyTypeMap(
            typeof(TodoItem),
            (type, columnName) => type.GetProperty(
                columnName,
                System.Reflection.BindingFlags.IgnoreCase | 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.Instance
            ));

        SqlMapper.SetTypeMap(typeof(TodoItem), todoTypeMap);
    }
}
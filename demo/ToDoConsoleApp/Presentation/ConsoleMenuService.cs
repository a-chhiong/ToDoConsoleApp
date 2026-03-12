using Microsoft.Extensions.Logging;
using ToDoConsoleApp.Application.DTOs;
using ToDoConsoleApp.Application.Interfaces;

namespace ToDoConsoleApp.Presentation;

/// <summary>
/// Console UI service for displaying interactive menu and handling user interactions.
/// </summary>
public class ConsoleMenuService
{
    private readonly ITodoService _todoService;
    private readonly ILogger<ConsoleMenuService> _logger;

    public ConsoleMenuService(ITodoService todoService, ILogger<ConsoleMenuService> logger)
    {
        _todoService = todoService ?? throw new ArgumentNullException(nameof(todoService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays the main interactive menu loop.
    /// </summary>
    public async Task DisplayMenuAsync()
    {
        bool running = true;
        while (running)
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("                   TODO APPLICATION MENU");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine("1.   📋 View All Todos");
            Console.WriteLine("2.   🔍 Search Todos");
            Console.WriteLine("3.   👁️ View Todo Details");
            Console.WriteLine("4.   ✏️ Create Todo");
            Console.WriteLine("5.   ♻️ Update Todo");
            Console.WriteLine("6.   🗑 Delete Todo");
            Console.WriteLine("7.   📊 Show Statistics");
            Console.WriteLine("8.   🚪 Exit");
            Console.WriteLine(new string('=', 60));
            Console.Write("Select option (1-8): ");

            if (int.TryParse(Console.ReadLine(), out var choice))
            {
                try
                {
                    switch (choice)
                    {
                        case 1:
                            await ViewAllTodosAsync();
                            break;
                        case 2:
                            await SearchTodosAsync();
                            break;
                        case 3:
                            await ViewTodoByIdAsync();
                            break;
                        case 4:
                            await CreateTodoAsync();
                            break;
                        case 5:
                            await UpdateTodoAsync();
                            break;
                        case 6:
                            await DeleteTodoAsync();
                            break;
                        case 7:
                            await ShowStatisticsAsync();
                            break;
                        case 8:
                            running = false;
                            Console.WriteLine("\n👋 Goodbye!");
                            break;
                        default:
                            Console.WriteLine("\n❌ Invalid option. Please enter a number between 1 and 8.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing menu operation");
                    Console.WriteLine($"\n❌ Error: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("\n❌ Invalid input. Please enter a valid number.");
            }
        }
    }

    private async Task ViewAllTodosAsync()
    {
        Console.WriteLine("\n📋 Fetching all todos...");
        var result = await _todoService.GetAllTodosAsync();

        if (!result.IsSuccess)
        {
            Console.WriteLine($"❌ {result.Message}: {result.ErrorDetails}");
            return;
        }

        var todos = result.Data?.ToList();
        if (todos == null || !todos.Any())
        {
            Console.WriteLine("📝 No todos found.");
            return;
        }

        Console.WriteLine("\n" + new string('-', 100));
        foreach (var todo in todos)
        {
            Console.WriteLine(todo);
            if (!string.IsNullOrEmpty(todo.Description))
                Console.WriteLine($"   └─ Description: {todo.Description}");
            if (todo.CompletedAt.HasValue)
                Console.WriteLine($"   └─ Completed at: {todo.CompletedAt:g}");
        }
        Console.WriteLine(new string('-', 100));
        Console.WriteLine($"Total: {todos.Count} items");
    }

    private async Task SearchTodosAsync()
    {
        Console.Write("\nEnter title to search (or press Enter to skip): ");
        var title = Console.ReadLine();

        Console.Write("Filter by status - Completed (Y/N/Enter to skip): ");
        var statusInput = Console.ReadLine()?.ToUpper();
        bool? isCompleted = statusInput == "Y" ? true : (statusInput == "N" ? false : null);

        Console.WriteLine("\n🔍 Searching todos...");
        var result = await _todoService.SearchTodosAsync(title, isCompleted);

        if (!result.IsSuccess)
        {
            Console.WriteLine($"❌ {result.Message}: {result.ErrorDetails}");
            return;
        }

        var todos = result.Data?.ToList();
        if (todos == null || !todos.Any())
        {
            Console.WriteLine("❌ No matching todos found.");
            return;
        }

        Console.WriteLine($"\n✓ Found {todos.Count} matching todo(s):\n");
        foreach (var todo in todos)
        {
            Console.WriteLine($"  {todo}");
        }
    }

    private async Task ViewTodoByIdAsync()
    {
        Console.Write("\nEnter Todo ID: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("❌ Invalid ID format.");
            return;
        }

        var result = await _todoService.GetTodoByIdAsync(id);

        if (!result.IsSuccess)
        {
            Console.WriteLine($"❌ {result.Message}: {result.ErrorDetails}");
            return;
        }

        var todo = result.Data;
        Console.WriteLine($"\n📌 Todo Details:");
        Console.WriteLine($"   ID: {todo.Id}");
        Console.WriteLine($"   Title: {todo.Title}");
        Console.WriteLine($"   Status: {(todo.IsCompleted ? "✓ Completed" : "○ Pending")}");
        if (!string.IsNullOrEmpty(todo.Description))
            Console.WriteLine($"   Description: {todo.Description}");
        Console.WriteLine($"   Created: {todo.CreatedAt:g}");
        if (todo.CompletedAt.HasValue)
            Console.WriteLine($"   Completed: {todo.CompletedAt:g}");
    }

    private async Task CreateTodoAsync()
    {
        Console.Write("\nEnter todo title: ");
        var title = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(title))
        {
            Console.WriteLine("❌ Title cannot be empty.");
            return;
        }

        Console.Write("Enter description (optional): ");
        var description = Console.ReadLine() ?? string.Empty;

        var dto = new CreateTodoDto { Title = title, Description = description, IsCompleted = false };

        Console.WriteLine("\n✏️  Creating todo...");
        var result = await _todoService.CreateTodoAsync(dto);

        if (!result.IsSuccess)
        {
            Console.WriteLine($"❌ {result.Message}: {result.ErrorDetails}");
            return;
        }

        Console.WriteLine($"✅ Todo created successfully with ID: {result.Data}");
    }

    private async Task UpdateTodoAsync()
    {
        Console.Write("\nEnter Todo ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("❌ Invalid ID format.");
            return;
        }

        var getTodoResult = await _todoService.GetTodoByIdAsync(id);
        if (!getTodoResult.IsSuccess)
        {
            Console.WriteLine($"❌ {getTodoResult.Message}");
            return;
        }

        var todo = getTodoResult.Data;

        Console.WriteLine($"\nCurrent title: {todo.Title}");
        Console.Write("Enter new title (press Enter to keep current): ");
        var newTitle = Console.ReadLine();
        if (string.IsNullOrEmpty(newTitle))
            newTitle = todo.Title;

        Console.WriteLine($"Current description: {todo.Description}");
        Console.Write("Enter new description (press Enter to keep current): ");
        var newDescription = Console.ReadLine();
        if (newDescription == null)
            newDescription = todo.Description;

        Console.Write($"Is completed? (current: {todo.IsCompleted}). Enter Y/N (press Enter to keep current): ");
        var completeInput = Console.ReadLine()?.ToUpper();
        var isCompleted = completeInput == "Y" ? true : (completeInput == "N" ? false : todo.IsCompleted);

        var dto = new CreateTodoDto { Title = newTitle, Description = newDescription, IsCompleted = isCompleted };

        Console.WriteLine("\n♻️  Updating todo...");
        var result = await _todoService.UpdateTodoAsync(id, dto);

        if (!result.IsSuccess)
        {
            Console.WriteLine($"❌ {result.Message}: {result.ErrorDetails}");
            return;
        }

        Console.WriteLine("✅ Todo updated successfully.");
    }

    private async Task DeleteTodoAsync()
    {
        Console.Write("\nEnter Todo ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("❌ Invalid ID format.");
            return;
        }

        Console.Write("Are you sure? (Y/N): ");
        if (Console.ReadLine()?.ToUpper() != "Y")
        {
            Console.WriteLine("❌ Deletion cancelled.");
            return;
        }

        Console.WriteLine("\n🗑️  Deleting todo...");
        var result = await _todoService.DeleteTodoAsync(id);

        if (!result.IsSuccess)
        {
            Console.WriteLine($"❌ {result.Message}: {result.ErrorDetails}");
            return;
        }

        Console.WriteLine("✅ Todo deleted successfully.");
    }

    private async Task ShowStatisticsAsync()
    {
        Console.WriteLine("\n📊 Fetching statistics...");
        var result = await _todoService.GetStatisticsAsync();

        if (!result.IsSuccess)
        {
            Console.WriteLine($"❌ {result.Message}: {result.ErrorDetails}");
            return;
        }

        var stats = result.Data;
        Console.WriteLine("\n" + new string('─', 40));
        Console.WriteLine("             TODO STATISTICS");
        Console.WriteLine(new string('─', 40));
        Console.WriteLine($"Total todos:        {stats.Total}");
        Console.WriteLine($"Completed:          {stats.Completed}");
        Console.WriteLine($"Pending:            {stats.Pending}");
        if (stats.Total > 0)
            Console.WriteLine($"Completion rate:    {stats.CompletionRate:F1}%");
        Console.WriteLine(new string('─', 40));
    }
}
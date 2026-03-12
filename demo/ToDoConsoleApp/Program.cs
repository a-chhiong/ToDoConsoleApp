using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ToDoConsoleApp.Application.Interfaces;
using ToDoConsoleApp.Application.Services;
using ToDoConsoleApp.Infrastructure.Database;
using ToDoConsoleApp.Infrastructure.Encryption;
using ToDoConsoleApp.Infrastructure.Persistence;
using ToDoConsoleApp.Presentation;
using ToDoConsoleApp.Utils;

Console.WriteLine("Hello, World!");

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Setup dependency injection
var services = new ServiceCollection();

// Logging
services.AddLogging(builder =>
{
    builder
        .ClearProviders()
        .AddConsole()
        .AddConfiguration(configuration.GetSection("Logging"))
        .SetMinimumLevel(LogLevel.Debug);
});

// Configuration
services.AddSingleton(configuration);

// encryption configuration

// encryption configuration
var encryptionConfig = new FileEncryptionConfiguration(
    pfxPath: configuration.GetValue<string>("AlwaysEncrypt:CertificatePath") ?? "",
    password: configuration.GetValue<string>("AlwaysEncrypt:CertificatePassword") ?? ""
);

// Infrastructure
services.AddSingleton<DatabaseConnectionFactory>(provider => 
    new DatabaseConnectionFactory(
        configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found"),
        encryptionConfig,
        provider.GetRequiredService<ILogger<DatabaseConnectionFactory>>())
);

services.AddSingleton<SqlScriptLoader>(provider => 
    new SqlScriptLoader(
        sqlScriptBasePath: "Infrastructure/Sql", 
        preferEmbedded: configuration.GetValue<bool>("ApplicationSettings:PreferEmbeddedSqlScripts", true), 
        logger: provider.GetRequiredService<ILogger<SqlScriptLoader>>())
);

// Database
services.AddScoped<IUnitOfWork>(provider =>
{
    var factory = provider.GetRequiredService<DatabaseConnectionFactory>();
    var connection = factory.CreateConnection();
    return new UnitOfWork(connection);
});

// Application Services
services.AddScoped<ITodoService, TodoService>();

// Presentation
services.AddScoped<ConsoleMenuService>();

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

logger.LogInformation("====== Todo Application - Production Ready Demo ======");
logger.LogInformation("Using Dapper ORM with Clean Architecture");

try
{
    // Initialize database
    logger.LogInformation("Initializing database...");
    var connectionFactory = serviceProvider.GetRequiredService<DatabaseConnectionFactory>();
    var scriptLoader = serviceProvider.GetRequiredService<SqlScriptLoader>();
    var dbInitializer = new DatabaseInitializer(
        connectionFactory.CreateConnection(),
        scriptLoader,
        serviceProvider.GetRequiredService<ILogger<DatabaseInitializer>>()
    );

    await dbInitializer.InitializeAsync();

    // Configure Dapper
    DapperConfiguration.Configure();
    logger.LogInformation("Dapper configuration applied");

    // Log cache statistics
    var (cachedScripts, memoryBytes) = scriptLoader.GetCacheStats();
    logger.LogDebug("SQL Script Cache: {Count} scripts, ~{Memory} bytes", cachedScripts, memoryBytes);

    // Display interactive menu
    var menuService = serviceProvider.GetRequiredService<ConsoleMenuService>();
    await menuService.DisplayMenuAsync();
}
catch (Exception ex)
{
    logger.LogError(ex, "Application error occurred");
    Console.WriteLine($"\n❌ Fatal Error: {ex.Message}");
}
finally
{
    logger.LogInformation("Application terminated");
    await serviceProvider.DisposeAsync();
}
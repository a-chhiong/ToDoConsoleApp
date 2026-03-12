# ToDoConsoleApp – Demo
> A .net core Console App to demostrate Dapper with SqlBuilder and/or custom SqlScriptLoader approach

**A production‑ready console application** built with .NET 8.0, demonstrating clean architecture, Dapper ORM integration, outsourced SQL scripts, and dependency injection. This demo is designed to showcase maintainable backend patterns with clear separation of concerns.

## 📂 Project Structure

```
demo/
├── ToDoConsoleApp.sln             # Solution file
├── global.json                    # SDK version pinning
└── ToDoConsoleApp/
    ├── Application/               # Business logic layer
    │   ├── Interfaces/            # Contracts (ITodoService, etc.)
    │   └── Services/              # Implementations (TodoService)
    ├── Infrastructure/            # Data access and persistence
    │   ├── Database/              # Connection factory, UnitOfWork
    │   ├── Persistence/           # Database initializer
    │   ├── Repositories/          # Dapper repositories
    │   └── Sql/                   # Outsourced SQL scripts
    │       ├── Schema/            # Table creation scripts
    │       └── Todos/             # CRUD SQL scripts
    ├── Presentation/              # Console UI layer
    │   └── ConsoleMenuService.cs  # Interactive menu
    ├── Utils/                     # Helpers (SqlScriptLoader, ResultWrapper)
    ├── Program.cs                 # Application entry point
    └── appsettings.json           # Configuration (logging, connection string)
```

## 🚀 Features

- Clean Architecture: Clear separation of Application, Infrastructure, and Presentation layers.
- Dapper ORM: Lightweight data access with SqlBuilder for dynamic queries.
- SQL Outsourcing: All queries stored in .sql files for maintainability.
- Dependency Injection: Configured via Microsoft.Extensions.DependencyInjection.
- Logging: Configurable via appsettings.json, supports Debug/Info/Warn/Error levels.
- Database Initialization: Automatic schema creation using outsourced scripts.
- Interactive Console Menu: Simple UI for CRUD operations on Todo items.

## ⚙️ Setup

- Clone the repository:
```
git clone https://github.com/a-chhiong/ToDoConsoleApp.git
cd ToDoConsoleApp/demo
```
- Configure database connection in appsettings.json:
```
"ConnectionStrings": {
  "DefaultConnection": "Server=...; Database=...; User Id=...; Password=...;"
}
```
- Build and run:
```
dotnet build
dotnet run --project ToDoConsoleApp
```
## 🗄️ SQL Scripts

- Schema/CreateTodoTable.sql → Creates the Todos table.
- Todos/Create.sql → Insert new todo.
- Todos/GetAll.sql → Retrieve all todos.
- Todos/GetById.sql → Retrieve todo by ID.
- Todos/Update.sql → Update existing todo.
- Todos/Delete.sql → Delete todo by ID.

### Scripts are loaded via SqlScriptLoader, which supports embedded resources and file‑based loading with caching.

## 📝 Logging

- Configured in appsettings.json:
```
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft": "Warning",
    "System": "Warning",
    "Todo_ConsoleApp": "Debug"
  },
  "Console": {
    "IncludeScopes": true,
    "TimestampFormat": "yyyy-MM-dd HH:mm:ss"
  }
}
```
- Example log output:
```
2026-03-12 13:20:45 | DEBUG | Todo_ConsoleApp.Utils.SqlScriptLoader | Attempting to load embedded resource: Todo_ConsoleApp.Infrastructure.Sql.Todos.GetAll.sql
```
## 🎮 Usage

- When you run the app:

  1. The database is initialized automatically.
  2. Dapper is configured.

- An interactive menu appears, allowing you to:

  1. Create new todos
  2. List all todos
  3. Update or delete todos
  4. Filter todos dynamically

## 📌 Notes

*Requires .NET 8.0 SDK (pinned via global.json).*<br>
*SQL scripts must be marked as EmbeddedResource or copied to output for file‑based loading.*<br>
*Logging can be extended with providers like NLog or Serilog if file logging is desired.*<br>

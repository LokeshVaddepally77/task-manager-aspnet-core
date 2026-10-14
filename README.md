# Task Manager (ASP.NET Core MVC)

A layered task-management web app for teams, with real access control: managers and employees see and do different things, and every change to a task is recorded in an audit trail.

**Live demo:** https://taskmanager-lokesh-ezaah3c8dmdpbnae.canadacentral-01.azurewebsites.net

## Features

- **Role-based access control:** roles, custom authorization filters, and an access-denied flow, so managers and employees get different permissions.
- **Authentication:** cookie and session sign-in with hashed passwords, plus Google sign-in.
- **Tasks:** create, edit, assign, prioritize and track tasks, with a dashboard and a "My Tasks" view.
- **Audit trail:** a full history of changes for every task.
- **Collaboration:** comments, attachments, tags, categories and notifications.
- **Teams:** team overview and member details.
- **Razor Tag Helpers:** reusable status badges, priority badges and progress bars.
- **Swappable database:** MySQL, SQL Server or in-memory, chosen with one setting.

## Tech stack

.NET 8 · C# · ASP.NET Core MVC · Entity Framework Core · MySQL / SQL Server · Razor · Bootstrap · Azure App Service · GitHub Actions

## Architecture

```
TaskManagerSolution.sln
├── TaskManagerMVC/              presentation layer
│   ├── Controllers/             Account, Task, Team, Role, Home
│   ├── Filters/                 custom authorization filters
│   ├── TagHelpers/              status, priority, progress badges
│   └── Views/
└── TaskManagerMVC.DataAccess/   data-access layer
    ├── Data/                    DbContext, model configuration
    ├── Migrations/              EF Core migrations
    ├── Models/                  TaskItem, TaskHistory, Comment, Attachment, Tag, Role…
    ├── Repositories/            ITaskRepository (SQL and mock implementations)
    └── Services/                authentication, roles, password hashing
```

## Run it locally

Requirements: the .NET 8 SDK, plus MySQL or SQL Server LocalDB (or neither, if you use the in-memory database).

1. In `TaskManagerMVC/appsettings.json`, set `DatabaseProvider` to `MySql`, `SQLServer` or `InMemory`, and fill in the matching connection string.
2. To enable Google sign-in, create OAuth credentials in Google Cloud Console and store them with user secrets, not in the file:
   ```bash
   cd TaskManagerMVC
   dotnet user-secrets set "Authentication:Google:ClientId" "<your client id>"
   dotnet user-secrets set "Authentication:Google:ClientSecret" "<your client secret>"
   ```
3. Apply the migrations and run:
   ```bash
   dotnet ef database update --project TaskManagerMVC.DataAccess --startup-project TaskManagerMVC
   dotnet run --project TaskManagerMVC
   ```

## CI/CD

GitHub Actions builds the solution on every push and pull request. The production copy deploys to Azure App Service.

Built for INFO 6250 (Web Development Tools and Methods) at Northeastern University.

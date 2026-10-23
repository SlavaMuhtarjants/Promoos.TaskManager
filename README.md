# Promoos.TaskManager

Promoos.TaskManager is an asynchronous RESTful Task Management API built with .NET 10 Minimal APIs, PostgreSQL, and RabbitMQ. It provides lightweight, high-performance endpoints for managing task lifecycles and publishing event notifications upon task completion.

## Tech Stack

- **Runtime & Framework**: .NET 10, ASP.NET Core (Minimal APIs), C# 14
- **Data Persistence**: PostgreSQL 18, Entity Framework Core 10 (Npgsql provider), EF Core Migrations
- **Messaging & Event Streaming**: RabbitMQ 4 (Management) with `RabbitMQ.Client`
- **Logging & API Docs**: `NReco.Logging.File` (daily rolling log files) and `Microsoft.AspNetCore.OpenApi`
- **Testing**: xUnit v3, `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`), `Microsoft.EntityFrameworkCore.InMemory`, `NSubstitute`
- **Containerization**: Docker & Docker Compose

## API Endpoints

The API exposes the following endpoints for task management:

- `POST /tasks` — Creates a new task.
- `GET /tasks` — Retrieves a list of all existing tasks.
- `DELETE /tasks/{id}` — Deletes a task.
- `PUT /tasks/{id}/complete` — Marks a task as completed and publishes a task completion event to RabbitMQ.

## Configuration & Sensitive Data

Sensitive credentials and environment-specific settings are excluded from source control and managed through two mechanisms:

1. **Docker Compose (`.env`)**:
   Container secrets such as PostgreSQL credentials and RabbitMQ login information are loaded from the root `.env` file when running Docker Compose:
   - `POSTGRES_USER` / `POSTGRES_PASS`
   - `RABBITMQ_USER` / `RABBITMQ_PASS`

2. **.NET User Secrets**:
   Local development settings (connection strings and RabbitMQ credentials) are managed using the .NET Secret Manager (`dotnet user-secrets`) to avoid committing secrets to `appsettings.json`:
   ```bash
   dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5434;Database=tasks;Username=<POSTGRES_USER>;Password=<POSTGRES_PASS>" --project Promoos.TaskManager.Api
   dotnet user-secrets set "RabbitMq:HostName" "localhost" --project Promoos.TaskManager.Api
   dotnet user-secrets set "RabbitMq:Port" "5671" --project Promoos.TaskManager.Api
   dotnet user-secrets set "RabbitMq:UserName" "<RABBITMQ_USER>" --project Promoos.TaskManager.Api
   dotnet user-secrets set "RabbitMq:Password" "<RABBITMQ_PASS>" --project Promoos.TaskManager.Api
   dotnet user-secrets set "RabbitMq:QueueName" "task-completed" --project Promoos.TaskManager.Api
   ```

## How to Run the Solution

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) and Docker Compose

### Steps

1. **Start backing services** (PostgreSQL and RabbitMQ):
   ```bash
   docker compose up -d
   ```
   - PostgreSQL is exposed on port `5434`.
   - RabbitMQ AMQP is exposed on port `5671`, and Management UI is accessible at `http://localhost:15671`.

2. **Configure local user secrets** (if not already set up as shown in the Configuration section).

3. **Run the API application**:
   ```bash
   dotnet run --project Promoos.TaskManager.Api
   ```

## How to Run Tests

To execute the automated test suite across the solution:

```bash
dotnet test
```

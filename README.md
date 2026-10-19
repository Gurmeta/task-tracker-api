# Task Tracker API

A small REST API for managing tasks, built with C# and .NET 10 (ASP.NET Core Minimal API).
It is a learning project that follows a production-style workflow: tests, containerization and CI/CD.

## Status

| Stage | Topic | State |
|---|---|---|
| 1 | Solution, Web API and test project | done |
| 2 | `TaskItem` model and in-memory endpoints | done |
| 3 | Full CRUD, validation, correct HTTP status codes | done |
| 4 | SQLite with Entity Framework Core | planned |
| 5 | xUnit tests | planned |
| 6 | Dockerfile | planned |
| 7 | GitHub Actions: build, test, publish image to GHCR | planned |

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Run locally

```bash
dotnet run --project src/TaskTracker.Api
```

In Development the OpenAPI document is available at `/openapi/v1.json`.
Sample requests live in `src/TaskTracker.Api/TaskTracker.Api.http`
(use the REST Client extension for VS Code).

## Test

```bash
dotnet test
```

## Project layout

```
src/TaskTracker.Api/     Web API
tests/TaskTracker.Tests/ xUnit tests
docs/                    Notes and project site
```

## License

[MIT](LICENSE)

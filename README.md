# Task Tracker API

[![CI](https://github.com/Gurmeta/task-tracker-api/actions/workflows/ci.yml/badge.svg)](https://github.com/Gurmeta/task-tracker-api/actions/workflows/ci.yml)

A small REST API for managing tasks, built with C# and .NET 10 (ASP.NET Core Minimal API).
It is a learning project that follows a production-style workflow: tests, containerization and CI/CD.

## Status

| Stage | Topic                                              | State   |
| ----- | -------------------------------------------------- | ------- |
| 1     | Solution, Web API and test project                 | done    |
| 2     | `TaskItem` model and in-memory endpoints           | done    |
| 3     | Full CRUD, validation, correct HTTP status codes   | done    |
| 4     | SQLite with Entity Framework Core                  | done    |
| 5     | xUnit tests                                        | done    |
| 6     | Dockerfile                                         | done    |
| 7     | GitHub Actions: build, test, publish image to GHCR | done    |

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Run locally

```bash
dotnet run --project src/TaskTracker.Api
```

In Development the OpenAPI document is available at `/openapi/v1.json`.
Sample requests live in `src/TaskTracker.Api/TaskTracker.Api.http`
(use the REST Client extension for VS Code).

## CI/CD

Two GitHub Actions workflows live in `.github/workflows`:

- `ci.yml` runs on every pull request and on pushes to `main`. It builds, runs the tests, checks that the
  EF Core migrations match the model and fails on known vulnerable NuGet packages. It then builds the Docker
  image and smoke-tests the running container (health, create, list, non-root user).
  On pushes to `main` and on `v*` tags the same image is published to the GitHub Container Registry
  with the built-in `GITHUB_TOKEN` (no personal tokens).
- `pages.yml` deploys the project page from `site/` to GitHub Pages. The endpoint list on that page is
  generated from the API's own OpenAPI document.

Third-party actions are pinned to full commit SHAs, and Dependabot proposes updates weekly.

Pull the published image:

```bash
docker pull ghcr.io/gurmeta/task-tracker-api:latest
```

## Docker

The [Dockerfile](Dockerfile) is a multi-stage build: the .NET SDK image compiles the app,
and only the published output is copied into the smaller ASP.NET runtime image.
The container runs as a non-root user, listens on port 8080 and keeps the SQLite file in `/data`.

```bash
docker build -t task-tracker-api .
docker run --rm -p 8080:8080 -v task-tracker-data:/data task-tracker-api
curl http://localhost:8080/health
```

The container runs in Production mode, so the OpenAPI document is not exposed.
`GET /health` returns `200 Healthy` and can be used as a liveness probe.

## Configuration

Data is stored in a SQLite file. The schema is created and updated automatically at startup
through EF Core migrations. The location comes from the `ConnectionStrings:Default` setting
(default `Data Source=tasktracker.db`) and can be overridden with an environment variable:

```bash
ConnectionStrings__Default="Data Source=/data/tasktracker.db" dotnet run --project src/TaskTracker.Api
```

## API

| Method | Path          | Success                   | Errors   |
| ------ | ------------- | ------------------------- | -------- |
| GET    | `/tasks`      | 200 (optional `?status=`) | 400      |
| GET    | `/tasks/{id}` | 200                       | 404      |
| POST   | `/tasks`      | 201 + `Location`          | 400      |
| PUT    | `/tasks/{id}` | 200                       | 400, 404 |
| DELETE | `/tasks/{id}` | 204                       | 404      |

## Test

```bash
dotnet test
```

## Project layout

```text
src/TaskTracker.Api/     Web API
tests/TaskTracker.Tests/ xUnit tests
docs/                    Notes and project site
```

## License

[MIT](LICENSE)

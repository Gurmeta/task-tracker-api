# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `PATCH /tasks/{id}` changes only the properties that are sent.
- `GET /tasks` supports `search`, `overdue`, `dueBefore`, `sort` and `order`.
- `GET /tasks/stats` returns the number of tasks per status and the number of overdue tasks.
- Tasks have `updatedAt` and `completedAt` timestamps (new migration `AddUpdatedAndCompletedAt`; existing tasks get `null`).

## [1.1.0] - 2026-10-19

### Added

- `GET /tasks` is paged: `limit` (1 to 500, default 100) and `offset`, with the total in the `X-Total-Count` header. Out-of-range values return `400`.
- Requests larger than 64 KB are rejected with `413`.
- A database that cannot be opened at start-up logs a clear message and exits with code 1 instead of printing a stack trace.
- Favicon, a semantic endpoint list and screen reader labels for the response codes on the project page.

### Changed

- `GET /tasks` returns at most 100 tasks when `limit` is not given. Earlier versions returned all of them.
- Every error response now has a problem details JSON body (404, malformed JSON, unknown status filter, unsupported content type, unexpected failures). Status codes are unchanged.
- The release workflow refuses tags whose commit is not on `main`.
- Dependabot uses Conventional Commit prefixes and groups the test package updates.

### Documentation

- "Scope and limitations" and an error format section in the README.
- Tests for the error response format and for the OpenAPI document not being served in Production.

## [1.0.1] - 2026-10-19

### Added

- Test coverage report in CI (job summary and downloadable artifact).
- `SECURITY.md`, `CONTRIBUTING.md`, a pull request template and `.editorconfig`.
- `/health` and `/version` requests in the sample `.http` file.
- Release info, an example request and a CI/CD section on the project page.

### Changed

- The `latest` image tag now moves only on stable releases. Builds of `main` are published as `edge` and `sha-*`.
- README rewritten with badges, an architecture diagram, quick start, configuration and release documentation.
- Updated test dependencies: `coverlet.collector`, `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio`.

### Fixed

- The published OpenAPI document no longer lists `http://localhost:5000/` as a server.
- The architecture diagram on the project page stays readable on small screens.

## [1.0.0] - 2026-10-19

First stable release.

### Added

- REST API for tasks (create, list, filter by status, get, update, delete) built with ASP.NET Core Minimal API on .NET 10.
- Request validation with clear error responses.
- Persistence with EF Core and SQLite, including database migrations applied at startup.
- `GET /health` liveness endpoint.
- `GET /version` endpoint, and the same version in the OpenAPI document (`info.version`).
- Automated tests with xUnit and `WebApplicationFactory`.
- Multi-stage Dockerfile that runs as a non-root user and stores data in a `/data` volume.
- GitHub Actions CI: build, test, migration check, vulnerable package check, image build and smoke test.
- Container images published to GitHub Container Registry.
- API documentation site published to GitHub Pages.
- Release workflow that publishes versioned images and a GitHub Release when a `v*` tag is pushed.
- Dependabot updates for NuGet packages and GitHub Actions.

[Unreleased]: https://github.com/Gurmeta/task-tracker-api/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/Gurmeta/task-tracker-api/compare/v1.0.1...v1.1.0
[1.0.1]: https://github.com/Gurmeta/task-tracker-api/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/Gurmeta/task-tracker-api/releases/tag/v1.0.0

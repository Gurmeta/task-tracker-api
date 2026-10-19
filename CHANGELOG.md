# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Test coverage report in CI (job summary and downloadable artifact).
- `/health` and `/version` requests in the sample `.http` file.

### Changed

- The `latest` image tag now moves only on stable releases. Builds of `main` are published as `edge` and `sha-*`.

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

[Unreleased]: https://github.com/Gurmeta/task-tracker-api/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/Gurmeta/task-tracker-api/releases/tag/v1.0.0

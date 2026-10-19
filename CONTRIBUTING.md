# Contributing

Thanks for your interest in Task Tracker API. Issues and pull requests are welcome.

## Getting started

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). Docker is optional.

```bash
git clone https://github.com/Gurmeta/task-tracker-api.git
cd task-tracker-api
dotnet restore
dotnet build
dotnet test
dotnet run --project src/TaskTracker.Api
```

## Workflow

1. Fork the repository or create a branch from `main`. Never commit directly to `main`.
2. Use a short branch name with a prefix: `feat/...`, `fix/...`, `docs/...`, `ci/...`.
3. Make a focused change and add or update tests.
4. Run `dotnet build` and `dotnet test` locally. Both must pass.
5. If you change the EF Core model, add a migration:
   `dotnet ef migrations add <Name> --project src/TaskTracker.Api`.
   CI fails when the model and the migrations are out of sync.
6. Open a pull request against `main`. CI must be green before it can be merged.

## Commit messages

This project uses [Conventional Commits](https://www.conventionalcommits.org/):

```text
feat: add due date filter
fix: return 404 for an unknown task id
docs: explain the configuration options
ci: pin an action to a commit SHA
```

## Code style

The rules are in [.editorconfig](.editorconfig); most editors apply them automatically.
Keep comments short and useful: explain *why*, not what the code already says.

## Releases

Maintainers release by updating `Version` in `src/TaskTracker.Api/TaskTracker.Api.csproj`,
adding an entry to [CHANGELOG.md](CHANGELOG.md) and pushing a tag such as `v1.2.3`.
The release workflow publishes the container image and the GitHub Release.

## Security issues

Do not report vulnerabilities in a public issue. See [SECURITY.md](SECURITY.md).

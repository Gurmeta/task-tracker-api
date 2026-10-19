# Security Policy

## Supported versions

Only the latest release receives security fixes.

| Version | Supported |
| ------- | --------- |
| 1.x     | yes       |
| < 1.0   | no        |

## Reporting a vulnerability

Please do **not** open a public issue for a security problem.

Report it privately through GitHub:
[Report a vulnerability](https://github.com/Gurmeta/task-tracker-api/security/advisories/new).

Include what you found, how to reproduce it and which version is affected.
You can expect an acknowledgement within a few days. Because this is a small
learning project maintained by one person, fixes are made on a best-effort basis.

## Security practices in this repository

- The CI pipeline fails when a NuGet package with a known vulnerability is found.
- Dependabot proposes dependency and GitHub Actions updates every week.
- Third-party GitHub Actions are pinned to full commit SHAs.
- Workflows run with read-only permissions by default. Write access is granted only to the single job that needs it.
- Images are published with the built-in `GITHUB_TOKEN`. No personal access tokens or other secrets are stored in the repository.
- The container runs as a non-root user, and CI fails if that changes.
- Configuration (such as the connection string) comes from environment variables, never from source code.

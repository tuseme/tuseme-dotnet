# Contributing to Tuseme .NET SDK

Thank you for your interest in contributing!

## Development Setup

```bash
git clone https://github.com/tuseme/tuseme-dotnet.git
cd tuseme-dotnet
dotnet build src/Tuseme/Tuseme.csproj
```

## Running Tests

```bash
dotnet test
```

## Branch Protection & Pull Requests

The `main` branch is protected with the following rules:

- **Pull requests are required** — direct pushes to `main` are blocked.
- **1 approving review** is needed before merging.
- **CI status checks** (`test`) must pass.
- **Branches must be up to date** with `main` before merging.
- **Linear history** is enforced (squash or rebase merges only).
- **Stale approvals are dismissed** when new commits are pushed.

### How to Submit a PR

1. **Fork** the repo and clone your fork.
2. Create a **feature branch** from `main`:
   ```bash
   git checkout -b feature/my-improvement
   ```
3. Make your changes and **add tests** for any new functionality.
4. Ensure `dotnet build` and `dotnet test` pass:
   ```bash
   dotnet build src/Tuseme/Tuseme.csproj
   dotnet test
   ```
5. Commit with a descriptive message following [Conventional Commits](https://www.conventionalcommits.org/):
   ```bash
   git commit -m "feat: add batch message scheduling"
   ```
6. Push your branch and **open a PR** against `main`:
   ```bash
   git push origin feature/my-improvement
   gh pr create --title "feat: add batch message scheduling" --body "Description..."
   ```
7. Wait for CI to pass and a maintainer to review.

## Release Process

Releases are handled by maintainers. The process is:

1. Update the version in `src/Tuseme/Tuseme.csproj` (`<Version>X.Y.Z</Version>`).
2. Update `CHANGELOG.md` with the new version section.
3. Commit: `git commit -m "chore: bump version to vX.Y.Z"`
4. Tag: `git tag -a vX.Y.Z -m "vX.Y.Z — Description"`
5. Push: `git push origin main && git push origin vX.Y.Z`
6. CI automatically packs and publishes to **NuGet**.
7. Create a GitHub Release: `gh release create vX.Y.Z --repo tuseme/tuseme-dotnet`

We follow [Semantic Versioning](https://semver.org/) (`MAJOR.MINOR.PATCH`).

## Reporting Issues

Open an issue at [github.com/tuseme/tuseme-dotnet/issues](https://github.com/tuseme/tuseme-dotnet/issues) with:
- SDK version and .NET version
- Minimal reproduction steps
- Expected vs. actual behavior

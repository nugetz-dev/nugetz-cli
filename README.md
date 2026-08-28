# Nugetz CLI

Repository-aware NuGet audits, upgrade plans, package validation, and safer publishing for .NET teams.

## Install

```bash
dotnet tool install -g Nugetz.Cli
```

Nugetz currently requires the .NET 10 SDK. Native downloads are shown on [nugetz.dev/cli](https://nugetz.dev/cli) only after matching release assets have been published.

## Audit a repository

```bash
nugetz doctor
nugetz doctor --why System.Text.Json
nugetz doctor --format json --fail-on high
nugetz doctor --fail-on-outdated
```

`doctor` delegates dependency resolution to the installed .NET SDK, so it understands target frameworks, top-level and transitive dependencies, Central Package Management, and configured/private NuGet sources. Update or advisory failures remain `unknown` rather than being counted as healthy.

Useful options:

- `--project, -p <path>` audits one project.
- `--source, -s <url>` selects a package source for update/advisory checks.
- `--include-prerelease` considers preview updates.
- `--no-restore` uses existing project assets.
- `--format table|json` selects human or automation output.
- `--fail-on low|moderate|high|critical` configures the advisory exit threshold.
- `--fail-on-outdated` fails CI when updates exist.

## Plan and apply upgrades

```bash
nugetz upgrade
nugetz upgrade Serilog --to 4.3.0
nugetz upgrade Serilog --apply
nugetz upgrade --apply --yes
```

Upgrade is preview-only by default. It identifies major-version transitions and Central Package Management files. `--apply` uses the official `dotnet add package` workflow; confirmation is required unless `--yes` is supplied.

## Validate and publish packages

```bash
nugetz validate --project src/MyLibrary/MyLibrary.csproj
nugetz validate ./nupkg/MyLibrary.1.0.0.nupkg --format json
nugetz publish --dry-run
nugetz publish --project src/MyLibrary/MyLibrary.csproj
nugetz publish ./nupkg/MyLibrary.1.0.0.nupkg --source https://api.nuget.org/v3/index.json
```

Validation checks required manifest metadata, declared README/icon/license files, repository and release-note metadata, package size, and potentially sensitive files. Publish always validates first and displays the exact package ID, version, and destination before pushing. Use `--skip-duplicate` for idempotent CI publishing and `--yes` only in intentional non-interactive workflows.

API keys can come from `--api-key`, `NUGET_API_KEY`, or `nugetz apikey set <key>`. Stored keys use restricted file permissions on Unix.

## Search, inspect, and install

```bash
nugetz search logging --limit 20
nugetz info Serilog
nugetz install Serilog
nugetz install Serilog@4.2.0 --project src/App/App.csproj
nugetz install FluentValidation --all --yes
```

Package installation discovers `.csproj` files recursively and offers an interactive project selector when needed.

## Exit codes

- `0`: command completed and configured policy thresholds passed.
- `1`: a policy threshold, validation, apply, or publishing operation failed.
- `2`: the requested audit or input was unavailable or invalid.

## Development

```bash
dotnet restore Nugetz.Cli.Tests/Nugetz.Cli.Tests.csproj
dotnet build Nugetz.Cli.csproj
dotnet test Nugetz.Cli.Tests/Nugetz.Cli.Tests.csproj
```

## Links

- [nugetz.dev](https://nugetz.dev)
- [Web documentation](https://nugetz.dev/docs)
- [GitHub](https://github.com/nugetz-dev/nugetz-cli)

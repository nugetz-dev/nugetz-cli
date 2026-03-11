# Nugetz CLI

A modern terminal-first tool for discovering, installing, and publishing NuGet packages.

## Install

```bash
dotnet tool install -g Nugetz.Cli
```

## Commands

### `nugetz install <package>`

Install a package into one or more projects. Nugetz discovers `.csproj` files automatically.

```bash
# Install latest version
nugetz install Serilog

# Install specific version
nugetz install Serilog@4.0.0
nugetz install Serilog --version 4.0.0

# Install into all projects without prompting
nugetz install Serilog --all

# Install into a specific project
nugetz install Serilog --project src/MyApp/MyApp.csproj
```

**How it works:**

- Scans recursively from the current directory for `.csproj` files
- If one project is found, installs directly
- If multiple projects are found, shows an interactive checkbox selector
- Projects are sorted by folder priority: `src/` > `apps/` > `services/` > `tests/`

### `nugetz search <query>`

Search for NuGet packages from the terminal.

```bash
nugetz search logging
nugetz search json --limit 20
nugetz search grpc --prerelease
```

### `nugetz info <package>`

Show detailed package information including health score and community signals.

```bash
nugetz info Serilog
nugetz info Newtonsoft.Json
```

Displays: version, downloads, license, frameworks, dependencies, vulnerabilities, health grade (A-F), and GitHub community signals (stars, issues, contributors).

### `nugetz publish`

Pack and publish a package to nuget.org in one step.

```bash
# Pack and publish the current project
nugetz publish

# Publish a specific project
nugetz publish --project src/MyLib/MyLib.csproj

# Publish an existing .nupkg file
nugetz publish ./nupkg/MyLib.1.0.0.nupkg

# Pass API key inline (for CI)
nugetz publish --api-key oy2m...
```

**How it works:**

- Discovers `.csproj` files (or use `--project` to specify one)
- Runs `dotnet pack -c Release -o ./nupkg`
- Pushes the resulting `.nupkg` to nuget.org
- If you pass a `.nupkg` path directly, it skips packing

### `nugetz apikey`

Manage your NuGet API key.

```bash
# Store your API key
nugetz apikey set oy2m...abc123

# Check stored key status
nugetz apikey status

# Remove stored key
nugetz apikey remove
```

Keys are stored in `~/.nugetz/config.json` with restricted file permissions. The `NUGET_API_KEY` environment variable overrides the stored key (useful for CI).

## Options

| Flag | Command | Description |
|------|---------|-------------|
| `--version` | install | Package version to install |
| `--all` | install | Install into all discovered projects |
| `--project` | install, publish | Path to a specific .csproj file |
| `--yes` | install | Skip confirmation prompt |
| `--prerelease` | install, search | Include prerelease versions |
| `--limit` | search | Maximum number of results (default: 10) |
| `--api-key` | publish | NuGet API key (overrides stored key) |

## Requirements

- .NET 10 SDK or later

## Links

- [nugetz.dev](https://nugetz.dev) - Browse NuGet packages online
- [nugetz.dev/docs](https://nugetz.dev/docs) - Full documentation

# ConfigTransformer

[![CI](https://github.com/debugger90/ConfigTransformer/actions/workflows/ci.yml/badge.svg)](https://github.com/debugger90/ConfigTransformer/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/ConfigTransformer.svg)](https://www.nuget.org/packages/ConfigTransformer)
[![Downloads](https://img.shields.io/nuget/dt/ConfigTransformer.svg)](https://www.nuget.org/packages/ConfigTransformer)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Resolve `{Section:Key}` placeholders in .NET configuration, so one value can be built from others instead of being repeated in every file and environment.

```json
{
  "Api": { "BaseUrl": "https://api.example.com" },
  "Orders": { "Endpoint": "{Api:BaseUrl}/v1/orders" }
}
```

After `Transform()`, `Orders:Endpoint` is `https://api.example.com/v1/orders`. Override `Api__BaseUrl` with an environment variable and every value that references it follows.

## Install

```bash
dotnet add package ConfigTransformer
```

Targets .NET 8 and later. The only dependency is `Microsoft.Extensions.Configuration`.

## Usage

Call `Transform()` after every configuration source has been added.

**ASP.NET Core**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .Transform();
```

`builder.Configuration` is a `ConfigurationManager`, so the values are resolved in place and the return value can be ignored.

**Console apps and tests**

```csharp
IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Transform();
```

Here `Transform()` builds the configuration and returns the resolved result, so use the returned value.

## What it resolves

| Case | Example | Result |
|---|---|---|
| Value from another section | `"{Api:BaseUrl}/v1"` | `https://api.example.com/v1` |
| Several placeholders | `"Host={Db:Host};Port={Db:Port}"` | `Host=db.local;Port=5432` |
| Chained references | `A = "{B}/a"`, `B = "{C}/b"`, `C = "root"` | `A = root/b/a` |
| Nested key | `"{Urls:{Environment}}"` with `Environment = Staging` | value of `Urls:Staging` |
| Arrays and objects in arrays | `Endpoints:0:Url = "https://{Host}/a"` | `https://example.com/a` |
| Any provider | JSON, environment variables, command line, Key Vault, in-memory | later providers win, as usual |
| Unknown key | `"{Missing:Key}"` | left unchanged |

## How it works

1. The configuration is built from all sources.
2. Every value containing a `{Key}` placeholder is replaced with the value of `Key`, looked up through the normal configuration precedence.
3. The pass repeats until nothing changes, so chained and nested placeholders resolve. Passes are capped at `ConfigTransformer.MaxPasses` (10), so circular references stop instead of looping; their placeholders stay in the value.

## Limitations

- **Reloading.** Resolved values are written into the providers. When a source with `reloadOnChange: true` reloads, its raw values, placeholders included, come back. Call `Transform()` again from a change callback if you rely on reloading.
- **No escaping.** A value that should keep a literal `{Name}` must not match an existing key.
- **Strings only.** Placeholders are resolved as text; this is not an expression language.

## Build and test

```bash
dotnet build
dotnet test
```

CI runs the tests on every push and pull request. Pushing a `v*` tag packs the library and publishes it to NuGet.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## License

[MIT](LICENSE) © Ozair Khan

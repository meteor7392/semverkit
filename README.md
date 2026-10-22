# SemVerKit

SemVerKit is a dependency-free .NET 8 library and command-line tool for working with [Semantic Versioning 2.0.0](https://semver.org/) versions and version ranges.

## Features

- Parse and validate strict SemVer 2.0.0 versions.
- Compare versions using SemVer precedence rules.
- Ignore build metadata in equality and precedence, as required by SemVer.
- Match versions against ranges:
  - exact versions
  - comparators (`>`, `>=`, `<`, `<=`, `=`)
  - caret ranges (`^1.2.3`)
  - tilde ranges (`~1.2.3`)
  - hyphen ranges (`1.0.0 - 2.0.0`)
  - AND combinations (`>=1.0.0 <2.0.0`)
  - OR combinations (`1.0.0 || 2.0.0`)
- Bump major, minor, or patch versions from the CLI.

## Requirements

- .NET SDK 8.0 or later

## Build and test

```bash
dotnet build
dotnet test
```

## CLI usage

```bash
dotnet run --project src/SemVerKit/SemVerKit.csproj -- parse 1.2.3-rc.1+build.5
dotnet run --project src/SemVerKit/SemVerKit.csproj -- compare 1.2.3 1.2.4
dotnet run --project src/SemVerKit/SemVerKit.csproj -- satisfies 1.2.3 ">=1.0.0 <2.0.0"
dotnet run --project src/SemVerKit/SemVerKit.csproj -- bump 1.2.3 minor
```

## Library example

```csharp
using SemVerKit;

var version = SemanticVersion.Parse("1.2.3-rc.1");
var range = new VersionRange("^1.2.0");

Console.WriteLine(version);                 // 1.2.3-rc.1
Console.WriteLine(version > SemanticVersion.Parse("1.2.2")); // True
Console.WriteLine(range.IsSatisfiedBy(version)); // True
```

## Notes

- Versions with invalid numeric identifiers, such as leading zeroes, are rejected.
- Build metadata is retained for formatting but does not affect comparison.
- The library has no runtime dependencies.

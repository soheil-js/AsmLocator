# Getting started

## Requirements

- .NET 10 SDK
- AsmResolver.DotNet 6.1.0 (a direct dependency of AsmLocator)

## Add AsmLocator

Reference the project from your solution:

```bash
dotnet add reference ../AsmLocator/AsmLocator.csproj
```

When the package is published, install it with `dotnet add package AsmLocator`.

## Load a module

```csharp
using AsmLocator;

var locator = AssemblyLocator.FromFile("Sample.dll");
Console.WriteLine(locator.Module.Name);
```

You can also wrap an existing `ModuleDefinition` or load from a byte array. The locator owns no separate copy of the module: edits are made to the underlying AsmResolver model.

## Find a method

```csharp
TypeSpec type = TypeSpec.ByFullName("Sample.Services.UserService");
var method = locator.GetMethod(type, MethodSpec.Of("Find", "System.String"));
Console.WriteLine(method.FullName);
```

## Save

```csharp
locator.Save("Sample.modified.dll");
```

Prefer writing to a separate output file until the result has been validated.

# Getting started

This page takes you from nothing to a working query in about five minutes.

## Requirements

- the **.NET 10 SDK** (`dotnet --version` should print `10.x`);
- a .NET assembly to inspect (any `.dll` or `.exe` built from C#, F# or VB — you will create one below);
- AsmResolver.DotNet 6.1.0 — restored automatically as a dependency of AsmLocator.

## Step 1 — Create a project

```bash
dotnet new console -n MyFirstLocator
cd MyFirstLocator
```

## Step 2 — Add AsmLocator

AsmLocator is currently consumed as a project reference:

```bash
dotnet add reference path/to/AsmLocator/src/AsmLocator/AsmLocator.csproj
```

> [!NOTE]
> Once the package is published on NuGet you will be able to use `dotnet add package AsmLocator` instead.

## Step 3 — Pick an assembly to inspect

For your first experiment, inspect AsmLocator's own assembly — it is always available:

```csharp
using AsmLocator;

string path = typeof(AssemblyLocator).Assembly.Location;
var locator = AssemblyLocator.FromFile(path);

Console.WriteLine(locator.Module.Name);
```

Run it with `dotnet run`. You should see `AsmLocator.dll`.

You can also load from a byte array, or wrap a module you already opened with AsmResolver:

```csharp
var fromBytes  = AssemblyLocator.FromBytes(File.ReadAllBytes("Sample.dll"));
var fromModule = new AssemblyLocator(someModuleDefinition);   // your own ModuleDefinition
```

> [!WARNING]
> `FromFile` may keep the input file open or memory-mapped while the module is alive. If you plan to **save over the same path** or delete the file afterwards, load with `FromBytes(File.ReadAllBytes(path))` instead. Better still, always save to a different file name.

## Step 4 — Find things

List every type whose name ends in `Spec`:

```csharp
foreach (var type in locator.FindTypes(TypeSpec.ByName("*Spec")))
    Console.WriteLine(type.FullName);
```

Find one specific method. `GetMethod` demands exactly one match and throws `MemberNotFoundException` otherwise:

```csharp
TypeSpec matcherType = TypeSpec.ByFullName("AsmLocator.StringMatcher");

var method = locator.GetMethod(matcherType, MethodSpec.ByName("Wildcard"));
Console.WriteLine(method.FullName);
```

Search inside a method body — which constructors does `Wildcard` call?

```csharp
var ctorCalls = locator.FindInstructions(
    matcherType,
    MethodSpec.ByName("Wildcard"),
    InstructionSpec.NewObj("*Regex::.ctor*"));

foreach (var match in ctorCalls)
    Console.WriteLine($"[{match.Index}] {match.Instruction}");
```

## Step 5 — Save (when you edit)

Reading never changes a file. When you later edit something, write the result to a **new** path:

```csharp
locator.Save("Output.dll");
```

## What you just used

| You wrote | Meaning |
|---|---|
| `TypeSpec.ByName("*Spec")` | a type specification: name matches the wildcard |
| `TypeSpec.ByFullName(...)` | a type specification: exact full name |
| `MethodSpec.ByName(...)` | a method specification: name only |
| `InstructionSpec.NewObj(...)` | an instruction specification: `newobj` whose constructor matches |
| `FindTypes` | lazily returns **all** matching types |
| `GetMethod` | returns **exactly one** method or throws |
| `FindInstructions` | returns all matching instructions of one method, with their indexes |

## Where next?

- Want a guided hands-on session? Do the [Tutorial: your first patch](tutorial.md).
- Want to understand the building blocks? Read [Specifications](specifications.md).
- Hit an error? See [Troubleshooting](troubleshooting.md).

# AsmLocator

AsmLocator is a .NET library that provides a specification-based API for locating, inspecting, and editing .NET assembly metadata and CIL method bodies. It is built on [AsmResolver](https://github.com/Washi1337/AsmResolver).

## Features

- Find types, fields, properties, events, methods, parameters, local variables, and CIL instructions.
- Describe queries with reusable `TypeSpec`, `MethodSpec`, `FieldSpec`, `PropertySpec`, `EventSpec`, `ParameterSpec`, `LocalSpec`, and `InstructionSpec` objects.
- Match strings using exact, wildcard (`*`, `?`), regular-expression, or custom-predicate matching.
- Search for contiguous instruction patterns and inspect their original instruction indexes.
- Edit metadata and method bodies through callbacks.
- Search nested types directly or recursively.
- Save the modified module through AsmResolver.

## Requirements

- .NET 10 SDK
- AsmResolver.DotNet 6.1.0 (restored automatically)

## Installation

Reference the project from your solution:

```bash
dotnet add reference path/to/AsmLocator.csproj
```

The project is configured to produce a NuGet package named `AsmLocator`; publish it to NuGet before instructing consumers to install it with `dotnet add package AsmLocator`.

## Quick start

```csharp
using AsmLocator;

var locator = AssemblyLocator.FromFile("MyApplication.dll");
TypeSpec type = TypeSpec.ByFullName("MyApplication.Services.UserService");

var method = locator.GetMethod(
    type,
    MethodSpec.Of("FindUser", "System.String"));

var calls = locator.FindInstructions(
    type,
    MethodSpec.ByName("FindUser"),
    InstructionSpec.Call("*System.Console::WriteLine*"));

foreach (var call in calls)
    Console.WriteLine($"IL_{call.Instruction.Offset:X4}: {call.Instruction}");

locator.Save("MyApplication.patched.dll");
```

## Lookup semantics

| API | Behavior |
|---|---|
| `FindX` | Returns the first match or `null`. |
| `FindXs` | Returns all matches as a lazy `IEnumerable<T>`. |
| `GetX` | Requires exactly one match; otherwise throws `MemberNotFoundException`. |
| `EditX` | Resolves exactly one target and invokes an edit callback. |

`GetX` uses `MemberNotFoundException` both when no result exists and when the query is ambiguous. Make the specification more specific to resolve ambiguity.

## Matching strings

```csharp
StringMatcher.Exact("Execute");
StringMatcher.Wildcard("Get*");
StringMatcher.Wildcard("get*", ignoreCase: true);
StringMatcher.FromRegex(@"^Get[A-Z]\w+$");
StringMatcher.Where(value => value.Contains("Service"));
```

Implicit string conversion uses `StringMatcher.Parse`: strings containing `*` or `?` are treated as wildcard patterns; other strings are exact matches. Wildcard and regular-expression matching have a two-second timeout.

## Searching methods

```csharp
var spec = MethodSpec.ByName("Execute") with
{
    IsPublic = true,
    IsStatic = true,
    ParameterCount = 2,
    ReturnType = "System.Boolean",
    HasBody = true
};

var method = locator.GetMethod(type, spec);
```

Use `MethodSpec.Of(name, parameterTypes...)` to match an exact ordered parameter signature, `NoParameters` for parameterless methods, and `WithParameters` for detailed parameter matching.

## Searching CIL

```csharp
var pattern = locator.FindPattern(
    type,
    MethodSpec.ByName("Execute"),
    InstructionSpec.LdStr("secret"),
    InstructionSpec.AnyInstruction,
    InstructionSpec.Call("*WriteLine*"));
```

`InstructionSpec.Call` matches only `call` and `callvirt`; it deliberately excludes `calli`. `InstructionMatch.Index` and `InstructionSequence.StartIndex` refer to the instruction collection at search time.

## Editing

```csharp
locator.EditBody(type, MethodSpec.ByName("Execute"), body =>
{
    // Modify body.Instructions here.
});

locator.EditInstructions(
    type,
    MethodSpec.ByName("Execute"),
    InstructionSpec.LdStr("old"),
    (body, match) => match.Instruction.Operand = "new");
```

The editing helpers recalculate instruction offsets in a `finally` block. They do not automatically verify or repair stack behavior, branch targets, exception-handler boundaries, or other semantic consequences of arbitrary IL changes. When editing instruction collections, process matches carefully; `EditInstructions` invokes callbacks from the last match to the first to reduce index invalidation.

## Nested types

```csharp
var direct = typeDefinition.FindNestedTypes(TypeSpec.ByName("*")).ToList();
var allDescendants = typeDefinition.FindNestedTypes(TypeSpec.ByName("*"), recursive: true).ToList();
```

## Documentation

The DocFX documentation source is in the `docs/` directory. Build it with:

```bash
dotnet tool restore
dotnet docfx docs/docfx.json
```

The generated site is written to `docs/_site`.

## Development

```bash
dotnet restore
dotnet build
dotnet test
```

## License

No license has been selected for this source distribution yet. Add a `LICENSE` file and set the package license metadata before publishing the package publicly.

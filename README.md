# AsmLocator

> Locate, inspect and edit .NET assembly metadata and CIL method bodies — **with readable specifications instead of nested loops.**

AsmLocator is a .NET library built on top of [AsmResolver](https://github.com/Washi1337/AsmResolver). Instead of writing long `foreach` / `if` chains, you **describe what you are looking for** (`TypeSpec`, `MethodSpec`, `InstructionSpec`, ...) and let the library find it.

## Table of contents

- [A real-world example: the target application](#a-real-world-example-the-target-application)
- [Using plain AsmResolver](#using-plain-asmresolver)
- [Using AsmLocator](#using-asmlocator)
- [Side-by-side comparison](#side-by-side-comparison)
- [Features](#features)
- [Installation](#installation)
- [Quick reference](#quick-reference)
- [Documentation](#documentation)
- [Credits](#credits)
- [License](#license)

## A real-world example: the target application

Imagine an application called `MyApp.dll` with a license-validation class. This is its source code (which you do **not** have — you only have the compiled file):

```csharp
namespace MyApp.Licensing;

public class LicenseValidator
{
    private const string ServerUrl = "https://license.example.com/check";
    private int _attempts;

    // Overload #1 — the method we are after
    public bool Validate(string key)
    {
        Console.WriteLine("Validating license...");

        if (key.StartsWith("PRO-"))
        {
            Console.WriteLine("License accepted.");
            return true;
        }

        _attempts++;
        Console.WriteLine("Invalid key.");
        return false;
    }

    // Overload #2 — same name, different signature
    public bool Validate(string key, bool online)
    {
        Console.WriteLine($"Contacting {ServerUrl}");
        return online && key.Length > 8;
    }

    private static string Hash(string input) => input.GetHashCode().ToString("X");
}
```

### Our goal

In `MyApp.dll` we want to:

1. Find the type `MyApp.Licensing.LicenseValidator`.
2. Find **only** `public bool Validate(string)` (not the second overload).
3. List every `Console.WriteLine` call inside that method.
4. Find the pattern `ldarg.1 → ldstr "PRO-" → String.StartsWith`, change `"PRO-"` to `"ENT-"`, and save a new file.

## Using plain AsmResolver

AsmResolver is excellent, but for every lookup you write the loops, conditions, null checks and the "exactly one result" logic yourself:

```csharp
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

var module = ModuleDefinition.FromFile("MyApp.dll");

// ── 1) Find the type ──────────────────────────────────────────────
var type = module.GetAllTypes()
    .FirstOrDefault(t => t.FullName == "MyApp.Licensing.LicenseValidator")
    ?? throw new InvalidOperationException("Type not found.");

// ── 2) Find the method (handling overloads and ambiguity) ─────────
var candidates = type.Methods
    .Where(m => m.Name == "Validate"
             && m.IsPublic
             && !m.IsStatic
             && m.Parameters.Count == 1
             && m.Parameters[0].ParameterType.FullName == "System.String"
             && m.Signature?.ReturnType.FullName == "System.Boolean")
    .ToList();

if (candidates.Count == 0)
    throw new InvalidOperationException("Method not found.");
if (candidates.Count > 1)
    throw new InvalidOperationException("Method is ambiguous.");

var method = candidates[0];
var body = method.CilMethodBody
    ?? throw new InvalidOperationException("Method has no CIL body.");

// ── 3) Find every Console.WriteLine call ──────────────────────────
for (int i = 0; i < body.Instructions.Count; i++)
{
    var ins = body.Instructions[i];

    bool isCall = ins.OpCode.Code is CilCode.Call or CilCode.Callvirt;
    if (isCall
        && ins.Operand is IMethodDescriptor callee
        && callee.FullName?.Contains("System.Console::WriteLine") == true)
    {
        Console.WriteLine($"[{i}] {ins}");
    }
}

// ── 4) Find the pattern and edit it ───────────────────────────────
var ins2 = body.Instructions;
var hits = new List<int>();

for (int i = 0; i + 2 < ins2.Count; i++)
{
    bool match =
        ins2[i].OpCode.Code == CilCode.Ldarg_1
        && ins2[i + 1].OpCode.Code == CilCode.Ldstr
        && ins2[i + 1].Operand is string s && s == "PRO-"
        && ins2[i + 2].OpCode.Code is CilCode.Call or CilCode.Callvirt
        && ins2[i + 2].Operand is IMethodDescriptor m
        && m.FullName == "System.Boolean System.String::StartsWith(System.String)";

    if (match) hits.Add(i);
}

if (hits.Count != 1)
    throw new InvalidOperationException($"Expected one pattern, found {hits.Count}.");

ins2[hits[0] + 1].Operand = "ENT-";
ins2.CalculateOffsets();

module.Write("MyApp.patched.dll");
```

**About 50 lines of code (excluding comments and blank lines)** — and every block is a place to make a mistake: forgetting `Callvirt`, comparing a `null` `FullName`, counting overloads, or using a wrong index in the pattern.

## Using AsmLocator

The same four tasks, with the same behavior (including throwing when the result is missing or ambiguous):

```csharp
using AsmLocator;

var locator = AssemblyLocator.FromFile("MyApp.dll");

// Describe what we want
TypeSpec validator = "MyApp.Licensing.LicenseValidator";

var validate = MethodSpec.Of("Validate", "System.String") with
{
    IsPublic   = true,
    IsStatic   = false,
    ReturnType = "System.Boolean"
};

// ── 1 & 2) Type and method; MemberNotFoundException if missing or ambiguous ─
var method = locator.GetMethod(validator, validate);

// ── 3) Every Console.WriteLine call ───────────────────────────────
foreach (var call in locator.FindInstructions(
             validator, validate,
             InstructionSpec.Call("*System.Console::WriteLine*")))
{
    Console.WriteLine($"[{call.Index}] {call.Instruction}");
}

// ── 4) Pattern + edit + save ──────────────────────────────────────
var pattern = locator.GetPattern(
    validator, validate,
    InstructionSpec.WithMnemonic("ldarg.1"),
    InstructionSpec.LdStr("PRO-"),
    InstructionSpec.Call("*System.String::StartsWith(System.String)"));

// Edit: offsets are recalculated automatically
locator.EditInstructions(
    validator, validate,
    InstructionSpec.LdStr("PRO-"),
    (body, match) => match.Instruction.Operand = "ENT-");

locator.Save("MyApp.patched.dll");
```

> Note: `GetPattern` is for **finding** a pattern; it throws if the pattern is missing or matches more than once. Editing is done with `EditInstructions`.

**About 24 lines** — and most of it simply describes what you have in mind.

## Side-by-side comparison

| Topic | Plain AsmResolver | AsmLocator |
|---|---|---|
| Find a type | `GetAllTypes().FirstOrDefault(...)` + null check | `TypeSpec x = "Ns.Type"` |
| Pick one overload | Manual conditions on `Parameters` and `Signature` | `MethodSpec.Of("Validate", "System.String")` |
| "Exactly one result" | Manual `Count` check and `throw` | Built into `GetX`, with a readable error message |
| Method calls (call/callvirt) | Manual `CilCode` and `Operand` checks | `InstructionSpec.Call("*...*")` |
| Instruction patterns | Loops with hand-written indexes | `GetPattern(...)` / `FindPattern(...)` |
| Editing many locations | Risk of shifting indexes | `EditInstructions` runs from last match to first |
| Offset recalculation | You must remember it | Done automatically in a `finally` block |
| Name matching | Exact comparison only | Exact, wildcard (`*`, `?`), regex, or predicate |
| Lines in this example | ~50 | ~24 |

> AsmLocator does not replace AsmResolver; it is a search layer on top of it. You always have access to `locator.Module` and every AsmResolver object (`MethodDefinition`, `CilMethodBody`, ...).

## Features

- Find types, fields, properties, events, methods, parameters, local variables, and CIL instructions.
- Describe queries with reusable `TypeSpec`, `MethodSpec`, `FieldSpec`, `PropertySpec`, `EventSpec`, `ParameterSpec`, `LocalSpec`, and `InstructionSpec` objects.
- Match strings using exact, wildcard (`*`, `?`), regular-expression, or custom-predicate matching.
- Search for contiguous instruction patterns and inspect their original instruction indexes.
- Edit metadata and method bodies through callbacks.
- Search nested types directly or recursively.
- Save the modified module through AsmResolver.

## Installation

Requirements:

- .NET 10 SDK
- AsmResolver.DotNet 6.1.0 (restored automatically)

Install from [NuGet](https://www.nuget.org/packages/AsmLocator):

```bash
dotnet add package AsmLocator
```

## Quick reference

### Lookup semantics

| API | Behavior |
|---|---|
| `FindX` | Returns the first match or `null`. |
| `FindXs` | Returns all matches as a lazy `IEnumerable<T>`. |
| `GetX` | Requires exactly one match; otherwise throws `MemberNotFoundException`. |
| `EditX` | Resolves exactly one target and invokes an edit callback. |

`GetX` throws `MemberNotFoundException` both when nothing matches and when the query is ambiguous. Make the specification more specific to resolve ambiguity.

### Matching strings

```csharp
StringMatcher.Exact("Execute");
StringMatcher.Wildcard("Get*");
StringMatcher.Wildcard("get*", ignoreCase: true);
StringMatcher.FromRegex(@"^Get[A-Z]\w+$");
StringMatcher.Where(value => value.Contains("Service"));
```

Implicit string conversion uses `StringMatcher.Parse`: strings containing `*` or `?` are wildcard patterns; all others are exact matches. Wildcard and regular-expression matching have a two-second timeout.

### Searching methods

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

- `MethodSpec.Of(name, parameterTypes...)` — exact, ordered parameter signature
- `MethodSpec.NoParameters(name)` — parameterless methods
- `MethodSpec.WithParameters(name, ...)` — detailed matching with `ParameterSpec`

### Searching CIL

```csharp
var pattern = locator.FindPattern(
    type,
    MethodSpec.ByName("Execute"),
    InstructionSpec.LdStr("secret"),
    InstructionSpec.AnyInstruction,
    InstructionSpec.Call("*WriteLine*"));
```

`InstructionSpec.Call` matches only `call` and `callvirt`; it deliberately excludes `calli`. `InstructionMatch.Index` and `InstructionSequence.StartIndex` refer to the instruction collection at search time.

### Editing

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

The editing helpers recalculate instruction offsets in a `finally` block. They do **not** verify or repair stack behavior, branch targets, exception-handler boundaries, or other semantic consequences of arbitrary IL changes. Always save the result to a new file and test it.

### Nested types

```csharp
var direct = typeDefinition.FindNestedTypes(TypeSpec.ByName("*")).ToList();
var all    = typeDefinition.FindNestedTypes(TypeSpec.ByName("*"), recursive: true).ToList();
```

## Credits

- [AsmResolver](https://github.com/Washi1337/AsmResolver) — a library for creating, reading and editing PE files and .NET modules.

## License

MIT — see the [LICENSE](LICENSE) file.

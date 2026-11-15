# AsmLocator documentation

AsmLocator is a .NET library for **finding, inspecting and editing** parts of compiled .NET assemblies — types, fields, properties, events, methods, parameters, local variables and individual CIL instructions — by *describing what you are looking for* instead of writing nested loops.

It is built on top of [AsmResolver](https://github.com/Washi1337/AsmResolver), so everything AsmLocator returns is a normal AsmResolver object that you can keep working with.

```csharp
using AsmLocator;

var locator = AssemblyLocator.FromFile("Sample.dll");

// "Which calls to Console.WriteLine are inside Greeter.Greet?"
var calls = locator.FindInstructions(
    TypeSpec.ByFullName("Sample.Greeter"),
    MethodSpec.ByName("Greet"),
    InstructionSpec.Call("*Console::WriteLine*"));

foreach (var call in calls)
    Console.WriteLine($"IL_{call.Instruction.Offset:X4}: {call.Instruction}");
```

## Where do I start?

Pick the path that matches your experience. Every page builds on the one before it, so you can also simply read them top to bottom.

| You are... | Read this |
|---|---|
| New to .NET assemblies and IL | [What is AsmLocator?](articles/introduction.md) → [Concepts](articles/concepts.md) → [Getting started](articles/getting-started.md) → [Tutorial](articles/tutorial.md) |
| Comfortable with C#, new to AsmLocator | [Getting started](articles/getting-started.md) → [Tutorial](articles/tutorial.md) → [Specifications](articles/specifications.md) |
| Looking for a specific task | [Cookbook](articles/cookbook.md) |
| Hitting an error | [Troubleshooting](articles/troubleshooting.md) |
| Looking for exact property and method names | [Specification reference](articles/reference-specs.md), [Locator reference](articles/reference-locator.md), [API reference](api/toc.yml) |

## The learning path

**Beginner**

1. [What is AsmLocator?](articles/introduction.md) — the problem it solves and when to use it.
2. [Concepts](articles/concepts.md) — assemblies, metadata and CIL in plain language.
3. [Getting started](articles/getting-started.md) — install, load a file, run your first query.
4. [Tutorial: your first patch](articles/tutorial.md) — explore, search, edit, save and verify a real assembly.

**Intermediate**

5. [Specifications](articles/specifications.md) — describing what you want.
6. [String matching](articles/string-matching.md) — exact, wildcard, regex and custom matching.
7. [Find, Get and Edit](articles/lookup-semantics.md) — the rules every lookup method follows.
8. [Searching members](articles/searching-members.md) — types, fields, properties, events, methods, parameters and locals.
9. [Searching CIL instructions](articles/cil-search.md) — single instructions and instruction patterns.
10. [Editing assemblies](articles/editing.md) — changing method bodies and metadata safely.

**Advanced**

11. [Cookbook](articles/cookbook.md) — complete, copy-ready recipes.
12. [Advanced topics](articles/advanced.md) — performance, custom predicates, obfuscated code, testing your patches.
13. [Troubleshooting](articles/troubleshooting.md) — error messages and what they mean.

**Reference**

- [Specification reference](articles/reference-specs.md)
- [Locator reference](articles/reference-locator.md)
- [Glossary](articles/glossary.md)
- [API reference](api/toc.yml)

## The core workflow in five lines

1. Load a module with `AssemblyLocator.FromFile` or `AssemblyLocator.FromBytes`.
2. Describe what you want with specification objects (`TypeSpec`, `MethodSpec`, ...).
3. Ask with `FindX` (first or `null`), `FindXs` (all) or `GetX` (exactly one).
4. Inspect or edit the returned AsmResolver objects.
5. `Save` to a **new** file and verify it.

# Advanced topics

This page covers performance, composing specs, hard targets such as obfuscated code, structuring and testing patchers, and the limits of the library.

## Performance

**Cheap constraints first.** Metadata checks (name, flags, counts, types) are fast. Anything that reads a method body — `HasBody`, `Locals`, `BodyContains`, and all instruction searches — has to decode CIL. AsmLocator evaluates cheap constraints first and reads the body only if a candidate survives, so add selective cheap constraints next to expensive ones:

```csharp
// Good: the name filter rejects almost every method before any body is read.
MethodSpec.ByName("Decrypt*") with { BodyContains = new[] { InstructionSpec.LdStr("key") } }

// Slow on big assemblies: every method body is decoded.
new MethodSpec { BodyContains = new[] { InstructionSpec.LdStr("key") } }
```

**Enumeration is lazy and repeatable.** `FindXs` re-runs the search on every enumeration. Call `ToList()` once and reuse the list.

**The type list is cached.** `AssemblyLocator` builds the list of all types once. Repeated `FindTypes` calls are cheap; the cost is the spec evaluation. Call `Refresh()` only after adding or removing types.

**Load once.** `FromBytes(File.ReadAllBytes(path))` and `FromFile(path)` both parse the module; do it once and reuse the locator.

**Decoded bodies are kept.** Once a method body has been read, AsmResolver keeps it on the `MethodDefinition`. A full-module scan of a very large assembly therefore uses memory proportional to the amount of code it touched.

## Composing specs

Specs are plain values, so ordinary C# composes them.

**Helper methods that build specs:**

```csharp
static MethodSpec PublicStatic(string name) =>
    MethodSpec.ByName(name) with { IsPublic = true, IsStatic = true };
```

**OR logic** — run several searches, or use a regex or predicate:

```csharp
var either = locator.FindMethods(type, MethodSpec.ByName(StringMatcher.FromRegex("^(Load|Read)$")));

var union = new[] { "Load", "Read" }
    .SelectMany(n => locator.FindMethods(type, MethodSpec.ByName(n)));
```

**NOT logic** has no dedicated property. Use `Where`, for example "every method *except* ones named `Dispose`":

```csharp
MethodSpec.ByName("*") with { Where = m => m.Name != "Dispose" }
```

**Share common parts:**

```csharp
var core = new MethodSpec { IsPublic = true, HasBody = true };
var getters = core with { Name = "get_*" };
var setters = core with { Name = "set_*" };
```

## Targets that cannot be named

When names are unreliable (obfuscated code, generated code, code that changes between versions), describe the target by **what it does** instead of what it is called.

| Instead of... | Describe... |
|---|---|
| a method name | its signature (`ReturnType`, `ParameterCount`, `Parameters`), its flags, and `BodyContains` |
| a field name | its type and flags |
| a specific position | an instruction **pattern**, not an index or offset |
| a type name | its base type, interfaces, attributes or members |

Example — find a method that decodes strings: static, takes an `int`, returns a `string`, and calls `String.Intern`:

```csharp
var decoders = locator.FindTypes(new TypeSpec())
    .SelectMany(t => t.FindMethods(MethodSpec.Of("*", "System.Int32") with
    {
        IsStatic = true,
        ReturnType = "System.String",
        BodyContains = new[] { InstructionSpec.Call("*String::Intern*") }
    }))
    .ToList();
```

For odd *names* use a predicate matcher:

```csharp
// single-character or non-ASCII names
StringMatcher.Where(n => n.Length == 1 || n.Any(c => c > 127), "suspicious name");
```

Avoid hard-coding **offsets** and **instruction indexes**. They change with every compiler version and every edit; patterns and operand matching do not.

## Working with AsmResolver directly

`locator.Module` is the underlying `ModuleDefinition`. Anything AsmLocator does not cover is a normal AsmResolver call away, and the objects you get back are shared — edits made either way are the same edits:

```csharp
var module = locator.Module;
Console.WriteLine(module.Assembly?.Name);

foreach (var reference in module.AssemblyReferences)
    Console.WriteLine(reference.Name);
```

Typical reasons to drop down to AsmResolver:

- creating new types, fields or methods;
- changing assembly references or resources;
- configuring the PE writer (preserving metadata tokens, signing, ...) — write with `locator.Module` yourself instead of calling `locator.Save`;
- reading the data of a field or resource.

If you add or remove types, call `locator.Refresh()` afterwards.

## Structuring a patcher

A maintainable patcher usually follows this shape:

1. **Declare targets once** — `TypeSpec` and `MethodSpec` values in a static class.
2. **Use `Get`/`Edit` for required targets** so a changed input fails loudly.
3. **Use `Find` to detect "already patched"** so running the patcher twice is harmless.
4. **Write to a new file**, never over the input.
5. **Verify** by reloading the output and searching again.
6. **Report** what changed (counts, method names).

A dry-run mode is cheap to add: run every search, print what *would* change, skip `Save`.

## Testing patchers

Because AsmLocator works on any assembly, you can test a patcher against a small fixture class compiled into your test project:

1. Add a fixture class whose methods contain the code you patch.
2. Load it with `AssemblyLocator.FromFile(typeof(Fixture).Assembly.Location)`.
3. Apply your patch, `Save` to a temporary file, reload with `FromBytes(File.ReadAllBytes(tmp))`.
4. Assert on what the reloaded module contains.

Remember that compiler output differs between Debug and Release and between compiler versions (extra `nop`s, locals, different branch shapes). Make assertions on *operands and calls*, not on exact instruction counts, unless you pin the build configuration.

## Compiler-generated code

C# features compile into extra methods and types, so the code you wrote may not be where you expect:

| C# feature | Where the code ends up |
|---|---|
| `async` methods | a nested state-machine type; the logic is in its `MoveNext` method |
| iterators (`yield`) | a nested type with `MoveNext` |
| lambdas and local functions | methods like `<Run>b__0_0` inside a compiler-generated nested type (often named `<>c`) |
| closures | nested `<>c__DisplayClass...` types |
| string interpolation | depending on the compiler version, `string.Concat`, `string.Format` or an interpolated-string handler |
| auto-properties | a hidden backing field named `<Name>k__BackingField` |

Search them with the same specs, e.g. `new TypeSpec { IsNested = true, Name = "<*" }`.

## Error handling

| Exception | Raised when |
|---|---|
| `MemberNotFoundException` | a `GetX`/`EditX` call finds nothing, finds several, lacks an accessor, or hits a method with no body |
| `ArgumentNullException` | you passed `null` for a spec, callback or required argument |
| `ArgumentException` | a pattern is empty or contains `null` |
| `RegexMatchTimeoutException` | a wildcard or regex took longer than two seconds on one input |
| `BadImageFormatException` | the file is not a valid managed assembly |
| `IOException` | the file cannot be read or written (for example because it is in use) |

Exceptions thrown inside your own edit callbacks propagate unchanged, after AsmLocator has recalculated offsets.

## Thread safety and lifetime

An `AssemblyLocator` and the AsmResolver objects behind it are **not designed for concurrent use**, and edits are not synchronized. Use one locator per thread, or guard access with your own lock. Specs are immutable and can be shared freely. `AssemblyLocator` does not implement `IDisposable`; it holds no unmanaged resources of its own.

## Limits

- Managed .NET assemblies only.
- No IL verification — see [Editing assemblies](editing.md).
- Instruction searching works on one method at a time; module-wide searches are loops you write (see the [Cookbook](cookbook.md)).
- Specs are AND-only; use `Where`, regexes or multiple searches for OR and NOT.
- No built-in cross-reference index (who-calls-whom); build one with the loop from the Cookbook if you need it.

# What is AsmLocator?

## The problem

A compiled .NET program (`.dll` or `.exe`) is not just machine code. It carries a rich description of itself — every type, method and field — plus the instructions for each method in an intermediate language called **CIL** (see [Concepts](concepts.md)). Tools such as analyzers, patchers, instrumentation tools, deobfuscators and test utilities need to find specific pieces of that structure and sometimes change them.

[AsmResolver](https://github.com/Washi1337/AsmResolver) gives you full access to all of it. But "find the method `Greet` of type `Sample.Greeter` that takes one string and calls `Console.WriteLine`" quickly turns into nested loops and manual checks:

```csharp
// Plain AsmResolver
var module = ModuleDefinition.FromFile("Sample.dll");

foreach (var type in module.GetAllTypes())
{
    if (type.FullName != "Sample.Greeter") continue;

    foreach (var method in type.Methods)
    {
        if (method.Name != "Greet" || method.Parameters.Count != 1) continue;
        if (method.CilMethodBody is not { } body) continue;

        foreach (var instruction in body.Instructions)
        {
            if (instruction.OpCode.Code is CilCode.Call or CilCode.Callvirt
                && instruction.Operand is IMethodDescriptor target
                && target.FullName.Contains("Console::WriteLine"))
            {
                Console.WriteLine(instruction);
            }
        }
    }
}
```

## The AsmLocator approach

You *describe* the target, and AsmLocator does the searching:

```csharp
// AsmLocator
var locator = AssemblyLocator.FromFile("Sample.dll");

var calls = locator.FindInstructions(
    TypeSpec.ByFullName("Sample.Greeter"),
    MethodSpec.Of("Greet", "System.String"),
    InstructionSpec.Call("*Console::WriteLine*"));
```

The pieces you describe with — `TypeSpec`, `MethodSpec`, `InstructionSpec` and friends — are small, reusable, immutable objects. You can store them in fields, share them between searches, tweak them with `with`, and print them for diagnostics.

## What you get

- **One consistent API** for eight kinds of targets: types, fields, properties, events, methods, parameters, local variables and instructions.
- **Flexible text matching**: exact, wildcard (`*`, `?`), regular expression or your own predicate.
- **CIL searching**, including contiguous *patterns* of instructions with wildcards.
- **Controlled editing** through callbacks, with automatic offset recalculation.
- **Predictable behaviour**: `Find` returns first-or-null, `FindXs` returns everything, `Get` demands exactly one result, `Edit` resolves one target and runs your callback.
- **No lock-in**: every result is an ordinary AsmResolver object, and `locator.Module` gives you the underlying `ModuleDefinition`.

## What AsmLocator is *not*

| It is not... | Because... |
|---|---|
| A decompiler | It shows you IL instructions, not C#. |
| An IL verifier | It recalculates offsets after edits but does **not** check stack balance, branch targets or exception regions. See [Editing assemblies](editing.md). |
| A runtime hooking or injection framework | It works on files and byte arrays, not on running processes. |
| A replacement for AsmResolver | It is a thin lookup-and-edit layer *on top of* AsmResolver. For anything AsmLocator does not cover, use AsmResolver directly through `locator.Module`. |
| Able to read native code | Only managed .NET assemblies can be loaded. |

## When should I use it?

Use AsmLocator when you want to:

- locate members or instructions without writing boilerplate loops;
- write patchers or analyzers whose targets are described declaratively;
- express targets that survive small changes (a wildcard name, a parameter count, an instruction pattern) rather than hard-coded metadata tokens;
- keep search logic readable and testable.

Reach for plain AsmResolver (via `locator.Module`) when you need to build new types or methods from scratch, control the PE writer, or work with structures AsmLocator has no spec for.

## What next?

[Concepts](concepts.md) explains the vocabulary — assembly, metadata, CIL, opcode, operand — in plain language. If you already know it, jump to [Getting started](getting-started.md).

# Searching CIL instructions

Method bodies are lists of CIL instructions. AsmLocator lets you search them with `InstructionSpec`, either one instruction at a time or as a contiguous pattern. If CIL is new to you, read [Concepts](concepts.md) first.

All instruction searches work on **one method**: the `TypeSpec` and `MethodSpec` you pass must each resolve to exactly one type and one method (see [Find, Get and Edit](lookup-semantics.md)).

## Finding single instructions

```csharp
var calls = locator.FindInstructions(
    typeSpec,
    MethodSpec.ByName("Run"),
    InstructionSpec.Call("*System.Console::WriteLine*"));

foreach (var match in calls)
    Console.WriteLine($"[{match.Index}] IL_{match.Instruction.Offset:X4}: {match.Instruction}");
```

Each result is an `InstructionMatch` with:

| Member | Meaning |
|---|---|
| `Instruction` | the real `CilInstruction` |
| `Index` | its position in `body.Instructions` **at the moment of the search** |

`GetInstruction` demands exactly one match and throws `MemberNotFoundException` otherwise.

## Built-in instruction specs

| Factory | Matches |
|---|---|
| `InstructionSpec.Call(method)` | `call` and `callvirt` whose called method matches. **Not** `calli`. |
| `InstructionSpec.NewObj(ctor)` | `newobj` whose constructor matches |
| `InstructionSpec.LdStr(text)` | `ldstr` whose string literal matches |
| `InstructionSpec.Op(opCode, operand?)` | an exact opcode, optionally with an operand matcher |
| `InstructionSpec.WithMnemonic(mnemonic, operand?)` | opcodes whose name matches (wildcards allowed) |
| `InstructionSpec.AnyInstruction` | any single instruction (a pattern wildcard) |

For anything else, build a spec with the properties directly:

```csharp
new InstructionSpec
{
    Code = CilCode.Ldfld,                    // exact opcode (needs: using AsmResolver.PE.DotNet.Cil;)
    Mnemonic = "ldc.i4*",                    // opcode name matcher
    Operand = "*Sample.Greeter::_prefix",    // operand text matcher
    Offset = 0x1A,                           // exact byte offset
    Where = i => /* any custom test */ true
}
```

Examples:

```csharp
// every load of the field _prefix
InstructionSpec.WithMnemonic("ldfld", "*Greeter::_prefix");

// every ret
InstructionSpec.Op(CilOpCodes.Ret);

// every conditional branch on true/false
InstructionSpec.WithMnemonic("brtrue*");
InstructionSpec.WithMnemonic("brfalse*");
```

## How operands become text

`Operand` is a `StringMatcher`, so the operand has to be turned into text first. `InstructionSpec.DescribeOperand` does that:

| Operand | Text |
|---|---|
| Method, constructor or field reference | its `FullName`, e.g. `System.Void System.Console::WriteLine(System.String)` |
| Type reference | its `FullName` |
| String (`ldstr`) | the literal value itself |
| Local variable | `V_0`, `V_1`, ... |
| Parameter | the parameter's name |
| Branch target | `IL_XXXX` (hexadecimal offset) |
| Switch targets | comma-separated `IL_XXXX` values |
| Numbers and anything else | invariant-culture text, e.g. `50` |
| *no operand* | `null` — matches only matchers that accept empty text, such as `"*"` |

To see what text an instruction produces, print it:

```csharp
Console.WriteLine(InstructionSpec.DescribeOperand(instruction));
```

### Numeric constants and the "no operand" trap

`ldc.i4.5` pushes 5 but carries its value in the opcode, so it has **no operand**. A search like `InstructionSpec.WithMnemonic("ldc.i4*", "5")` therefore finds `ldc.i4.s 5` but misses `ldc.i4.5`. Two ways around it:

```csharp
// 1. Test with a predicate using AsmResolver's constant helper
var five = new InstructionSpec
{
    Where = i => i.IsLdcI4() && i.GetLdcI4Constant() == 5
};

// 2. Expand the "short form" opcodes first (this modifies the body!)
body.Instructions.ExpandMacros();
```

Strings (`ldstr`) have no such problem — the literal is always the operand.

## Contiguous patterns

`FindPattern` finds runs of consecutive instructions that match a list of specs, in order:

```csharp
var matches = locator.FindPattern(
    typeSpec,
    MethodSpec.ByName("Run"),
    InstructionSpec.LdStr("token"),            // 1st instruction
    InstructionSpec.AnyInstruction,            // 2nd: anything
    InstructionSpec.Call("*WriteLine*"));      // 3rd

foreach (var sequence in matches)
{
    Console.WriteLine($"pattern at index {sequence.StartIndex}");
    foreach (var instruction in sequence.Instructions)
        Console.WriteLine($"  {instruction}");
}
```

Rules:

- A pattern matches **adjacent** instructions. There is no "any number of instructions in between" — use `AnyInstruction` once per skipped instruction.
- `AnyInstruction` consumes exactly one instruction.
- An empty pattern, or one that contains `null`, throws `ArgumentException` immediately.
- Matches can overlap. Searching for `[nop, nop]` in `nop nop nop` returns two matches.
- `InstructionSequence.StartIndex` and `Instructions` describe the match at search time.

`GetPattern` demands exactly one match.

## Indexes are snapshots

`InstructionMatch.Index` and `InstructionSequence.StartIndex` are positions in the instruction list at the moment you searched. Inserting or removing instructions changes the positions of everything after them. After any edit, **search again** instead of reusing old indexes. Offsets (`instruction.Offset`) are byte positions that only become correct after AsmLocator (or you) recalculates them, and `InstructionSpec.Offset` compares against the *current* value. See [Editing assemblies](editing.md).

## Common searches

```csharp
// Where is a string literal used?
locator.FindInstructions(type, "Run", InstructionSpec.LdStr("secret"));

// Every call to a method declared in the System.IO namespace.
// The operand text starts with the return type, so the declaring type comes after a space:
//   System.Void System.IO.File::Delete(System.String)
locator.FindInstructions(type, "Run", InstructionSpec.Call("* System.IO.*::*"));

// Every object creation of a type
locator.FindInstructions(type, "Run", InstructionSpec.NewObj("*System.Text.StringBuilder::.ctor*"));

// Every write to a static field
locator.FindInstructions(type, "Run", InstructionSpec.WithMnemonic("stsfld", "*Settings::Mode"));

// Every throw
locator.FindInstructions(type, "Run", InstructionSpec.Op(CilOpCodes.Throw));
```

To search *every* method of a type or module rather than one, loop over the methods yourself — see [Cookbook](cookbook.md).

## Searching a body you already hold

If you already have a `CilMethodBody` you can search it without a locator:

```csharp
var matches   = body.FindInstructions(InstructionSpec.Call("*WriteLine*"));
var sequences = body.FindPattern(new[] { InstructionSpec.LdStr("x"), InstructionSpec.AnyInstruction });
var locals    = body.FindLocals(LocalSpec.OfType("System.Int32"));
```

## Where next?

[Editing assemblies](editing.md) turns what you found into changes.

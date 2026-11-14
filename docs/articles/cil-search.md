# Searching CIL instructions

`InstructionSpec` can constrain the opcode (`Code`), mnemonic, operand text, offset, or a custom predicate.

```csharp
var calls = locator.FindInstructions(
    typeSpec,
    MethodSpec.ByName("Run"),
    InstructionSpec.Call("*System.Console::WriteLine*"));
```

`InstructionSpec.Call` matches only the `call` and `callvirt` opcodes. It does not match `calli`. `NewObj` matches `newobj`; `LdStr` matches `ldstr`.

## Operand text

`DescribeOperand` converts common operands as follows:

| Operand | Representation |
|---|---|
| Member descriptor | `FullName` |
| Type descriptor | `FullName` |
| String | Literal value |
| Local | `V_0`, `V_1`, ... |
| Parameter | Parameter name |
| Branch label | `IL_XXXX` |
| Switch labels | Comma-separated `IL_XXXX` values |
| Other values | Invariant-culture string conversion |

## Contiguous patterns

```csharp
var matches = locator.FindPattern(
    typeSpec,
    MethodSpec.ByName("Run"),
    InstructionSpec.LdStr("token"),
    InstructionSpec.AnyInstruction,
    InstructionSpec.Call("*WriteLine*"));
```

Patterns are contiguous and ordered. `AnyInstruction` consumes exactly one instruction. Empty patterns throw `ArgumentException`.

`InstructionMatch.Index` and `InstructionSequence.StartIndex` are indexes into the instruction collection at search time. They are not stable identifiers after edits.

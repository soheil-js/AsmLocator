# Concepts

This page explains just enough about .NET assemblies to use AsmLocator confidently. If you already know what a method body and an opcode are, skim the tables and move on to [Getting started](getting-started.md).

## Assemblies, modules, types and members

When you compile C#, you get an **assembly** — normally a single `.dll` or `.exe` file. Inside it:

```text
Assembly
└── Module                      <- what AssemblyLocator loads (ModuleDefinition)
    └── Types                   <- classes, structs, interfaces, enums (TypeDefinition)
        ├── Fields              <- FieldDefinition
        ├── Properties          <- PropertyDefinition (get/set accessors are methods)
        ├── Events              <- EventDefinition   (add/remove accessors are methods)
        ├── Methods             <- MethodDefinition
        │   ├── Parameters      <- Parameter
        │   └── Body            <- CilMethodBody
        │       ├── Local variables   <- CilLocalVariable
        │       └── Instructions      <- CilInstruction
        └── Nested types        <- more TypeDefinitions
```

Each level has an AsmResolver class (shown on the right). AsmLocator has a *specification* class for each one:

| You are looking for | AsmLocator spec | AsmResolver result |
|---|---|---|
| a type | `TypeSpec` | `TypeDefinition` |
| a field | `FieldSpec` | `FieldDefinition` |
| a property | `PropertySpec` | `PropertyDefinition` |
| an event | `EventSpec` | `EventDefinition` |
| a method or constructor | `MethodSpec` | `MethodDefinition` |
| a parameter | `ParameterSpec` | `Parameter` |
| a local variable | `LocalSpec` | `CilLocalVariable` |
| a CIL instruction | `InstructionSpec` | `CilInstruction` |

## Metadata versus code

A .NET file contains two kinds of information:

- **Metadata** — the *names and shapes*: which types exist, which methods they have, what each parameter is called and what type it has. Searching metadata is cheap.
- **CIL code** — the *method bodies*: the actual instructions that run. Reading a body means decoding bytes into instructions, which costs more. AsmLocator only reads a body when a constraint needs it (for example `HasBody`, `Locals`, `BodyContains`, or any instruction search).

## CIL in two minutes

CIL (Common Intermediate Language) is the instruction set that C# compiles to. It is a **stack machine**: instructions push values onto an evaluation stack, and other instructions pop them.

The C# method

```csharp
public int Add(int a, int b) => a + b;
```

compiles to roughly:

```text
ldarg.1     // push argument a   (argument 0 is "this" in an instance method)
ldarg.2     // push argument b
add         // pop both, push their sum
ret         // return the value on top of the stack
```

Every **instruction** has:

| Part | Meaning | Example |
|---|---|---|
| **Opcode** | What to do. Its text name is the *mnemonic*. | `ldstr`, `call`, `ldc.i4.5`, `ret` |
| **Operand** | Optional extra data the opcode needs. | the string for `ldstr`, the method for `call`, a branch target |
| **Offset** | The instruction's byte position inside the method body. | `IL_005E` |

Opcodes you will meet most often:

| Opcode | What it does | Operand |
|---|---|---|
| `ldstr` | push a string literal | the string |
| `ldc.i4`, `ldc.i4.s`, `ldc.i4.0`...`ldc.i4.8` | push a 32-bit integer | a number, **or none** for the short `ldc.i4.N` forms |
| `ldarg`, `ldloc`, `stloc` | load an argument, load or store a local | argument or local |
| `call`, `callvirt` | call a method (direct / virtual) | the method |
| `newobj` | create an object by calling a constructor | the constructor |
| `ldfld`, `stfld`, `ldsfld`, `stsfld` | read or write an instance or static field | the field |
| `br`, `brtrue`, `brfalse`, `beq`... | jump | branch target label |
| `ret` | return | none |
| `nop` | do nothing | none |

> [!NOTE]
> Some constants have their value built into the opcode. `ldc.i4.5` pushes the number 5 and has **no operand**, whereas `ldc.i4.s 50` has an operand. This matters when you search by operand text — see [Searching CIL instructions](cil-search.md).

## Names: how things are written

AsmLocator matches against names written the way AsmResolver writes them. Learn these shapes once and string matching becomes predictable.

| Thing | Typical text |
|---|---|
| Type full name | `Sample.Greeter` |
| Nested type | `Sample.Outer+Inner` (a `+` separates the nested type) |
| Generic type | ``System.Collections.Generic.List`1`` (backtick plus the number of generic parameters) |
| Parameter or field type | `System.String`, `System.Int32`, `System.String[]` |
| Method *as an operand* | `System.Void System.Console::WriteLine(System.String)` (return type, declaring type, `::`, name, parameters) |
| Constructor name | `.ctor` (instance) and `.cctor` (static) |
| Property accessors | `get_Name`, `set_Name` |
| Event accessors | `add_Changed`, `remove_Changed` |

> [!TIP]
> If you are unsure how something is written, **print it**: `Console.WriteLine(method.FullName)`, `Console.WriteLine(instruction)`. Then copy the text into a wildcard such as `*Console::WriteLine*`.

## Three layers

```text
 Your code
    │
    ▼
 AsmLocator      <- describe targets, find them, run edit callbacks
    │
    ▼
 AsmResolver     <- reads, models and writes the .NET file
    │
    ▼
 .dll / .exe on disk (or a byte array)
```

AsmLocator never hides AsmResolver. A found method is a real `MethodDefinition`; a found instruction is a real `CilInstruction`. If AsmLocator does not offer something, use the AsmResolver API on that object, or on `locator.Module`.

## Next

[Getting started](getting-started.md) takes you from an empty project to a working query.

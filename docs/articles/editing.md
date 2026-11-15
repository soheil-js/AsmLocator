# Editing assemblies

AsmLocator edits through **callbacks**. It finds the target, hands you the underlying AsmResolver object, and cleans up afterwards. The editing itself uses the AsmResolver API you already have in your hands.

> [!WARNING]
> Editing compiled code can produce a file that crashes or misbehaves. AsmLocator recalculates instruction offsets for you, but it does **not** verify the IL. Read [What stays your responsibility](#what-stays-your-responsibility) before patching anything important, and always [verify the result](#verifying-your-patch).

## The edit methods

| Method | Your callback receives | Typical use |
|---|---|---|
| `EditType` | `TypeDefinition` | change attributes, add members |
| `EditField` | `FieldDefinition` | change attributes or default values |
| `EditProperty` | `PropertyDefinition` | change accessors |
| `EditEvent` | `EventDefinition` | change accessors |
| `EditMethod` | `MethodDefinition` | change flags, name, signature |
| `EditBody` | `CilMethodBody` | rewrite the whole method |
| `EditInstructions` | `CilMethodBody` + one `InstructionMatch` per match | change selected instructions |

Each `EditX` resolves **exactly one** target (like `GetX`) and throws `MemberNotFoundException` if there is none or more than one. `EditInstructions` also throws when no instruction matches, so a patch can never silently do nothing.

`EditBody` has overloads that take a property or event accessor, so you can edit a getter in one call:

```csharp
locator.EditBody(type, PropertySpec.ByName("IsLicensed"), PropertyAccessor.Get, body =>
{
    // rewrite the getter
});
```

## What AsmLocator does for you

After `EditBody` or `EditInstructions`, AsmLocator calls `body.Instructions.CalculateOffsets()` in a `finally` block. That means offsets are consistent even if your callback threw halfway through a change.

`EditInstructions` calls your callback once for each match, from the **last match to the first**. Edits near the end of the method therefore cannot shift the position of matches you have not processed yet.

## What stays your responsibility

Recalculating offsets is not verification. Arbitrary edits can still produce invalid IL. You must make sure of:

- **Stack balance.** Every path through the method must leave the evaluation stack in the state the following instructions expect, and `ret` must find exactly the return value (or nothing for `void`).
- **Branch and switch targets** still point at instructions that exist.
- **Exception-handler boundaries** (`try`/`catch`/`finally` ranges) still enclose the right instructions.
- **Locals and parameters** you reference exist and have the right type.
- **Metadata references.** A `call` operand must be a member that belongs to (or is imported into) the module you will save.
- **Mutating the collection inside callbacks.** If you insert or remove instructions inside `EditInstructions`, remember that other `InstructionMatch.Index` values refer to the old layout.

## Recipes

All snippets assume `using AsmResolver.DotNet.Code.Cil;` and `using AsmResolver.PE.DotNet.Cil;`, plus `typeSpec` for the type.

### Change a string literal

```csharp
locator.EditInstructions(
    typeSpec,
    MethodSpec.ByName("Greet"),
    InstructionSpec.LdStr("Hello, "),
    (body, match) => match.Instruction.Operand = "Hi, ");
```

Changing only an operand never changes the stack shape, so this is the safest kind of edit.

### Make a method return a constant

Replace the whole body with `return true;`:

```csharp
locator.EditBody(typeSpec, MethodSpec.ByName("IsEnabled"), body =>
{
    body.Instructions.Clear();
    body.LocalVariables.Clear();
    body.ExceptionHandlers.Clear();

    body.Instructions.Add(new CilInstruction(CilOpCodes.Ldc_I4_1));
    body.Instructions.Add(new CilInstruction(CilOpCodes.Ret));
});
```

Clearing locals and exception handlers matters: leftover handlers would refer to instructions that no longer exist.

For a method returning `void`, the body is just `ret`:

```csharp
body.Instructions.Clear();
body.Instructions.Add(new CilInstruction(CilOpCodes.Ret));
```

### Disable an instruction

Replace it with `nop` (do nothing). This is only safe when the instruction does not change the stack, or when you fix the stack yourself:

```csharp
locator.EditInstructions(
    typeSpec,
    MethodSpec.ByName("Run"),
    InstructionSpec.Call("*Logger::Debug*"),
    (body, match) => match.Instruction.ReplaceWithNop());
```

> [!NOTE]
> A call to a method that takes arguments *pops* those arguments from the stack. Replacing such a call with `nop` leaves the arguments on the stack. In that case remove the argument-loading instructions too, or replace the call with `pop` instructions (one per argument).

### Insert an instruction

```csharp
locator.EditBody(typeSpec, MethodSpec.ByName("Run"), body =>
{
    body.Instructions.Insert(0, new CilInstruction(CilOpCodes.Nop));
});
```

### Redirect a call to another method

Point the operand at a method that is **imported into your module**. `Module.DefaultImporter` imports runtime members into the module you are editing. This example redirects `Console.Write(string)` to `Console.WriteLine(string)`; both take one string and return nothing, so the stack stays valid:

```csharp
var replacement = locator.Module.DefaultImporter.ImportMethod(
    typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) })!);

locator.EditInstructions(
    typeSpec,
    MethodSpec.ByName("Run"),
    InstructionSpec.Call("*Console::Write(System.String)"),
    (body, match) => match.Instruction.Operand = replacement);
```

Always choose a replacement with the same parameter types and return type as the original.

### Edit metadata instead of code

```csharp
locator.EditMethod(typeSpec, MethodSpec.ByName("Run"), method =>
{
    method.IsPublic = true;
});

locator.EditType(typeSpec, type => type.IsSealed = false);
```

## Saving

```csharp
locator.Save("Sample.patched.dll");
```

`Save` writes the module with AsmResolver's default settings. If you need custom writer behaviour (for example to preserve metadata tokens, or to sign the assembly), use AsmResolver directly on `locator.Module`.

Always write to a **new path**. The input file may still be in use by the loaded module, which can make overwriting it fail, and a bad patch should never destroy your original.

## Verifying your patch

A patch is not finished until you have tested it.

1. **Search again.** Reload the saved file with `AssemblyLocator.FromBytes(File.ReadAllBytes(path))` and check that the change is present (and the old thing is gone). This is how AsmLocator's own tests work.
2. **Run it.** Load the patched assembly and call the changed code, ideally in a separate process.
3. **Verify the IL.** A tool such as ILVerify can check stack balance and branch targets in the saved file.
4. **Keep the original.** Compare behaviour of original and patched files on the same inputs.

## Where next?

- [Cookbook](cookbook.md) — full recipes that combine search and edit.
- [Advanced topics](advanced.md) — structuring patchers and testing them.

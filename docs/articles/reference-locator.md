# Locator reference

A compact index of everything on `AssemblyLocator`, its extension methods and its result types. For details and behaviour see [Find, Get and Edit](lookup-semantics.md); for generated signatures see the [API reference](../api/toc.yml).

## Creating and saving

| Member | Purpose |
|---|---|
| `AssemblyLocator.FromFile(path)` | load a module from a file |
| `AssemblyLocator.FromBytes(bytes)` | load a module from memory (does not keep the file open) |
| `new AssemblyLocator(module)` | wrap an existing AsmResolver `ModuleDefinition` |
| `Module` | the underlying `ModuleDefinition` |
| `Refresh()` | rebuild the cached type list after adding or removing types |
| `Save(path)` | write the module to `path` (use a different path from the input) |

## Lookup methods

`type` is a `TypeSpec`. A blank means "not available".

| Target | Find first or `null` | Find all (lazy) | Get exactly one | Edit |
|---|---|---|---|---|
| Type | `FindType(type)` | `FindTypes(type)` | `GetType(type)` | `EditType(type, edit)` |
| Field | `FindField(type, field)` | `FindFields(type, field)` | `GetField(type, field)` | `EditField(type, field, edit)` |
| Property | `FindProperty(type, property)` | `FindProperties(type, property)` | `GetProperty(type, property)` | `EditProperty(type, property, edit)` |
| Event | `FindEvent(type, ev)` | `FindEvents(type, ev)` | `GetEvent(type, ev)` | `EditEvent(type, ev, edit)` |
| Method | `FindMethod(type, method)` | `FindMethods(type, method)` | `GetMethod(type, method)` | `EditMethod(type, method, edit)` |
| Property accessor | | | `GetMethod(type, property, PropertyAccessor)` | |
| Event accessor | | | `GetMethod(type, ev, EventAccessor)` | |
| Parameter | `FindParameter(type, method, parameter)` | `FindParameters(type, method, parameter)` | `GetParameter(type, method, parameter)` | |
| Method body | `FindBody(type, method)` | | `GetBody(type, method)` | `EditBody(type, method, edit)` |
| Accessor body | | | `GetBody(type, property, accessor)`, `GetBody(type, ev, accessor)` | `EditBody(type, property, accessor, edit)`, `EditBody(type, ev, accessor, edit)` |
| Local variable | | `FindLocals(type, method, local)` | `GetLocal(type, method, local)` | |
| Instruction | | `FindInstructions(type, method, instruction)` → `IReadOnlyList<InstructionMatch>` | `GetInstruction(type, method, instruction)` | `EditInstructions(type, method, instruction, (body, match) => ...)` |
| Instruction pattern | | `FindPattern(type, method, params InstructionSpec[])` → `IReadOnlyList<InstructionSequence>` | `GetPattern(type, method, params InstructionSpec[])` | |

Callback types: `Action<TypeDefinition>`, `Action<FieldDefinition>`, `Action<PropertyDefinition>`, `Action<EventDefinition>`, `Action<MethodDefinition>`, `Action<CilMethodBody>`, and `Action<CilMethodBody, InstructionMatch>` for `EditInstructions`.

Search scope in short: `FindX`/`FindXs` search all types matching the `TypeSpec`; every `Get`, `Edit`, body and instruction method requires exactly one type, and then exactly one method or member. `FindLocals` and `FindParameters` search all matching methods. Details in [Find, Get and Edit](lookup-semantics.md#two-levels-the-type-then-the-member).

## Extension methods (`LocatorExtensions`)

Use specs directly on AsmResolver objects, without an `AssemblyLocator`. Each searches only the object it is called on.

| On | Methods |
|---|---|
| `TypeDefinition` | `FindMethods`, `FindMethod`, `FindFields`, `FindField`, `FindProperties`, `FindProperty`, `FindEvents`, `FindEvent`, `FindNestedTypes(spec, recursive: false)` |
| `PropertyDefinition` | `GetAccessor(PropertyAccessor)` → `MethodDefinition?` |
| `EventDefinition` | `GetAccessor(EventAccessor)` → `MethodDefinition?` |
| `MethodDefinition` | `FindParameters`, `FindParameter` |
| `CilMethodBody` | `FindLocals`, `FindInstructions`, `FindPattern(IReadOnlyList<InstructionSpec>)` |

## Result and helper types

| Type | Shape | Meaning |
|---|---|---|
| `InstructionMatch` | `record struct (int Index, CilInstruction Instruction)` | one matched instruction and its index at search time |
| `InstructionSequence` | `record (int StartIndex, IReadOnlyList<CilInstruction> Instructions)` | one matched pattern |
| `PropertyAccessor` | enum: `Get`, `Set` | which property accessor |
| `EventAccessor` | enum: `Add`, `Remove`, `Fire` | which event accessor (`Fire` is the optional raise method) |
| `MemberNotFoundException` | exception | not found, ambiguous, missing accessor or missing body |

## Exceptions at a glance

| Exception | From |
|---|---|
| `MemberNotFoundException` | `GetX`, `EditX`, `GetBody`, `GetPattern`, `GetInstruction`, accessor lookups |
| `ArgumentNullException` | null spec, callback or argument |
| `ArgumentException` | empty or `null`-containing pattern |
| `RegexMatchTimeoutException` | wildcard or regex timeout (2 s) |
| `BadImageFormatException`, `IOException` | loading or saving files |

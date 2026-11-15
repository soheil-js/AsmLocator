# Find, Get and Edit

Every lookup in AsmLocator follows one of four naming patterns. Learn them once and the whole API becomes predictable.

| Pattern | Returns | When nothing matches | When several match |
|---|---|---|---|
| `FindX` | the **first** match | `null` | the first one |
| `FindXs` | **all** matches, lazily (`IEnumerable<T>`) | an empty sequence | all of them |
| `GetX` | **exactly one** match | throws `MemberNotFoundException` | throws `MemberNotFoundException` |
| `EditX` | nothing; runs your callback on exactly one target | throws `MemberNotFoundException` | throws `MemberNotFoundException` |

`X` is `Type`, `Field`, `Property`, `Event`, `Method`, `Parameter`, `Local` and so on. Instruction lookups use `FindInstructions` / `GetInstruction` / `EditInstructions` and `FindPattern` / `GetPattern`.

## Which one should I use?

- **`GetX`** when your program *requires* the target to exist and be unique — patchers, tests, automation. A clear exception is better than silently doing nothing.
- **`FindX`** when the target is optional ("patch it if it is there").
- **`FindXs`** when you want to process every match (reports, bulk edits).
- **`EditX`** when you want to find-then-change in one step.

```csharp
// Required: throw if missing or ambiguous.
var method = locator.GetMethod(type, MethodSpec.Of("Greet", "System.String"));

// Optional: null if missing.
var maybe = locator.FindMethod(type, MethodSpec.ByName("Optional"));
if (maybe is not null) { /* ... */ }

// All matches.
foreach (var m in locator.FindMethods(type, MethodSpec.ByName("Get*")))
    Console.WriteLine(m.Name);
```

## `MemberNotFoundException`

One exception covers every failure of a `GetX` or `EditX` call:

- no match was found;
- more than one match was found ("... is ambiguous (more than one match)");
- a property or event accessor you asked for is not defined;
- the method has no CIL body (abstract, `extern`, native).

The message contains the spec's readable description, so it names exactly what failed. To fix an *ambiguous* error, make the spec more specific — add parameter types, a return type, or a flag.

## Two levels: the type, then the member

Member lookups on `AssemblyLocator` take a `TypeSpec` first and a member spec second:

```csharp
locator.GetMethod(typeSpec, methodSpec);
locator.FindFields(typeSpec, fieldSpec);
locator.FindInstructions(typeSpec, methodSpec, instructionSpec);
```

The two levels behave slightly differently, and this is the most important subtlety in the API:

| Method kind | What happens with the `TypeSpec` |
|---|---|
| `FindX`, `FindXs` | searches **every type** that matches the `TypeSpec` |
| `GetX`, `EditX` | the `TypeSpec` must resolve to **exactly one** type; then the member must be unique in it |
| Instruction and body lookups (`FindInstructions`, `GetBody`, `EditBody`, `FindPattern`, ...) | require exactly one type **and** exactly one method |

So a broad `TypeSpec` such as `TypeSpec.ByName("*Service")` works with `FindMethods` (searching all matching types), but makes `GetMethod` throw an ambiguity error — even if only one of those types contains the method you want. When you need `GetX`, give the type a precise full name.

One exception worth remembering: `FindLocals` and `FindParameters` search *all* methods that match the method spec, whereas `GetLocal` and `GetParameter` require a unique method.

## Lazy enumeration

`FindXs` methods return a *lazy* `IEnumerable<T>`. The search runs while you iterate, and runs **again** each time you iterate.

```csharp
var methods = locator.FindMethods(type, MethodSpec.ByName("Get*"));

var count = methods.Count();       // runs the search
foreach (var m in methods) { }     // runs the search again
```

Materialize the results when you need a stable snapshot, or when you will enumerate more than once — or when you will *edit* while looping:

```csharp
var methods = locator.FindMethods(type, MethodSpec.ByName("Get*")).ToList();
```

The instruction methods (`FindInstructions`, `FindPattern`, `GetPattern`) on `AssemblyLocator` already return a `List`, and their arguments are validated immediately.

## The type cache and `Refresh`

`AssemblyLocator` builds a list of all types in the module the first time it needs one and keeps it. If you **add or remove types** through AsmResolver after that, call `Refresh()` so later searches see the change:

```csharp
// ... add or remove a TypeDefinition through the AsmResolver API on locator.Module ...
locator.Refresh();   // later searches now see the change
```

Editing existing types, methods or instructions does not require `Refresh`.

## Using lookups without a locator

All member-level lookups are also available as extension methods directly on AsmResolver objects, so you can use specs without an `AssemblyLocator`:

```csharp
TypeDefinition type = /* from AsmResolver */;

var method  = type.FindMethod(MethodSpec.ByName("Execute"));
var fields  = type.FindFields(FieldSpec.ByName("_*"));
var nested  = type.FindNestedTypes(TypeSpec.ByName("*"), recursive: true);

CilMethodBody body = method!.CilMethodBody!;
var calls   = body.FindInstructions(InstructionSpec.Call("*WriteLine*"));
var pattern = body.FindPattern(new[] { InstructionSpec.LdStr("x"), InstructionSpec.AnyInstruction });
```

Extension methods search only the object they are called on: `TypeDefinition.FindMethods` looks at methods declared directly on that type (not inherited methods, not nested types'). Property and event accessors are available through `GetAccessor`:

```csharp
MethodDefinition? getter = property.GetAccessor(PropertyAccessor.Get);
MethodDefinition? adder  = eventDef.GetAccessor(EventAccessor.Add);
```

## Where next?

[Searching members](searching-members.md) shows concrete searches for each kind of target.

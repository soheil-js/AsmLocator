# Specifications

A **specification** (spec) is a small, immutable object that describes what you are looking for. AsmLocator has one spec type per kind of target:

| Specification | Matches | Result type |
|---|---|---|
| `TypeSpec` | types | `TypeDefinition` |
| `FieldSpec` | fields | `FieldDefinition` |
| `PropertySpec` | properties | `PropertyDefinition` |
| `EventSpec` | events | `EventDefinition` |
| `MethodSpec` | methods and constructors | `MethodDefinition` |
| `ParameterSpec` | method parameters | `Parameter` |
| `LocalSpec` | local variables | `CilLocalVariable` |
| `InstructionSpec` | CIL instructions | `CilInstruction` |

## The one rule: unset means "don't care"

Every property of a spec is optional. A candidate matches when it satisfies **every property you set**; properties you leave unset do not constrain anything.

```csharp
var anything   = new MethodSpec();                          // matches every method
var byName     = MethodSpec.ByName("Execute");              // only the name is checked
var staticOnes = new MethodSpec { IsStatic = true };        // only "is static" is checked
var both       = MethodSpec.ByName("Execute") with { IsStatic = true }; // name AND static
```

Constraints are always combined with **AND**. For OR, run two searches, use a regular expression such as `StringMatcher.FromRegex("^(Get|Set)Value$")` for names, or use `Where` (see below).

## Three ways to create a spec

### 1. Factory methods — the shortest way

```csharp
TypeSpec.ByName("Service*")
TypeSpec.ByFullName("Sample.Greeter")
MethodSpec.Of("Find", "System.String")
FieldSpec.Of("_name", "System.String")
```

### 2. Object initializers — the most flexible way

```csharp
var spec = new TypeSpec
{
    Namespace = "Sample.Services",
    IsAbstract = false,
    BaseType = "System.Object"
};
```

### 3. `with` expressions — start from something and refine it

Specs are C# `record`s, so you can copy one and change a few properties without touching the original:

```csharp
MethodSpec baseSpec = MethodSpec.ByName("Execute");

MethodSpec publicStatic = baseSpec with { IsPublic = true, IsStatic = true };
MethodSpec twoArgs      = baseSpec with { ParameterCount = 2 };
```

## Implicit conversions from strings

For convenience, a plain string converts to a spec where that makes sense:

| Where a ... is expected | A string means |
|---|---|
| `StringMatcher` | exact text, or a wildcard if it contains `*` or `?` (see [String matching](string-matching.md)) |
| `TypeSpec` | the type's **full name** |
| `MethodSpec`, `FieldSpec`, `PropertySpec`, `EventSpec` | the member's **name** |
| `ParameterSpec`, `LocalSpec` | the **type's full name** |

So these are equivalent:

```csharp
locator.GetMethod(TypeSpec.ByFullName("Sample.Greeter"), MethodSpec.ByName("Greet"));
locator.GetMethod("Sample.Greeter", "Greet");
```

## Reusing and sharing specs

Because specs are immutable you can safely store them in static fields and use them from many places:

```csharp
static class Targets
{
    public static readonly TypeSpec Greeter = TypeSpec.ByFullName("Sample.Greeter");
    public static readonly MethodSpec Greet = MethodSpec.Of("Greet", "System.String");
}

var greet = locator.GetMethod(Targets.Greeter, Targets.Greet);
```

## Describing members in detail

### Methods

```csharp
var spec = MethodSpec.ByName("Execute") with
{
    IsPublic = true,
    IsStatic = true,
    ParameterCount = 2,
    ReturnType = "System.Boolean",
    HasBody = true
};
```

Signatures:

| Factory | Meaning |
|---|---|
| `MethodSpec.ByName(name)` | name only; parameters unconstrained |
| `MethodSpec.Of(name)` | same as `ByName` |
| `MethodSpec.Of(name, "System.String", "System.Int32")` | name **and** exact parameter count and order |
| `MethodSpec.NoParameters(name)` | name and an empty parameter list |
| `MethodSpec.WithParameters(name, ParameterSpec...)` | name and detailed parameter specs |
| `MethodSpec.Constructor("System.String")` | instance constructor with those parameter types |
| `MethodSpec.StaticConstructor()` | the type initializer `.cctor` |

A `*` as a parameter type means "any type" in that position: `MethodSpec.Of("Copy", "*", "System.Int32")`.

The `this` reference is **not** counted as a parameter.

### Looking inside the method body

Two constraints read the method body instead of the metadata:

```csharp
var spec = MethodSpec.ByName("*") with
{
    // body must contain a Console.WriteLine call AND a string literal "token" (order irrelevant)
    BodyContains = new[]
    {
        InstructionSpec.Call("*Console::WriteLine*"),
        InstructionSpec.LdStr("token")
    },

    // body must declare at least one local of this type
    Locals = new[] { LocalSpec.OfType("System.Int32") }
};
```

> [!WARNING]
> Body constraints (`HasBody`, `Locals`, `BodyContains`) force AsmResolver to decode method bodies. Put cheap constraints (name, namespace, parameter count) alongside them so most candidates are rejected before their body is read. AsmLocator already checks the cheap constraints first.

### Types

```csharp
var spec = new TypeSpec
{
    Namespace = "Sample.Services",
    BaseType = "Sample.ServiceBase",
    Interfaces = new StringMatcher[] { "System.IDisposable" },
    Attribute = "*ObsoleteAttribute",
    IsAbstract = false,
    GenericParameterCount = 0
};
```

Nested types: `TypeSpec.NestedIn(outerSpec, "Inner")` or the `DeclaringType` property.

## Custom predicates with `Where`

Every spec has a `Where` property taking a function over the candidate object. It runs **after** all the built-in constraints have passed, so it only sees plausible candidates:

```csharp
var spec = MethodSpec.ByName("*") with
{
    IsStatic = true,
    Where = method => method.Parameters.Count >= 2
                   && method.Signature!.ReturnType.FullName == "System.Void"
};
```

Use `Where` for anything the built-in properties cannot express: arithmetic on counts, OR logic, checks against AsmResolver properties that have no spec property.

## Debugging a spec

Every spec prints a readable description, including the constraints that are set:

```csharp
Console.WriteLine(MethodSpec.Of("Find", "System.String"));
// Method(Name=Find, Params=[System.String])
```

`MemberNotFoundException` messages use the same description, so they tell you exactly which spec failed.

## Where next?

- Exact, wildcard and regex text matching: [String matching](string-matching.md).
- The meaning of `Find`, `Get` and `Edit`: [Find, Get and Edit](lookup-semantics.md).
- Every property of every spec in one table: [Specification reference](reference-specs.md).

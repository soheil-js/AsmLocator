# Searching members

This page shows, kind by kind, how to find things in the metadata of an assembly. All examples assume:

```csharp
using AsmLocator;

var locator = AssemblyLocator.FromFile("Sample.dll");
TypeSpec type = TypeSpec.ByFullName("Sample.Services.UserService");
```

## Types

```csharp
// By exact full name
var t = locator.GetType(TypeSpec.ByFullName("Sample.Services.UserService"));

// By simple name, any namespace
var services = locator.FindTypes(TypeSpec.ByName("*Service"));

// By namespace
var all = locator.FindTypes(new TypeSpec { Namespace = "Sample.Services" });

// By shape
var abstractBases = locator.FindTypes(new TypeSpec { IsAbstract = true, IsSealed = false });
var enums         = locator.FindTypes(new TypeSpec { IsEnum = true });
var interfaces    = locator.FindTypes(new TypeSpec { IsInterface = true });

// By relationships
var disposables = locator.FindTypes(new TypeSpec
{
    Interfaces = new StringMatcher[] { "System.IDisposable" }
});
var derived = locator.FindTypes(new TypeSpec { BaseType = "Sample.ServiceBase" });

// By attribute
var obsolete = locator.FindTypes(new TypeSpec { Attribute = "System.ObsoleteAttribute" });

// Generic types: the name includes the arity
var generics = locator.FindTypes(new TypeSpec { GenericParameterCount = 1 });
```

`FindTypes` searches **every** type in the module, including nested types. To restrict to nested or non-nested types use `IsNested`.

### Nested types

```csharp
// Nested types appear in FindTypes too. Narrow with IsNested or the declaring type:
var nested = locator.FindTypes(new TypeSpec { IsNested = true });
var innerOfOuter = locator.FindTypes(TypeSpec.NestedIn(TypeSpec.ByFullName("Sample.Outer"), "Inner"));

// Or walk down from a TypeDefinition:
var direct = typeDefinition.FindNestedTypes(TypeSpec.ByName("*")).ToList();
var deep   = typeDefinition.FindNestedTypes(TypeSpec.ByName("*"), recursive: true).ToList();
```

A nested type's full name uses `+`: `Sample.Outer+Inner`.

## Fields

```csharp
var field   = locator.GetField(type, FieldSpec.ByName("_connection"));
var typed   = locator.GetField(type, FieldSpec.Of("_name", "System.String"));

var statics = locator.FindFields(type, new FieldSpec { IsStatic = true });
var consts  = locator.FindFields(type, new FieldSpec { IsLiteral = true });          // const
var ro      = locator.FindFields(type, new FieldSpec { IsInitOnly = true });         // readonly
var privateStrings = locator.FindFields(type, new FieldSpec
{
    IsPrivate = true,
    FieldType = "System.String"
});
```

## Properties

```csharp
var prop = locator.GetProperty(type, PropertySpec.ByName("Name"));

var readOnly = locator.FindProperties(type, new PropertySpec { HasGetter = true, HasSetter = false });
var ints     = locator.FindProperties(type, PropertySpec.Of("*", "System.Int32"));
```

A property's accessors are ordinary methods. Fetch them directly:

```csharp
MethodDefinition getter = locator.GetMethod(type, PropertySpec.ByName("Name"), PropertyAccessor.Get);
MethodDefinition setter = locator.GetMethod(type, PropertySpec.ByName("Name"), PropertyAccessor.Set);
```

`GetMethod` with an accessor throws `MemberNotFoundException` if the property has no such accessor.

## Events

```csharp
var ev = locator.GetEvent(type, EventSpec.ByName("Changed"));

MethodDefinition add    = locator.GetMethod(type, EventSpec.ByName("Changed"), EventAccessor.Add);
MethodDefinition remove = locator.GetMethod(type, EventSpec.ByName("Changed"), EventAccessor.Remove);
```

`EventAccessor.Fire` returns the optional "raise" method, which most compilers do not emit.

## Methods and constructors

```csharp
// Just by name (may be ambiguous if the method is overloaded)
var any = locator.FindMethods(type, MethodSpec.ByName("Find"));

// Exact signature: name + ordered parameter types
var one = locator.GetMethod(type, MethodSpec.Of("Find", "System.String"));

// No parameters
var parameterless = locator.GetMethod(type, MethodSpec.NoParameters("Start"));

// Flags and counts
var publicStatic = locator.FindMethods(type, MethodSpec.ByName("*") with
{
    IsPublic = true,
    IsStatic = true,
    ParameterCount = 2
});

// Return type
var returnsBool = locator.FindMethods(type, MethodSpec.ByName("*") with { ReturnType = "System.Boolean" });

// Constructors
var ctor     = locator.GetMethod(type, MethodSpec.Constructor("System.String"));
var cctor    = locator.GetMethod(type, MethodSpec.StaticConstructor());
var allCtors = locator.FindMethods(type, new MethodSpec { IsConstructor = true });

// Exclude accessors, operators and constructors ("special name" methods)
var plain = locator.FindMethods(type, MethodSpec.ByName("*") with { IsSpecialName = false });

// Generic methods
var generic = locator.FindMethods(type, new MethodSpec { GenericParameterCount = 1 });

// By attribute
var tests = locator.FindMethods(type, new MethodSpec { Attribute = "*FactAttribute" });
```

### Methods by what their body contains

```csharp
var usesConsole = locator.FindMethods(type, MethodSpec.ByName("*") with
{
    BodyContains = new[] { InstructionSpec.Call("*System.Console::WriteLine*") }
});
```

See [Specifications](specifications.md) for `Locals` and the cost of body constraints.

## Parameters

Parameters are found *through their method*:

```csharp
var method = MethodSpec.Of("Find", "System.String");

var first  = locator.GetParameter(type, method, ParameterSpec.At(0));
var named  = locator.GetParameter(type, method, ParameterSpec.Named("name"));
var strs   = locator.FindParameters(type, MethodSpec.ByName("*"), ParameterSpec.OfType("System.String"));
```

`ParameterSpec.At(0)` is the first **declared** parameter; the implicit `this` is not included. Names may be missing in assemblies compiled without that metadata or obfuscated.

## Local variables

Locals exist only in methods that have a CIL body:

```csharp
var ints = locator.FindLocals(type, MethodSpec.ByName("Run"), LocalSpec.OfType("System.Int32"));
var second = locator.GetLocal(type, MethodSpec.ByName("Run"), LocalSpec.At(1));
```

> [!NOTE]
> Locals have no names in the binary (only an index and a type), so `LocalSpec` matches by index and type, plus `Where`. Optimized (Release) builds may remove or merge locals that you see in Debug builds.

## Methods of one specific object

If you already hold a `TypeDefinition` (for example from an earlier search) you can search inside it directly with the extension methods:

```csharp
foreach (var m in typeDefinition.FindMethods(MethodSpec.ByName("Get*")))
    Console.WriteLine(m.FullName);
```

## Where next?

[Searching CIL instructions](cil-search.md) goes inside method bodies.

# Specification reference

Every spec property is optional. **Unset means unconstrained**; constraints combine with AND. `Where` runs last, on candidates that passed everything else.

Common conversions: a `string` converts implicitly to `StringMatcher` (exact, or wildcard if it contains `*` or `?`), and to `TypeSpec` (full name), `MethodSpec`/`FieldSpec`/`PropertySpec`/`EventSpec` (name), `ParameterSpec`/`LocalSpec` (type full name).

## TypeSpec

Candidate: `TypeDefinition`.

| Property | Type | Constrains |
|---|---|---|
| `Namespace` | `StringMatcher?` | namespace |
| `Name` | `StringMatcher?` | simple name (generic arity included) |
| `FullName` | `StringMatcher?` | full name; nested types use `+` |
| `BaseType` | `StringMatcher?` | full name of the base type |
| `Interfaces` | `IReadOnlyList<StringMatcher>?` | every matcher must match at least one directly declared interface |
| `Attribute` | `StringMatcher?` | full name of a custom attribute type on the type |
| `DeclaringType` | `TypeSpec?` | the enclosing type (nested types only) |
| `GenericParameterCount` | `int?` | number of generic parameters |
| `IsNested` | `bool?` | is a nested type |
| `IsAbstract` | `bool?` | abstract |
| `IsSealed` | `bool?` | sealed |
| `IsInterface` | `bool?` | interface |
| `IsEnum` | `bool?` | enum |
| `IsValueType` | `bool?` | value type (struct or enum) |
| `Where` | `Func<TypeDefinition, bool>?` | custom predicate |

Factories: `Of(namespace, name)`, `ByName(name)`, `ByFullName(fullName)`, `NestedIn(outer, name)`. Implicit from `string` = `ByFullName`.

## MethodSpec

Candidate: `MethodDefinition` (methods and constructors).

| Property | Type | Constrains |
|---|---|---|
| `Name` | `StringMatcher?` | method name (`.ctor`, `get_X`, ...) |
| `Parameters` | `IReadOnlyList<ParameterSpec>?` | exact parameter count **and** order; each entry matches positionally |
| `ParameterCount` | `int?` | number of parameters (the implicit `this` is not counted) |
| `ReturnType` | `StringMatcher?` | full name of the return type |
| `Attribute` | `StringMatcher?` | full name of a custom attribute type |
| `GenericParameterCount` | `int?` | number of generic parameters |
| `IsStatic` | `bool?` | static |
| `IsPublic` | `bool?` | public |
| `IsPrivate` | `bool?` | private |
| `IsAbstract` | `bool?` | abstract |
| `IsVirtual` | `bool?` | virtual |
| `IsConstructor` | `bool?` | constructor |
| `IsSpecialName` | `bool?` | marked `specialname`: accessors, operators and constructors |
| `HasBody` | `bool?` | has a CIL body |
| `Locals` | `IReadOnlyList<LocalSpec>?` | every spec must match at least one local (requires a body) |
| `BodyContains` | `IReadOnlyList<InstructionSpec>?` | every spec must match at least one instruction; order ignored (requires a body) |
| `Where` | `Func<MethodDefinition, bool>?` | custom predicate |

Factories: `ByName(name)`, `Of(name)`, `Of(name, params string[] parameterTypes)`, `NoParameters(name)`, `WithParameters(name, params ParameterSpec[])`, `Constructor(params string[])`, `StaticConstructor()`. Implicit from `string` = `ByName`.

Body constraints (`HasBody`, `Locals`, `BodyContains`) decode the method body; cheap constraints are evaluated first.

## FieldSpec

Candidate: `FieldDefinition`.

| Property | Type | Constrains |
|---|---|---|
| `Name` | `StringMatcher?` | field name |
| `FieldType` | `StringMatcher?` | full name of the field type |
| `Attribute` | `StringMatcher?` | custom attribute type |
| `IsStatic` | `bool?` | static |
| `IsPublic` | `bool?` | public |
| `IsPrivate` | `bool?` | private |
| `IsLiteral` | `bool?` | `const` |
| `IsInitOnly` | `bool?` | `readonly` |
| `Where` | `Func<FieldDefinition, bool>?` | custom predicate |

Factories: `ByName(name)`, `Of(name, fieldType = null)`. Implicit from `string` = `ByName`.

## PropertySpec

Candidate: `PropertyDefinition`.

| Property | Type | Constrains |
|---|---|---|
| `Name` | `StringMatcher?` | property name |
| `PropertyType` | `StringMatcher?` | full name of the property type |
| `Attribute` | `StringMatcher?` | custom attribute type |
| `HasGetter` | `bool?` | has a getter |
| `HasSetter` | `bool?` | has a setter |
| `IsStatic` | `bool?` | accessors are static (getter preferred, else setter) |
| `Where` | `Func<PropertyDefinition, bool>?` | custom predicate |

Factories: `ByName(name)`, `Of(name, propertyType = null)`. Implicit from `string` = `ByName`.

## EventSpec

Candidate: `EventDefinition`.

| Property | Type | Constrains |
|---|---|---|
| `Name` | `StringMatcher?` | event name |
| `EventType` | `StringMatcher?` | full name of the event type |
| `Attribute` | `StringMatcher?` | custom attribute type |
| `Where` | `Func<EventDefinition, bool>?` | custom predicate |

Factories: `ByName(name)`, `Of(name, eventType = null)`. Implicit from `string` = `ByName`.

## ParameterSpec

Candidate: `Parameter`.

| Property | Type | Constrains |
|---|---|---|
| `Name` | `StringMatcher?` | parameter name (may be absent in some assemblies) |
| `ParameterType` | `StringMatcher?` | full name of the parameter type |
| `Index` | `int?` | zero-based declared position; `this` is not included |
| `Attribute` | `StringMatcher?` | custom attribute type on the parameter |
| `Where` | `Func<Parameter, bool>?` | custom predicate |

Factories: `OfType(type)`, `Named(name)`, `At(index)`, `Of(name, type)`. Implicit from `string` = `OfType`.

## LocalSpec

Candidate: `CilLocalVariable`.

| Property | Type | Constrains |
|---|---|---|
| `Index` | `int?` | zero-based index among the method's locals |
| `VariableType` | `StringMatcher?` | full name of the local's type |
| `Where` | `Func<CilLocalVariable, bool>?` | custom predicate |

Factories: `OfType(type)`, `At(index)`. Implicit from `string` = `OfType`.

## InstructionSpec

Candidate: `CilInstruction`. An empty spec matches every instruction.

| Property | Type | Constrains |
|---|---|---|
| `Code` | `CilCode?` | exact opcode |
| `Mnemonic` | `StringMatcher?` | opcode name, e.g. `call`, `ldc.i4*` |
| `Operand` | `StringMatcher?` | operand text from `DescribeOperand` |
| `Offset` | `int?` | current byte offset (stale until offsets are recalculated) |
| `Where` | `Func<CilInstruction, bool>?` | custom predicate |

Factories and members:

| Member | Meaning |
|---|---|
| `Op(opCode, operand = null)` | exact opcode, optional operand matcher |
| `WithMnemonic(mnemonic, operand = null)` | opcode-name matcher, optional operand matcher |
| `Call(method)` | `call` or `callvirt` whose operand matches (not `calli`) |
| `NewObj(ctor)` | `newobj` whose operand matches |
| `LdStr(text)` | `ldstr` whose string matches |
| `AnyInstruction` | matches any single instruction |
| `DescribeOperand(instruction)` | the operand text used for `Operand` matching |

## StringMatcher

| Member | Meaning |
|---|---|
| `Exact(value, comparison = Ordinal)` | full-string equality |
| `Wildcard(pattern, ignoreCase = false)` | `*` any run, `?` one character; whole-string, 2 s timeout |
| `FromRegex(pattern, options = None)` | .NET regex; unanchored unless you add `^`/`$`; 2 s timeout |
| `Where(predicate, description = "<predicate>")` | custom predicate |
| `Parse(pattern)` | `*` → `Any`; contains `*` or `?` → wildcard; otherwise exact |
| `Any` | matches everything |
| `IsMatch(value)` | `null` is treated as an empty string |
| implicit from `string` | `Parse` |

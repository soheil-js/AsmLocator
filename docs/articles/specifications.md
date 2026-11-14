# Specifications and matching

A specification is a reusable set of optional constraints. Unset properties do not constrain a candidate. Each specification also exposes `Where` for custom predicates.

| Specification | Candidate |
|---|---|
| `TypeSpec` | `TypeDefinition` |
| `FieldSpec` | `FieldDefinition` |
| `PropertySpec` | `PropertyDefinition` |
| `EventSpec` | `EventDefinition` |
| `MethodSpec` | `MethodDefinition` |
| `ParameterSpec` | `Parameter` |
| `LocalSpec` | `CilLocalVariable` |
| `InstructionSpec` | `CilInstruction` |

## StringMatcher

- `Exact`: full string equality.
- `Wildcard`: `*` means any number of characters; `?` means one character. Matching is anchored to the whole input.
- `FromRegex`: uses .NET regular expressions; add `^` and `$` for full-string matching.
- `Where`: supplies a custom predicate.
- `Parse`: selects wildcard matching if the string contains `*` or `?`; otherwise it selects exact matching.

Wildcard and regex matchers use a two-second regex timeout. Null candidate values are treated as empty strings by `IsMatch`.

## Method signatures

`MethodSpec.ByName` only constrains the method name. `MethodSpec.Of(name, types...)` requires an exact parameter count and ordered parameter type list. `MethodSpec.NoParameters` requires an empty parameter list. `MethodSpec.WithParameters` accepts full `ParameterSpec` objects.

## Find, Get, and Edit

- `FindX`: first match or `null`.
- `FindXs`: lazy enumeration of all matches.
- `GetX`: exactly one match; zero or multiple matches throw `MemberNotFoundException`.
- `EditX`: resolves one target and invokes a callback.

Materialize lazy enumerables with `ToList()` if you need a stable snapshot or multiple enumerations.

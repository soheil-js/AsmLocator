# String matching

Almost every spec property that holds text — names, namespaces, type names, operand text — is a `StringMatcher`. A `StringMatcher` is a reusable rule that answers one question: *does this text match?*

## Quick reference

| You write | Meaning | Case-sensitive? |
|---|---|---|
| `"Execute"` | exactly `Execute` | yes |
| `"Get*"` | starts with `Get` | yes |
| `"*Service"` | ends with `Service` | yes |
| `"*Console::WriteLine*"` | contains `Console::WriteLine` | yes |
| `"Item?"` | `Item` followed by exactly one character | yes |
| `"*"` | anything | — |

A plain string is converted to a `StringMatcher` automatically: if it contains `*` or `?` it becomes a **wildcard**, otherwise an **exact** match. (That conversion is `StringMatcher.Parse`.)

## The five kinds of matcher

### `StringMatcher.Exact`

Full-string equality. Case-sensitive by default (`StringComparison.Ordinal`); pass another comparison to change that.

```csharp
StringMatcher.Exact("Execute");
StringMatcher.Exact("execute", StringComparison.OrdinalIgnoreCase);
```

### `StringMatcher.Wildcard`

`*` matches any number of characters (including none). `?` matches exactly one character. The pattern must match the **whole** text — there is no implicit "contains".

```csharp
StringMatcher.Wildcard("Get*");                  // GetName, GetValue, Get
StringMatcher.Wildcard("get*", ignoreCase: true);
StringMatcher.Wildcard("*Service*");
```

All other characters are literal. A `.` in `System.Console` matches a dot only, not "any character" as in a regular expression.

### `StringMatcher.FromRegex`

A .NET regular expression. Unlike wildcards, a regex matches **anywhere** in the text unless you anchor it with `^` and `$`.

```csharp
StringMatcher.FromRegex(@"^Get[A-Z]\w+$");                          // anchored: whole text
StringMatcher.FromRegex(@"^(Get|Set)Value$");                       // alternatives
StringMatcher.FromRegex("service", RegexOptions.IgnoreCase);        // unanchored: contains
```

### `StringMatcher.Where`

Your own predicate, plus an optional description that appears in diagnostics:

```csharp
StringMatcher.Where(name => name.Length == 1, "single-letter name");
```

This is the tool for obfuscated names, for example "a single character" or "contains non-printable characters".

### `StringMatcher.Any`

Matches every input. `"*"` is converted to `Any`, so `"*"` and `StringMatcher.Any` behave the same.

## Using matchers

Anywhere a `StringMatcher` is accepted you can pass a plain string or a matcher object:

```csharp
MethodSpec.ByName("Get*");                                         // string -> wildcard
MethodSpec.ByName(StringMatcher.FromRegex(@"^Get[A-Z]"));          // explicit regex
TypeSpec.ByFullName("Sample.Greeter");                             // string -> exact
InstructionSpec.Call("*Console::WriteLine*");                      // string -> wildcard
```

## Null and empty values

`IsMatch(null)` treats `null` as an empty string. A matcher that cannot match an empty string (for example `"Get*"`) therefore does not match `null`. `Any` matches `null`.

## Timeouts

Wildcard and regex matchers have a **two-second** timeout, which protects you from pathological patterns. If a pattern runs longer than that on one input, .NET throws `RegexMatchTimeoutException`. For normal names this never happens; if it does, simplify the pattern.

## What text is being matched?

The most common reason a pattern "does not work" is that the text it is matched against does not look the way you expect. This table shows what each property is compared with.

| Spec property | Compared text | Example |
|---|---|---|
| `TypeSpec.Name` | simple name (generic arity included) | `Greeter`, ``List`1`` |
| `TypeSpec.Namespace` | namespace | `Sample.Services` |
| `TypeSpec.FullName` | namespace + name; nested types use `+` | `Sample.Outer+Inner` |
| `TypeSpec.BaseType` | full name of the base type | `System.Object` |
| `TypeSpec.Interfaces` | full name of each directly declared interface | `System.IDisposable` |
| any `Attribute` | full name of the attribute's type | `System.ObsoleteAttribute` |
| `MethodSpec.Name` | method name | `Greet`, `.ctor`, `get_Name` |
| `MethodSpec.ReturnType` | full name of the return type | `System.Void` |
| `ParameterSpec.ParameterType`, `FieldSpec.FieldType`, `PropertySpec.PropertyType`, `LocalSpec.VariableType` | full name of the type | `System.String`, `System.Int32[]` |
| `InstructionSpec.Mnemonic` | opcode name | `ldstr`, `call`, `ldc.i4.s` |
| `InstructionSpec.Operand` | operand converted to text (see [Searching CIL instructions](cil-search.md)) | `System.Void System.Console::WriteLine(System.String)` |

Notes:

- Attribute matching looks at the **full name of the attribute type**, e.g. `System.ObsoleteAttribute`, including the `Attribute` suffix.
- Interface matching looks only at interfaces **declared directly** on the type, not those inherited from a base type.
- Type names inside signatures are the CLR names, e.g. `System.Int32`, not the C# keyword `int`.

> [!TIP]
> Not sure what the text is? Print it and copy it:
>
> ```csharp
> foreach (var m in locator.FindMethods(type, MethodSpec.ByName("*")))
>     Console.WriteLine($"{m.Name}  returns {m.Signature?.ReturnType.FullName}");
>
> foreach (var i in locator.FindInstructions(type, "Run", InstructionSpec.AnyInstruction))
>     Console.WriteLine($"{i.Instruction.OpCode.Mnemonic}  {InstructionSpec.DescribeOperand(i.Instruction)}");
> ```

## Gotchas

**Names containing `*` or `?`.** Because a string with those characters becomes a wildcard, you cannot match them literally through the implicit conversion. Use `StringMatcher.Exact("...")` for such names.

**Wildcards are anchored, regexes are not.** `"Service"` matches only the exact text `Service`. To find "contains service" use `"*Service*"` or the regex `"Service"`.

**Case matters.** Matching is case-sensitive unless you ask otherwise (`ignoreCase: true`, `RegexOptions.IgnoreCase`, or `StringComparison.OrdinalIgnoreCase`). Note that the implicit string conversion cannot request case-insensitivity.

**`.` is literal in wildcards but "any character" in regexes.** Escape it (`\.`) in regexes when you mean a dot.

## Where next?

[Find, Get and Edit](lookup-semantics.md) explains what happens once your specs have identified candidates.

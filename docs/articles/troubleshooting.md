# Troubleshooting

Find your symptom, read the likely causes, apply the fix.

## `MemberNotFoundException: ... was not found`

The spec matched nothing. Work through this checklist:

1. **Print what is really there.** Names are the most common problem.
   ```csharp
   foreach (var t in locator.FindTypes(new TypeSpec())) Console.WriteLine(t.FullName);
   foreach (var m in locator.FindMethods(type, new MethodSpec())) Console.WriteLine($"{m.Name}  {m.FullName}");
   ```
2. **Full name or simple name?** `TypeSpec.ByName("Greeter")` compares the simple name; `ByFullName` needs `Sample.Greeter` including the namespace.
3. **Nested types** use `+`: `Sample.Outer+Inner`, not `Sample.Outer.Inner`.
4. **Generic types** carry their arity: ``List`1``, not `List`.
5. **Case matters.** `greet` does not match `Greet`.
6. **Type names are CLR names**: `System.Int32`, not `int`; `System.String`, not `string`.
7. **Special names**: constructors are `.ctor`, static constructors `.cctor`, accessors `get_Name`/`set_Name`.
8. **Literal `*` or `?` in a name** are treated as wildcards by the string conversion. Use `StringMatcher.Exact`.
9. **Too many constraints.** Constraints combine with AND. Remove them one by one to see which one excludes your target.
10. **Print the spec** — `Console.WriteLine(spec)` shows exactly what is being matched.

## `MemberNotFoundException: ... is ambiguous (more than one match)`

More than one candidate matched, and `GetX`/`EditX` require exactly one.

- Add more constraints: parameter types (`MethodSpec.Of(name, types...)`), `ReturnType`, flags like `IsStatic`.
- If the **type** spec is ambiguous, use the full name. Remember `GetX` needs a unique type even though `FindXs` would search all matching types (see [Find, Get and Edit](lookup-semantics.md)).
- If duplicates are expected, use `FindXs` and choose in your own code.

## `MemberNotFoundException: ... has no CIL body`

The method is abstract, an interface member, `extern`, or native (P/Invoke), so there is nothing to read or edit. Use `FindBody` (returns `null`) or add `HasBody = true` to the spec to skip such methods.

## `MemberNotFoundException: ... has no Get accessor` (or `Set`, `Add`, ...)

The property or event does not define that accessor. Check `HasGetter`/`HasSetter` on the spec, or use `FindProperty` and inspect `GetMethod`/`SetMethod`.

## An instruction search returns nothing

- **Wrong operand text.** Print it: `InstructionSpec.DescribeOperand(instruction)`. Method operands start with the return type: `System.Void System.Console::WriteLine(System.String)`. A pattern like `System.Console::WriteLine*` does *not* match that text; use `*Console::WriteLine*`.
- **Short constants have no operand.** `ldc.i4.5` carries its value in the opcode. See [Searching CIL instructions](cil-search.md#numeric-constants-and-the-no-operand-trap).
- **`call` versus `callvirt` versus `calli`.** `InstructionSpec.Call` covers the first two, not `calli`. Interface and virtual calls are usually `callvirt`; even many non-virtual instance calls are compiled as `callvirt`.
- **Different compiler output.** Debug builds contain extra `nop`s and locals; Release builds inline and reorder. Pattern searches see adjacent instructions only.
- **Code is not where you think.** Lambdas, `async` methods, iterators and closures are compiled into other methods and nested types. See [Advanced topics](advanced.md#compiler-generated-code).
- **You searched the wrong method.** Instruction searches work on exactly one method; check that the `MethodSpec` is not matching an overload.

## A pattern search does not match

Patterns need **adjacent** instructions. Insert `InstructionSpec.AnyInstruction` for each instruction between the ones you care about, and print the real instruction sequence to compare:

```csharp
foreach (var i in locator.GetBody(type, method).Instructions) Console.WriteLine(i);
```

## `RegexMatchTimeoutException`

A wildcard or regular expression took more than two seconds on one input. Simplify the pattern, anchor it, or avoid nested quantifiers such as `(a+)+`.

## `Save` throws, or the output file is empty

- **File in use.** Do not overwrite the file you loaded. Save to a new name, or load with `FromBytes(File.ReadAllBytes(path))`.
- **Missing folder.** Create the output directory first (`Directory.CreateDirectory`).
- **Read-only location.** Choose a writable folder.
- **Edits left the module in an inconsistent state.** AsmResolver reports problems while writing (for example dangling branch targets). Read the exception message; it names the member.

## The saved assembly crashes or throws `InvalidProgramException`

The IL you produced is invalid. Typical causes:

- the stack was not balanced at `ret` or at a branch merge point;
- an instruction that popped arguments was replaced with `nop` (the arguments stay on the stack);
- `try`/`catch` ranges refer to removed instructions (clear `body.ExceptionHandlers` when you clear `body.Instructions`);
- a `call` operand refers to a member that was never imported into the module;
- locals were removed while instructions still use them.

Verify the file with an IL verifier, compare the IL before and after, and apply the patch in smaller steps. See [Editing assemblies](editing.md).

## The patched assembly still behaves like the original

- You edited in memory but never called `Save`.
- You are running a *different copy* (another folder, the GAC, a cached or ReadyToRun/native-image version, a published single-file bundle).
- The code you changed is not the code that runs — for example a generic or compiler-generated method elsewhere does the real work.

## The patched assembly fails to load

- **Strong-named assemblies.** Editing invalidates the original strong-name signature. Runtimes that validate strong names (such as .NET Framework in some configurations) may refuse to load it; modern .NET does not validate them. Re-sign with your own key, or remove the signature requirement, using AsmResolver or standard signing tools.
- **Other assemblies reference an exact version or public key token.** Keep the identity unchanged unless you update the references too.

## `BadImageFormatException` when loading

The file is not a managed .NET assembly (native DLL, packed or encrypted executable) or is corrupt. Packed or protected assemblies must be unpacked first; AsmLocator does not do that.

## Names look like `a`, `b`, `\u0001`

The assembly is obfuscated. Describe targets by structure instead of by name: see [Targets that cannot be named](advanced.md#targets-that-cannot-be-named).

## Searches are slow

Body constraints and instruction searches decode method bodies. Add cheap constraints (name, flags, parameter count) to every spec that has a body constraint, and materialize results with `ToList()`. See [Advanced topics](advanced.md#performance).

## My results change when I edit while looping

You are iterating a lazy `FindXs` sequence while changing what it searches. Call `.ToList()` first, then edit.

## Still stuck?

Print the spec, print the real names and instructions, and compare them side by side. Almost every "not found" problem is a mismatch between the text you expect and the text that is actually there.

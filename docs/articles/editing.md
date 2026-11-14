# Editing assemblies

AsmLocator exposes callback-based editing methods for types, fields, properties, events, methods, method bodies, and matching instructions. The callbacks receive the underlying AsmResolver objects.

```csharp
locator.EditBody(typeSpec, MethodSpec.ByName("Run"), body =>
{
    // Modify body.Instructions.
});
```

The body editing helpers recalculate instruction offsets in a `finally` block, including when a callback throws after partially changing the body. `EditInstructions` invokes callbacks from the last match to the first, which reduces invalidation of earlier indexes when later instructions are inserted or removed.

## Responsibilities that remain with the caller

Offset recalculation is not full IL verification. Arbitrary edits can still produce invalid or semantically incorrect IL. The caller is responsible for checking:

- evaluation-stack correctness and max stack requirements;
- branch and switch targets;
- exception-handler boundaries;
- local and parameter references;
- metadata tokens and member references;
- instruction collection mutation inside callbacks.

Save to a new file and validate the output with the runtime or another inspection tool before replacing the original assembly.

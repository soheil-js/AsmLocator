# Glossary

**Assembly** — a compiled .NET unit, usually one `.dll` or `.exe` file. Contains one or more modules.

**Module** — the part of an assembly that holds code and metadata. AsmLocator loads one module (`ModuleDefinition` in AsmResolver).

**Metadata** — the description of an assembly's structure: types, members, signatures, attributes, references. Searching metadata is cheap.

**CIL / IL / MSIL** — Common Intermediate Language, the instruction set that .NET compilers emit. The runtime compiles it to machine code (JIT) when the program runs.

**Method body** — the CIL instructions, local variables and exception handlers of one method (`CilMethodBody`).

**Instruction** — one CIL operation: an opcode plus an optional operand (`CilInstruction`).

**Opcode** — the operation an instruction performs, such as `ldstr` or `call`. Its text name is the **mnemonic**.

**Operand** — extra data an instruction carries: a string, a method, a field, a number or a branch target. Some opcodes (like `ldc.i4.5`) have none.

**Offset** — the byte position of an instruction within its method body, shown as `IL_005E`.

**Evaluation stack** — the stack on which CIL instructions push and pop values. Valid code leaves it in the correct state at every branch and at `ret`.

**Label** — a branch instruction's target, pointing at another instruction.

**Exception handler** — a `try`/`catch`/`finally`/`fault` region in a method body, defined by instruction ranges.

**Local variable** — a method-scoped variable with an index and a type, no name in the binary.

**Token** — a numeric identifier of a metadata item inside one module. Tokens change when a module is rewritten unless the writer is told to preserve them.

**Full name** — a member's or type's complete name as AsmResolver writes it, for example `Sample.Greeter` or `System.Void System.Console::WriteLine(System.String)`.

**Specification (spec)** — an immutable AsmLocator object describing what to find (`TypeSpec`, `MethodSpec`, ...). Unset properties do not constrain.

**StringMatcher** — a text rule: exact, wildcard, regular expression, predicate or "anything".

**Pattern** — a contiguous, ordered list of instruction specs matched against adjacent instructions.

**Special name** — a flag on methods that are accessors (`get_X`), operators (`op_Implicit`) or constructors.

**Obfuscation** — deliberate renaming and restructuring of code to make analysis harder. Describe such targets by structure, not by name.

**Strong name** — an assembly signature that identifies it by name, version and public key token. Editing an assembly invalidates it.

**Round trip** — saving an edited module, reloading it, and checking that the change is there.

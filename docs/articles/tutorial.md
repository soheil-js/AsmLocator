# Tutorial: your first patch

In this tutorial you will build a tiny target program, then use AsmLocator to **explore** it, **search** its code, **edit** it, **save** the result and **verify** the change. It takes about 20 minutes and uses only the concepts from [Concepts](concepts.md).

> [!NOTE]
> Everything here runs on your own files. Never patch software you do not own or do not have permission to modify.

## 1. Create the target

Create a solution with two projects: the *target* we will patch, and the *patcher* that uses AsmLocator.

```bash
mkdir PatchDemo && cd PatchDemo
dotnet new classlib -n Sample
dotnet new console  -n Patcher
dotnet add Patcher reference path/to/AsmLocator/src/AsmLocator/AsmLocator.csproj
```

Replace the contents of `Sample/Class1.cs` (and rename it to `Greeter.cs`):

```csharp
namespace Sample;

public sealed class Greeter
{
    public static bool IsEnabled() => false;

    public string Greet(string name)
    {
        Console.WriteLine("Greeting " + name);
        return "Hello, " + name;
    }

    public int Add(int a, int b) => a + b;
}
```

Build it:

```bash
dotnet build Sample -c Release
```

The assembly is now at `Sample/bin/Release/net10.0/Sample.dll` (the exact folder depends on your target framework). Note this path.

## 2. Explore

Open `Patcher/Program.cs`. We will build it up step by step.

```csharp
using AsmLocator;

string input  = args[0];   // path to Sample.dll
string output = args[1];   // where to write the patched copy

var locator = AssemblyLocator.FromFile(input);
TypeSpec greeter = TypeSpec.ByFullName("Sample.Greeter");

// 2a. What types are in the Sample namespace?
foreach (var type in locator.FindTypes(new TypeSpec { Namespace = "Sample" }))
    Console.WriteLine($"type: {type.FullName}");

// 2b. What methods does Greeter have?
foreach (var method in locator.FindMethods(greeter, MethodSpec.ByName("*")))
    Console.WriteLine($"method: {method.Name} ({method.Parameters.Count} parameters)");
```

Run it:

```bash
dotnet run --project Patcher -- Sample/bin/Release/net10.0/Sample.dll patched/Sample.dll
```

Expected output (the order of methods can differ):

```text
type: Sample.Greeter
method: IsEnabled (0 parameters)
method: Greet (1 parameters)
method: Add (2 parameters)
method: .ctor (0 parameters)
```

You just used a `TypeSpec` with an object initializer (`new TypeSpec { Namespace = ... }`) and the factory `MethodSpec.ByName("*")`, where `"*"` means "any name".

## 3. Search the code

Which method calls does `Greet` make?

```csharp
// 3a. Every call to Console.WriteLine in Greet
var writes = locator.FindInstructions(
    greeter,
    MethodSpec.ByName("Greet"),
    InstructionSpec.Call("*Console::WriteLine*"));

foreach (var match in writes)
    Console.WriteLine($"IL_{match.Instruction.Offset:X4}: {match.Instruction}");

// 3b. Every string literal in Greet
var strings = locator.FindInstructions(
    greeter,
    MethodSpec.ByName("Greet"),
    InstructionSpec.LdStr("*"));

foreach (var match in strings)
    Console.WriteLine($"string at index {match.Index}: {match.Instruction.Operand}");
```

`"*"` as an operand matcher means "any string". You should see two strings: `"Greeting "` and `"Hello, "`.

## 4. Find a pattern

A **pattern** is a contiguous run of instructions. `"Hello, " + name` compiles to *load string, load argument, call `String.Concat`*:

```csharp
var concat = locator.FindPattern(
    greeter,
    MethodSpec.ByName("Greet"),
    InstructionSpec.LdStr("Hello, "),          // the literal
    InstructionSpec.AnyInstruction,            // exactly one instruction we do not care about
    InstructionSpec.Call("*String::Concat*")); // the concatenation

foreach (var sequence in concat)
    Console.WriteLine($"pattern starts at instruction {sequence.StartIndex}");
```

`AnyInstruction` stands for exactly one instruction of any kind.

## 5. Edit

Two edits: change the greeting text, and make `IsEnabled` return `true`.

Add these `using` directives at the **top** of `Program.cs`:

```csharp
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
```

Then add the edits:

```csharp
// 5a. Change "Hello, " to "Hi, ".
locator.EditInstructions(
    greeter,
    MethodSpec.ByName("Greet"),
    InstructionSpec.LdStr("Hello, "),
    (body, match) => match.Instruction.Operand = "Hi, ");

// 5b. Replace the whole body of IsEnabled with: return true;
locator.EditBody(greeter, MethodSpec.ByName("IsEnabled"), body =>
{
    body.Instructions.Clear();
    body.Instructions.Add(new CilInstruction(CilOpCodes.Ldc_I4_1)); // push 1 (true)
    body.Instructions.Add(new CilInstruction(CilOpCodes.Ret));      // return it
});
```

Things worth noticing:

- `EditInstructions` calls your callback once per match and **throws** if nothing matches — a patch that silently does nothing would be worse.
- `EditBody` gives you the whole `CilMethodBody`. AsmLocator recalculates the instruction offsets for you afterwards.
- AsmLocator does **not** check that your IL is valid. Here it is: the stack holds one `int32` when `ret` runs, which matches the `bool` return type.

## 6. Save

```csharp
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
locator.Save(output);
Console.WriteLine($"saved {output}");
```

## 7. Verify

Never trust a patched file you have not run. Add a small check at the end of `Program.cs` that loads the **patched** copy and calls it by reflection:

Add `using System.Reflection;` at the top of the file, then:

```csharp
var patched = Assembly.LoadFrom(Path.GetFullPath(output));
var type = patched.GetType("Sample.Greeter")!;

Console.WriteLine("IsEnabled: " + type.GetMethod("IsEnabled")!.Invoke(null, null));

var instance = Activator.CreateInstance(type)!;
Console.WriteLine("Greet: " + type.GetMethod("Greet")!.Invoke(instance, new object[] { "Ada" }));
```

Expected output:

```text
IsEnabled: True
Greeting Ada
Greet: Hi, Ada
```

`IsEnabled` now returns `True`, and the greeting says `Hi, Ada` instead of `Hello, Ada`. 

> [!TIP]
> Run the verification in a **separate process** for real projects, so the patched assembly does not clash with other loaded assemblies. For tests, saving and reloading with `AssemblyLocator.FromBytes(File.ReadAllBytes(output))` and searching again (as AsmLocator's own tests do) is a quick structural check.

## What you learned

| Skill | Where it is covered in depth |
|---|---|
| Describing targets with specs | [Specifications](specifications.md) |
| Wildcards like `*Console::WriteLine*` | [String matching](string-matching.md) |
| `Find` / `Get` / `Edit` rules | [Find, Get and Edit](lookup-semantics.md) |
| Searching metadata | [Searching members](searching-members.md) |
| Searching instructions and patterns | [Searching CIL instructions](cil-search.md) |
| Editing safely | [Editing assemblies](editing.md) |
| More patch ideas | [Cookbook](cookbook.md) |

# Cookbook

Complete, copy-ready recipes. Each one stands alone. They assume:

```csharp
using AsmLocator;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

var locator = AssemblyLocator.FromFile("Sample.dll");
```

Recipes that edit code follow the safety rules in [Editing assemblies](editing.md). Always write the result to a **new file** and test it.

The "every method in the module" loop appears in several recipes:

```csharp
foreach (var type in locator.FindTypes(new TypeSpec()))                           // all types
    foreach (var method in type.FindMethods(new MethodSpec { HasBody = true }))   // all methods with code
    {
        var body = method.CilMethodBody!;
        // ...
    }
```

An empty spec (`new TypeSpec()`) matches everything, and `HasBody = true` guarantees `CilMethodBody` is not null.

---

## Inspect

### List the public API surface

```csharp
foreach (var type in locator.FindTypes(new TypeSpec { IsNested = false }).Where(t => t.IsPublic))
{
    Console.WriteLine(type.FullName);

    foreach (var method in type.FindMethods(new MethodSpec { IsPublic = true, IsSpecialName = false }))
        Console.WriteLine($"    {method.FullName}");
}
```

### Find every caller of a method

```csharp
var target = InstructionSpec.Call("*System.Console::WriteLine*");

foreach (var type in locator.FindTypes(new TypeSpec()))
    foreach (var method in type.FindMethods(new MethodSpec { HasBody = true }))
    {
        var hits = method.CilMethodBody!.FindInstructions(target).ToList();
        if (hits.Count > 0)
            Console.WriteLine($"{method.FullName}: {hits.Count} call(s)");
    }
```

### Find where a string literal is used

```csharp
var literal = InstructionSpec.LdStr("*password*");

foreach (var type in locator.FindTypes(new TypeSpec()))
    foreach (var method in type.FindMethods(new MethodSpec { HasBody = true }))
        foreach (var hit in method.CilMethodBody!.FindInstructions(literal))
            Console.WriteLine($"{method.FullName} [{hit.Index}]: {hit.Instruction.Operand}");
```

### Find implementations of an interface, or subclasses

```csharp
var plugins = locator.FindTypes(new TypeSpec
{
    Interfaces = new StringMatcher[] { "Sample.IPlugin" },
    IsAbstract = false,
    IsInterface = false
});

var viewModels = locator.FindTypes(new TypeSpec { BaseType = "Sample.ViewModelBase" });
```

Note that `Interfaces` sees interfaces declared directly on the type. Types that inherit the interface from a base class are not listed; search for subclasses of that base class as well.

### Find methods carrying an attribute

```csharp
foreach (var type in locator.FindTypes(new TypeSpec()))
    foreach (var method in type.FindMethods(new MethodSpec { Attribute = "*TestMethodAttribute" }))
        Console.WriteLine(method.FullName);
```

### Find the ten largest methods

```csharp
var biggest = locator.FindTypes(new TypeSpec())
    .SelectMany(t => t.FindMethods(new MethodSpec { HasBody = true }))
    .OrderByDescending(m => m.CilMethodBody!.Instructions.Count)
    .Take(10);

foreach (var m in biggest)
    Console.WriteLine($"{m.CilMethodBody!.Instructions.Count,6}  {m.FullName}");
```

### Find methods that use a type, without naming the method

```csharp
var users = locator.FindTypes(new TypeSpec())
    .SelectMany(t => t.FindMethods(new MethodSpec
    {
        BodyContains = new[] { InstructionSpec.NewObj("*System.Net.Http.HttpClient::.ctor*") }
    }));
```

---

## Edit

### Replace a string everywhere

```csharp
int replaced = 0;

foreach (var type in locator.FindTypes(new TypeSpec()))
    foreach (var method in type.FindMethods(new MethodSpec { HasBody = true }))
    {
        var body = method.CilMethodBody!;

        // ToList(): finish searching before changing anything.
        foreach (var hit in body.FindInstructions(InstructionSpec.LdStr("http://old.example.com")).ToList())
        {
            hit.Instruction.Operand = "https://new.example.com";
            replaced++;
        }
    }

Console.WriteLine($"{replaced} replacement(s)");
locator.Save("Sample.patched.dll");
```

### Change a numeric constant

Constants from 128 upwards are stored with an operand (`ldc.i4 30000`), so operand matching works:

```csharp
locator.EditInstructions(
    TypeSpec.ByFullName("Sample.Client"),
    MethodSpec.ByName("Connect"),
    InstructionSpec.WithMnemonic("ldc.i4", "30000"),     // timeout in milliseconds
    (body, match) => match.Instruction.Operand = 60000);
```

Small constants (0–8, and -1) are encoded in the opcode with no operand — see [Searching CIL instructions](cil-search.md#numeric-constants-and-the-no-operand-trap).

### Force a property getter to return true

```csharp
locator.EditBody(
    TypeSpec.ByFullName("Sample.Features"),
    PropertySpec.ByName("IsPremium"),
    PropertyAccessor.Get,
    body =>
    {
        body.Instructions.Clear();
        body.LocalVariables.Clear();
        body.ExceptionHandlers.Clear();
        body.Instructions.Add(new CilInstruction(CilOpCodes.Ldc_I4_1));
        body.Instructions.Add(new CilInstruction(CilOpCodes.Ret));
    });
```

### Remove logging statements of the form `Console.WriteLine("literal")`

A `ldstr` followed by the call forms one self-contained unit that leaves the stack unchanged, so both can safely become `nop`:

```csharp
var pattern = new[]
{
    InstructionSpec.LdStr("*"),
    InstructionSpec.Call("*Console::WriteLine(System.String)")
};

locator.EditBody(TypeSpec.ByFullName("Sample.Worker"), MethodSpec.ByName("Run"), body =>
{
    foreach (var sequence in body.FindPattern(pattern).ToList())
        foreach (var instruction in sequence.Instructions)
            instruction.ReplaceWithNop();
});
```

### Apply a patch to many files

```csharp
foreach (var file in Directory.EnumerateFiles("input", "*.dll"))
{
    try
    {
        var l = AssemblyLocator.FromBytes(File.ReadAllBytes(file));

        l.EditInstructions(
            TypeSpec.ByFullName("Sample.Greeter"),
            MethodSpec.ByName("Greet"),
            InstructionSpec.LdStr("Hello, "),
            (body, match) => match.Instruction.Operand = "Hi, ");

        l.Save(Path.Combine("output", Path.GetFileName(file)));
        Console.WriteLine($"patched  {file}");
    }
    catch (Exception ex) when (ex is MemberNotFoundException or BadImageFormatException)
    {
        Console.WriteLine($"skipped  {file}: {ex.Message}");
    }
}
```

`MemberNotFoundException` means "this file does not contain the target"; `BadImageFormatException` means "this is not a managed assembly".

---

## Make patches robust

### A patcher template: required target, idempotent, verified

```csharp
static void Patch(string input, string output)
{
    var locator = AssemblyLocator.FromBytes(File.ReadAllBytes(input));

    TypeSpec type = TypeSpec.ByFullName("Sample.Greeter");
    MethodSpec greet = MethodSpec.ByName("Greet");

    // Idempotent: do nothing if already patched.
    bool alreadyPatched = locator.FindInstructions(type, greet, InstructionSpec.LdStr("Hi, ")).Count > 0;
    if (alreadyPatched)
    {
        Console.WriteLine("already patched");
        return;
    }

    // Required: throws MemberNotFoundException if the target is missing.
    locator.EditInstructions(type, greet, InstructionSpec.LdStr("Hello, "),
        (body, match) => match.Instruction.Operand = "Hi, ");

    locator.Save(output);

    // Verify by reloading the saved file.
    var check = AssemblyLocator.FromBytes(File.ReadAllBytes(output));
    if (check.FindInstructions(type, greet, InstructionSpec.LdStr("Hi, ")).Count == 0)
        throw new InvalidOperationException("Patch verification failed.");
}
```

### Test your patch with xUnit

Compile the code you patch into a test fixture, patch it, save, reload and assert — the same approach AsmLocator's own tests use:

```csharp
public sealed class Fixture
{
    public static string Greet() => "Hello, ";
}

public sealed class PatchTests
{
    [Fact]
    public void Patch_ReplacesGreeting()
    {
        var locator = AssemblyLocator.FromFile(typeof(Fixture).Assembly.Location);
        TypeSpec type = TypeSpec.ByFullName(typeof(Fixture).FullName!);

        locator.EditInstructions(type, MethodSpec.ByName("Greet"), InstructionSpec.LdStr("Hello, "),
            (body, match) => match.Instruction.Operand = "Hi, ");

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dll");
        try
        {
            locator.Save(path);

            var reloaded = AssemblyLocator.FromBytes(File.ReadAllBytes(path));
            Assert.NotEmpty(reloaded.FindInstructions(type, MethodSpec.ByName("Greet"), InstructionSpec.LdStr("Hi, ")));
            Assert.Empty(reloaded.FindInstructions(type, MethodSpec.ByName("Greet"), InstructionSpec.LdStr("Hello, ")));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
```

More on structuring patchers: [Advanced topics](advanced.md).

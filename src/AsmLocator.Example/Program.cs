using AsmLocator;

// Usage: AsmLocator.Example [path-to-assembly]
// Without an argument the example inspects AsmLocator's own assembly.
string path = args.Length > 0
    ? args[0].Trim('"')
    : typeof(AssemblyLocator).Assembly.Location;

if (!File.Exists(path))
{
    Console.Error.WriteLine($"Assembly not found: {path}");
    return 1;
}

try
{
    var locator = AssemblyLocator.FromFile(path);

    // 1. Find types with a wildcard name matcher.
    Console.WriteLine("Types ending in 'Spec':");
    foreach (var type in locator.FindTypes(TypeSpec.ByName("*Spec")))
        Console.WriteLine($"  {type.FullName}");

    // 2. Find methods by metadata constraints.
    var matchers = new TypeSpec { FullName = "AsmLocator.StringMatcher" };
    var factories = locator.FindMethods(
        matchers,
        MethodSpec.ByName("*") with
        {
            IsPublic = true,
            IsStatic = true,
            HasBody = true,
            IsSpecialName = false
        });

    Console.WriteLine();
    Console.WriteLine("Public static methods of StringMatcher:");
    foreach (var method in factories)
        Console.WriteLine($"  {method.Name}");

    // 3. Search CIL: where is the Regex constructor called?
    var regexCalls = locator.FindInstructions(
        matchers,
        MethodSpec.ByName("Wildcard"),
        InstructionSpec.NewObj("*System.Text.RegularExpressions.Regex::.ctor*"));

    Console.WriteLine();
    Console.WriteLine($"Regex constructions inside StringMatcher.Wildcard: {regexCalls.Count}");
    foreach (var call in regexCalls)
        Console.WriteLine($"  [{call.Index}] {call.Instruction}");

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("Failed to analyse the assembly.");
    Console.Error.WriteLine(ex);
    return 1;
}

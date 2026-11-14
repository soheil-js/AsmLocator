using AsmLocator;
using Xunit;

namespace AsmLocator.Tests;

public sealed class AssemblyLocatorTests
{
    [Fact]
    public void FromFile_CanFindItsOwnPublicTypeAndMethod()
    {
        var assemblyPath = typeof(AssemblyLocator).Assembly.Location;
        var locator = AssemblyLocator.FromFile(assemblyPath);

        var type = locator.GetType(TypeSpec.ByFullName("AsmLocator.AssemblyLocator"));
        var refresh = locator.GetMethod(
            TypeSpec.ByFullName("AsmLocator.AssemblyLocator"),
            MethodSpec.ByName("Refresh") with { IsPublic = true });

        Assert.NotNull(type);
        Assert.Equal("Refresh", refresh.Name);
    }

    [Fact]
    public void FindTypes_ReturnsAllMatches()
    {
        var locator = AssemblyLocator.FromFile(typeof(AssemblyLocator).Assembly.Location);

        var matches = locator.FindTypes(TypeSpec.ByName("*Spec")).ToList();

        Assert.Contains(matches, type => type.Name == "MethodSpec");
        Assert.Contains(matches, type => type.Name == "InstructionSpec");
    }
}

public sealed class CilFixture
{
    public static void WriteLine(string value) => Console.WriteLine(value);

    public static object CreateObject() => new object();

    public static int GetLength(string value) => value.Length;
}

public sealed class CilLookupTests
{
    [Fact]
    public void Call_FindsCallAndCallvirtInstructionsByOperand()
    {
        var locator = AssemblyLocator.FromFile(typeof(CilFixture).Assembly.Location);
        TypeSpec type = TypeSpec.ByFullName("AsmLocator.Tests.CilFixture");

        var matches = locator.FindInstructions(
            type,
            MethodSpec.ByName("WriteLine"),
            InstructionSpec.Call("*System.Console::WriteLine*"));

        Assert.NotEmpty(matches);

        var virtualCall = locator.FindInstructions(
            type,
            MethodSpec.ByName("GetLength"),
            InstructionSpec.Call("*System.String::get_Length*"));

        Assert.NotEmpty(virtualCall);
    }

    [Fact]
    public void NewObj_FindsConstructorCalls()
    {
        var locator = AssemblyLocator.FromFile(typeof(CilFixture).Assembly.Location);
        TypeSpec type = TypeSpec.ByFullName("AsmLocator.Tests.CilFixture");

        var matches = locator.FindInstructions(
            type,
            MethodSpec.ByName("CreateObject"),
            InstructionSpec.NewObj("*System.Object::.ctor*"));

        Assert.NotEmpty(matches);
    }
}

using AsmResolver.PE.DotNet.Cil;

namespace AsmLocator.Tests;

public sealed class LookupSemanticsTests
{
    private static readonly TypeSpec Fixture = TypeSpec.ByFullName("AsmLocator.Tests.CilFixture");

    private static AssemblyLocator Load() =>
        AssemblyLocator.FromFile(typeof(CilFixture).Assembly.Location);

    [Fact]
    public void GetMethod_WithAmbiguousSpec_Throws()
    {
        var locator = Load();

        var ex = Assert.Throws<MemberNotFoundException>(
            () => locator.GetMethod(Fixture, MethodSpec.ByName("Overloaded")));

        Assert.Contains("ambiguous", ex.Message);
    }

    [Fact]
    public void GetMethod_WithMissingMethod_Throws()
    {
        var locator = Load();

        Assert.Throws<MemberNotFoundException>(
            () => locator.GetMethod(Fixture, MethodSpec.ByName("DoesNotExist")));
    }

    [Fact]
    public void FindMethod_WithMissingMethod_ReturnsNull()
    {
        var locator = Load();

        Assert.Null(locator.FindMethod(Fixture, MethodSpec.ByName("DoesNotExist")));
    }

    [Fact]
    public void MethodSpecOf_ResolvesTheExactOverload()
    {
        var locator = Load();

        var method = locator.GetMethod(Fixture, MethodSpec.Of("Overloaded", "System.String"));

        Assert.Equal("System.String", method.Signature!.ReturnType.FullName);
    }

    [Fact]
    public void GetMethod_ByPropertyAccessor_ReturnsGetterAndSetter()
    {
        var locator = Load();
        var property = PropertySpec.ByName("Counter");

        var getter = locator.GetMethod(Fixture, property, PropertyAccessor.Get);
        var setter = locator.GetMethod(Fixture, property, PropertyAccessor.Set);

        Assert.Equal("get_Counter", getter.Name);
        Assert.Equal("set_Counter", setter.Name);
    }

    [Fact]
    public void FindNestedTypes_RespectsTheRecursiveFlag()
    {
        var locator = Load();
        var fixture = locator.GetType(Fixture);

        var direct = fixture.FindNestedTypes(TypeSpec.ByName("*")).Select(t => t.Name?.ToString()).ToList();
        var all = fixture.FindNestedTypes(TypeSpec.ByName("*"), recursive: true).Select(t => t.Name?.ToString()).ToList();

        Assert.Contains("Nested", direct);
        Assert.DoesNotContain("Deep", direct);
        Assert.Contains("Nested", all);
        Assert.Contains("Deep", all);
    }

    [Fact]
    public void FindPattern_ReturnsContiguousSequenceWithStartIndex()
    {
        var locator = Load();

        var sequences = locator.FindPattern(
            Fixture,
            MethodSpec.ByName("GetLength"),
            InstructionSpec.Op(CilOpCodes.Ldarg_0),
            InstructionSpec.Call("*get_Length*"));

        var sequence = Assert.Single(sequences);
        Assert.Equal(0, sequence.StartIndex);
        Assert.Equal(2, sequence.Instructions.Count);
    }

    [Fact]
    public void FindPattern_AnyInstruction_ActsAsSingleInstructionWildcard()
    {
        var locator = Load();

        var sequences = locator.FindPattern(
            Fixture,
            MethodSpec.ByName("GetLength"),
            InstructionSpec.Call("*get_Length*"),
            InstructionSpec.AnyInstruction);

        var sequence = Assert.Single(sequences);
        Assert.Equal(2, sequence.Instructions.Count);
        Assert.Contains("get_Length", InstructionSpec.DescribeOperand(sequence.Instructions[0]));
    }

    [Fact]
    public void FindPattern_WithEmptyPattern_ThrowsWithoutEnumerating()
    {
        var locator = Load();
        var body = locator.GetBody(Fixture, MethodSpec.ByName("GetLength"));

        // Not enumerated on purpose: argument validation must happen eagerly.
        Assert.Throws<ArgumentException>(() => body.FindPattern(Array.Empty<InstructionSpec>()));
    }

    [Fact]
    public void Call_DoesNotMatchNewobj()
    {
        var locator = Load();

        var matches = locator.FindInstructions(
            Fixture,
            MethodSpec.ByName("CreateObject"),
            InstructionSpec.Call("*System.Object::.ctor*"));

        Assert.Empty(matches);
    }
}

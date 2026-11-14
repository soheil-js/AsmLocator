using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

namespace AsmLocator.Tests;

public sealed class EditingTests
{
    private static readonly TypeSpec Fixture = TypeSpec.ByFullName("AsmLocator.Tests.CilFixture");

    private static AssemblyLocator Load() =>
        AssemblyLocator.FromFile(typeof(CilFixture).Assembly.Location);

    [Fact]
    public void EditInstructions_ReplacesOperand_AndSurvivesSaveAndReload()
    {
        var locator = Load();

        locator.EditInstructions(
            Fixture,
            MethodSpec.ByName("GetGreeting"),
            InstructionSpec.LdStr("old"),
            (_, match) => match.Instruction.Operand = "new");

        string path = Path.Combine(Path.GetTempPath(), $"asmlocator-{Guid.NewGuid():N}.dll");
        try
        {
            locator.Save(path);

            // Load from bytes so the temp file is not kept open (matters on Windows).
            var reloaded = AssemblyLocator.FromBytes(File.ReadAllBytes(path));

            Assert.NotEmpty(reloaded.FindInstructions(
                Fixture, MethodSpec.ByName("GetGreeting"), InstructionSpec.LdStr("new")));
            Assert.Empty(reloaded.FindInstructions(
                Fixture, MethodSpec.ByName("GetGreeting"), InstructionSpec.LdStr("old")));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void EditInstructions_WithNoMatch_Throws()
    {
        var locator = Load();

        Assert.Throws<MemberNotFoundException>(() => locator.EditInstructions(
            Fixture,
            MethodSpec.ByName("GetGreeting"),
            InstructionSpec.LdStr("does-not-exist"),
            (_, _) => { }));
    }

    [Fact]
    public void EditInstructions_VisitsMatchesFromLastToFirst()
    {
        var locator = Load();
        var visited = new List<int>();

        locator.EditInstructions(
            Fixture,
            MethodSpec.ByName("WriteTwice"),
            InstructionSpec.WithMnemonic("ldstr"),
            (_, match) => visited.Add(match.Index));

        Assert.Equal(2, visited.Count);
        Assert.Equal(visited.OrderByDescending(i => i).ToList(), visited);
    }

    [Fact]
    public void EditBody_RecalculatesOffsets()
    {
        var locator = Load();

        locator.EditBody(Fixture, MethodSpec.ByName("GetLength"), body =>
            body.Instructions.Insert(0, new CilInstruction(CilOpCodes.Nop)));

        var body = locator.GetBody(Fixture, MethodSpec.ByName("GetLength"));
        Assert.Equal(0, body.Instructions[0].Offset);
        Assert.Equal(1, body.Instructions[1].Offset);
    }

    [Fact]
    public void EditBody_RecalculatesOffsets_EvenWhenCallbackThrows()
    {
        var locator = Load();

        Assert.Throws<InvalidOperationException>(() =>
            locator.EditBody(Fixture, MethodSpec.ByName("GetLength"), body =>
            {
                body.Instructions.Insert(0, new CilInstruction(CilOpCodes.Nop));
                throw new InvalidOperationException("boom");
            }));

        var body = locator.GetBody(Fixture, MethodSpec.ByName("GetLength"));
        Assert.Equal(1, body.Instructions[1].Offset);
    }
}

using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
using AsmResolver.PE.DotNet.Cil;
using System.Globalization;

namespace AsmLocator;

/// <summary>
/// Describes constraints for a CIL instruction. An empty specification matches every instruction
/// and can be used as a one-instruction wildcard inside a pattern.
/// </summary>
public sealed record InstructionSpec
{
    /// <summary>The exact CIL opcode code.</summary>
    public CilCode? Code { get; init; }

    /// <summary>The instruction mnemonic matcher, for example <c>call</c>, <c>ldstr</c>, or <c>ldc.i4*</c>.</summary>
    public StringMatcher? Mnemonic { get; init; }

    /// <summary>
    /// The operand matcher. Operand values are converted to text by <see cref="DescribeOperand"/>.
    /// For example, use <c>*Console::WriteLine*</c> to match a method descriptor.
    /// </summary>
    public StringMatcher? Operand { get; init; }

    /// <summary>The instruction offset to match. Offsets can be stale after edits until offsets are recalculated.</summary>
    public int? Offset { get; init; }

    /// <summary>An optional custom predicate applied after the built-in constraints.</summary>
    public Func<CilInstruction, bool>? Where { get; init; }

    /// <summary>Creates an instruction specification for an exact opcode and optional operand matcher.</summary>
    /// <param name="opCode">The CIL opcode to match.</param>
    /// <param name="operand">An optional matcher for the instruction operand.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static InstructionSpec Op(CilOpCode opCode, StringMatcher? operand = null) =>
        new() { Code = opCode.Code, Operand = operand };

    /// <summary>Creates an instruction specification for a mnemonic and optional operand matcher.</summary>
    /// <param name="mnemonic">The instruction mnemonic matcher.</param>
    /// <param name="operand">An optional matcher for the instruction operand.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static InstructionSpec WithMnemonic(StringMatcher mnemonic, StringMatcher? operand = null) =>
        new() { Mnemonic = mnemonic, Operand = operand };

    /// <summary>Matches <c>call</c> and <c>callvirt</c> instructions, but not <c>calli</c>.</summary>
    /// <param name="method">A matcher for the called method's full name.</param>
    /// <returns>An instruction specification for direct and virtual method calls.</returns>
    public static InstructionSpec Call(StringMatcher method)
    {
        ArgumentNullException.ThrowIfNull(method);
        return new()
        {
            Operand = method,
            Where = instruction => instruction.OpCode.Mnemonic is "call" or "callvirt"
        };
    }

    /// <summary>Matches <c>newobj</c> instructions whose constructor operand matches the supplied matcher.</summary>
    /// <param name="ctor">A matcher for the constructor descriptor.</param>
    /// <returns>The instruction specification.</returns>
    public static InstructionSpec NewObj(StringMatcher ctor)
    {
        ArgumentNullException.ThrowIfNull(ctor);
        return new() { Mnemonic = "newobj", Operand = ctor };
    }

    /// <summary>Matches <c>ldstr</c> instructions whose string operand matches the supplied matcher.</summary>
    /// <param name="text">A matcher for the string literal.</param>
    /// <returns>The instruction specification.</returns>
    public static InstructionSpec LdStr(StringMatcher text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new() { Mnemonic = "ldstr", Operand = text };
    }

    /// <summary>A specification that matches any single instruction, useful as a pattern wildcard.</summary>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static InstructionSpec AnyInstruction { get; } = new();

    /// <summary>Returns whether the supplied candidate satisfies all constraints configured in this specification.</summary>
    /// <param name="instruction">The candidate CIL instruction.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public bool Matches(CilInstruction instruction)
    {
        if (Code is { } code && instruction.OpCode.Code != code) return false;
        if (!Mnemonic.Test(instruction.OpCode.Mnemonic)) return false;
        if (Offset.Fails(instruction.Offset)) return false;
        if (Operand is not null && !Operand.IsMatch(DescribeOperand(instruction))) return false;

        return Where?.Invoke(instruction) ?? true;
    }

    /// <summary>
    /// Converts common operand types to the textual representation used by <see cref="Operand"/> matching:
    /// member and type descriptors use <c>FullName</c>; strings use their literal value; locals use <c>V_n</c>;
    /// parameters use their names; labels use <c>IL_XXXX</c>; switch labels are comma-separated; other values use invariant conversion.
    /// </summary>
    /// <param name="instruction">The candidate CIL instruction.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static string? DescribeOperand(CilInstruction instruction)
    {
        var operand = instruction.Operand;

        if (operand is null) return null;
        if (operand is string s) return s;
        if (operand is IMemberDescriptor member) return member.FullName;
        if (operand is ITypeDescriptor type) return type.FullName;
        if (operand is CilLocalVariable local) return $"V_{local.Index}";
        if (operand is Parameter p) return p.Name;
        if (operand is ICilLabel label) return $"IL_{label.Offset:X4}";
        if (operand is IEnumerable<ICilLabel> labels)
            return string.Join(",", labels.Select(l => $"IL_{l.Offset:X4}"));

        return Convert.ToString(operand, CultureInfo.InvariantCulture);
    }

    /// <summary>Returns a readable representation of this object.</summary>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public override string ToString() => MatchHelpers.Format("Instruction",
        ("Code", Code), ("Mnemonic", Mnemonic), ("Operand", Operand), ("Offset", Offset));
}

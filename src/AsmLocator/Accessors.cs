using AsmResolver.PE.DotNet.Cil;

namespace AsmLocator;

/// <summary>Identifies the property accessor method to retrieve.</summary>
public enum PropertyAccessor
{
    /// <summary>The property getter.</summary>
    Get,
    /// <summary>The property setter.</summary>
    Set
}

/// <summary>Identifies the event accessor method to retrieve.</summary>
public enum EventAccessor
{
    /// <summary>The event add-handler method.</summary>
    Add,
    /// <summary>The event remove-handler method.</summary>
    Remove,
    /// <summary>The optional event fire method.</summary>
    Fire
}

/// <summary>Represents a matched instruction and its index in the method instruction collection at search time.</summary>
/// <param name="Index">The zero-based instruction index at the time of the search.</param>
/// <param name="Instruction">The matched CIL instruction.</param>
public readonly record struct InstructionMatch(int Index, CilInstruction Instruction);

/// <summary>Represents a contiguous sequence of CIL instructions matching an instruction pattern.</summary>
/// <param name="StartIndex">The zero-based index of the first matched instruction at search time.</param>
/// <param name="Instructions">The instructions in the matched sequence.</param>
public sealed record InstructionSequence(int StartIndex, IReadOnlyList<CilInstruction> Instructions);

using AsmResolver.DotNet.Code.Cil;

namespace AsmLocator;

/// <summary>Describes constraints used to match a CIL local variable.</summary>
public sealed record LocalSpec
{
    /// <summary>The zero-based index within the parameter or local-variable collection.</summary>
    public int? Index { get; init; }
    /// <summary>The local variable type full-name matcher.</summary>
    public StringMatcher? VariableType { get; init; }

    /// <summary>An optional custom predicate applied after the built-in constraints.</summary>
    public Func<CilLocalVariable, bool>? Where { get; init; }

    /// <summary>Creates a specification constrained by the full name of a type.</summary>
    /// <param name="type">The local variable type full-name matcher.</param>
    /// <returns>A new specification with the described constraints.</returns>
    public static LocalSpec OfType(StringMatcher type) => new() { VariableType = type };

    /// <summary>Creates a specification constrained by a zero-based index.</summary>
    /// <param name="index">The zero-based local-variable index.</param>
    /// <returns>A new specification with the described constraints.</returns>
    public static LocalSpec At(int index) => new() { Index = index };

    /// <summary>Converts a string into a LocalSpec that matches the local type full name.</summary>
    /// <param name="type">The local variable type full-name matcher.</param>
    /// <returns>The specification created from the supplied string.</returns>
    public static implicit operator LocalSpec(string type) => OfType(type);

    /// <summary>Returns whether the supplied candidate satisfies all constraints configured in this specification.</summary>
    /// <param name="local">The candidate CIL local variable.</param>
    /// <returns><see langword="true"/> if every configured constraint is satisfied; otherwise <see langword="false"/>.</returns>
    public bool Matches(CilLocalVariable local)
    {
        if (Index.Fails(local.Index)) return false;
        if (!VariableType.Test(local.VariableType.FullName)) return false;

        return Where?.Invoke(local) ?? true;
    }

    /// <summary>Returns a readable representation of this object.</summary>
    /// <returns>A readable description.</returns>
    public override string ToString() => MatchHelpers.Format("Local",
        ("Index", Index), ("Type", VariableType));
}

using AsmResolver.DotNet;
using AsmResolver.DotNet.Collections;

namespace AsmLocator;

    /// <summary>Describes constraints used to match a method parameter.</summary>
public sealed record ParameterSpec
{
    /// <summary>The member name matcher. When null, the name is not used as a constraint.</summary>
    public StringMatcher? Name { get; init; }
    /// <summary>The parameter type full-name matcher.</summary>
    public StringMatcher? ParameterType { get; init; }

    /// <summary>The zero-based parameter index. The implicit <c>this</c> reference is not part of the parameter collection.</summary>
    public int? Index { get; init; }

    /// <summary>The custom attribute type full-name matcher.</summary>
    public StringMatcher? Attribute { get; init; }

    /// <summary>An optional custom predicate applied after the built-in constraints.</summary>
    public Func<Parameter, bool>? Where { get; init; }

    /// <summary>Creates a specification constrained by the full name of a type.</summary>
    /// <param name="type">The parameter type full-name matcher.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static ParameterSpec OfType(StringMatcher type) => new() { ParameterType = type };

    /// <summary>Creates a parameter specification constrained by parameter name.</summary>
    /// <param name="name">The name matcher for the target member.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static ParameterSpec Named(StringMatcher name) => new() { Name = name };

    /// <summary>Creates a specification constrained by a zero-based index.</summary>
    /// <param name="index">The zero-based parameter index.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static ParameterSpec At(int index) => new() { Index = index };

    /// <summary>Creates a parameter specification constrained by parameter name and type.</summary>
    /// <param name="name">The name matcher for the target member.</param>
    /// <param name="type">The parameter type full-name matcher.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static ParameterSpec Of(StringMatcher name, StringMatcher type) =>
        new() { Name = name, ParameterType = type };

    /// <summary>Converts a string into a ParameterSpec constrained by parameter type or name, as appropriate.</summary>
    /// <param name="type">The parameter type full-name matcher.</param>
    /// <returns>The result of the lookup, matching, conversion, or formatting operation.</returns>
    public static implicit operator ParameterSpec(string type) => OfType(type);

    /// <summary>Returns whether the supplied candidate satisfies all constraints configured in this specification.</summary>
    /// <param name="parameter">The candidate method parameter.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public bool Matches(Parameter parameter)
    {
        string? name = parameter.Name;
        if (!Name.Test(name)) return false;
        if (!ParameterType.Test(parameter.ParameterType.FullName)) return false;
        if (Index.Fails(parameter.Index)) return false;

        if (Attribute is not null &&
            (parameter.Definition is null || !parameter.Definition.HasAttribute(Attribute)))
            return false;

        return Where?.Invoke(parameter) ?? true;
    }

    /// <summary>Returns a readable representation of this object.</summary>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public override string ToString() => MatchHelpers.Format("Parameter",
        ("Index", Index), ("Name", Name), ("Type", ParameterType), ("Attribute", Attribute));
}

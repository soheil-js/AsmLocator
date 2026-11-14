using AsmResolver.DotNet;

namespace AsmLocator;

    /// <summary>Describes constraints used to match a field definition.</summary>
public sealed record FieldSpec
{
    /// <summary>The member name matcher. When null, the name is not used as a constraint.</summary>
    public StringMatcher? Name { get; init; }
    /// <summary>The field type full-name matcher.</summary>
    public StringMatcher? FieldType { get; init; }
    /// <summary>The custom attribute type full-name matcher.</summary>
    public StringMatcher? Attribute { get; init; }
    /// <summary>Whether the candidate must be static.</summary>
    public bool? IsStatic { get; init; }
    /// <summary>Whether the candidate must be public.</summary>
    public bool? IsPublic { get; init; }
    /// <summary>Whether the candidate must be private.</summary>
    public bool? IsPrivate { get; init; }
    /// <summary>Whether the field must be a literal constant.</summary>
    public bool? IsLiteral { get; init; }
    /// <summary>Whether the field must be init-only (readonly).</summary>
    public bool? IsInitOnly { get; init; }

    /// <summary>An optional custom predicate applied after the built-in constraints.</summary>
    public Func<FieldDefinition, bool>? Where { get; init; }

    /// <summary>Creates a specification constrained by member name.</summary>
    /// <param name="name">The name matcher for the target member.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static FieldSpec ByName(StringMatcher name) => new() { Name = name };

    /// <summary>When only a name is supplied, matches by name; when a type is supplied, also matches the field type.</summary>
    /// <param name="name">The name matcher for the target member.</param>
    /// <param name="fieldType">The fieldType argument for this operation.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static FieldSpec Of(StringMatcher name, StringMatcher? fieldType = null) =>
        new() { Name = name, FieldType = fieldType };

    /// <summary>Converts a string into a FieldSpec constrained by field type or name, as appropriate.</summary>
    /// <param name="name">The name matcher for the target member.</param>
    /// <returns>The result of the lookup, matching, conversion, or formatting operation.</returns>
    public static implicit operator FieldSpec(string name) => ByName(name);

    /// <summary>Returns whether the supplied candidate satisfies all constraints configured in this specification.</summary>
    /// <param name="field">The candidate AsmResolver field definition.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public bool Matches(FieldDefinition field)
    {
        if (!Name.Test(field.Name)) return false;
        if (!FieldType.Test(field.Signature?.FieldType.FullName)) return false;

        if (IsStatic.Fails(field.IsStatic)) return false;
        if (IsPublic.Fails(field.IsPublic)) return false;
        if (IsPrivate.Fails(field.IsPrivate)) return false;
        if (IsLiteral.Fails(field.IsLiteral)) return false;
        if (IsInitOnly.Fails(field.IsInitOnly)) return false;

        if (Attribute is not null && !field.HasAttribute(Attribute)) return false;

        return Where?.Invoke(field) ?? true;
    }

    /// <summary>Returns a readable representation of this object.</summary>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public override string ToString() => MatchHelpers.Format("Field",
        ("Name", Name), ("Type", FieldType), ("Attribute", Attribute));
}

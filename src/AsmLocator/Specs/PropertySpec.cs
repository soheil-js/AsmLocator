using AsmResolver.DotNet;

namespace AsmLocator;

/// <summary>Describes constraints used to match a property definition.</summary>
public sealed record PropertySpec
{
    /// <summary>The member name matcher. When null, the name is not used as a constraint.</summary>
    public StringMatcher? Name { get; init; }
    /// <summary>The property type full-name matcher.</summary>
    public StringMatcher? PropertyType { get; init; }
    /// <summary>The custom attribute type full-name matcher.</summary>
    public StringMatcher? Attribute { get; init; }
    /// <summary>Whether the property must have a getter.</summary>
    public bool? HasGetter { get; init; }
    /// <summary>Whether the property must have a setter.</summary>
    public bool? HasSetter { get; init; }

    /// <summary>Whether the property accessors are static; the getter is preferred when present, otherwise the setter is inspected.</summary>
    public bool? IsStatic { get; init; }

    /// <summary>An optional custom predicate applied after the built-in constraints.</summary>
    public Func<PropertyDefinition, bool>? Where { get; init; }

    /// <summary>Creates a specification constrained by member name.</summary>
    /// <param name="name">The name matcher for the target member.</param>
    /// <returns>A new specification with the described constraints.</returns>
    public static PropertySpec ByName(StringMatcher name) => new() { Name = name };

    /// <summary>When only a name is supplied, matches by name; when a property type is supplied, also matches its type.</summary>
    /// <param name="name">The name matcher for the target member.</param>
    /// <param name="propertyType">An optional matcher for the property type full name.</param>
    /// <returns>A new specification with the described constraints.</returns>
    public static PropertySpec Of(StringMatcher name, StringMatcher? propertyType = null) =>
        new() { Name = name, PropertyType = propertyType };

    /// <summary>Converts a string into a PropertySpec that matches the property name.</summary>
    /// <param name="name">The name matcher for the target member.</param>
    /// <returns>The specification created from the supplied string.</returns>
    public static implicit operator PropertySpec(string name) => ByName(name);

    /// <summary>Returns whether the supplied candidate satisfies all constraints configured in this specification.</summary>
    /// <param name="property">The candidate AsmResolver property definition.</param>
    /// <returns><see langword="true"/> if every configured constraint is satisfied; otherwise <see langword="false"/>.</returns>
    public bool Matches(PropertyDefinition property)
    {
        if (!Name.Test(property.Name)) return false;
        if (!PropertyType.Test(property.Signature?.ReturnType.FullName)) return false;

        if (HasGetter.Fails(property.GetMethod is not null)) return false;
        if (HasSetter.Fails(property.SetMethod is not null)) return false;

        if (IsStatic is { } wantStatic)
        {
            var accessor = property.GetMethod ?? property.SetMethod;
            if (accessor is null || accessor.IsStatic != wantStatic) return false;
        }

        if (Attribute is not null && !property.HasAttribute(Attribute)) return false;

        return Where?.Invoke(property) ?? true;
    }

    /// <summary>Returns a readable representation of this object.</summary>
    /// <returns>A readable description.</returns>
    public override string ToString() => MatchHelpers.Format("Property",
        ("Name", Name), ("Type", PropertyType), ("Attribute", Attribute));
}

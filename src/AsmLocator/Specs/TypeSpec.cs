using AsmResolver.DotNet;

namespace AsmLocator;

/// <summary>Describes constraints used to match a type definition.</summary>
public sealed record TypeSpec
{
    /// <summary>The namespace matcher. When null, the namespace is not used as a constraint.</summary>
    public StringMatcher? Namespace { get; init; }
    /// <summary>The member name matcher. When null, the name is not used as a constraint.</summary>
    public StringMatcher? Name { get; init; }

    /// <summary>Nested type full names use the plus sign separator, for example <c>Example.Outer+Inner</c>.</summary>
    public StringMatcher? FullName { get; init; }

    /// <summary>The base type full-name matcher.</summary>
    public StringMatcher? BaseType { get; init; }

    /// <summary>The candidate type must implement every interface listed here.</summary>
    public IReadOnlyList<StringMatcher>? Interfaces { get; init; }

    /// <summary>The full name matcher for a custom attribute type.</summary>
    public StringMatcher? Attribute { get; init; }

    /// <summary>The specification of the declaring type for nested-type matching.</summary>
    public TypeSpec? DeclaringType { get; init; }

    /// <summary>The required number of generic parameters.</summary>
    public int? GenericParameterCount { get; init; }
    /// <summary>Whether the candidate must be a nested type.</summary>
    public bool? IsNested { get; init; }
    /// <summary>Whether the type must be abstract.</summary>
    public bool? IsAbstract { get; init; }
    /// <summary>Whether the candidate must be sealed.</summary>
    public bool? IsSealed { get; init; }
    /// <summary>Whether the candidate must be an interface.</summary>
    public bool? IsInterface { get; init; }
    /// <summary>Whether the candidate must be an enum.</summary>
    public bool? IsEnum { get; init; }
    /// <summary>Whether the candidate must be a value type.</summary>
    public bool? IsValueType { get; init; }

    /// <summary>An optional custom predicate applied after the built-in constraints.</summary>
    public Func<TypeDefinition, bool>? Where { get; init; }

    /// <summary>Creates a type specification constrained by namespace and type name.</summary>
    /// <param name="namespace">The namespace matcher.</param>
    /// <param name="name">The type name matcher.</param>
    /// <returns>A new specification with the described constraints.</returns>
    public static TypeSpec Of(StringMatcher @namespace, StringMatcher name) =>
        new() { Namespace = @namespace, Name = name };

    /// <summary>Creates a specification constrained by member name.</summary>
    /// <param name="name">The type name matcher.</param>
    /// <returns>A new specification with the described constraints.</returns>
    public static TypeSpec ByName(StringMatcher name) => new() { Name = name };

    /// <summary>Creates a type specification constrained by full name.</summary>
    /// <param name="fullName">The type full-name matcher.</param>
    /// <returns>A new specification with the described constraints.</returns>
    public static TypeSpec ByFullName(StringMatcher fullName) => new() { FullName = fullName };

    /// <summary>Creates a specification for a nested type with the supplied declaring type and nested type name.</summary>
    /// <param name="outer">The specification of the declaring type.</param>
    /// <param name="name">The type name matcher.</param>
    /// <returns>A new specification with the described constraints.</returns>
    public static TypeSpec NestedIn(TypeSpec outer, StringMatcher name) =>
        new() { DeclaringType = outer, Name = name };

    /// <summary>Converts a full type name string into a <see cref="TypeSpec"/>.</summary>
    /// <param name="fullName">The type full-name matcher.</param>
    /// <returns>The specification created from the supplied string.</returns>
    public static implicit operator TypeSpec(string fullName) => ByFullName(fullName);

    /// <summary>Returns whether the supplied candidate satisfies all constraints configured in this specification.</summary>
    /// <param name="type">The candidate AsmResolver type definition.</param>
    /// <returns><see langword="true"/> if every configured constraint is satisfied; otherwise <see langword="false"/>.</returns>
    public bool Matches(TypeDefinition type)
    {
        if (!FullName.Test(type.FullName)) return false;
        if (!Namespace.Test(type.Namespace)) return false;
        if (!Name.Test(type.Name)) return false;

        if (IsNested.Fails(type.DeclaringType is not null)) return false;
        if (IsAbstract.Fails(type.IsAbstract)) return false;
        if (IsSealed.Fails(type.IsSealed)) return false;
        if (IsInterface.Fails(type.IsInterface)) return false;
        if (IsEnum.Fails(type.IsEnum)) return false;
        if (IsValueType.Fails(type.IsValueType)) return false;
        if (GenericParameterCount.Fails(type.GenericParameters.Count)) return false;

        if (DeclaringType is not null &&
            (type.DeclaringType is null || !DeclaringType.Matches(type.DeclaringType)))
            return false;

        if (BaseType is not null && !BaseType.IsMatch(type.BaseType?.FullName)) return false;

        if (Interfaces is not null &&
            !Interfaces.All(m => type.Interfaces.Any(impl => m.IsMatch(impl.Interface?.FullName))))
            return false;

        if (Attribute is not null && !type.HasAttribute(Attribute)) return false;

        return Where?.Invoke(type) ?? true;
    }

    /// <summary>Returns a readable representation of this object.</summary>
    /// <returns>A readable description.</returns>
    public override string ToString() => MatchHelpers.Format("Type",
        ("FullName", FullName), ("Namespace", Namespace), ("Name", Name),
        ("BaseType", BaseType), ("Attribute", Attribute));
}

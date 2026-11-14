using AsmResolver.DotNet;

namespace AsmLocator;

/// <summary>Describes constraints used to match a method definition, including its signature and optional body contents.</summary>
public sealed record MethodSpec
{
    /// <summary>The member name matcher. When null, the name is not used as a constraint.</summary>
    public StringMatcher? Name { get; init; }

    /// <summary>
    /// When null, the parameter count and types are unconstrained. When set, the count and order must match exactly.
    /// </summary>
    public IReadOnlyList<ParameterSpec>? Parameters { get; init; }

    /// <summary>The required number of parameters.</summary>
    public int? ParameterCount { get; init; }
    /// <summary>The return type full-name matcher.</summary>
    public StringMatcher? ReturnType { get; init; }
    /// <summary>The custom attribute type full-name matcher.</summary>
    public StringMatcher? Attribute { get; init; }
    /// <summary>The required number of generic parameters.</summary>
    public int? GenericParameterCount { get; init; }

    /// <summary>Whether the candidate must be static.</summary>
    public bool? IsStatic { get; init; }
    /// <summary>Whether the candidate must be public.</summary>
    public bool? IsPublic { get; init; }
    /// <summary>Whether the candidate must be private.</summary>
    public bool? IsPrivate { get; init; }
    /// <summary>Whether the method must be abstract.</summary>
    public bool? IsAbstract { get; init; }
    /// <summary>Whether the method must be virtual.</summary>
    public bool? IsVirtual { get; init; }
    /// <summary>Whether the method must be a constructor.</summary>
    public bool? IsConstructor { get; init; }
    /// <summary>
    /// Whether the method must be marked <c>specialname</c>. Property and event accessors
    /// (<c>get_X</c>, <c>set_X</c>, <c>add_X</c>, <c>remove_X</c>), operator overloads (<c>op_Implicit</c>)
    /// and constructors carry this flag. Use <see langword="false"/> to exclude them.
    /// </summary>
    public bool? IsSpecialName { get; init; }

    /// <summary>Whether the method must contain a CIL method body.</summary>
    public bool? HasBody { get; init; }

    /// <summary>Each local specification must match at least one local variable in the method body.</summary>
    public IReadOnlyList<LocalSpec>? Locals { get; init; }

    /// <summary>Each instruction specification must match at least one instruction in the body; list order is ignored.</summary>
    public IReadOnlyList<InstructionSpec>? BodyContains { get; init; }

    /// <summary>An optional custom predicate applied after the built-in constraints.</summary>
    public Func<MethodDefinition, bool>? Where { get; init; }

    /// <summary>Creates a method specification constrained only by name, without constraining its parameters.</summary>
    /// <param name="name">The name matcher for the target member.</param>
    /// <returns>A new specification with the described constraints.</returns>
    public static MethodSpec ByName(StringMatcher name) => new() { Name = name };

    /// <summary>Creates a method specification constrained only by the supplied name.</summary>
    /// <param name="name">The name matcher for the target member.</param>
    /// <returns>A new specification with the described constraints.</returns>
    public static MethodSpec Of(StringMatcher name) => ByName(name);

    /// <summary>
    /// The resulting specification requires the exact parameter count and order. A <c>*</c> type matcher accepts any parameter type.
    /// </summary>
    /// <param name="name">The name matcher for the target member.</param>
    /// <param name="parameterTypes">The ordered parameter type full names.</param>
    /// <returns>A new specification with the described constraints.</returns>
    public static MethodSpec Of(StringMatcher name, params string[] parameterTypes) =>
        new() { Name = name, Parameters = parameterTypes.Select(t => ParameterSpec.OfType(t)).ToArray() };

    /// <summary>Creates a method specification that requires no parameters.</summary>
    /// <param name="name">The name matcher for the target member.</param>
    /// <returns>A new specification with the described constraints.</returns>
    public static MethodSpec NoParameters(StringMatcher name) =>
        new() { Name = name, Parameters = Array.Empty<ParameterSpec>() };

    /// <summary>Creates a method specification using detailed, ordered parameter specifications.</summary>
    /// <param name="name">The name matcher for the target member.</param>
    /// <param name="parameters">The ordered parameter specifications.</param>
    /// <returns>A new specification with the described constraints.</returns>
    public static MethodSpec WithParameters(StringMatcher name, params ParameterSpec[] parameters) =>
        new() { Name = name, Parameters = parameters };

    /// <summary>Creates a specification for an instance constructor with the supplied parameter types.</summary>
    /// <param name="parameterTypes">The ordered parameter type full names.</param>
    /// <returns>A new specification with the described constraints.</returns>
    public static MethodSpec Constructor(params string[] parameterTypes) =>
        Of(".ctor", parameterTypes);

    /// <summary>Creates a specification for a parameterless static constructor.</summary>
    /// <returns>A new specification with the described constraints.</returns>
    public static MethodSpec StaticConstructor() => NoParameters(".cctor");

    /// <summary>Converts a method name string into a <see cref="MethodSpec"/>.</summary>
    /// <param name="name">The name matcher for the target member.</param>
    /// <returns>The specification created from the supplied string.</returns>
    public static implicit operator MethodSpec(string name) => ByName(name);

    /// <summary>Returns whether the supplied candidate satisfies all constraints configured in this specification.</summary>
    /// <param name="method">The candidate AsmResolver method definition.</param>
    /// <returns><see langword="true"/> if every configured constraint is satisfied; otherwise <see langword="false"/>.</returns>
    public bool Matches(MethodDefinition method)
    {
        if (!Name.Test(method.Name)) return false;

        if (IsStatic.Fails(method.IsStatic)) return false;
        if (IsPublic.Fails(method.IsPublic)) return false;
        if (IsPrivate.Fails(method.IsPrivate)) return false;
        if (IsAbstract.Fails(method.IsAbstract)) return false;
        if (IsVirtual.Fails(method.IsVirtual)) return false;
        if (IsConstructor.Fails(method.IsConstructor)) return false;
        if (IsSpecialName.Fails(method.IsSpecialName)) return false;
        if (GenericParameterCount.Fails(method.GenericParameters.Count)) return false;
        if (ParameterCount.Fails(method.Parameters.Count)) return false;

        if (ReturnType is not null && !ReturnType.IsMatch(method.Signature?.ReturnType.FullName))
            return false;

        if (Parameters is not null)
        {
            if (method.Parameters.Count != Parameters.Count) return false;
            for (int i = 0; i < Parameters.Count; i++)
                if (!Parameters[i].Matches(method.Parameters[i])) return false;
        }

        if (Attribute is not null && !method.HasAttribute(Attribute)) return false;

        // Body inspection is deferred until the inexpensive metadata constraints have passed,
        // and the body is only read when a constraint actually needs it.
        if (HasBody is not null || Locals is not null || BodyContains is not null)
        {
            var body = method.CilMethodBody;
            if (HasBody.Fails(body is not null)) return false;

            if (Locals is not null || BodyContains is not null)
            {
                if (body is null) return false;

                if (Locals is not null && !Locals.All(l => body.LocalVariables.Any(l.Matches)))
                    return false;

                if (BodyContains is not null && !BodyContains.All(s => body.Instructions.Any(s.Matches)))
                    return false;
            }
        }

        return Where?.Invoke(method) ?? true;
    }

    /// <summary>Returns a readable representation of this object.</summary>
    /// <returns>A readable description.</returns>
    public override string ToString() => MatchHelpers.Format("Method",
        ("Name", Name),
        ("Params", Parameters is null ? null : "[" + string.Join(", ", Parameters.Select(p => p.ParameterType?.ToString() ?? "*")) + "]"),
        ("Returns", ReturnType),
        ("Attribute", Attribute));
}

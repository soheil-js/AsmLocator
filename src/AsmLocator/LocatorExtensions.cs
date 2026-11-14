using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
using AsmResolver.PE.DotNet.Cil;

namespace AsmLocator;

/// <summary>
/// Provides lookup operations directly on AsmResolver objects without creating an <see cref="AssemblyLocator"/>.
/// For example: <c>typeDef.FindMethod(MethodSpec.ByName("Execute"))</c>.
/// </summary>
public static class LocatorExtensions
{
    // ---------- TypeDefinition ----------

    /// <summary>Enumerates methods declared directly on this type that satisfy the supplied method specification.</summary>
    /// <param name="type">The AsmResolver type definition on which the extension method operates.</param>
    /// <param name="spec">The specification that candidate objects must satisfy.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static IEnumerable<MethodDefinition> FindMethods(this TypeDefinition type, MethodSpec spec)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(spec);
        return type.Methods.Where(spec.Matches);
    }

    /// <summary>Returns the first method declared on this type that satisfies the specification, or null.</summary>
    /// <param name="type">The AsmResolver type definition on which the extension method operates.</param>
    /// <param name="spec">The specification that candidate objects must satisfy.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static MethodDefinition? FindMethod(this TypeDefinition type, MethodSpec spec) =>
        type.FindMethods(spec).FirstOrDefault();

    /// <summary>Enumerates fields declared directly on this type that satisfy the supplied field specification.</summary>
    /// <param name="type">The AsmResolver type definition on which the extension method operates.</param>
    /// <param name="spec">The specification that candidate objects must satisfy.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static IEnumerable<FieldDefinition> FindFields(this TypeDefinition type, FieldSpec spec)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(spec);
        return type.Fields.Where(spec.Matches);
    }

    /// <summary>Returns the first field declared on this type that satisfies the specification, or null.</summary>
    /// <param name="type">The AsmResolver type definition on which the extension method operates.</param>
    /// <param name="spec">The specification that candidate objects must satisfy.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static FieldDefinition? FindField(this TypeDefinition type, FieldSpec spec) =>
        type.FindFields(spec).FirstOrDefault();

    /// <summary>Enumerates properties declared directly on this type that satisfy the supplied property specification.</summary>
    /// <param name="type">The AsmResolver type definition on which the extension method operates.</param>
    /// <param name="spec">The specification that candidate objects must satisfy.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static IEnumerable<PropertyDefinition> FindProperties(this TypeDefinition type, PropertySpec spec)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(spec);
        return type.Properties.Where(spec.Matches);
    }

    /// <summary>Returns the first property declared on this type that satisfies the specification, or null.</summary>
    /// <param name="type">The AsmResolver type definition on which the extension method operates.</param>
    /// <param name="spec">The specification that candidate objects must satisfy.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static PropertyDefinition? FindProperty(this TypeDefinition type, PropertySpec spec) =>
        type.FindProperties(spec).FirstOrDefault();

    /// <summary>Enumerates events declared directly on this type that satisfy the supplied event specification.</summary>
    /// <param name="type">The AsmResolver type definition on which the extension method operates.</param>
    /// <param name="spec">The specification that candidate objects must satisfy.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static IEnumerable<EventDefinition> FindEvents(this TypeDefinition type, EventSpec spec)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(spec);
        return type.Events.Where(spec.Matches);
    }

    /// <summary>Returns the first event declared on this type that satisfies the specification, or null.</summary>
    /// <param name="type">The AsmResolver type definition on which the extension method operates.</param>
    /// <param name="spec">The specification that candidate objects must satisfy.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static EventDefinition? FindEvent(this TypeDefinition type, EventSpec spec) =>
        type.FindEvents(spec).FirstOrDefault();

    /// <summary>Finds nested types declared directly by this type, optionally including all descendants.</summary>
    /// <param name="type">The declaring type to search.</param>
    /// <param name="spec">The specification that candidate nested types must satisfy.</param>
    /// <param name="recursive">When <see langword="true"/>, also searches nested types at every depth.</param>
    /// <returns>Matching nested type definitions.</returns>
    public static IEnumerable<TypeDefinition> FindNestedTypes(
        this TypeDefinition type, TypeSpec spec, bool recursive = false)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(spec);
        return Enumerate(type).Where(spec.Matches);

        IEnumerable<TypeDefinition> Enumerate(TypeDefinition parent)
        {
            foreach (var nested in parent.NestedTypes)
            {
                yield return nested;
                if (recursive)
                {
                    foreach (var descendant in Enumerate(nested))
                        yield return descendant;
                }
            }
        }
    }

    // ---------- Property / Event accessors ----------

    /// <summary>Returns the requested accessor method for this property or event, or null when that accessor is not defined.</summary>
    /// <param name="property">The property definition whose accessor is requested.</param>
    /// <param name="accessor">The property or event accessor to retrieve.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static MethodDefinition? GetAccessor(this PropertyDefinition property, PropertyAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(property);
        return accessor switch
        {
            PropertyAccessor.Get => property.GetMethod,
            PropertyAccessor.Set => property.SetMethod,
            _ => throw new ArgumentOutOfRangeException(nameof(accessor), accessor, "Unknown property accessor.")
        };
    }

    /// <summary>Returns the requested accessor method for this property or event, or null when that accessor is not defined.</summary>
    /// <param name="ev">The event definition whose accessor is requested.</param>
    /// <param name="accessor">The property or event accessor to retrieve.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static MethodDefinition? GetAccessor(this EventDefinition ev, EventAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(ev);
        return accessor switch
        {
            EventAccessor.Add => ev.AddMethod,
            EventAccessor.Remove => ev.RemoveMethod,
            EventAccessor.Fire => ev.FireMethod,
            _ => throw new ArgumentOutOfRangeException(nameof(accessor), accessor, "Unknown event accessor.")
        };
    }

    // ---------- MethodDefinition ----------

    /// <summary>Enumerates parameters of this method that satisfy the supplied parameter specification.</summary>
    /// <param name="method">The AsmResolver method definition whose parameters are searched.</param>
    /// <param name="spec">The specification that candidate objects must satisfy.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static IEnumerable<Parameter> FindParameters(this MethodDefinition method, ParameterSpec spec)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(spec);
        return method.Parameters.Where(spec.Matches);
    }

    /// <summary>Returns the first matching parameter, or null when no parameter matches.</summary>
    /// <param name="method">The AsmResolver method definition whose parameters are searched.</param>
    /// <param name="spec">The specification that candidate objects must satisfy.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static Parameter? FindParameter(this MethodDefinition method, ParameterSpec spec) =>
        method.FindParameters(spec).FirstOrDefault();

    // ---------- CilMethodBody ----------

    /// <summary>Enumerates local variables in this method body that satisfy the supplied local specification.</summary>
    /// <param name="body">The CIL method body containing the local variables or instructions to search.</param>
    /// <param name="spec">The specification that candidate objects must satisfy.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static IEnumerable<CilLocalVariable> FindLocals(this CilMethodBody body, LocalSpec spec)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(spec);
        return body.LocalVariables.Where(spec.Matches);
    }

    /// <summary>Enumerates matching instructions in this method body and includes each instruction's current index.</summary>
    /// <param name="body">The CIL method body containing the local variables or instructions to search.</param>
    /// <param name="spec">The specification that candidate objects must satisfy.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static IEnumerable<InstructionMatch> FindInstructions(this CilMethodBody body, InstructionSpec spec)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(spec);
        var instructions = body.Instructions;
        for (int i = 0; i < instructions.Count; i++)
            if (spec.Matches(instructions[i]))
                yield return new InstructionMatch(i, instructions[i]);
    }

    /// <summary>Enumerates contiguous instruction sequences that exactly match the supplied ordered pattern.</summary>
    /// <param name="body">The CIL method body containing the local variables or instructions to search.</param>
    /// <param name="pattern">The ordered instruction specifications that form the pattern.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static IEnumerable<InstructionSequence> FindPattern(
        this CilMethodBody body, IReadOnlyList<InstructionSpec> pattern)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(pattern);
        if (pattern.Count == 0)
            throw new ArgumentException("Pattern must contain at least one InstructionSpec.", nameof(pattern));
        if (pattern.Any(spec => spec is null))
            throw new ArgumentException("Pattern cannot contain null specifications.", nameof(pattern));

        var instructions = body.Instructions;
        for (int start = 0; start + pattern.Count <= instructions.Count; start++)
        {
            bool matched = true;
            for (int k = 0; k < pattern.Count; k++)
            {
                if (!pattern[k].Matches(instructions[start + k]))
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                var slice = new CilInstruction[pattern.Count];
                for (int k = 0; k < slice.Length; k++) slice[k] = instructions[start + k];
                yield return new InstructionSequence(start, slice);
            }
        }
    }
}

using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;

namespace AsmLocator;

/// <summary>
/// Lookup naming conventions:
/// <list type="bullet">
/// <item><description><c>FindX</c> returns the first match or null.</description></item>
/// <item><description><c>FindXs</c> lazily enumerates all matches.</description></item>
/// <item><description><c>GetX</c> requires exactly one match and throws <see cref="MemberNotFoundException"/> otherwise.</description></item>
/// <item><description><c>EditX</c> resolves one match and invokes an edit callback.</description></item>
/// </list>
/// </summary>
public sealed class AssemblyLocator
{
    private IReadOnlyList<TypeDefinition>? _types;

    /// <summary>The underlying AsmResolver module being searched or edited.</summary>
    public ModuleDefinition Module { get; }

    /// <summary>Creates a locator around an existing AsmResolver module.</summary>
    /// <param name="module">The module to inspect and edit.</param>
    /// <exception cref="ArgumentNullException"><paramref name="module"/> is null.</exception>
    public AssemblyLocator(ModuleDefinition module) =>
        Module = module ?? throw new ArgumentNullException(nameof(module));

    /// <summary>Loads a .NET module from the specified file and creates a locator for it.</summary>
    /// <param name="path">The path of the input assembly or module file.</param>
    /// <returns>A locator that wraps the loaded module.</returns>
    public static AssemblyLocator FromFile(string path) => new(ModuleDefinition.FromFile(path));

    /// <summary>Loads a .NET module from a byte array and creates a locator for it.</summary>
    /// <param name="data">The byte array containing the assembly image.</param>
    /// <returns>A locator that wraps the loaded module.</returns>
    public static AssemblyLocator FromBytes(byte[] data) => new(ModuleDefinition.FromBytes(data));

    /// <summary>Invalidates the cached type list after types are added to or removed from the underlying module.</summary>
    public void Refresh() => _types = null;

    private IReadOnlyList<TypeDefinition> AllTypes => _types ??= Module.GetAllTypes().ToList();

    // ================= Type =================

    /// <summary>Returns the first type matching the supplied specification, or null when no type matches.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public TypeDefinition? FindType(TypeSpec type) => FindTypes(type).FirstOrDefault();

    /// <summary>Enumerates all types matching the supplied specification.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public IEnumerable<TypeDefinition> FindTypes(TypeSpec type) => AllTypes.Where(type.Matches);

    /// <summary>Returns the unique type matching the supplied specification.</summary>
    /// <exception cref="MemberNotFoundException">No type matches, or more than one type matches.</exception>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <returns>The matching member or sequence, according to the method name and lookup semantics.</returns>
    public TypeDefinition GetType(TypeSpec type) => One(FindTypes(type), $"{type}");

    /// <summary>Finds exactly one type and invokes the supplied callback to edit it.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="edit">The callback that performs the requested edit.</param>
    public void EditType(TypeSpec type, Action<TypeDefinition> edit)
    {
        ArgumentNullException.ThrowIfNull(edit);
        edit(GetType(type));
    }

    // ================= Field =================

    /// <summary>Enumerates matching fields from every type that matches the supplied type specification.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="field">The field specification used to select candidate fields.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public IEnumerable<FieldDefinition> FindFields(TypeSpec type, FieldSpec field) =>
        FindTypes(type).SelectMany(t => t.FindFields(field));

    /// <summary>Returns the first matching field, or null when no field matches.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="field">The field specification used to select candidate fields.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public FieldDefinition? FindField(TypeSpec type, FieldSpec field) =>
        FindFields(type, field).FirstOrDefault();

    /// <summary>Returns exactly one matching field from the uniquely matched type.</summary>
    /// <exception cref="MemberNotFoundException">The type or field is missing, or the field match is ambiguous.</exception>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="field">The field specification used to select candidate fields.</param>
    /// <returns>The matching member or sequence, according to the method name and lookup semantics.</returns>
    public FieldDefinition GetField(TypeSpec type, FieldSpec field) =>
        One(GetType(type).FindFields(field), $"{field} in {type}");

    /// <summary>Finds exactly one field and invokes the supplied callback to edit it.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="field">The field specification used to select candidate fields.</param>
    /// <param name="edit">The callback that performs the requested edit.</param>
    public void EditField(TypeSpec type, FieldSpec field, Action<FieldDefinition> edit)
    {
        ArgumentNullException.ThrowIfNull(edit);
        edit(GetField(type, field));
    }

    // ================= Property =================

    /// <summary>Enumerates matching properties from every type that matches the supplied type specification.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="property">The property specification used to select candidate properties.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public IEnumerable<PropertyDefinition> FindProperties(TypeSpec type, PropertySpec property) =>
        FindTypes(type).SelectMany(t => t.FindProperties(property));

    /// <summary>Returns the first matching property, or null when no property matches.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="property">The property specification used to select candidate properties.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public PropertyDefinition? FindProperty(TypeSpec type, PropertySpec property) =>
        FindProperties(type, property).FirstOrDefault();

    /// <summary>Returns exactly one matching property from the uniquely matched type.</summary>
    /// <exception cref="MemberNotFoundException">The type or property is missing, or the property match is ambiguous.</exception>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="property">The property specification used to select candidate properties.</param>
    /// <returns>The matching member or sequence, according to the method name and lookup semantics.</returns>
    public PropertyDefinition GetProperty(TypeSpec type, PropertySpec property) =>
        One(GetType(type).FindProperties(property), $"{property} in {type}");

    /// <summary>Finds exactly one property and invokes the supplied callback to edit it.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="property">The property specification used to select candidate properties.</param>
    /// <param name="edit">The callback that performs the requested edit.</param>
    public void EditProperty(TypeSpec type, PropertySpec property, Action<PropertyDefinition> edit)
    {
        ArgumentNullException.ThrowIfNull(edit);
        edit(GetProperty(type, property));
    }

    // ================= Event =================

    /// <summary>Enumerates matching events from every type that matches the supplied type specification.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="ev">The event specification used to select candidate events.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public IEnumerable<EventDefinition> FindEvents(TypeSpec type, EventSpec ev) =>
        FindTypes(type).SelectMany(t => t.FindEvents(ev));

    /// <summary>Returns the first matching event, or null when no event matches.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="ev">The event specification used to select candidate events.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public EventDefinition? FindEvent(TypeSpec type, EventSpec ev) =>
        FindEvents(type, ev).FirstOrDefault();

    /// <summary>Returns exactly one matching event from the uniquely matched type.</summary>
    /// <exception cref="MemberNotFoundException">The type or event is missing, or the event match is ambiguous.</exception>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="ev">The event specification used to select candidate events.</param>
    /// <returns>The matching member or sequence, according to the method name and lookup semantics.</returns>
    public EventDefinition GetEvent(TypeSpec type, EventSpec ev) =>
        One(GetType(type).FindEvents(ev), $"{ev} in {type}");

    /// <summary>Finds exactly one event and invokes the supplied callback to edit it.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="ev">The event specification used to select candidate events.</param>
    /// <param name="edit">The callback that performs the requested edit.</param>
    public void EditEvent(TypeSpec type, EventSpec ev, Action<EventDefinition> edit)
    {
        ArgumentNullException.ThrowIfNull(edit);
        edit(GetEvent(type, ev));
    }

    // ================= Method =================

    /// <summary>Enumerates matching methods from every type that matches the supplied type specification.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="method">The method specification used to select candidate methods.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public IEnumerable<MethodDefinition> FindMethods(TypeSpec type, MethodSpec method) =>
        FindTypes(type).SelectMany(t => t.FindMethods(method));

    /// <summary>Returns the first matching method, or null when no method matches.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="method">The method specification used to select candidate methods.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public MethodDefinition? FindMethod(TypeSpec type, MethodSpec method) =>
        FindMethods(type, method).FirstOrDefault();

    /// <summary>Returns exactly one method matching the supplied type and method specifications.</summary>
    /// <exception cref="MemberNotFoundException">The type or method is missing, or the method match is ambiguous.</exception>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="method">The method specification used to select candidate methods.</param>
    /// <returns>The matching member or sequence, according to the method name and lookup semantics.</returns>
    public MethodDefinition GetMethod(TypeSpec type, MethodSpec method) =>
        One(GetType(type).FindMethods(method), $"{method} in {type}");

    /// <summary>Finds exactly one method and invokes the supplied callback to edit it.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="method">The method specification used to select candidate methods.</param>
    /// <param name="edit">The callback that performs the requested edit.</param>
    public void EditMethod(TypeSpec type, MethodSpec method, Action<MethodDefinition> edit)
    {
        ArgumentNullException.ThrowIfNull(edit);
        edit(GetMethod(type, method));
    }

    /// <summary>Gets the requested getter or setter method of a property.</summary>
    /// <exception cref="MemberNotFoundException">The property or requested accessor is missing.</exception>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="property">The property specification used to select candidate properties.</param>
    /// <param name="accessor">The property or event accessor to retrieve.</param>
    /// <returns>The matching member or sequence, according to the method name and lookup semantics.</returns>
    public MethodDefinition GetMethod(TypeSpec type, PropertySpec property, PropertyAccessor accessor) =>
        GetProperty(type, property).GetAccessor(accessor)
        ?? throw new MemberNotFoundException($"{property} in {type} has no {accessor} accessor.");

    /// <summary>Gets the requested add, remove, or fire method of an event.</summary>
    /// <exception cref="MemberNotFoundException">The event or requested accessor is missing.</exception>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="ev">The event specification used to select candidate events.</param>
    /// <param name="accessor">The property or event accessor to retrieve.</param>
    /// <returns>The matching member or sequence, according to the method name and lookup semantics.</returns>
    public MethodDefinition GetMethod(TypeSpec type, EventSpec ev, EventAccessor accessor) =>
        GetEvent(type, ev).GetAccessor(accessor)
        ?? throw new MemberNotFoundException($"{ev} in {type} has no {accessor} accessor.");

    // ================= Parameter =================

    /// <summary>Enumerates parameters from matching methods that satisfy the parameter specification.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="method">The method specification used to select candidate methods.</param>
    /// <param name="parameter">The parameter specification used to select candidate parameters.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public IEnumerable<Parameter> FindParameters(TypeSpec type, MethodSpec method, ParameterSpec parameter) =>
        FindMethods(type, method).SelectMany(m => m.FindParameters(parameter));

    /// <summary>Returns the first matching parameter, or null when no parameter matches.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="method">The method specification used to select candidate methods.</param>
    /// <param name="parameter">The parameter specification used to select candidate parameters.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public Parameter? FindParameter(TypeSpec type, MethodSpec method, ParameterSpec parameter) =>
        FindParameters(type, method, parameter).FirstOrDefault();

    /// <summary>Returns exactly one matching parameter from the uniquely matched method.</summary>
    /// <exception cref="MemberNotFoundException">The method or parameter is missing, or the parameter match is ambiguous.</exception>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="method">The method specification used to select candidate methods.</param>
    /// <param name="parameter">The parameter specification used to select candidate parameters.</param>
    /// <returns>The matching member or sequence, according to the method name and lookup semantics.</returns>
    public Parameter GetParameter(TypeSpec type, MethodSpec method, ParameterSpec parameter) =>
        One(GetMethod(type, method).FindParameters(parameter), $"{parameter} in {method} of {type}");

    // ================= CilMethodBody =================

    /// <summary>Returns the body of the first matching method, or null when the method or its body is absent.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="method">The method specification used to select candidate methods.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public CilMethodBody? FindBody(TypeSpec type, MethodSpec method) =>
        FindMethod(type, method)?.CilMethodBody;

    /// <summary>Returns the CIL body of the uniquely matched method or accessor; throws if it is unavailable.</summary>
    /// <exception cref="MemberNotFoundException">The method, accessor, or CIL body is missing, or the method match is ambiguous.</exception>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="method">The method specification used to select candidate methods.</param>
    /// <returns>The matching member or sequence, according to the method name and lookup semantics.</returns>
    public CilMethodBody GetBody(TypeSpec type, MethodSpec method) =>
        BodyOf(GetMethod(type, method), $"{method} in {type}");

    /// <summary>Returns the CIL body of the uniquely matched method or accessor; throws if it is unavailable.</summary>
    /// <exception cref="MemberNotFoundException">The method, accessor, or CIL body is missing, or the method match is ambiguous.</exception>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="property">The property specification used to select candidate properties.</param>
    /// <param name="accessor">The property or event accessor to retrieve.</param>
    /// <returns>The matching member or sequence, according to the method name and lookup semantics.</returns>
    public CilMethodBody GetBody(TypeSpec type, PropertySpec property, PropertyAccessor accessor) =>
        BodyOf(GetMethod(type, property, accessor), $"{accessor} of {property} in {type}");

    /// <summary>Returns the CIL body of the uniquely matched method or accessor; throws if it is unavailable.</summary>
    /// <exception cref="MemberNotFoundException">The method, accessor, or CIL body is missing, or the method match is ambiguous.</exception>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="ev">The event specification used to select candidate events.</param>
    /// <param name="accessor">The property or event accessor to retrieve.</param>
    /// <returns>The matching member or sequence, according to the method name and lookup semantics.</returns>
    public CilMethodBody GetBody(TypeSpec type, EventSpec ev, EventAccessor accessor) =>
        BodyOf(GetMethod(type, ev, accessor), $"{accessor} of {ev} in {type}");

    /// <summary>Invokes the callback to edit a method body and recalculates instruction offsets afterward.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="method">The method specification used to select candidate methods.</param>
    /// <param name="edit">The callback that performs the requested edit.</param>
    public void EditBody(TypeSpec type, MethodSpec method, Action<CilMethodBody> edit) =>
        EditBody(GetBody(type, method), edit);

    /// <summary>Invokes the callback to edit a method or accessor body and recalculates instruction offsets afterward.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="property">The property specification used to select candidate properties.</param>
    /// <param name="accessor">The property or event accessor to retrieve.</param>
    /// <param name="edit">The callback that performs the requested edit.</param>
    public void EditBody(TypeSpec type, PropertySpec property, PropertyAccessor accessor, Action<CilMethodBody> edit) =>
        EditBody(GetBody(type, property, accessor), edit);

    /// <summary>Invokes the callback to edit a method or accessor body and recalculates instruction offsets afterward.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="ev">The event specification used to select candidate events.</param>
    /// <param name="accessor">The property or event accessor to retrieve.</param>
    /// <param name="edit">The callback that performs the requested edit.</param>
    public void EditBody(TypeSpec type, EventSpec ev, EventAccessor accessor, Action<CilMethodBody> edit) =>
        EditBody(GetBody(type, ev, accessor), edit);

    // ================= Local variables =================

    /// <summary>Enumerates local variables from matching method bodies that satisfy the local specification.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="method">The method specification used to select candidate methods.</param>
    /// <param name="local">The local-variable specification used to select candidate locals.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public IEnumerable<CilLocalVariable> FindLocals(TypeSpec type, MethodSpec method, LocalSpec local) =>
        FindMethods(type, method)
            .Select(m => m.CilMethodBody)
            .OfType<CilMethodBody>()
            .SelectMany(b => b.FindLocals(local));

    /// <summary>Returns exactly one local variable from the uniquely matched method body.</summary>
    /// <exception cref="MemberNotFoundException">The method, body, or local variable is missing, or the local match is ambiguous.</exception>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="method">The method specification used to select candidate methods.</param>
    /// <param name="local">The local-variable specification used to select candidate locals.</param>
    /// <returns>The matching member or sequence, according to the method name and lookup semantics.</returns>
    public CilLocalVariable GetLocal(TypeSpec type, MethodSpec method, LocalSpec local) =>
        One(GetBody(type, method).FindLocals(local), $"{local} in {method} of {type}");

    // ================= Instructions =================
    // Instruction indexes are local to one method body, so these methods require one uniquely matched method.

    /// <summary>Returns all matching instructions from the uniquely matched method body, including their indexes.</summary>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="method">The method specification used to select candidate methods.</param>
    /// <param name="instruction">The instruction specification used to select candidate instructions.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public IReadOnlyList<InstructionMatch> FindInstructions(TypeSpec type, MethodSpec method, InstructionSpec instruction) =>
        GetBody(type, method).FindInstructions(instruction).ToList();

    /// <summary>Returns exactly one matching instruction from the uniquely matched method body.</summary>
    /// <exception cref="MemberNotFoundException">The method, body, or instruction is missing, or the instruction match is ambiguous.</exception>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="method">The method specification used to select candidate methods.</param>
    /// <param name="instruction">The instruction specification used to select candidate instructions.</param>
    /// <returns>The matching member or sequence, according to the method name and lookup semantics.</returns>
    public InstructionMatch GetInstruction(TypeSpec type, MethodSpec method, InstructionSpec instruction) =>
        One(GetBody(type, method).FindInstructions(instruction), $"{instruction} in {method} of {type}");

    /// <summary>Returns all contiguous instruction sequences matching the ordered pattern.</summary>
    /// <exception cref="ArgumentException">The pattern is empty or contains a null specification.</exception>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="method">The method specification used to select candidate methods.</param>
    /// <param name="pattern">The wildcard or regular-expression pattern.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public IReadOnlyList<InstructionSequence> FindPattern(TypeSpec type, MethodSpec method, params InstructionSpec[] pattern) =>
        GetBody(type, method).FindPattern(pattern).ToList();

    /// <summary>Returns exactly one contiguous instruction sequence matching the ordered pattern.</summary>
    /// <exception cref="MemberNotFoundException">No sequence matches, or more than one sequence matches.</exception>
    /// <exception cref="ArgumentException">The pattern is empty.</exception>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="method">The method specification used to select candidate methods.</param>
    /// <param name="pattern">The wildcard or regular-expression pattern.</param>
    /// <returns>The matching member or sequence, according to the method name and lookup semantics.</returns>
    public InstructionSequence GetPattern(TypeSpec type, MethodSpec method, params InstructionSpec[] pattern) =>
        One(GetBody(type, method).FindPattern(pattern), $"instruction pattern in {method} of {type}");

    /// <summary>
    /// Invokes the callback for each matching instruction, from the last match to the first, to reduce index invalidation
    /// when callbacks insert or remove instructions. Throws when no instruction matches.
    /// </summary>
    /// <exception cref="MemberNotFoundException">The method/body is missing or no instruction matches.</exception>
    /// <param name="type">The type specification used to select candidate types.</param>
    /// <param name="method">The method specification used to select candidate methods.</param>
    /// <param name="instruction">The instruction specification used to select candidate instructions.</param>
    /// <param name="edit">The callback that performs the requested edit.</param>
    public void EditInstructions(TypeSpec type, MethodSpec method, InstructionSpec instruction,
        Action<CilMethodBody, InstructionMatch> edit)
    {
        ArgumentNullException.ThrowIfNull(edit);
        var body = GetBody(type, method);
        var matches = body.FindInstructions(instruction).ToList();
        if (matches.Count == 0)
            throw new MemberNotFoundException($"{instruction} was not found in {method} of {type}.");

        try
        {
            for (int i = matches.Count - 1; i >= 0; i--)
                edit(body, matches[i]);
        }
        finally
        {
            // Keep offsets consistent even if a callback throws after partially mutating the body.
            body.Instructions.CalculateOffsets();
        }
    }

    // ================= Save =================

    /// <summary>Writes the underlying module to the specified output path.</summary>
    /// <param name="path">The input or output file path.</param>
    public void Save(string path) => Module.Write(path);

    // ================= helpers =================

    private static void EditBody(CilMethodBody body, Action<CilMethodBody> edit)
    {
        ArgumentNullException.ThrowIfNull(edit);
        try
        {
            edit(body);
        }
        finally
        {
            body.Instructions.CalculateOffsets();
        }
    }

    private static CilMethodBody BodyOf(MethodDefinition method, string what) =>
        method.CilMethodBody
        ?? throw new MemberNotFoundException($"{what} has no CIL body (abstract / extern / native?).");

    private static T One<T>(IEnumerable<T> source, string what)
    {
        var items = source.Take(2).ToList();
        return items.Count switch
        {
            0 => throw new MemberNotFoundException($"{what} was not found."),
            1 => items[0],
            _ => throw new MemberNotFoundException($"{what} is ambiguous (more than one match). Make the spec more specific.")
        };
    }
}

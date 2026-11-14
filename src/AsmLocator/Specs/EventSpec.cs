using AsmResolver.DotNet;

namespace AsmLocator;

    /// <summary>Describes constraints used to match an event definition.</summary>
public sealed record EventSpec
{
    /// <summary>The member name matcher. When null, the name is not used as a constraint.</summary>
    public StringMatcher? Name { get; init; }
    /// <summary>The event type full-name matcher.</summary>
    public StringMatcher? EventType { get; init; }
    /// <summary>The custom attribute type full-name matcher.</summary>
    public StringMatcher? Attribute { get; init; }

    /// <summary>An optional custom predicate applied after the built-in constraints.</summary>
    public Func<EventDefinition, bool>? Where { get; init; }

    /// <summary>Creates a specification constrained by member name.</summary>
    /// <param name="name">The name matcher for the target member.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static EventSpec ByName(StringMatcher name) => new() { Name = name };

    /// <summary>When only a name is supplied, matches by name; when an event type is supplied, also matches its type.</summary>
    /// <param name="name">The name matcher for the target member.</param>
    /// <param name="eventType">The eventType argument for this operation.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static EventSpec Of(StringMatcher name, StringMatcher? eventType = null) =>
        new() { Name = name, EventType = eventType };

    /// <summary>Converts a string into a EventSpec constrained by event type or name, as appropriate.</summary>
    /// <param name="name">The name matcher for the target member.</param>
    /// <returns>The result of the lookup, matching, conversion, or formatting operation.</returns>
    public static implicit operator EventSpec(string name) => ByName(name);

    /// <summary>Returns whether the supplied candidate satisfies all constraints configured in this specification.</summary>
    /// <param name="ev">The candidate AsmResolver event definition.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public bool Matches(EventDefinition ev)
    {
        if (!Name.Test(ev.Name)) return false;
        if (!EventType.Test(ev.EventType?.FullName)) return false;
        if (Attribute is not null && !ev.HasAttribute(Attribute)) return false;

        return Where?.Invoke(ev) ?? true;
    }

    /// <summary>Returns a readable representation of this object.</summary>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public override string ToString() => MatchHelpers.Format("Event",
        ("Name", Name), ("Type", EventType), ("Attribute", Attribute));
}

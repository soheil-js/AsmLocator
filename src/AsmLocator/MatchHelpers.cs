using AsmResolver.DotNet;

namespace AsmLocator;

internal static class MatchHelpers
{
    /// <summary>Returns true when the matcher is null (meaning that the value is unconstrained); otherwise tests the value.</summary>
    /// <param name="matcher">The string matcher to apply.</param>
    /// <param name="value">The value to compare or match.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static bool Test(this StringMatcher? matcher, string? value) =>
        matcher is null || matcher.IsMatch(value);

    /// <summary>Returns true when an expected value is specified and differs from the actual value.</summary>
    /// <param name="expected">The optional expected value.</param>
    /// <param name="actual">The actual value to compare.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static bool Fails(this bool? expected, bool actual) =>
        expected.HasValue && expected.Value != actual;

    /// <summary>Returns true when the expected value is specified and differs from the actual value.</summary>
    /// <param name="expected">The optional expected value.</param>
    /// <param name="actual">The actual value to compare.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static bool Fails(this int? expected, int actual) =>
        expected.HasValue && expected.Value != actual;

    /// <summary>Determines whether the metadata owner declares a custom attribute matching the supplied type-name matcher.</summary>
    /// <param name="owner">The metadata owner whose custom attributes are inspected.</param>
    /// <param name="matcher">The string matcher to apply.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static bool HasAttribute(this IHasCustomAttribute owner, StringMatcher matcher) =>
        owner.CustomAttributes.Any(a => matcher.IsMatch(a.Constructor?.DeclaringType?.FullName));

    /// <summary>Creates a readable diagnostic representation of a specification and its configured constraints.</summary>
    /// <param name="kind">The name of the specification kind.</param>
    /// <param name="parts">The named values to include in the formatted description.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
    public static string Format(string kind, params (string Name, object? Value)[] parts)
    {
        var text = string.Join(", ",
            parts.Where(p => p.Value is not null).Select(p => $"{p.Name}={p.Value}"));
        return text.Length == 0 ? $"{kind}(any)" : $"{kind}({text})";
    }
}

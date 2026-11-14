using System.Text.RegularExpressions;

namespace AsmLocator;

/// <summary>
/// Represents a reusable string-matching strategy. Implicit conversion from <see cref="string"/> uses exact matching
/// unless the value contains <c>*</c> or <c>?</c>, in which case wildcard matching is used.
/// Use <see cref="FromRegex"/> or <see cref="Where"/> for regular expressions or custom predicates.
/// </summary>
public sealed class StringMatcher
{
    private readonly Func<string, bool> _test;
    private readonly string _description;

    private StringMatcher(Func<string, bool> test, string description)
    {
        _test = test;
        _description = description;
    }

    /// <summary>
    /// Gets a matcher that accepts every input string.
    /// </summary>
    public static StringMatcher Any { get; } = new(_ => true, "*");

    /// <summary>Creates a matcher that compares the complete input with <paramref name="value"/>.</summary>
    /// <param name="value">The value to match. Cannot be <see langword="null"/>.</param>
    /// <param name="comparison">The string comparison rules.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <returns>A new matcher.</returns>
    public static StringMatcher Exact(string value, StringComparison comparison = StringComparison.Ordinal)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(v => string.Equals(v, value, comparison), value);
    }

    /// <summary>Creates a wildcard matcher where <c>*</c> matches any number of characters and <c>?</c> matches one character.</summary>
    /// <param name="pattern">The wildcard pattern. Cannot be <see langword="null"/>.</param>
    /// <param name="ignoreCase"><see langword="true"/> to ignore character casing.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <returns>A new matcher.</returns>
    public static StringMatcher Wildcard(string pattern, bool ignoreCase = false)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        var options = RegexOptions.Singleline | RegexOptions.CultureInvariant;
        if (ignoreCase) options |= RegexOptions.IgnoreCase;

        var regex = new Regex(
            "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$",
            options,
            TimeSpan.FromSeconds(2));
        return new(v => regex.IsMatch(v), pattern);
    }

    /// <summary>Creates a matcher from a .NET regular expression.</summary>
    /// <param name="pattern">The regular expression pattern. Cannot be <see langword="null"/>.</param>
    /// <param name="options">Regular expression options.</param>
    /// <remarks>Matching uses a two-second timeout. Add <c>^</c> and <c>$</c> to require a full-string match.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is not a valid regular expression.</exception>
    /// <returns>A new matcher.</returns>
    public static StringMatcher FromRegex(string pattern, RegexOptions options = RegexOptions.None)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        var regex = new Regex(pattern, options, TimeSpan.FromSeconds(2));
        return new(v => regex.IsMatch(v), "/" + pattern + "/");
    }

    /// <summary>Creates a matcher backed by a custom predicate.</summary>
    /// <param name="predicate">The predicate to invoke for each candidate.</param>
    /// <param name="description">A display description used by <see cref="ToString"/>.</param>
    /// <returns>A new matcher.</returns>
    public static StringMatcher Where(Func<string, bool> predicate, string description = "<predicate>")
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(description);
        return new(predicate, description);
    }

    /// <summary>Creates an exact matcher or wildcard matcher depending on whether the pattern contains <c>*</c> or <c>?</c>.</summary>
    /// <param name="pattern">The pattern to parse.</param>
    /// <returns>A new matcher.</returns>
    public static StringMatcher Parse(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        if (pattern == "*") return Any;
        return pattern.IndexOfAny(['*', '?']) >= 0 ? Wildcard(pattern) : Exact(pattern);
    }

    /// <summary>Tests the value; a null input is treated as an empty string.</summary>
    /// <param name="value">The value to compare or match.</param>
    /// <returns><see langword="true"/> if every configured constraint is satisfied; otherwise <see langword="false"/>.</returns>
    public bool IsMatch(string? value) => _test(value ?? string.Empty);

    /// <summary>Converts a string into a <see cref="StringMatcher"/> using <see cref="Parse"/>.</summary>
    /// <param name="pattern">The exact string or wildcard pattern.</param>
    /// <returns>The matcher created from the supplied string.</returns>
    public static implicit operator StringMatcher(string pattern) => Parse(pattern);

    /// <summary>Returns a readable representation of this object.</summary>
    /// <returns>A readable description.</returns>
    public override string ToString() => _description;
}

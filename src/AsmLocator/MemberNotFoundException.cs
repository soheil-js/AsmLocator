namespace AsmLocator;

/// <summary>Represents a lookup failure, including missing or ambiguous results and unavailable method bodies or accessors.</summary>
/// <param name="message">A message describing the lookup failure.</param>
    /// <returns>The result of the requested lookup or matching operation.</returns>
public sealed class MemberNotFoundException(string message) : Exception(message);

namespace AsmLocator;

/// <summary>
/// Thrown when a <c>Get*</c> or <c>Edit*</c> lookup finds no match, finds more than one match,
/// or reaches a member that has no CIL body or no requested accessor.
/// </summary>
/// <param name="message">A message describing the lookup failure.</param>
public sealed class MemberNotFoundException(string message) : Exception(message);

using AsmLocator;
using Xunit;

namespace AsmLocator.Tests;

public sealed class StringMatcherTests
{
    [Fact]
    public void Exact_MatchesOnlyTheCompleteValue()
    {
        var matcher = StringMatcher.Exact("Execute");

        Assert.True(matcher.IsMatch("Execute"));
        Assert.False(matcher.IsMatch("execute"));
        Assert.False(matcher.IsMatch("ExecuteAsync"));
    }

    [Fact]
    public void Exact_CanUseCaseInsensitiveComparison()
    {
        var matcher = StringMatcher.Exact("Execute", StringComparison.OrdinalIgnoreCase);

        Assert.True(matcher.IsMatch("execute"));
    }

    [Theory]
    [InlineData("Get*", "GetUser", true)]
    [InlineData("Get*", "Get", true)]
    [InlineData("Get*", "UserGet", false)]
    [InlineData("A?C", "ABC", true)]
    [InlineData("A?C", "AC", false)]
    public void Wildcard_UsesGlobStyleMatching(string pattern, string value, bool expected)
    {
        Assert.Equal(expected, StringMatcher.Wildcard(pattern).IsMatch(value));
    }

    [Fact]
    public void Wildcard_CanIgnoreCase()
    {
        Assert.True(StringMatcher.Wildcard("get*", ignoreCase: true).IsMatch("GetUser"));
    }

    [Fact]
    public void Parse_UsesWildcardOnlyWhenWildcardTokensExist()
    {
        Assert.True(StringMatcher.Parse("Find*").IsMatch("FindUser"));
        Assert.False(StringMatcher.Parse("Find").IsMatch("FindUser"));
        Assert.Same(StringMatcher.Any, StringMatcher.Parse("*"));
    }

    [Fact]
    public void Any_MatchesNullAndNonNullValues()
    {
        Assert.True(StringMatcher.Any.IsMatch(null));
        Assert.True(StringMatcher.Any.IsMatch("anything"));
    }

    [Fact]
    public void IsMatch_TreatsNullAsEmptyString()
    {
        Assert.True(StringMatcher.Exact(string.Empty).IsMatch(null));
        Assert.False(StringMatcher.Exact("value").IsMatch(null));
    }

    [Fact]
    public void FromRegex_UsesRegularExpressionMatching()
    {
        var matcher = StringMatcher.FromRegex(@"^Get[A-Z]\w+$");

        Assert.True(matcher.IsMatch("GetUser"));
        Assert.False(matcher.IsMatch("getUser"));
        Assert.False(matcher.IsMatch("Get"));
    }

    [Fact]
    public void Where_UsesTheProvidedPredicate()
    {
        var matcher = StringMatcher.Where(value => value.StartsWith("System.", StringComparison.Ordinal));

        Assert.True(matcher.IsMatch("System.String"));
        Assert.False(matcher.IsMatch("MyApp.User"));
    }

    [Fact]
    public void Factories_RejectNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => StringMatcher.Exact(null!));
        Assert.Throws<ArgumentNullException>(() => StringMatcher.Wildcard(null!));
        Assert.Throws<ArgumentNullException>(() => StringMatcher.FromRegex(null!));
        Assert.Throws<ArgumentNullException>(() => StringMatcher.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => StringMatcher.Where(null!));
    }
}

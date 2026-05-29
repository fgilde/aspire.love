using AspireLove.Core.Naming;

namespace AspireLove.Core.Tests;

public class NameConventionsTests
{
    [Theory]
    [InlineData("My Cool App", "MyCoolApp")]
    [InlineData("my-cool-app", "MyCoolApp")]
    [InlineData("my_cool_app", "MyCoolApp")]
    [InlineData("mandate-manager-ai", "MandateManagerAi")]
    [InlineData("already PascalButSpaced", "AlreadyPascalButSpaced")]
    public void ToPascalIdentifier_produces_pascal_case(string input, string expected) =>
        Assert.Equal(expected, NameConventions.ToPascalIdentifier(input));

    [Fact]
    public void ToPascalIdentifier_prefixes_underscore_when_starting_with_digit() =>
        Assert.Equal("_123app", NameConventions.ToPascalIdentifier("123app"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    public void ToPascalIdentifier_falls_back_when_no_word_characters(string input) =>
        Assert.Equal("MyAspireLove", NameConventions.ToPascalIdentifier(input));

    [Theory]
    [InlineData("My Cool App", "my-cool-app")]
    [InlineData("MyCoolApp", "mycoolapp")]
    [InlineData("mandate-manager-ai", "mandate-manager-ai")]
    public void ToResourceSlug_produces_lowercase_hyphenated(string input, string expected) =>
        Assert.Equal(expected, NameConventions.ToResourceSlug(input));

    [Fact]
    public void ToResourceSlug_falls_back_when_no_word_characters() =>
        Assert.Equal("myaspirelove", NameConventions.ToResourceSlug("***"));
}

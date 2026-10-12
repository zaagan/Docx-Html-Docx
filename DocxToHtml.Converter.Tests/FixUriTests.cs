using DocxToHtml.Converter;

namespace DocxToHtml.Converter.Tests;

/// <summary>
/// The handler <see cref="HtmlConverter"/> hands to
/// <see cref="UriFixer.FixInvalidUri"/> for every relationship target the framework
/// rejects: a mailto: target keeps its address, anything else is dropped.
/// </summary>
public class FixUriTests
{
    private const string Dropped = " ";

    [Fact]
    public void FixUri_MailtoUri_ReturnsTheAddressWithoutTheScheme()
    {
        Assert.Equal("someone@example.com", HtmlConverter.FixUri("mailto:someone@example.com"));
    }

    [Theory]
    [InlineData("MAILTO:someone@example.com")]
    [InlineData("Mailto:someone@example.com")]
    [InlineData("mailTo:someone@example.com")]
    public void FixUri_MailtoSchemeInAnyCase_ReturnsTheAddressWithoutTheScheme(string brokenUri)
    {
        // URI schemes are case-insensitive (RFC 3986 §3.1), so the casing cannot change
        // what the target means.
        Assert.Equal("someone@example.com", HtmlConverter.FixUri(brokenUri));
    }

    [Fact]
    public void FixUri_MailtoUriWithSeveralAddresses_KeepsThemAll()
    {
        Assert.Equal("first@example.com second@example.com",
            HtmlConverter.FixUri("mailto:first@example.com second@example.com"));
    }

    [Theory]
    [InlineData("http://bad host")]
    [InlineData("https://")]
    [InlineData("not a uri at all")]
    [InlineData("")]
    public void FixUri_UriThatIsNotMailto_IsDropped(string brokenUri)
    {
        Assert.Equal(Dropped, HtmlConverter.FixUri(brokenUri));
    }

    [Theory]
    [InlineData("http://example.com/?to=mailto:someone@example.com")]
    [InlineData("see mailto:someone@example.com")]
    [InlineData("x-mailto:someone@example.com")]
    public void FixUri_TextWhereMailtoIsNotTheScheme_IsDropped(string brokenUri)
    {
        // Only a leading "mailto:" is a scheme. Stripping seven characters from anything
        // that merely contains the word truncates the target into nonsense.
        Assert.Equal(Dropped, HtmlConverter.FixUri(brokenUri));
    }

    [Fact]
    public void FixUri_BareMailtoScheme_ReturnsAnEmptyAddress()
    {
        Assert.Equal(string.Empty, HtmlConverter.FixUri("mailto:"));
    }
}

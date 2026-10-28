using Buildi.Primitives.Contact;

namespace Buildi.Primitives.Tests.Contact;

public class ContactMaskingExtensionsTests
{
    [Fact]
    public void Phone_Swedish_MasksMiddleDigits()
    {
        var phone = PhoneNumber.Parse("0701740633");
        var masked = phone.ToMaskedString();
        Assert.Contains("**", masked);
        Assert.EndsWith("33", masked);
    }

    [Fact]
    public void Phone_Swedish_VisibleDigitsAtEnd0_MasksAllSubscriberDigits()
    {
        var phone = PhoneNumber.Parse("0701740633");
        var masked = phone.ToMaskedString(visibleDigitsAtEnd: 0);
        Assert.DoesNotContain("33", masked);
    }

    [Theory]
    [InlineData("0701740633", 2, "070-*** ** 33")]
    [InlineData("0701740633", 0, "070-*** ** **")]
    [InlineData("0701740633", 4, "070-*** 06 33")]
    [InlineData("0846500456", 2, "08-*** *** 56")]
    [InlineData("0980319247", 2, "0980-** ** 47")]
    public void Phone_Swedish_MasksSubscriberDigitsAfterAreaCode(string input, int visibleDigitsAtEnd, string expected)
    {
        Assert.Equal(expected, PhoneNumber.Parse(input).ToMaskedString(visibleDigitsAtEnd));
    }

    [Fact]
    public void Phone_International_MasksMiddleDigits()
    {
        var phone = PhoneNumber.Parse("+44 20 7946 0958");
        var masked = phone.ToMaskedString();
        Assert.StartsWith("+44", masked);
        Assert.EndsWith("58", masked);
        Assert.Contains("*", masked);
    }

    [Fact]
    public void Phone_International_PreservesCountryCode()
    {
        var phone = PhoneNumber.Parse("+44 20 7946 0958");
        var masked = phone.ToMaskedString();
        Assert.StartsWith("+44", masked);
    }

    [Fact]
    public void Phone_Swedish_PreservesFormattingSeparators()
    {
        var phone = PhoneNumber.Parse("0701740633");
        var masked = phone.ToMaskedString();
        Assert.Contains("-", masked);
        Assert.Contains(" ", masked);
    }
}

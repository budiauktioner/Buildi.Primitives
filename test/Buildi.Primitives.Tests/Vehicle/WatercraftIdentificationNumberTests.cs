using Buildi.Primitives.TextScanning;
using Buildi.Primitives.Validation;
using Buildi.Primitives.Vehicle;

namespace Buildi.Primitives.Tests.Vehicle;

public class WatercraftIdentificationNumberTests
{
    [Theory]
    [InlineData("NL-HXAB7A33G506")]
    [InlineData("NLHXAB7A33G506")]
    [InlineData("nl-hxab7a33g506")]
    [InlineData("  NL-HXAB7A33G506  ")]
    [InlineData("NL - HXA B7A33 G 5 06")]
    [InlineData("NL\tHXAB7A33G506")]
    [InlineData("SE-KIX4567AI925")]
    [InlineData("SE-A2B12345C023")]
    [InlineData("US-ZZZ00001A020")]
    [InlineData("ZZZ00001A020")]
    [InlineData("zzz 00001 a020")]
    [InlineData("A0112345L999")]
    public void IsValid_ReturnsTrue_ForValidInputs(string input)
    {
        Assert.True(WatercraftIdentificationNumber.IsValid(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("NL-HXAB7A33G50")]
    [InlineData("NL-HXAB7A33G5066")]
    [InlineData("ZZZ00001A02")]
    [InlineData("ZZZ00001A0200")]
    [InlineData("XX-HXAB7A33G506")]
    [InlineData("UK-HXAB7A33G506")]
    [InlineData("N1-HXAB7A33G506")]
    [InlineData("NL-H0AB7A33G506")]
    [InlineData("NL-1XAB7A33G506")]
    [InlineData("NL-HXAI7A33G506")]
    [InlineData("NL-HXAB7O33G506")]
    [InlineData("NL-HXAB7A3QG506")]
    [InlineData("ZZZI0001A020")]
    [InlineData("NL-HXAB7A33M506")]
    [InlineData("ZZZ00001M020")]
    [InlineData("NL-HXAB7A33GA06")]
    [InlineData("NL-HXAB7A33G5A6")]
    [InlineData("ZZZ00001M83J")]
    [InlineData("NL-HXA-B7A33G506")]
    [InlineData("NLH-XAB7A33G506")]
    [InlineData("-NLHXAB7A33G506")]
    [InlineData("NLHXAB7A33G506-")]
    [InlineData("ZZZ-00001A020")]
    [InlineData("ZZ-Z00001A020")]
    [InlineData("NL/HXAB7A33G506")]
    [InlineData("NL.HXAB7A33G506")]
    [InlineData("SE-ÅZZ00001A020")]
    [InlineData("ABCDEFGH123456789")]
    public void IsValid_ReturnsFalse_ForInvalidInputs(string? input)
    {
        Assert.False(WatercraftIdentificationNumber.IsValid(input));
    }

    // Whitespace that .NET and JavaScript classify differently must be handled the same in both ports.
    [Theory]
    [InlineData("NL-HXA\u0085B7A33G506")] // next line
    [InlineData("NL-HXA\uFEFFB7A33G506")] // zero-width no-break space
    [InlineData("NL-HXA\u00A0B7A33G506")] // no-break space
    [InlineData("NL-HXA\u3000B7A33G506")] // ideographic space
    public void TryParse_IgnoresWhitespaceThatTheEnginesClassifyDifferently(string input)
    {
        Assert.True(WatercraftIdentificationNumber.TryParse(input, out var win));
        Assert.Equal("NLHXAB7A33G506", win!.Value);
    }

    // char.IsWhiteSpace counts U+0085 and not U+FEFF; the empty check and the fallback text follow it.
    [Theory]
    [InlineData("\u0085", ValidationErrorReason.InputIsEmpty, null)]
    [InlineData(" \u0085 ", ValidationErrorReason.InputIsEmpty, null)]
    [InlineData("\uFEFF", ValidationErrorReason.InvalidCharacters, "\uFEFF")]
    [InlineData(" \u0085X\uFEFF ", ValidationErrorReason.InvalidLength, "X\uFEFF")]
    public void ValidateFormatAndNormalize_TreatWhitespaceAsCharIsWhiteSpaceDoes(
        string input, ValidationErrorReason expectedReason, string? expectedFallback)
    {
        Assert.Equal(expectedReason, Assert.Single(WatercraftIdentificationNumber.Validate(input).Issues).Reason);
        Assert.Equal(expectedFallback, WatercraftIdentificationNumber.Format(input, fallbackToTrimmedInputWhenInvalid: true));
        Assert.Equal(expectedFallback, WatercraftIdentificationNumber.Normalize(input, fallbackToTrimmedInputWhenInvalid: true));
        Assert.Null(WatercraftIdentificationNumber.Format(input));
        Assert.Null(WatercraftIdentificationNumber.Normalize(input));
    }

    // Letters whose culture-aware upper case is, or looks like, an ASCII letter must not pass as A–Z.
    [Theory]
    [InlineData("SE-K\u0131X4567AI925")] // dotless ı
    [InlineData("U\u017F-ZZZ00001A020")] // long ſ
    [InlineData("NL-HXAB7A33G50\u00DF")] // ß
    [InlineData("NL-HXA\u212A7A33G506")] // Kelvin sign
    [InlineData("SE-\u0130XA12345C323")] // dotted İ
    [InlineData("\uFF3AZZ00001A020")] // fullwidth Ｚ
    public void Validate_RejectsNonAsciiLookAlikeLetters_AsInvalidCharacters(string input)
    {
        var result = WatercraftIdentificationNumber.Validate(input);

        Assert.False(result.IsValid);
        Assert.Equal(ValidationErrorReason.InvalidCharacters, Assert.Single(result.Issues).Reason);
    }

    [Fact]
    public void TryParse_UpperCasesEveryAsciiLetter()
    {
        for (var letter = 'a'; letter <= 'z'; letter++)
        {
            var input = $"{letter}{letter}{letter}00001a020";

            Assert.True(WatercraftIdentificationNumber.TryParse(input, out var win), input);
            Assert.Equal(input.ToUpperInvariant(), win!.Value);
        }
    }

    [Fact]
    public void TryParse_ReturnsExpectedProperties_ForIsoNumber()
    {
        var ok = WatercraftIdentificationNumber.TryParse("NL-HXAB7A33G506", out var win);

        Assert.True(ok);
        Assert.NotNull(win);
        Assert.Equal(WatercraftIdentificationNumberScheme.Iso10087, win!.Scheme);
        Assert.Equal("NLHXAB7A33G506", win.Value);
        Assert.Equal("NL-HXAB7A33G506", win.Formatted);
        Assert.Equal("NL", win.CountryCode);
        Assert.Equal("NL", win.Country!.Alpha2Code);
        Assert.Equal("HXA", win.ManufacturerCode);
        Assert.Equal("B7A33", win.SerialNumber);
        Assert.Equal('G', win.ProductionMonthCode);
        Assert.Equal(7, win.ProductionMonth);
        Assert.Equal(5, win.ProductionYearDigit);
        Assert.Equal("06", win.ModelYearDigits);
    }

    [Fact]
    public void TryParse_ReturnsExpectedProperties_ForUnitedStatesNumber()
    {
        var ok = WatercraftIdentificationNumber.TryParse("zzz00001a020", out var win);

        Assert.True(ok);
        Assert.Equal(WatercraftIdentificationNumberScheme.UnitedStatesHullIdentificationNumber, win!.Scheme);
        Assert.Equal("ZZZ00001A020", win.Value);
        Assert.Equal("ZZZ00001A020", win.Formatted);
        Assert.Null(win.CountryCode);
        Assert.Null(win.Country);
        Assert.Equal("ZZZ", win.ManufacturerCode);
        Assert.Equal("00001", win.SerialNumber);
        Assert.Equal('A', win.ProductionMonthCode);
        Assert.Equal(1, win.ProductionMonth);
        Assert.Equal(0, win.ProductionYearDigit);
        Assert.Equal("20", win.ModelYearDigits);
    }

    [Fact]
    public void TryParse_AcceptsTheLetterI_InManufacturerCodeAndAsSeptember()
    {
        Assert.True(WatercraftIdentificationNumber.TryParse("SE-KIX4567AI925", out var win));
        Assert.Equal("KIX", win!.ManufacturerCode);
        Assert.Equal('I', win.ProductionMonthCode);
        Assert.Equal(9, win.ProductionMonth);
    }

    [Theory]
    [InlineData('A', 1)]
    [InlineData('F', 6)]
    [InlineData('I', 9)]
    [InlineData('L', 12)]
    public void TryParse_DecodesProductionMonth(char code, int expectedMonth)
    {
        Assert.True(WatercraftIdentificationNumber.TryParse($"NL-HXAB7A33{code}506", out var win));
        Assert.Equal(expectedMonth, win!.ProductionMonth);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NL-HXAB7A33M506")]
    public void TryParse_ReturnsNull_ForInvalidInput(string? input)
    {
        var ok = WatercraftIdentificationNumber.TryParse(input, out var win);

        Assert.False(ok);
        Assert.Null(win);
    }

    [Fact]
    public void Parse_Throws_ForInvalidInput()
    {
        Assert.Throws<ArgumentException>(() => WatercraftIdentificationNumber.Parse("INVALID"));
    }

    [Theory]
    [InlineData("nlhxab7a33g506", "NL-HXAB7A33G506")]
    [InlineData("NL - HXA B7A33 G506", "NL-HXAB7A33G506")]
    [InlineData("zzz00001a020", "ZZZ00001A020")]
    [InlineData("invalid", null)]
    [InlineData(null, null)]
    public void Format_ReturnsExpected(string? input, string? expected)
    {
        Assert.Equal(expected, WatercraftIdentificationNumber.Format(input));
    }

    [Fact]
    public void Format_ReturnsTrimmedInput_WhenFallbackRequested()
    {
        Assert.Equal("SE 1234", WatercraftIdentificationNumber.Format("  SE 1234 ", fallbackToTrimmedInputWhenInvalid: true));
    }

    [Theory]
    [InlineData("NL-HXAB7A33G506", "NLHXAB7A33G506")]
    [InlineData("nl hxab7a33g506", "NLHXAB7A33G506")]
    [InlineData("zzz00001a020", "ZZZ00001A020")]
    [InlineData("invalid", null)]
    [InlineData("", null)]
    public void Normalize_ReturnsExpected(string? input, string? expected)
    {
        Assert.Equal(expected, WatercraftIdentificationNumber.Normalize(input));
    }

    [Theory]
    [InlineData("NLHXAB7A33G506", true)]
    [InlineData("ZZZ00001A020", true)]
    [InlineData("NL-HXAB7A33G506", false)]
    [InlineData("zzz00001a020", false)]
    [InlineData(null, false)]
    public void IsNormalized_ReturnsExpected(string? input, bool expected)
    {
        Assert.Equal(expected, WatercraftIdentificationNumber.IsNormalized(input));
    }

    [Theory]
    [InlineData("NLHXAB7A33G506", "NL-HXAB7A33G506")]
    [InlineData("ZZZ00001A020", "ZZZ00001A020")]
    public void ToString_ReturnsFormattedValue(string input, string expected)
    {
        Assert.Equal(expected, WatercraftIdentificationNumber.Parse(input).ToString());
    }

    [Theory]
    [InlineData(null, false, ValidationErrorReason.InputIsEmpty)]
    [InlineData("", false, ValidationErrorReason.InputIsEmpty)]
    [InlineData("NL/HXAB7A33G506", false, ValidationErrorReason.InvalidCharacters)]
    [InlineData("NL-HXA-B7A33G506", false, ValidationErrorReason.InvalidFormat)]
    [InlineData("ZZZ-00001A020", false, ValidationErrorReason.InvalidFormat)]
    [InlineData("NL-HXAB7A33G50", false, ValidationErrorReason.InvalidLength)]
    [InlineData("XX-HXAB7A33G506", false, ValidationErrorReason.UnknownCountryCode)]
    [InlineData("NL-H0AB7A33G506", false, ValidationErrorReason.InvalidManufacturerCode)]
    [InlineData("NL-HXAB7A3QG506", false, ValidationErrorReason.InvalidSerialNumber)]
    [InlineData("NL-HXAB7A33M506", false, ValidationErrorReason.InvalidMonthCode)]
    [InlineData("NL-HXAB7A33G5A6", false, ValidationErrorReason.InvalidYearDigits)]
    [InlineData("NL-HXAB7A33G506", true, null)]
    [InlineData("ZZZ00001A020", true, null)]
    public void Validate_ReturnsExpectedResult(string? input, bool expectedIsValid, ValidationErrorReason? expectedReason)
    {
        var result = WatercraftIdentificationNumber.Validate(input);

        Assert.Equal(expectedIsValid, result.IsValid);
        if (expectedReason is null)
            Assert.Empty(result.Issues);
        else
            Assert.Equal(expectedReason, Assert.Single(result.Issues).Reason);
    }

    [Fact]
    public void Validate_ReportsEverySegmentThatFails()
    {
        var result = WatercraftIdentificationNumber.Validate("N1-H0AI7A33M5A6");

        Assert.False(result.IsValid);
        Assert.Equal(
            [
                ValidationErrorReason.UnknownCountryCode,
                ValidationErrorReason.InvalidManufacturerCode,
                ValidationErrorReason.InvalidSerialNumber,
                ValidationErrorReason.InvalidMonthCode,
                ValidationErrorReason.InvalidYearDigits,
            ],
            result.Issues.Select(i => i.Reason));
    }

    [Fact]
    public void Validate_Issues_ContainBothLanguageDescriptions()
    {
        var result = WatercraftIdentificationNumber.Validate("NL-HXAB7A33M506");

        var issue = Assert.Single(result.Issues);
        Assert.False(string.IsNullOrWhiteSpace(issue.EnglishDescription));
        Assert.False(string.IsNullOrWhiteSpace(issue.LocalizedDescription));
        Assert.NotEqual(issue.EnglishDescription, issue.LocalizedDescription);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("NL-HXAB7A33G506")]
    [InlineData("NLHXAB7A33G506")]
    [InlineData("ZZZ00001A020")]
    [InlineData("NL-HXAB7A33M506")]
    [InlineData("ZZZ-00001A020")]
    [InlineData("UK-HXAB7A33G506")]
    public void Validate_IsValid_MatchesIsValid(string? input)
    {
        Assert.Equal(WatercraftIdentificationNumber.IsValid(input), WatercraftIdentificationNumber.Validate(input).IsValid);
    }

    [Theory]
    [InlineData("NL-HXAB7A33G506", "NL-HXA*********")]
    [InlineData("ZZZ00001A020", "ZZZ*********")]
    public void ToMaskedString_ShowsOnlyCountryAndManufacturer(string input, string expected)
    {
        var masked = WatercraftIdentificationNumber.Parse(input).ToMaskedString();

        Assert.Equal(expected, masked);
        Assert.Equal(WatercraftIdentificationNumber.Format(input)!.Length, masked.Length);
    }

    [Fact]
    public void FindCandidatesInText_FindsHyphenatedIsoNumberWithMediumConfidence()
    {
        var results = WatercraftIdentificationNumber.FindCandidatesInText("Skrovnummer: NL-HXAB7A33G506 enligt skylten.");

        var candidate = Assert.Single(results);
        Assert.Equal("NL-HXAB7A33G506", candidate.OriginalText);
        Assert.Equal("NLHXAB7A33G506", candidate.NormalizedForm);
        Assert.Equal("NL-HXAB7A33G506", candidate.FormattedForm);
        Assert.Equal(TextMatchConfidence.Medium, candidate.Confidence);
    }

    [Theory]
    [InlineData("CIN NLHXAB7A33G506 on the transom")]
    [InlineData("HIN zzz00001a020 on the transom")]
    public void FindCandidatesInText_FindsUnseparatedNumbersWithLowConfidence(string text)
    {
        var candidate = Assert.Single(WatercraftIdentificationNumber.FindCandidatesInText(text));

        Assert.Equal(TextMatchConfidence.Low, candidate.Confidence);
    }

    [Theory]
    [InlineData("VIN ABCDEFGH123456789")]
    [InlineData("Part NL-HXAB7A33G506-2")]
    [InlineData("Order ZZZ00001M020")]
    public void FindCandidatesInText_IgnoresLongerRunsAndInvalidNumbers(string text)
    {
        Assert.Empty(WatercraftIdentificationNumber.FindCandidatesInText(text));
    }

    // Non-ASCII letters are not A–Z, so next to a number they neither block nor join the match.
    [Theory]
    [InlineData("\u0131NL-HXAB7A33G506", 1)]
    [InlineData("NL-HXAB7A33G506\u0131", 0)]
    [InlineData("\u017FNL-HXAB7A33G506", 1)]
    [InlineData("NL-HXAB7A33G506\u017F", 0)]
    [InlineData("\u00DFNL-HXAB7A33G506", 1)]
    [InlineData("NL-HXAB7A33G506\u212A", 0)]
    [InlineData("\u0130NL-HXAB7A33G506", 1)]
    [InlineData("\uFF21NL-HXAB7A33G506", 1)]
    public void FindCandidatesInText_TreatsNonAsciiLettersAsBoundaries(string text, int expectedStart)
    {
        var candidate = Assert.Single(WatercraftIdentificationNumber.FindCandidatesInText(text));

        Assert.Equal(expectedStart, candidate.StartIndex);
        Assert.Equal("NL-HXAB7A33G506", candidate.OriginalText);
    }

    [Fact]
    public void FindCandidatesInText_ReturnsEmpty_ForNullOrEmpty()
    {
        Assert.Empty(WatercraftIdentificationNumber.FindCandidatesInText(null!));
        Assert.Empty(WatercraftIdentificationNumber.FindCandidatesInText(""));
    }

    [Fact]
    public void DefaultTextScanner_ReportsWatercraftIdentificationNumbers()
    {
        var result = new TextScanner().Scan("Båten har skrovnummer NL-HXAB7A33G506.");

        var candidate = Assert.Single(result.WatercraftIdentificationNumbers);
        Assert.Equal("NLHXAB7A33G506", candidate.NormalizedForm);
    }

    [Fact]
    public void Equality_ComparesNormalizedValue()
    {
        var a = WatercraftIdentificationNumber.Parse("NL-HXAB7A33G506");
        var b = WatercraftIdentificationNumber.Parse("nlhxab7a33g506");
        var c = WatercraftIdentificationNumber.Parse("ZZZ00001A020");

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.True(a != c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.True(a < c);
        Assert.Equal(0, a.CompareTo(b));
        Assert.Equal(1, a.CompareTo(null));
    }
}

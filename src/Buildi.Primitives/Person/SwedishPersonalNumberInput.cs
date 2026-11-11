namespace Buildi.Primitives.Person;

internal static class SwedishPersonalNumberInput
{
    private const int TwelveDigitWithSeparatorLength = 13;
    private const int SeparatorIndex = 8;

    // ActiveLogin's TenOrTwelveDigits mode rejects YYYYMMDD-NNNC, and its Off mode also accepts
    // spaces, en dashes and '+'. Only the hyphen is removed: with a four-digit year, '+' would
    // contradict the century the input already states.
    internal static string RemoveTwelveDigitHyphen(string input)
    {
        if (input.Length != TwelveDigitWithSeparatorLength || input[SeparatorIndex] != '-') return input;
        for (var i = 0; i < input.Length; i++)
        {
            if (i != SeparatorIndex && !char.IsAsciiDigit(input[i])) return input;
        }
        return string.Concat(input.AsSpan(0, SeparatorIndex), input.AsSpan(SeparatorIndex + 1));
    }
}

using System.Text.RegularExpressions;
using Buildi.Primitives;
using Buildi.Primitives.Geography;
using Buildi.Primitives.TextScanning;
using Buildi.Primitives.Validation;

namespace Buildi.Primitives.Vehicle;

/// <summary>
/// The scheme a <see cref="WatercraftIdentificationNumber"/> was written in.
/// </summary>
public enum WatercraftIdentificationNumberScheme
{
    Unknown = 0,

    /// <summary>
    /// ISO 10087 craft or watercraft identification number (CIN/WIN): a two-letter country code,
    /// a hyphen and twelve characters, e.g. <c>NL-HXAB7A33G506</c>.
    /// </summary>
    Iso10087 = 1,

    /// <summary>
    /// United States hull identification number (HIN) in the twelve-character format required
    /// since 1 August 1984 by 33 CFR 181.25, e.g. <c>ZZZ00001A020</c>.
    /// </summary>
    UnitedStatesHullIdentificationNumber = 2,
}

/// <summary>
/// A watercraft identification number (<c>skrovnummer</c>), the hull marking of a small craft:
/// either the ISO 10087 craft identification number (CIN, now called WIN by Transportstyrelsen) of
/// fourteen characters plus a hyphen, or the twelve-character US hull identification number (HIN).
/// </summary>
/// <remarks>
/// <para>The number is assigned by the manufacturer and carries no check digit, so a parsed value
/// is structurally valid only — it does not prove that the manufacturer code is registered or that
/// the number belongs to a particular craft. The production year and model year are encoded as one
/// and two digits respectively, so the century cannot be derived from the number alone; they are
/// exposed as digits rather than full years.</para>
/// <para>Older markings that predate these formats (for example US HINs from before August 1984)
/// are not accepted.</para>
/// <para>Sources:</para>
/// <list type="bullet">
/// <item><description><see href="https://www.iso.org/standard/82383.html">ISO 10087:2022</see> — Small craft — Craft identification — Coding system</description></item>
/// <item><description><see href="https://www.ecfr.gov/current/title-33/chapter-I/subchapter-S/part-181/subpart-C/section-181.25">33 CFR 181.25</see> — US hull identification number format</description></item>
/// <item><description><see href="https://www.transportstyrelsen.se/sv/sjofart/fritidsbatar/lagar-och-regler-for-fritidsbatar/cemarkning/bygga-om-tillverka-distribuera-och-importera/tillverka/instruktionsbok-tillverkarskylt-och-win/">Transportstyrelsen — Instruktionsbok, tillverkarskylt och WIN</see></description></item>
/// </list>
/// </remarks>
public sealed class WatercraftIdentificationNumber : IEquatable<WatercraftIdentificationNumber>, IComparable<WatercraftIdentificationNumber>
{
    public static PrimitiveTypeInfo TypeInfo { get; } = new(
        "Watercraft Identification Number",
        "Skrovnummer",
        "🚤",
        [
            "https://www.iso.org/standard/82383.html",
            "https://www.ecfr.gov/current/title-33/chapter-I/subchapter-S/part-181/subpart-C/section-181.25",
            "https://www.transportstyrelsen.se/sv/sjofart/fritidsbatar/lagar-och-regler-for-fritidsbatar/cemarkning/bygga-om-tillverka-distribuera-och-importera/tillverka/instruktionsbok-tillverkarskylt-och-win/",
        ]);

    private const int MaxInputLength = 30;
    private const int IsoLength = 14;
    private const int UnitedStatesLength = 12;

    private static readonly Regex AllowedCharactersPattern = new(@"^[A-Z0-9-]+$", RegexOptions.Compiled);
    // SanitizeInput has already turned every whitespace character into an ASCII space and dropped
    // U+0085, U+FEFF, \v and \f, so a space is all that is left to strip. \s would differ from the
    // TypeScript port, where it also matches U+FEFF but not U+0085.
    private static readonly Regex WhitespacePattern = new(" +", RegexOptions.Compiled);
    private static readonly Regex CountryCodePattern = new(@"^[A-Z]{2}$", RegexOptions.Compiled);

    // ISO 10087 §4.3: letters and numerals, except the numerals 0 and 1.
    private static readonly Regex IsoManufacturerCodePattern = new(@"^[A-Z2-9]{3}$", RegexOptions.Compiled);

    // 33 CFR 181.25(a) leaves the characters of a Coast Guard assigned code unrestricted.
    private static readonly Regex UnitedStatesManufacturerCodePattern = new(@"^[A-Z0-9]{3}$", RegexOptions.Compiled);

    // ISO 10087 §4.4 and 33 CFR 181.25(b): letters and numerals, except the letters I, O and Q.
    private static readonly Regex SerialNumberPattern = new(@"^[A-HJ-NPR-Z0-9]{5}$", RegexOptions.Compiled);

    private static readonly Regex YearDigitsPattern = new(@"^[0-9]{3}$", RegexOptions.Compiled);

    /// <summary>
    /// The number in uppercase without separators: fourteen characters for ISO 10087, e.g.
    /// <c>NLHXAB7A33G506</c>, or twelve for a US HIN, e.g. <c>ZZZ00001A020</c>.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// The number as it is marked on the hull: ISO 10087 with the hyphen after the country code,
    /// e.g. <c>NL-HXAB7A33G506</c>, and a US HIN unchanged, e.g. <c>ZZZ00001A020</c>.
    /// </summary>
    public string Formatted { get; }

    /// <summary>Which of the two supported formats the number follows.</summary>
    public WatercraftIdentificationNumberScheme Scheme { get; }

    /// <summary>
    /// The ISO 3166-1 alpha-2 code of the country where the manufacturer is established, e.g.
    /// <c>NL</c>, or <see langword="null"/> for a US HIN, which carries no country.
    /// </summary>
    public string? CountryCode { get; }

    /// <summary>The country of <see cref="CountryCode"/>, or <see langword="null"/> for a US HIN.</summary>
    public Country? Country { get; }

    /// <summary>The three-character manufacturer identification code (MIC), e.g. <c>HXA</c>.</summary>
    public string ManufacturerCode { get; }

    /// <summary>The five-character serial number assigned by the manufacturer, e.g. <c>B7A33</c>.</summary>
    public string SerialNumber { get; }

    /// <summary>
    /// The production month letter, <c>A</c> for January through <c>L</c> for December. For a US HIN
    /// this is the month of certification when one is required, otherwise the month of manufacture.
    /// </summary>
    public char ProductionMonthCode { get; }

    /// <summary>The production month decoded from <see cref="ProductionMonthCode"/>, 1–12.</summary>
    public int ProductionMonth { get; }

    /// <summary>
    /// The last digit of the production year, 0–9. The decade is not encoded.
    /// </summary>
    public int ProductionYearDigit { get; }

    /// <summary>
    /// The last two digits of the model year as written, e.g. <c>06</c>. The century is not encoded.
    /// </summary>
    public string ModelYearDigits { get; }

    private WatercraftIdentificationNumber(string value, WatercraftIdentificationNumberScheme scheme, Country? country)
    {
        Value = value;
        Scheme = scheme;
        Country = country;

        var body = scheme == WatercraftIdentificationNumberScheme.Iso10087 ? value[2..] : value;
        CountryCode = scheme == WatercraftIdentificationNumberScheme.Iso10087 ? value[..2] : null;
        Formatted = CountryCode is null ? value : $"{CountryCode}-{body}";
        ManufacturerCode = body[..3];
        SerialNumber = body[3..8];
        ProductionMonthCode = body[8];
        ProductionMonth = body[8] - 'A' + 1;
        ProductionYearDigit = body[9] - '0';
        ModelYearDigits = body[10..12];
    }

    public static bool TryParse(string? input, out WatercraftIdentificationNumber? result) =>
        TryParseCore(input, issues: null, out result);

    public static WatercraftIdentificationNumber Parse(string input)
    {
        if (!TryParse(input, out var result))
            throw new ArgumentException("Invalid watercraft identification number.", nameof(input));
        return result!;
    }

    public static bool IsValid(string? input) => TryParse(input, out _);

    /// <summary>
    /// Returns the number as it is marked on the hull, e.g. <c>NL-HXAB7A33G506</c> or <c>ZZZ00001A020</c>.
    /// Returns <see langword="null"/> when the input is invalid or empty.
    /// When <paramref name="fallbackToTrimmedInputWhenInvalid"/> is <see langword="true"/>, returns the trimmed original input instead of <see langword="null"/> for non-empty invalid input.
    /// </summary>
    public static string? Format(string? input, bool fallbackToTrimmedInputWhenInvalid = false) =>
        TryParse(input, out var r) ? r!.Formatted : fallbackToTrimmedInputWhenInvalid && !string.IsNullOrWhiteSpace(input) ? input!.Trim() : null;

    /// <summary>
    /// Returns the number in uppercase without separators, e.g. <c>NLHXAB7A33G506</c> or <c>ZZZ00001A020</c>.
    /// Returns <see langword="null"/> when the input is invalid.
    /// </summary>
    public static string? Normalize(string? input, bool fallbackToTrimmedInputWhenInvalid = false)
    {
        if (TryParse(input, out var r)) return r!.Value;
        if (!fallbackToTrimmedInputWhenInvalid || string.IsNullOrWhiteSpace(input)) return null;
        var trimmed = input!.Trim();
        return trimmed.Length > 0 ? trimmed : null;
    }

    /// <summary>
    /// Validates the input and returns a <see cref="ValidationResult"/> with one issue per invalid
    /// segment, or a valid result when the input is a well-formed watercraft identification number.
    /// </summary>
    public static ValidationResult Validate(string? input)
    {
        var issues = new List<ValidationIssue>();
        return TryParseCore(input, issues, out _) ? ValidationResult.Valid(input) : ValidationResult.Invalid(input, issues);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the input is valid and already in its normalized form.
    /// </summary>
    public static bool IsNormalized(string? input) => input is not null && Normalize(input) == input;

    /// <summary>Returns the number without separators, e.g. <c>NLHXAB7A33G506</c>.</summary>
    public string ToNormalizedString() => Value;

    /// <summary>Returns the number as it is marked on the hull, e.g. <c>NL-HXAB7A33G506</c>.</summary>
    public override string ToString() => Formatted;

    private static bool TryParseCore(string? input, List<ValidationIssue>? issues, out WatercraftIdentificationNumber? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(input))
            return Fail(issues, ValidationErrorReason.InputIsEmpty,
                "Input is empty or whitespace.", "Värdet är tomt.");

        var cleaned = ToAsciiUpper(WhitespacePattern.Replace(InputSanitization.SanitizeInput(input!), ""));

        if (cleaned.Length > MaxInputLength)
            return Fail(issues, ValidationErrorReason.InputTooLong,
                "Input contains too many characters.", "Värdet innehåller för många tecken.");

        if (!AllowedCharactersPattern.IsMatch(cleaned))
            return Fail(issues, ValidationErrorReason.InvalidCharacters,
                "Only letters A–Z, digits and the hyphen after the country code are allowed.",
                "Endast bokstäverna A–Z, siffror och bindestrecket efter landskoden är tillåtna.");

        var hyphenIndex = cleaned.IndexOf('-');
        var hasCountrySeparator = hyphenIndex >= 0;
        if (hasCountrySeparator && (hyphenIndex != 2 || cleaned.IndexOf('-', hyphenIndex + 1) >= 0))
            return Fail(issues, ValidationErrorReason.InvalidFormat,
                "A hyphen is only allowed directly after the two-letter country code.",
                "Bindestreck får bara stå direkt efter den tvåbokstaviga landskoden.");

        var compact = hasCountrySeparator ? cleaned.Remove(hyphenIndex, 1) : cleaned;
        var scheme = compact.Length switch
        {
            IsoLength => WatercraftIdentificationNumberScheme.Iso10087,
            UnitedStatesLength when !hasCountrySeparator => WatercraftIdentificationNumberScheme.UnitedStatesHullIdentificationNumber,
            _ => WatercraftIdentificationNumberScheme.Unknown,
        };

        if (scheme == WatercraftIdentificationNumberScheme.Unknown)
            return Fail(issues, ValidationErrorReason.InvalidLength,
                "Must be 14 characters after an ISO 10087 country code and hyphen, or 12 characters for a US HIN.",
                "Måste vara 14 tecken enligt ISO 10087 (landskod, bindestreck och 12 tecken) eller 12 tecken för ett amerikanskt HIN.");

        var isIso = scheme == WatercraftIdentificationNumberScheme.Iso10087;
        var body = isIso ? compact[2..] : compact;
        var isValid = true;
        Country? country = null;

        if (isIso && !TryResolveCountry(compact[..2], out country))
            isValid = Fail(issues, ValidationErrorReason.UnknownCountryCode,
                "The first two characters must be an ISO 3166-1 alpha-2 country code.",
                "De två första tecknen måste vara en landskod enligt ISO 3166-1 alpha-2.");

        if (isIso && !IsoManufacturerCodePattern.IsMatch(body[..3]))
            isValid = Fail(issues, ValidationErrorReason.InvalidManufacturerCode,
                "The manufacturer code must be three letters or digits, except the digits 0 and 1.",
                "Tillverkarkoden måste vara tre bokstäver eller siffror, men inte siffrorna 0 och 1.");

        if (!isIso && !UnitedStatesManufacturerCodePattern.IsMatch(body[..3]))
            isValid = Fail(issues, ValidationErrorReason.InvalidManufacturerCode,
                "The manufacturer code must be three letters or digits.",
                "Tillverkarkoden måste vara tre bokstäver eller siffror.");

        if (!SerialNumberPattern.IsMatch(body[3..8]))
            isValid = Fail(issues, ValidationErrorReason.InvalidSerialNumber,
                "The serial number must be five letters or digits, except the letters I, O and Q.",
                "Serienumret måste vara fem bokstäver eller siffror, men inte bokstäverna I, O och Q.");

        if (body[8] is < 'A' or > 'L')
            isValid = Fail(issues, ValidationErrorReason.InvalidMonthCode,
                "The production month must be a letter from A (January) to L (December).",
                "Tillverkningsmånaden måste vara en bokstav från A (januari) till L (december).");

        if (!YearDigitsPattern.IsMatch(body[9..12]))
            isValid = Fail(issues, ValidationErrorReason.InvalidYearDigits,
                "The production year and model year must be digits.",
                "Tillverkningsår och årsmodell måste anges med siffror.");

        if (!isValid) return false;

        result = new WatercraftIdentificationNumber(compact, scheme, country);
        return true;
    }

    private static bool TryResolveCountry(string code, out Country? country)
    {
        country = null;
        if (!CountryCodePattern.IsMatch(code)) return false;
        if (!Country.TryParse(code, out var parsed) || parsed!.Alpha2Code != code) return false;
        country = parsed;
        return true;
    }

    // Only a–z are upper-cased. Culture-aware casing maps some non-ASCII letters onto ASCII (ſ → S,
    // and ı → I in JavaScript) and differs between ICU and invariant globalization, which would let a
    // look-alike letter pass as A–Z here and differ from the TypeScript port.
    private static string ToAsciiUpper(string value)
    {
        return string.Create(value.Length, value, static (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                span[i] = c is >= 'a' and <= 'z' ? (char)(c - 32) : c;
            }
        });
    }

    private static bool Fail(List<ValidationIssue>? issues, ValidationErrorReason reason, string english, string swedish)
    {
        issues?.Add(new ValidationIssue(reason, english, swedish));
        return false;
    }

    private static readonly Regex ScanPattern = new(
        @"(?<![A-Z0-9-])(?:[A-Z]{2}-?[A-Z0-9]{12}|[A-Z0-9]{12})(?![A-Z0-9-])",
        RegexOptions.Compiled);

    /// <summary>
    /// Scans unstructured text for potential watercraft identification numbers. An ISO 10087 number
    /// written with its hyphen is reported with medium confidence; one without the hyphen, and a US
    /// HIN, are bare runs of letters and digits and reported with low confidence.
    /// Results are heuristic-based candidates and may include false positives.
    /// No guarantee is made that a candidate represents a real hull marking in its original context.
    /// </summary>
    public static IReadOnlyList<TextCandidate<WatercraftIdentificationNumber>> FindCandidatesInText(string text)
    {
        if (string.IsNullOrEmpty(text)) return [];

        var upper = ToAsciiUpper(text);
        var results = new List<TextCandidate<WatercraftIdentificationNumber>>();
        foreach (Match match in ScanPattern.Matches(upper))
        {
            if (!TryParse(match.Value, out var win)) continue;
            var confidence = match.Value.Contains('-') ? TextMatchConfidence.Medium : TextMatchConfidence.Low;
            results.Add(new TextCandidate<WatercraftIdentificationNumber>(
                match.Index,
                match.Length,
                text.Substring(match.Index, match.Length),
                nameof(WatercraftIdentificationNumber),
                TextCandidateCategory.Vehicle,
                win!.ToNormalizedString(),
                win.ToString(),
                win.ToMaskedString(),
                confidence,
                win));
        }
        return results;
    }

    public bool Equals(WatercraftIdentificationNumber? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is WatercraftIdentificationNumber other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    public static bool operator ==(WatercraftIdentificationNumber? a, WatercraftIdentificationNumber? b) => a is null ? b is null : a.Equals(b);
    public static bool operator !=(WatercraftIdentificationNumber? a, WatercraftIdentificationNumber? b) => !(a == b);
    public int CompareTo(WatercraftIdentificationNumber? other) => other is null ? 1 : string.Compare(Value, other.Value, StringComparison.Ordinal);
    public static bool operator <(WatercraftIdentificationNumber left, WatercraftIdentificationNumber right) => left.CompareTo(right) < 0;
    public static bool operator >(WatercraftIdentificationNumber left, WatercraftIdentificationNumber right) => left.CompareTo(right) > 0;
    public static bool operator <=(WatercraftIdentificationNumber left, WatercraftIdentificationNumber right) => left.CompareTo(right) <= 0;
    public static bool operator >=(WatercraftIdentificationNumber left, WatercraftIdentificationNumber right) => left.CompareTo(right) >= 0;
}

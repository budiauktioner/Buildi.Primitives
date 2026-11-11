using System.Text.Json;
using Buildi.Primitives.Organization;
using Buildi.Primitives.Person;

namespace Buildi.Primitives.Tests.Person;

public class PersonalNumberParityCasesTests
{
    public static TheoryData<string, string, string?> Cases { get; } = LoadCases();

    private static TheoryData<string, string, string?> LoadCases()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Person", "personal-number-parity-cases.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var data = new TheoryData<string, string, string?>();
        foreach (var c in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            data.Add(c.GetProperty("type").GetString()!, c.GetProperty("input").GetString()!, c.GetProperty("normalized").GetString());
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryEntryPoint_AgreesWithSharedCase(string type, string input, string? expected)
    {
        switch (type)
        {
            case nameof(SwedishPersonalIdentityNumber):
                AssertAgreement(expected,
                    SwedishPersonalIdentityNumber.Normalize(input),
                    SwedishPersonalIdentityNumber.IsValid(input),
                    SwedishPersonalIdentityNumber.Validate(input).IsValid,
                    SwedishPersonalIdentityNumber.TryParse(input, out var pin) ? pin!.ToNormalizedString() : null,
                    () => SwedishPersonalIdentityNumber.Parse(input).ToNormalizedString());
                break;
            case nameof(SwedishCoordinationNumber):
                AssertAgreement(expected,
                    SwedishCoordinationNumber.Normalize(input),
                    SwedishCoordinationNumber.IsValid(input),
                    SwedishCoordinationNumber.Validate(input).IsValid,
                    SwedishCoordinationNumber.TryParse(input, out var cn) ? cn!.ToNormalizedString() : null,
                    () => SwedishCoordinationNumber.Parse(input).ToNormalizedString());
                break;
            case nameof(SwedishOrganizationNumber):
                AssertAgreement(expected,
                    SwedishOrganizationNumber.Normalize(input),
                    SwedishOrganizationNumber.IsValid(input),
                    SwedishOrganizationNumber.Validate(input).IsValid,
                    SwedishOrganizationNumber.TryParse(input, out var org) ? org!.ToNormalizedString() : null,
                    () => SwedishOrganizationNumber.Parse(input).ToNormalizedString());
                break;
            default:
                Assert.Fail($"Unknown type '{type}' in personal-number-parity-cases.json.");
                break;
        }
    }

    private static void AssertAgreement(string? expected, string? normalized, bool isValid, bool validateIsValid, string? parsed, Func<string> parse)
    {
        Assert.Equal(expected, normalized);
        Assert.Equal(expected is not null, isValid);
        Assert.Equal(expected is not null, validateIsValid);
        Assert.Equal(expected, parsed);
        if (expected is null)
            Assert.Throws<ArgumentException>(() => parse());
        else
            Assert.Equal(expected, parse());
    }
}

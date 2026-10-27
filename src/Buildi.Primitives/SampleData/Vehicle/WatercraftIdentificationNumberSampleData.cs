namespace Buildi.Primitives.SampleData.Vehicle;

/// <summary>
/// Valid <see cref="Primitives.Vehicle.WatercraftIdentificationNumber"/> values, one per supported format.
/// </summary>
public static class WatercraftIdentificationNumberSampleData
{
    /// <summary>The worked example in ISO 10087:2022 §4.6.</summary>
    public static Primitives.Vehicle.WatercraftIdentificationNumber Iso10087Example { get; } = Primitives.Vehicle.WatercraftIdentificationNumber.Parse("NL-HXAB7A33G506");

    /// <summary>
    /// A US hull identification number constructed for this library: an all-Z manufacturer code and
    /// serial number <c>00001</c>. It has not been checked against the Coast Guard's register of
    /// manufacturer codes.
    /// </summary>
    public static Primitives.Vehicle.WatercraftIdentificationNumber UnitedStatesExample { get; } = Primitives.Vehicle.WatercraftIdentificationNumber.Parse("ZZZ00001A020");

    public static IReadOnlyList<Primitives.Vehicle.WatercraftIdentificationNumber> All { get; } =
        [Iso10087Example, UnitedStatesExample];
}

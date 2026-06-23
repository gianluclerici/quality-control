namespace Ficep.QualityControl.Core.Model;

/// <summary>
/// Full nominal description of a manufactured piece: its beam geometry plus the ordered
/// list of machining macros applied to it. Serialized alongside the generated point cloud
/// (as <c>*.macros.json</c>) so the downstream quality-control step receives exactly the
/// nominal Brep + macro list + parameters it must verify.
/// </summary>
public sealed record PieceSpec
{
    /// <summary>The nominal beam (profile + length).</summary>
    public required BeamSpec Beam { get; init; }

    /// <summary>Machining macros applied, in application order. Empty for a raw blank ("grezzo").</summary>
    public IReadOnlyList<MacroSpec> Macros { get; init; } = Array.Empty<MacroSpec>();
}

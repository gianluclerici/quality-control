namespace Ficep.QualityControl.Core.Features;

/// <summary>
/// Identity of one machined feature: a stable id plus its provenance back to the macro that produced
/// it. One macro can yield several features (e.g. multiple holes), so each cutter gets its own
/// descriptor. The special <see cref="Base"/> descriptor (<see cref="Id"/> 0) stands for the
/// unmachined beam body — the bucket every scan point that touches no feature falls into.
/// </summary>
/// <param name="Id">Stable feature id (1-based for features; 0 for <see cref="Base"/>).</param>
/// <param name="MacroIndex">Index of the originating macro in the piece's macro list (-1 for base).</param>
/// <param name="MacroClassName">Macro class name that produced the feature (e.g. "INTC01"); empty for base.</param>
/// <param name="Kind">Broad feature class (<see cref="FeatureKind"/>).</param>
/// <param name="Label">Human-readable label, e.g. "INTC01 #2".</param>
public readonly record struct FeatureDescriptor(
    int Id,
    int MacroIndex,
    string MacroClassName,
    FeatureKind Kind,
    string Label)
{
    /// <summary>The descriptor of the unmachined beam body.</summary>
    public static FeatureDescriptor Base { get; } =
        new(0, -1, string.Empty, FeatureKind.Base, "Base");
}

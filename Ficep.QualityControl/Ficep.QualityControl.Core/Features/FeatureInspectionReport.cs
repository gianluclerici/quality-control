using System.Linq;

namespace Ficep.QualityControl.Core.Features;

/// <summary>
/// Dimensional verdict for a single feature: its identity, the list of measured parameters (e.g. a
/// hole's diameter) each with its own tolerance verdict, and how many scan points the measurement used.
/// </summary>
public sealed record FeatureInspectionReport
{
    /// <summary>Identity/provenance of the inspected feature.</summary>
    public required FeatureDescriptor Feature { get; init; }

    /// <summary>The measured dimensional parameters, each judged against its nominal.</summary>
    public required IReadOnlyList<FeatureParameter> Parameters { get; init; }

    /// <summary>Number of scan points the measurement was fitted on.</summary>
    public int PointCount { get; init; }

    /// <summary>True when every measured parameter is within tolerance.</summary>
    public bool InTolerance => Parameters.All(p => p.InTolerance);
}

using Ficep.QualityControl.Core.Measurement;

namespace Ficep.QualityControl.Core.Features;

/// <summary>
/// The deviation measurement split by feature: the overall report (every scan point), the report for
/// the unmachined body, and one report per machined feature. The buckets partition the cloud — every
/// scan point lands in exactly one of <see cref="Base"/> or one <see cref="Features"/> entry — so the
/// per-feature counts plus the base count sum to the overall count.
/// </summary>
public sealed class SegmentedDeviationReport
{
    internal SegmentedDeviationReport(DeviationReport overall, DeviationReport @base, IReadOnlyList<FeatureDeviation> features)
    {
        Overall = overall;
        Base = @base;
        Features = features;
    }

    /// <summary>Deviation report over the whole cloud (all points, unsegmented).</summary>
    public DeviationReport Overall { get; }

    /// <summary>Deviation report over the scan points on the unmachined beam body.</summary>
    public DeviationReport Base { get; }

    /// <summary>One deviation report per machined feature, in segmentation order.</summary>
    public IReadOnlyList<FeatureDeviation> Features { get; }
}

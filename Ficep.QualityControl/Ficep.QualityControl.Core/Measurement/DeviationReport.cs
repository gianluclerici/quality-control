namespace Ficep.QualityControl.Core.Measurement;

/// <summary>
/// The result of comparing a scanned cloud against the nominal surface: the per-point signed deviations,
/// their summary <see cref="DeviationStatistics"/>, and — when a <see cref="ToleranceBand"/> was supplied —
/// the conformity verdict (how many points fall inside the band and whether the part is accepted).
/// </summary>
public sealed class DeviationReport
{
    internal DeviationReport(
        IReadOnlyList<PointDeviation> deviations,
        DeviationStatistics statistics,
        ToleranceBand? tolerance,
        int inToleranceCount)
    {
        Deviations = deviations;
        Statistics = statistics;
        Tolerance = tolerance;
        InToleranceCount = inToleranceCount;
    }

    /// <summary>Per-point signed deviations, in scan order (positions are in the aligned frame).</summary>
    public IReadOnlyList<PointDeviation> Deviations { get; }

    /// <summary>Aggregate statistics over <see cref="Deviations"/>.</summary>
    public DeviationStatistics Statistics { get; }

    /// <summary>The tolerance band the verdict was computed against, or null if none was supplied.</summary>
    public ToleranceBand? Tolerance { get; }

    /// <summary>Points whose signed deviation falls inside <see cref="Tolerance"/> (equals total when no band).</summary>
    public int InToleranceCount { get; }

    /// <summary>Points outside the band (0 when no band was supplied).</summary>
    public int OutOfToleranceCount => Deviations.Count - InToleranceCount;

    /// <summary>Fraction of points within tolerance, in [0,1] (1 when no band or no points).</summary>
    public double ConformanceRatio =>
        Deviations.Count == 0 ? 1.0 : (double)InToleranceCount / Deviations.Count;

    /// <summary>True if a band was supplied and every point falls within it.</summary>
    public bool IsConform => Tolerance.HasValue && OutOfToleranceCount == 0;
}

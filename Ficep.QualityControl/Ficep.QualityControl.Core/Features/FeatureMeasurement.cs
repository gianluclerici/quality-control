using Ficep.QualityControl.Core.Measurement;
using Ficep.QualityControl.Core.Registration;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Features;

/// <summary>
/// Step 5 (segmentation): measures a scan cloud against the nominal and splits the per-point signed
/// deviations by feature. It reuses the exact same projection and signed-distance convention as
/// <see cref="DeviationMeasurement"/> (via its shared core), then routes every point into the bucket of
/// the feature it belongs to (from <see cref="FeatureSegmentation"/>) — or the base body — and builds
/// one <see cref="DeviationReport"/> per bucket. This is the foundation for per-feature tolerances.
/// </summary>
public sealed class FeatureMeasurement
{
    /// <summary>
    /// Measures <paramref name="scan"/> against <paramref name="nominal"/>, optionally applying the rigid
    /// <paramref name="alignment"/> first, and segments the deviations with <paramref name="segmentation"/>.
    /// </summary>
    /// <param name="scan">The scanned cloud (only the positions are used).</param>
    /// <param name="nominal">The queryable nominal surface.</param>
    /// <param name="segmentation">Feature segmentation built from the nominal's feature cutters.</param>
    /// <param name="alignment">Rigid transform mapping the scan onto the nominal (identity if null).</param>
    /// <param name="tolerance">Acceptance band applied to every bucket; statistics only when null.</param>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    public SegmentedDeviationReport Measure(
        IReadOnlyList<SurfaceSample> scan,
        NominalSurface nominal,
        FeatureSegmentation segmentation,
        RigidTransform? alignment = null,
        ToleranceBand? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(scan);
        ArgumentNullException.ThrowIfNull(nominal);
        ArgumentNullException.ThrowIfNull(segmentation);

        RigidTransform transform = alignment ?? RigidTransform.Identity;
        (PointDeviation[] deviations, double[] signed) = DeviationMeasurement.ProjectAll(scan, nominal, transform);

        int featureCount = segmentation.Features.Count;
        // Bucket 0 = base (unmachined body); buckets 1..featureCount = feature index (fi) + 1.
        var devBuckets = new List<PointDeviation>[featureCount + 1];
        var signedBuckets = new List<double>[featureCount + 1];
        for (int k = 0; k <= featureCount; k++)
        {
            devBuckets[k] = new List<PointDeviation>();
            signedBuckets[k] = new List<double>();
        }

        for (int i = 0; i < deviations.Length; i++)
        {
            int fi = segmentation.Classify(deviations[i].Point);
            int bucket = fi < 0 ? 0 : fi + 1;
            devBuckets[bucket].Add(deviations[i]);
            signedBuckets[bucket].Add(signed[i]);
        }

        DeviationReport overall = BuildReport(deviations, signed, tolerance);
        DeviationReport baseReport = BuildReport(devBuckets[0], signedBuckets[0], tolerance);

        var featureReports = new FeatureDeviation[featureCount];
        for (int fi = 0; fi < featureCount; fi++)
        {
            DeviationReport r = BuildReport(devBuckets[fi + 1], signedBuckets[fi + 1], tolerance);
            featureReports[fi] = new FeatureDeviation(segmentation.Features[fi], r);
        }

        return new SegmentedDeviationReport(overall, baseReport, featureReports);
    }

    private static DeviationReport BuildReport(
        IReadOnlyList<PointDeviation> deviations, IReadOnlyList<double> signed, ToleranceBand? tolerance)
    {
        int inTolerance = signed.Count;
        if (tolerance is { } band)
        {
            inTolerance = 0;
            for (int i = 0; i < signed.Count; i++)
                if (band.Contains(signed[i]))
                    inTolerance++;
        }

        DeviationStatistics stats = DeviationStatistics.Compute(signed);
        return new DeviationReport(deviations, stats, tolerance, inTolerance);
    }
}

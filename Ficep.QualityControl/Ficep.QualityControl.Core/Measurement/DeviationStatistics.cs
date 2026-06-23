namespace Ficep.QualityControl.Core.Measurement;

/// <summary>
/// Summary statistics over a set of signed point-to-surface deviations (all in mm). Signed figures
/// (<see cref="MinMm"/>, <see cref="MaxMm"/>, <see cref="MeanMm"/>) describe bias and the worst pits/peaks;
/// magnitude figures (<see cref="RmsMm"/>, <see cref="MeanAbsMm"/>, <see cref="MaxAbsMm"/>,
/// <see cref="P95AbsMm"/>) describe the overall error level regardless of side.
/// </summary>
/// <param name="Count">Number of deviations.</param>
/// <param name="MinMm">Most negative signed deviation (deepest pit into the part).</param>
/// <param name="MaxMm">Most positive signed deviation (highest peak out of the part).</param>
/// <param name="MeanMm">Mean signed deviation (systematic bias).</param>
/// <param name="StdDevMm">Standard deviation of the signed deviations (spread about the mean).</param>
/// <param name="RmsMm">Root-mean-square deviation (overall magnitude, the usual fit-quality figure).</param>
/// <param name="MeanAbsMm">Mean of the absolute deviations.</param>
/// <param name="MaxAbsMm">Largest absolute deviation (worst point either side).</param>
/// <param name="P95AbsMm">95th percentile of the absolute deviations (robust worst-case, ignores few outliers).</param>
public readonly record struct DeviationStatistics(
    int Count,
    double MinMm,
    double MaxMm,
    double MeanMm,
    double StdDevMm,
    double RmsMm,
    double MeanAbsMm,
    double MaxAbsMm,
    double P95AbsMm)
{
    /// <summary>The empty statistics (no points).</summary>
    public static DeviationStatistics Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0);

    /// <summary>
    /// Computes the statistics over <paramref name="signedDeviations"/>. The 95th percentile is taken on
    /// a sorted copy of the absolute values (linear interpolation between ranks), so it is a stable,
    /// outlier-tolerant worst-case figure.
    /// </summary>
    public static DeviationStatistics Compute(IReadOnlyList<double> signedDeviations)
    {
        ArgumentNullException.ThrowIfNull(signedDeviations);
        int n = signedDeviations.Count;
        if (n == 0)
            return Empty;

        double min = double.PositiveInfinity, max = double.NegativeInfinity;
        double sum = 0, sumSq = 0, sumAbs = 0, maxAbs = 0;
        var abs = new double[n];
        for (int i = 0; i < n; i++)
        {
            double d = signedDeviations[i];
            if (d < min) min = d;
            if (d > max) max = d;
            sum += d;
            sumSq += d * d;
            double a = Math.Abs(d);
            abs[i] = a;
            sumAbs += a;
            if (a > maxAbs) maxAbs = a;
        }

        double mean = sum / n;
        double rms = Math.Sqrt(sumSq / n);
        double variance = Math.Max(0.0, sumSq / n - mean * mean); // guard tiny negative from round-off
        double stdDev = Math.Sqrt(variance);

        Array.Sort(abs);
        double p95 = Percentile(abs, 0.95);

        return new DeviationStatistics(n, min, max, mean, stdDev, rms, sumAbs / n, maxAbs, p95);
    }

    /// <summary>Linear-interpolated percentile of an already-sorted ascending array (<paramref name="fraction"/> in [0,1]).</summary>
    private static double Percentile(double[] sortedAscending, double fraction)
    {
        int n = sortedAscending.Length;
        if (n == 1)
            return sortedAscending[0];

        double rank = fraction * (n - 1);
        int lo = (int)Math.Floor(rank);
        int hi = (int)Math.Ceiling(rank);
        if (lo == hi)
            return sortedAscending[lo];
        double frac = rank - lo;
        return sortedAscending[lo] + (sortedAscending[hi] - sortedAscending[lo]) * frac;
    }
}

using Ficep.QualityControl.Core.Registration;

namespace Ficep.QualityControl.Core.Features;

/// <summary>
/// Robust <b>offset-only</b> plane fit: the plane normal is already known exactly (it comes from the
/// re-derived cutter face), so a wall plane has a single unknown — its signed offset along that normal.
/// Each point reduces to a scalar projection <c>t = n̂·p</c>, and the offset is the robust 1D location
/// (median) of those projections. This rejects the scan's tail outliers with no iteration, no
/// eigen-solver and no RANSAC — see <c>docs/research/notch-parameter-extraction.md</c> (technique A3).
/// </summary>
internal static class PlaneFit
{
    /// <summary>
    /// The robust signed offset (median of <c>n̂·p</c>) of <paramref name="projections"/> = the
    /// per-point dot products against the known unit normal.
    /// </summary>
    public static double Median(IReadOnlyList<double> projections)
    {
        ArgumentNullException.ThrowIfNull(projections);
        int n = projections.Count;
        if (n == 0)
            throw new ArgumentException("Cannot take the median of an empty set.", nameof(projections));

        double[] sorted = projections.ToArray();
        Array.Sort(sorted);
        int mid = n / 2;
        return (n & 1) == 1 ? sorted[mid] : 0.5 * (sorted[mid - 1] + sorted[mid]);
    }

    /// <summary>Signed offset of a unit <paramref name="normal"/> plane fitted (median) to <paramref name="points"/>.</summary>
    public static double RobustOffset(Vec3 normal, IEnumerable<Vec3> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        var t = new List<double>();
        foreach (Vec3 p in points)
            t.Add(Vec3.Dot(p, normal));
        return Median(t);
    }
}

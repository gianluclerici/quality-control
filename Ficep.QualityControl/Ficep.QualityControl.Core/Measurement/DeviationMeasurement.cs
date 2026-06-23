using devDept.Geometry;
using Ficep.QualityControl.Core.Registration;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Measurement;

/// <summary>
/// Step 4 of the pipeline: measures how far a scanned point cloud deviates from the nominal surface.
/// For every scan point it finds the closest point and outward normal on the nominal (via the shared
/// <see cref="NominalSurface"/>), takes the <b>signed</b> point-to-surface distance, then aggregates the
/// per-point deviations into statistics and a conformity verdict.
/// <para>
/// The deviation engine is our own <see cref="NominalSurface"/> rather than Eyeshot's
/// <c>ComputeDistances</c>: it yields a <i>signed</i> distance (which side of the surface — excess vs
/// missing material — is what tolerancing needs), it does not depend on the Ultimate licence, and it is
/// already validated against an exhaustive brute-force reference. See ARCHITECTURE §5.10.
/// </para>
/// </summary>
public sealed class DeviationMeasurement
{
    /// <summary>
    /// Compares <paramref name="scan"/> against <paramref name="nominal"/>, optionally applying the rigid
    /// <paramref name="alignment"/> from ICP first and judging each point against <paramref name="tolerance"/>.
    /// </summary>
    /// <param name="scan">The scanned cloud (only the positions are used).</param>
    /// <param name="nominal">The queryable nominal surface.</param>
    /// <param name="alignment">Rigid transform mapping the scan onto the nominal (e.g.
    /// <see cref="RegistrationResult.Transform"/>); identity if null.</param>
    /// <param name="tolerance">Acceptance band; when null the report carries statistics only (no verdict).</param>
    /// <exception cref="ArgumentNullException"><paramref name="scan"/> or <paramref name="nominal"/> is null.</exception>
    public DeviationReport Measure(
        IReadOnlyList<SurfaceSample> scan,
        NominalSurface nominal,
        RigidTransform? alignment = null,
        ToleranceBand? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(scan);
        ArgumentNullException.ThrowIfNull(nominal);

        RigidTransform transform = alignment ?? RigidTransform.Identity;
        int n = scan.Count;

        var deviations = new PointDeviation[n];
        var signed = new double[n];
        int inTolerance = 0;

        for (int i = 0; i < n; i++)
        {
            Point3D p = transform.Apply(scan[i].Position);
            SurfaceProjection proj = nominal.ClosestPoint(p);

            // Sign by the side of the nominal surface: project (p - closest) onto the outward normal.
            // >= 0 → outside the nominal (excess material), < 0 → inside it (missing material).
            double side =
                (p.X - proj.Point.X) * proj.Normal.X +
                (p.Y - proj.Point.Y) * proj.Normal.Y +
                (p.Z - proj.Point.Z) * proj.Normal.Z;
            double d = side >= 0 ? proj.Distance : -proj.Distance;

            signed[i] = d;
            deviations[i] = new PointDeviation(p, d);

            if (tolerance is { } band && band.Contains(d))
                inTolerance++;
        }

        if (!tolerance.HasValue)
            inTolerance = n; // no band → everything counts as "in tolerance" (ratio 1)

        DeviationStatistics stats = DeviationStatistics.Compute(signed);
        return new DeviationReport(deviations, stats, tolerance, inTolerance);
    }
}

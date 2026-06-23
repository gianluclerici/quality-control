using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Registration;

/// <summary>Tuning for <see cref="IcpRegistration"/>.</summary>
public sealed record IcpOptions
{
    /// <summary>Maximum ICP iterations before giving up convergence.</summary>
    public int MaxIterations { get; init; } = 60;

    /// <summary>Stop once an iteration's translation increment falls below this (mm).</summary>
    public double ConvergenceTranslationMm { get; init; } = 1e-5;

    /// <summary>Stop once an iteration's rotation increment falls below this (radians).</summary>
    public double ConvergenceRotationRad { get; init; } = 1e-6;

    /// <summary>
    /// Cap on how many scan points drive the fit. ICP needs only a representative subset; a few
    /// thousand points give a stable transform far faster than a million-point cloud. Set ≤ 0 to use
    /// every point.
    /// </summary>
    public int MaxSourcePoints { get; init; } = 20000;

    /// <summary>Seed for the deterministic subsampling of the source cloud.</summary>
    public int Seed { get; init; } = 12345;

    /// <summary>
    /// Reject correspondences farther than this (mm) from the surface, so gross outliers / unscanned
    /// regions don't drag the fit. Infinite by default (no rejection).
    /// </summary>
    public double MaxPairDistanceMm { get; init; } = double.PositiveInfinity;
}

/// <summary>Outcome of an ICP run.</summary>
/// <param name="Transform">Rigid transform mapping the source (scan) cloud onto the nominal surface.</param>
/// <param name="Iterations">Iterations actually performed.</param>
/// <param name="RmsErrorMm">Root-mean-square point-to-surface distance after alignment (mm).</param>
/// <param name="Converged">True if the increment fell below the convergence thresholds.</param>
public readonly record struct RegistrationResult(RigidTransform Transform, int Iterations, double RmsErrorMm, bool Converged);

/// <summary>
/// Point-to-plane Iterative Closest Point: aligns a scanned point cloud to the nominal surface. Each
/// iteration projects the (currently transformed) scan points to their closest points and surface
/// normals on the nominal, then solves the linearised least-squares system that minimises the
/// point-to-tangent-plane distances, and accumulates the increment. Point-to-plane is the standard
/// choice for fitting a cloud to a CAD surface: it slides freely along the surface and converges in
/// far fewer iterations than point-to-point.
/// <para>
/// Assumes a reasonable initial pose (the scan roughly overlaps the nominal); it refines alignment,
/// it does not perform global/coarse registration from an arbitrary orientation.
/// </para>
/// </summary>
public sealed class IcpRegistration
{
    /// <summary>Aligns <paramref name="source"/> (scan) to <paramref name="target"/> (nominal).</summary>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public RegistrationResult Register(IReadOnlyList<SurfaceSample> source, NominalSurface target, IcpOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        options ??= new IcpOptions();

        Vec3[] points = Subsample(source, options.MaxSourcePoints, options.Seed);
        if (points.Length < 6)
            return new RegistrationResult(RigidTransform.Identity, 0, double.NaN, false);

        RigidTransform cumulative = RigidTransform.Identity;
        var ata = new double[6, 6];
        var atb = new double[6];
        var solution = new double[6];
        var row = new double[6];

        // Per-iteration correspondence cache (filled in pass 1, consumed in pass 2).
        int m = points.Length;
        var ptCache = new Vec3[m];
        var nCache = new Vec3[m];
        var rCache = new double[m];
        var valid = new bool[m];

        bool converged = false;
        int iterationsDone = 0;
        double maxPairSq = double.IsPositiveInfinity(options.MaxPairDistanceMm)
            ? double.PositiveInfinity
            : options.MaxPairDistanceMm * options.MaxPairDistanceMm;

        for (int iter = 0; iter < options.MaxIterations; iter++)
        {
            Array.Clear(ata);
            Array.Clear(atb);

            // Pass 1: project every point, cache the correspondence, and accumulate the centroid of the
            // matched points. Forming the rotation about this centroid (rather than the world origin)
            // keeps the lever arms small, so the Gauss-Newton step stays stable for parts far from the
            // origin instead of overshooting.
            int used = 0;
            double cx = 0, cy = 0, cz = 0;
            for (int k = 0; k < m; k++)
            {
                Vec3 pt = cumulative.Apply(points[k]);
                SurfaceProjection proj = target.ClosestPoint(pt);
                bool ok = proj.Distance * proj.Distance <= maxPairSq;
                valid[k] = ok;
                if (!ok)
                    continue;

                var n = new Vec3(proj.Normal.X, proj.Normal.Y, proj.Normal.Z);
                var q = new Vec3(proj.Point.X, proj.Point.Y, proj.Point.Z);
                ptCache[k] = pt;
                nCache[k] = n;
                rCache[k] = Vec3.Dot(n, q - pt); // signed point-to-plane residual
                cx += pt.X; cy += pt.Y; cz += pt.Z;
                used++;
            }

            if (used < 6)
                break;

            var centroid = new Vec3(cx / used, cy / used, cz / used);

            // Pass 2: build the point-to-plane normal equations with centroid-relative lever arms.
            // Jacobian row a = [ (p - centroid) × n , n ], target = signed residual.
            for (int k = 0; k < m; k++)
            {
                if (!valid[k])
                    continue;

                Vec3 n = nCache[k];
                Vec3 lever = Vec3.Cross(ptCache[k] - centroid, n);
                row[0] = lever.X; row[1] = lever.Y; row[2] = lever.Z;
                row[3] = n.X; row[4] = n.Y; row[5] = n.Z;

                double r = rCache[k];
                for (int i = 0; i < 6; i++)
                {
                    double ai = row[i];
                    atb[i] += ai * r;
                    for (int j = i; j < 6; j++)
                        ata[i, j] += ai * row[j];
                }
            }

            // Mirror the upper triangle to make the matrix full-symmetric for the solver.
            for (int i = 0; i < 6; i++)
                for (int j = 0; j < i; j++)
                    ata[i, j] = ata[j, i];

            if (!Cholesky.Solve(ata, atb, solution, 6))
                break;

            // The solve gives a rotation about the centroid plus a translation. Re-express it as a
            // world-frame rigid transform: rotation R, translation = centroid - R·centroid + tSolved.
            RigidTransform rotationOnly = RigidTransform.FromRotationVector(
                solution[0], solution[1], solution[2], 0, 0, 0);
            Vec3 rotatedCentroid = rotationOnly.Apply(centroid);
            RigidTransform increment = RigidTransform.FromRotationVector(
                solution[0], solution[1], solution[2],
                centroid.X - rotatedCentroid.X + solution[3],
                centroid.Y - rotatedCentroid.Y + solution[4],
                centroid.Z - rotatedCentroid.Z + solution[5]);
            cumulative = increment.Compose(cumulative);
            iterationsDone = iter + 1;

            double rotMag = Math.Sqrt(solution[0] * solution[0] + solution[1] * solution[1] + solution[2] * solution[2]);
            double transMag = Math.Sqrt(solution[3] * solution[3] + solution[4] * solution[4] + solution[5] * solution[5]);
            if (rotMag < options.ConvergenceRotationRad && transMag < options.ConvergenceTranslationMm)
            {
                converged = true;
                break;
            }
        }

        double rms = EvaluateRms(points, target, cumulative);
        return new RegistrationResult(cumulative, iterationsDone, rms, converged);
    }

    private static double EvaluateRms(Vec3[] points, NominalSurface target, RigidTransform transform)
    {
        double sumSq = 0;
        foreach (Vec3 p0 in points)
        {
            SurfaceProjection proj = target.ClosestPoint(transform.Apply(p0));
            sumSq += proj.Distance * proj.Distance;
        }
        return Math.Sqrt(sumSq / points.Length);
    }

    private static Vec3[] Subsample(IReadOnlyList<SurfaceSample> source, int maxPoints, int seed)
    {
        int n = source.Count;
        if (maxPoints <= 0 || n <= maxPoints)
        {
            var all = new Vec3[n];
            for (int i = 0; i < n; i++)
                all[i] = new Vec3(source[i].Position.X, source[i].Position.Y, source[i].Position.Z);
            return all;
        }

        // Partial Fisher-Yates over an index array: an unbiased, deterministic subset without replacement.
        var indices = new int[n];
        for (int i = 0; i < n; i++)
            indices[i] = i;

        var rng = new Random(seed);
        var picked = new Vec3[maxPoints];
        for (int i = 0; i < maxPoints; i++)
        {
            int j = i + rng.Next(n - i);
            (indices[i], indices[j]) = (indices[j], indices[i]);
            var pos = source[indices[i]].Position;
            picked[i] = new Vec3(pos.X, pos.Y, pos.Z);
        }
        return picked;
    }
}

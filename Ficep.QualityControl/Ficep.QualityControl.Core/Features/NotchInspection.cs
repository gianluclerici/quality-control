using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.QualityControl.Core.Model;
using Ficep.QualityControl.Core.Registration;

namespace Ficep.QualityControl.Core.Features;

/// <summary>Nominal headline dimensions of a notch / cope (mm).</summary>
/// <param name="LengthMm">Cope length along the beam (macro <c>A</c>).</param>
/// <param name="DepthMm">Cope depth into the web (macro <c>B</c>).</param>
/// <param name="RadiusMm">Corner fillet radius (macro <c>R</c>).</param>
public readonly record struct NotchNominals(double LengthMm, double DepthMm, double RadiusMm);

/// <summary>Symmetric tolerance half-widths (mm) for the three notch parameters.</summary>
public readonly record struct NotchTolerance(double LengthMm, double DepthMm, double RadiusMm)
{
    /// <summary>A uniform ±0.5 mm band on all three parameters.</summary>
    public static NotchTolerance Default => new(0.5, 0.5, 0.5);
}

/// <summary>
/// Step 5.3: the dimensional verdict for a <see cref="FeatureKind.Notch"/> (cope, macro SCAI*). The cope
/// is a 2D contour extruded through the web, so its surface is a set of planar <i>walls</i> plus one
/// cylindrical <i>fillet</i> — hence we measure with plane fits + an arc fit
/// (see <c>docs/research/notch-parameter-extraction.md</c>).
/// <para>
/// Every primitive's orientation is known exactly from the re-derived cutter (Step 5.1): the extrusion
/// axis is the cap-face normal and each wall normal is a cutter face normal. So each fit collapses to its
/// minimal unknown — a robust 1D offset for a wall (<see cref="PlaneFit"/>, normal known) and a linear 2D
/// circle for the fillet (<see cref="CircleFit"/>, axis known, reused from the hole in Step 5.2). The
/// nominal values come from the macro parameters (A/B/R), exactly as the hole diameter did in 5.2.
/// </para>
/// <para>
/// Length and depth are measured as wall positions in the ICP-aligned frame, so they inherit the
/// registration accuracy; the radius is intrinsic. This matches the convention established in Step 5.2;
/// the alignment-invariant (feature-relative) upgrade is recorded in the research note.
/// </para>
/// </summary>
public sealed class NotchInspection
{
    /// <summary>Default half-thickness (mm) of the band used to collect a wall's scan points.</summary>
    public const double DefaultWallBandMm = 1.0;

    /// <summary>
    /// Measures the cope length, depth and fillet radius of a notch from <paramref name="scanPoints"/>
    /// (the union of the aligned points the segmentation routed to this notch's cutters) and judges each
    /// against <paramref name="nominal"/> within <paramref name="tolerance"/>.
    /// </summary>
    /// <param name="notchCutters">The notch's cutters (Step 5.1); the contour-extrusion cutter that
    /// carries the fillet provides the wall normals and the extrusion axis.</param>
    /// <param name="scanPoints">Aligned scan points belonging to the notch.</param>
    /// <param name="nominal">Nominal length/depth/radius; see <see cref="NominalsFromMacro"/>.</param>
    /// <param name="tolerance">Per-parameter symmetric tolerance; defaults to ±0.5 mm.</param>
    /// <param name="wallBandMm">Half-thickness of the band that collects a wall's points.</param>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="notchCutters"/> is empty / has no fillet cutter.</exception>
    /// <exception cref="InvalidOperationException">The cope geometry could not be derived from the inputs.</exception>
    public FeatureInspectionReport Inspect(
        IReadOnlyList<FeatureCutter> notchCutters,
        IReadOnlyList<Point3D> scanPoints,
        NotchNominals nominal,
        NotchTolerance? tolerance = null,
        double wallBandMm = DefaultWallBandMm)
    {
        ArgumentNullException.ThrowIfNull(notchCutters);
        ArgumentNullException.ThrowIfNull(scanPoints);
        if (notchCutters.Count == 0)
            throw new ArgumentException("At least one notch cutter is required.", nameof(notchCutters));
        if (scanPoints.Count < 3)
            throw new ArgumentException("A notch measurement needs at least 3 scan points.", nameof(scanPoints));

        NotchTolerance tol = tolerance ?? NotchTolerance.Default;
        FeatureCutter profile = SelectProfileCutter(notchCutters);
        ExtrudedProfile geom = ExtrudedProfile.FromCutter(profile.Cutter, nominal);

        // Keep each point in world coords (walls are measured there, so the offset equals the nominal)
        // and in the cope's profile plane (a, b) about a point on the axis (used for the fillet).
        int n = scanPoints.Count;
        var world = new Vec3[n];
        var a = new double[n];
        var b = new double[n];
        for (int i = 0; i < n; i++)
        {
            world[i] = new Vec3(scanPoints[i].X, scanPoints[i].Y, scanPoints[i].Z);
            Vec3 rel = world[i] - geom.Origin;
            a[i] = Vec3.Dot(rel, geom.U);
            b[i] = Vec3.Dot(rel, geom.V);
        }

        FeatureParameter length = MeasureWall(
            "Length", geom.BackWall, world, wallBandMm, nominal.LengthMm, tol.LengthMm);
        FeatureParameter depth = MeasureWall(
            "Depth", geom.DepthWall, world, wallBandMm, nominal.DepthMm, tol.DepthMm);
        FeatureParameter radius = MeasureFillet(geom, a, b, wallBandMm, nominal.RadiusMm, tol.RadiusMm);

        return new FeatureInspectionReport
        {
            Feature = profile.Descriptor,
            Parameters = new[] { length, depth, radius },
            PointCount = n,
        };
    }

    /// <summary>The nominal length (A), depth (B) and fillet radius (R) of a SCAI (notch) macro.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="macro"/> is null.</exception>
    /// <exception cref="ArgumentException">The macro is not a notch macro.</exception>
    public static NotchNominals NominalsFromMacro(MacroSpec macro)
    {
        ArgumentNullException.ThrowIfNull(macro);
        if (FeatureKinds.FromMacroClassName(macro.MacroClassName) != FeatureKind.Notch)
            throw new ArgumentException($"Macro '{macro.MacroClassName}' is not a notch macro.", nameof(macro));

        double a = macro.Parameters.GetValueOrDefault("A");
        double b = macro.Parameters.GetValueOrDefault("B");
        double r = macro.Parameters.GetValueOrDefault("R");
        return new NotchNominals(a, b, r);
    }

    /// <summary>The first notch cutter carrying a non-planar (fillet) lateral face — the contour extrusion.</summary>
    private static FeatureCutter SelectProfileCutter(IReadOnlyList<FeatureCutter> notchCutters)
    {
        foreach (FeatureCutter fc in notchCutters)
        {
            if (ExtrudedProfile.HasFillet(fc.Cutter))
                return fc;
        }
        throw new ArgumentException(
            "No contour-extrusion cutter with a fillet was found among the notch cutters.", nameof(notchCutters));
    }

    /// <summary>Measures a wall's offset (median of its band points, in world coords) and judges it against the nominal.</summary>
    private static FeatureParameter MeasureWall(
        string name, WallLine wall, Vec3[] world, double band, double nominal, double tol)
    {
        var proj = new List<double>();
        foreach (Vec3 p in world)
        {
            double t = Vec3.Dot(p, wall.Normal);
            if (Math.Abs(t - wall.Offset) <= band)
                proj.Add(t);
        }
        if (proj.Count < 3)
            throw new InvalidOperationException($"Too few scan points on the {name} wall ({proj.Count}).");

        // Express the measured position with the same sign convention as the (positive) nominal.
        double measured = Math.Sign(wall.Offset) * PlaneFit.Median(proj);
        return FeatureParameter.Judge(name, nominal, measured, tol);
    }

    /// <summary>
    /// Isolates the fillet points (near the corner, off both walls) and measures the radius with a
    /// <b>tangent-constrained</b> fit: the fillet rounds the right-angle corner between the two measured
    /// walls, so its centre is pinned to <c>corner + R·(û_back + û_depth)</c> (û = inward unit normals) and
    /// the only unknown is R. A free 2-DOF circle fit is ill-conditioned on the cope's short quarter-arc;
    /// tying the centre to the well-measured corner makes the 1-parameter fit stable. R is found by golden
    /// section on Σ(‖p − centre(R)‖ − R)².
    /// </summary>
    private static FeatureParameter MeasureFillet(
        ExtrudedProfile geom, double[] a, double[] b, double band, double nominal, double tol)
    {
        double reach = nominal + 2.0;     // points within R(+margin) of the corner
        var fu = new List<double>();
        var fv = new List<double>();
        double sumA = 0, sumB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            double da = a[i] - geom.CornerA, db = b[i] - geom.CornerB;
            if (da * da + db * db > reach * reach)
                continue;
            // Off-wall test is in profile coords, so compare the line LHS against D2 (not the world Offset).
            bool onBack = Math.Abs(geom.BackWall.Nu * a[i] + geom.BackWall.Nv * b[i] - geom.BackWall.D2) <= band;
            bool onDepth = Math.Abs(geom.DepthWall.Nu * a[i] + geom.DepthWall.Nv * b[i] - geom.DepthWall.D2) <= band;
            if (onBack || onDepth)
                continue;
            fu.Add(a[i]);
            fv.Add(b[i]);
            sumA += a[i];
            sumB += b[i];
        }
        if (fu.Count < 3)
            throw new InvalidOperationException($"Too few scan points on the fillet ({fu.Count}).");

        // Inward unit normals (sign chosen so the centre sits on the fillet-points side of each wall).
        double cA = sumA / fu.Count, cB = sumB / fu.Count;
        (double ux1, double uy1) = InwardNormal(geom.BackWall, geom.CornerA, geom.CornerB, cA, cB);
        (double ux2, double uy2) = InwardNormal(geom.DepthWall, geom.CornerA, geom.CornerB, cA, cB);
        double sx = ux1 + ux2, sy = uy1 + uy2;

        double measured = FitTangentRadius(fu, fv, geom.CornerA, geom.CornerB, sx, sy, nominal);
        return FeatureParameter.Judge("Radius", nominal, measured, tol);
    }

    /// <summary>Unit normal of <paramref name="wall"/> (profile coords) pointing from the corner toward the fillet centroid.</summary>
    private static (double X, double Y) InwardNormal(WallLine wall, double cornerA, double cornerB, double cA, double cB)
    {
        double g = Math.Sign((cA - cornerA) * wall.Nu + (cB - cornerB) * wall.Nv);
        if (g == 0) g = 1;
        return (g * wall.Nu, g * wall.Nv);
    }

    /// <summary>Radius minimising Σ(‖p − (corner + R·s)‖ − R)² by golden-section search.</summary>
    private static double FitTangentRadius(
        List<double> fu, List<double> fv, double cornerA, double cornerB, double sx, double sy, double nominal)
    {
        double Cost(double r)
        {
            double cx = cornerA + r * sx, cy = cornerB + r * sy, sum = 0;
            for (int i = 0; i < fu.Count; i++)
            {
                double dx = fu[i] - cx, dy = fv[i] - cy;
                double res = Math.Sqrt(dx * dx + dy * dy) - r;
                sum += res * res;
            }
            return sum;
        }

        double lo = Math.Max(0.05, nominal - 5.0), hi = nominal + 5.0;
        const double gr = 0.6180339887498949;
        double c = hi - gr * (hi - lo), d = lo + gr * (hi - lo);
        double fc = Cost(c), fd = Cost(d);
        for (int it = 0; it < 80 && hi - lo > 1e-6; it++)
        {
            if (fc < fd)
            {
                hi = d; d = c; fd = fc;
                c = hi - gr * (hi - lo); fc = Cost(c);
            }
            else
            {
                lo = c; c = d; fc = fd;
                d = lo + gr * (hi - lo); fd = Cost(d);
            }
        }
        return 0.5 * (lo + hi);
    }
}

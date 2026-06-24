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
/// The full nominal contour of a SCAI cope (mm) — all six macro parameters. Expressed in the cope's local
/// profile frame (origin = beam-end × flange-edge corner), per <c>docs/STEP5.5-HANDOFF.md</c> §4:
/// P0=(0,0), P1=(A,0), P2=(A,B) [fillet R], P3=(E, D−(D−B)·E/A), P4=(0, D+C).
/// </summary>
/// <param name="LengthMm">A — cope length along the beam (the back wall's distance from the end cap).</param>
/// <param name="DepthMm">B — height of the fillet corner P2 above the flange edge.</param>
/// <param name="TopRiseMm">C — extra rise of the top edge above the shoulder at the end cap (P4 height = D+C).</param>
/// <param name="ShoulderMm">D — height where the lower slant meets the end cap (its b-intercept at a=0).</param>
/// <param name="SlantMm">E — length of the shoulder break point P3 along the beam.</param>
/// <param name="RadiusMm">R — corner fillet radius (0 ⇒ a sharp corner with no fillet face).</param>
public readonly record struct NotchFullNominals(
    double LengthMm, double DepthMm, double TopRiseMm,
    double ShoulderMm, double SlantMm, double RadiusMm);

/// <summary>Symmetric tolerance half-widths (mm): one band for the five position parameters (A,B,C,D,E), one for the radius (R).</summary>
public readonly record struct NotchFullTolerance(double PositionMm, double RadiusMm)
{
    /// <summary>A uniform ±0.5 mm band on the positions and the radius.</summary>
    public static NotchFullTolerance Default => new(0.5, 0.5);
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
        FeatureCutter profile = SelectProfileCutter(notchCutters, requireFillet: true);
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

    /// <summary>All six nominal parameters (A,B,C,D,E,R) of a SCAI (notch) macro.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="macro"/> is null.</exception>
    /// <exception cref="ArgumentException">The macro is not a notch macro.</exception>
    public static NotchFullNominals FullNominalsFromMacro(MacroSpec macro)
    {
        ArgumentNullException.ThrowIfNull(macro);
        if (FeatureKinds.FromMacroClassName(macro.MacroClassName) != FeatureKind.Notch)
            throw new ArgumentException($"Macro '{macro.MacroClassName}' is not a notch macro.", nameof(macro));

        return new NotchFullNominals(
            macro.Parameters.GetValueOrDefault("A"),
            macro.Parameters.GetValueOrDefault("B"),
            macro.Parameters.GetValueOrDefault("C"),
            macro.Parameters.GetValueOrDefault("D"),
            macro.Parameters.GetValueOrDefault("E"),
            macro.Parameters.GetValueOrDefault("R"));
    }

    /// <summary>
    /// Step 5.5: the <b>complete</b> cope verdict — all six contour parameters A,B,C,D,E,R — measured
    /// <b>feature-relative</b> against the beam datum frame (<paramref name="datums"/>), so the lengths and
    /// heights are invariant to residual registration error (see <see cref="BeamDatumFrame"/>).
    /// <para>
    /// Each cope wall has a known normal from the cutter, so its world line in the length–height plane is a
    /// robust 1-DOF offset fit (<see cref="PlaneFit"/>). Intersecting the fitted walls — and the end-cap
    /// datum — recovers the contour vertices, whose length (X) and height (Y) are then dropped onto the datum
    /// planes: A and E from the end cap (<paramref name="vx"/>), B/C/D from the flange edge
    /// (<paramref name="vy"/>). R is intrinsic (the tangent fit reused from the 3-parameter overload).
    /// </para>
    /// <para>
    /// A nominal of 0 marks an <b>absent</b> element (handoff §5, Phase B): it is decided from the input, not
    /// the geometry — such a parameter is not fitted and is reported as 0. R=0 (a sharp corner) additionally
    /// skips the fillet fit and the contour cutter is chosen by wall count rather than by its fillet face.
    /// </para>
    /// </summary>
    /// <param name="notchCutters">The notch's cutters (Step 5.1).</param>
    /// <param name="scanPoints">Aligned scan points belonging to the notch (≥ 3).</param>
    /// <param name="nominal">Nominal A,B,C,D,E,R; see <see cref="FullNominalsFromMacro"/>.</param>
    /// <param name="datums">The beam datum frame the lengths/heights are measured against (Phase A).</param>
    /// <param name="vx">Length-end the cope anchors to: "I" (start) or "F" (far). Default "I".</param>
    /// <param name="vy">Flange the heights are measured from: "A" (Y-min) or "B" (Y-max). Default "A".</param>
    /// <param name="tolerance">Per-group symmetric tolerance; defaults to ±0.5 mm.</param>
    /// <param name="wallBandMm">Half-thickness of the band that collects a wall's points.</param>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="notchCutters"/> is empty / too few points.</exception>
    /// <exception cref="InvalidOperationException">A required cope wall/vertex could not be derived.</exception>
    public FeatureInspectionReport Inspect(
        IReadOnlyList<FeatureCutter> notchCutters,
        IReadOnlyList<Point3D> scanPoints,
        NotchFullNominals nominal,
        BeamDatumFrame datums,
        string vx = "I",
        string vy = "A",
        NotchFullTolerance? tolerance = null,
        double wallBandMm = DefaultWallBandMm)
    {
        ArgumentNullException.ThrowIfNull(notchCutters);
        ArgumentNullException.ThrowIfNull(scanPoints);
        if (notchCutters.Count == 0)
            throw new ArgumentException("At least one notch cutter is required.", nameof(notchCutters));
        if (scanPoints.Count < 3)
            throw new ArgumentException("A notch measurement needs at least 3 scan points.", nameof(scanPoints));

        NotchFullTolerance tol = tolerance ?? NotchFullTolerance.Default;
        bool hasFillet = nominal.RadiusMm > 0;

        FeatureCutter profile = SelectProfileCutter(notchCutters, requireFillet: hasFillet);
        var coreNominal = new NotchNominals(nominal.LengthMm, nominal.DepthMm, nominal.RadiusMm);
        ExtrudedProfile geom = ExtrudedProfile.FromCutter(profile.Cutter, coreNominal);

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

        double xEnd = string.Equals(vx, "F", StringComparison.OrdinalIgnoreCase) ? datums.XMaxMm : datums.XMinMm;

        // Reference contour vertices (from the cutter wall offsets) give each wall its tangent extent, used to
        // localize a wall's scan points to the actual face: the wall plane, extended, would otherwise also pass
        // near the neighbouring walls and pull the fit toward them.
        double flangeY = string.Equals(vy, "B", StringComparison.OrdinalIgnoreCase) ? datums.YMaxMm : datums.YMinMm;
        (double p2rx, double p2ry) = IntersectLines(
            geom.BackWall.Normal.X, geom.BackWall.Normal.Y, geom.BackWall.Offset,
            geom.DepthWall.Normal.X, geom.DepthWall.Normal.Y, geom.DepthWall.Offset);
        const double tanMargin = 3.0;

        // Back and lower walls bound the fillet corner. Fit each by total least squares (so a changed slant angle
        // is recovered from the points, not assumed from the reference normal) and read the P2 vertex (A, B).
        (double bnx, double bny, double boff) = FitWallTls(
            geom.BackWall, geom.Walls, world, wallBandMm, p2rx, flangeY, p2rx, p2ry, tanMargin);

        // The lower wall runs from P2 to either P3 (if there is an upper slant) or the end cap.
        double lowerEndX, lowerEndY;
        if (geom.UpperSlantWall is WallLine lowerFar)
            (lowerEndX, lowerEndY) = IntersectLines(
                geom.DepthWall.Normal.X, geom.DepthWall.Normal.Y, geom.DepthWall.Offset,
                lowerFar.Normal.X, lowerFar.Normal.Y, lowerFar.Offset);
        else
            (lowerEndX, lowerEndY) = IntersectLines(
                geom.DepthWall.Normal.X, geom.DepthWall.Normal.Y, geom.DepthWall.Offset, 1.0, 0.0, xEnd);
        (double lnx, double lny, double loff) = FitWallTls(
            geom.DepthWall, geom.Walls, world, wallBandMm, p2rx, p2ry, lowerEndX, lowerEndY, tanMargin);

        (double x2, double y2) = IntersectLines(bnx, bny, boff, lnx, lny, loff);

        double measuredA = nominal.LengthMm > 0 ? datums.LengthFromEnd(x2, vx) : 0.0;
        double measuredB = nominal.DepthMm > 0 ? HeightFromFlange(datums, y2, vy) : 0.0;

        // D: height where the lower slant meets the end-cap datum (its b-intercept at the beam end).
        double measuredD = 0.0;
        if (nominal.ShoulderMm > 0)
        {
            (double _, double yD) = IntersectLines(lnx, lny, loff, 1.0, 0.0, xEnd);
            measuredD = HeightFromFlange(datums, yD, vy);
        }

        // E and C need the upper slant (P3 = lower∩upper, P4 = upper∩end-cap).
        double measuredE = 0.0, measuredC = 0.0;
        if (nominal.SlantMm > 0 || nominal.TopRiseMm > 0)
        {
            WallLine upperWall = geom.UpperSlantWall
                ?? throw new InvalidOperationException(
                    "The cope cutter exposes no upper-slant wall; cannot measure C/E.");
            (double p3rx, double p3ry) = IntersectLines(
                geom.DepthWall.Normal.X, geom.DepthWall.Normal.Y, geom.DepthWall.Offset,
                upperWall.Normal.X, upperWall.Normal.Y, upperWall.Offset);
            (double p4rx, double p4ry) = IntersectLines(
                upperWall.Normal.X, upperWall.Normal.Y, upperWall.Offset, 1.0, 0.0, xEnd);
            (double unx, double uny, double uoff) = FitWallTls(
                upperWall, geom.Walls, world, wallBandMm, p3rx, p3ry, p4rx, p4ry, tanMargin);
            if (nominal.SlantMm > 0)
            {
                (double x3, double _) = IntersectLines(lnx, lny, loff, unx, uny, uoff);
                measuredE = datums.LengthFromEnd(x3, vx);
            }
            if (nominal.TopRiseMm > 0)
            {
                (double _, double y4) = IntersectLines(unx, uny, uoff, 1.0, 0.0, xEnd);
                double topHeight = HeightFromFlange(datums, y4, vy); // = D + C
                measuredC = topHeight - measuredD;
            }
        }

        // The fillet is pinned to the MEASURED corner (P2), with the reference wall normals passing through it —
        // when A/B deviate the real corner moves, and pinning to the cutter corner would bias the radius.
        double measuredR = 0.0;
        if (hasFillet)
        {
            Vec3 cornerWorld = new Vec3(x2, y2, geom.Origin.Z) - geom.Origin;
            double cornerA = Vec3.Dot(cornerWorld, geom.U);
            double cornerB = Vec3.Dot(cornerWorld, geom.V);
            double backD2 = geom.BackWall.Nu * cornerA + geom.BackWall.Nv * cornerB;
            double depthD2 = geom.DepthWall.Nu * cornerA + geom.DepthWall.Nv * cornerB;
            measuredR = FitFilletRadius(geom, a, b, wallBandMm, nominal.RadiusMm, cornerA, cornerB, backD2, depthD2);
        }

        var parameters = new[]
        {
            FeatureParameter.Judge("A", nominal.LengthMm, measuredA, tol.PositionMm),
            FeatureParameter.Judge("B", nominal.DepthMm, measuredB, tol.PositionMm),
            FeatureParameter.Judge("C", nominal.TopRiseMm, measuredC, tol.PositionMm),
            FeatureParameter.Judge("D", nominal.ShoulderMm, measuredD, tol.PositionMm),
            FeatureParameter.Judge("E", nominal.SlantMm, measuredE, tol.PositionMm),
            FeatureParameter.Judge("R", nominal.RadiusMm, measuredR, tol.RadiusMm),
        };

        return new FeatureInspectionReport
        {
            Feature = profile.Descriptor,
            Parameters = parameters,
            PointCount = n,
        };
    }

    /// <summary>
    /// The contour-extrusion cutter among the notch cutters: the one bearing a fillet face when
    /// <paramref name="requireFillet"/> is true (R&gt;0), else the one with the most lateral walls (the
    /// contour's 5 walls vs a flange rectangle's 4) — the robust choice when a sharp corner leaves no fillet.
    /// </summary>
    private static FeatureCutter SelectProfileCutter(IReadOnlyList<FeatureCutter> notchCutters, bool requireFillet)
    {
        if (requireFillet)
        {
            foreach (FeatureCutter fc in notchCutters)
            {
                if (ExtrudedProfile.HasFillet(fc.Cutter))
                    return fc;
            }
            throw new ArgumentException(
                "No contour-extrusion cutter with a fillet was found among the notch cutters.", nameof(notchCutters));
        }

        FeatureCutter? best = null;
        int bestWalls = -1;
        foreach (FeatureCutter fc in notchCutters)
        {
            int walls = ExtrudedProfile.LateralWallCount(fc.Cutter);
            if (walls > bestWalls)
            {
                bestWalls = walls;
                best = fc;
            }
        }
        return best ?? throw new ArgumentException(
            "No contour-extrusion cutter was found among the notch cutters.", nameof(notchCutters));
    }

    /// <summary>
    /// Fits a wall's actual world line in the length–height plane through its scan points, returning the line
    /// <c>Nx·X + Ny·Y = Off</c>.
    /// <para>
    /// An <b>axis-aligned</b> reference wall (back, bottom, or a horizontal lower slant) keeps its normal under
    /// any parameter change — only its offset moves — so it is fit with the known normal and a robust median
    /// offset (exact and low-noise). A genuinely <b>inclined</b> wall (an upper or tilted lower slant) can also
    /// change its angle, so it is fit by <b>total least squares</b>, which recovers slope and offset together —
    /// otherwise a drifted slant would be read at its centroid, biasing the vertices that depend on it.
    /// </para>
    /// <para>
    /// Points are localized to the face two ways: by the <b>tangent extent</b> between the wall's reference
    /// endpoints (<paramref name="e1x"/>..<paramref name="e2x"/>, plus <paramref name="tanMargin"/>), so the
    /// infinite plane's reach across neighbouring walls is cut off; and by dropping any point that also lies
    /// within <paramref name="band"/> of another wall (the shared corners).
    /// </para>
    /// </summary>
    private static (double NX, double NY, double Off) FitWallTls(
        WallLine wall, IReadOnlyList<WallLine> allWalls, Vec3[] world, double band,
        double e1x, double e1y, double e2x, double e2y, double tanMargin)
    {
        double tx = e2x - e1x, ty = e2y - e1y;
        double tl = Math.Sqrt(tx * tx + ty * ty);
        if (tl < 1e-9) { tx = 1; ty = 0; } else { tx /= tl; ty /= tl; }
        double s1 = e1x * tx + e1y * ty, s2 = e2x * tx + e2y * ty;
        double sMin = Math.Min(s1, s2) - tanMargin, sMax = Math.Max(s1, s2) + tanMargin;

        var xs = new List<double>();
        var ys = new List<double>();
        foreach (Vec3 p in world)
        {
            if (Math.Abs(Vec3.Dot(p, wall.Normal) - wall.Offset) > band)
                continue;
            double s = p.X * tx + p.Y * ty;
            if (s < sMin || s > sMax)
                continue;
            if (LiesOnAnotherWall(p, wall, allWalls, band))
                continue;
            xs.Add(p.X);
            ys.Add(p.Y);
        }
        if (xs.Count < 3)
            throw new InvalidOperationException($"Too few scan points on a cope wall ({xs.Count}).");

        int m = xs.Count;

        // Axis-aligned wall: the normal is fixed by the beam axis, so fit only the offset (robust median).
        if (Math.Abs(wall.Normal.X) > 0.99 || Math.Abs(wall.Normal.Y) > 0.99)
        {
            var proj = new List<double>(m);
            for (int i = 0; i < m; i++)
                proj.Add(xs[i] * wall.Normal.X + ys[i] * wall.Normal.Y);
            return (wall.Normal.X, wall.Normal.Y, PlaneFit.Median(proj));
        }

        double mx = 0, my = 0;
        for (int i = 0; i < m; i++) { mx += xs[i]; my += ys[i]; }
        mx /= m; my /= m;
        double sxx = 0, sxy = 0, syy = 0;
        for (int i = 0; i < m; i++)
        {
            double dx = xs[i] - mx, dy = ys[i] - my;
            sxx += dx * dx; sxy += dx * dy; syy += dy * dy;
        }

        // Line direction = eigenvector of the larger eigenvalue of [[sxx,sxy],[sxy,syy]]; the normal is ⊥ to it.
        double tr = sxx + syy, det = sxx * syy - sxy * sxy;
        double bigL = tr / 2.0 + Math.Sqrt(Math.Max(0.0, tr * tr / 4.0 - det));
        double dx2 = sxy, dy2 = bigL - sxx;
        if (Math.Abs(dx2) + Math.Abs(dy2) < 1e-12) { dx2 = bigL - syy; dy2 = sxy; }
        if (Math.Abs(dx2) + Math.Abs(dy2) < 1e-12) { dx2 = 1; dy2 = 0; }
        double dl = Math.Sqrt(dx2 * dx2 + dy2 * dy2);
        dx2 /= dl; dy2 /= dl;
        double nx = -dy2, ny = dx2;
        return (nx, ny, nx * mx + ny * my);
    }

    /// <summary>True if <paramref name="p"/> sits within <paramref name="band"/> of a wall other than <paramref name="target"/>.</summary>
    private static bool LiesOnAnotherWall(Vec3 p, WallLine target, IReadOnlyList<WallLine> allWalls, double band)
    {
        foreach (WallLine w in allWalls)
        {
            if (w.Equals(target))
                continue;
            if (Math.Abs(Vec3.Dot(p, w.Normal) - w.Offset) <= band)
                return true;
        }
        return false;
    }

    /// <summary>Intersection (X, Y) of two lines <c>a·X + b·Y = c</c> in the beam length–height plane.</summary>
    private static (double X, double Y) IntersectLines(
        double a1, double b1, double c1, double a2, double b2, double c2)
    {
        double det = a1 * b2 - a2 * b1;
        if (Math.Abs(det) < 1e-9)
            throw new InvalidOperationException("Two cope walls are parallel; their vertex is undefined.");
        double x = (c1 * b2 - c2 * b1) / det;
        double y = (a1 * c2 - a2 * c1) / det;
        return (x, y);
    }

    /// <summary>A world height (Y) expressed from the flange the cope anchors to: Y-min for "A", Y-max for "B".</summary>
    private static double HeightFromFlange(BeamDatumFrame datums, double y, string vy) =>
        string.Equals(vy, "B", StringComparison.OrdinalIgnoreCase) ? datums.YMaxMm - y : y - datums.YMinMm;

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
        // 3-parameter overload: pin the fillet to the cutter (reference) corner — its walls are not re-fitted.
        double measured = FitFilletRadius(
            geom, a, b, band, nominal, geom.CornerA, geom.CornerB, geom.BackWall.D2, geom.DepthWall.D2);
        return FeatureParameter.Judge("Radius", nominal, measured, tol);
    }

    /// <summary>
    /// The tangent-constrained fillet radius (mm), shared by both overloads, about the corner
    /// (<paramref name="cornerA"/>, <paramref name="cornerB"/>) where the back/depth walls meet (at offsets
    /// <paramref name="backD2"/>, <paramref name="depthD2"/> in the profile plane). See <see cref="MeasureFillet"/>
    /// for the method.
    /// </summary>
    private static double FitFilletRadius(
        ExtrudedProfile geom, double[] a, double[] b, double band, double nominal,
        double cornerA, double cornerB, double backD2, double depthD2)
    {
        double reach = nominal + 2.0;     // points within R(+margin) of the corner
        var fu = new List<double>();
        var fv = new List<double>();
        double sumA = 0, sumB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            double da = a[i] - cornerA, db = b[i] - cornerB;
            if (da * da + db * db > reach * reach)
                continue;
            // Off-wall test is in profile coords, so compare the line LHS against D2 (not the world Offset).
            bool onBack = Math.Abs(geom.BackWall.Nu * a[i] + geom.BackWall.Nv * b[i] - backD2) <= band;
            bool onDepth = Math.Abs(geom.DepthWall.Nu * a[i] + geom.DepthWall.Nv * b[i] - depthD2) <= band;
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
        (double ux1, double uy1) = InwardNormal(geom.BackWall, cornerA, cornerB, cA, cB);
        (double ux2, double uy2) = InwardNormal(geom.DepthWall, cornerA, cornerB, cA, cB);
        double sx = ux1 + ux2, sy = uy1 + uy2;

        return FitTangentRadius(fu, fv, cornerA, cornerB, sx, sy, nominal);
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

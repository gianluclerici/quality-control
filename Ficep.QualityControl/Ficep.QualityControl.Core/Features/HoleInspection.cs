using devDept.Geometry;
using Ficep.QualityControl.Core.Model;
using Ficep.QualityControl.Core.Registration;

namespace Ficep.QualityControl.Core.Features;

/// <summary>
/// Step 5.2: the first dimensional verdict. For a <see cref="FeatureKind.Hole"/> feature it measures the
/// bore diameter from the scan and judges it against the nominal.
/// <para>
/// The cylinder axis is taken from the cutter geometry (<see cref="CutterAxis"/>), so it is known exactly
/// and the only unknowns left are the in-plane centre and radius: the segmented hole points are projected
/// onto the plane perpendicular to the axis and fitted with an algebraic 2D circle
/// (<see cref="CircleFit"/>). The nominal diameter comes from the macro parameters (the value is part of
/// the inspection input — e.g. INTC01's <c>C</c> is the bore diameter), see
/// <see cref="NominalDiameterFromMacro"/>.
/// </para>
/// </summary>
public sealed class HoleInspection
{
    /// <summary>Default diameter tolerance half-width (mm).</summary>
    public const double DefaultDiameterToleranceMm = 0.5;

    /// <summary>
    /// Measures the bore diameter of <paramref name="hole"/> from <paramref name="scanPoints"/> (the
    /// aligned points the segmentation routed to this hole) and judges it against
    /// <paramref name="nominalDiameterMm"/> within ±<paramref name="diameterToleranceMm"/>.
    /// </summary>
    /// <param name="hole">The hole feature, whose cutter provides the (known) cylinder axis.</param>
    /// <param name="scanPoints">Aligned scan points belonging to the hole (≥ 3).</param>
    /// <param name="nominalDiameterMm">Nominal bore diameter (mm); see <see cref="NominalDiameterFromMacro"/>.</param>
    /// <param name="diameterToleranceMm">Symmetric tolerance half-width (mm).</param>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nominalDiameterMm"/> is not positive.</exception>
    /// <exception cref="ArgumentException">Fewer than 3 scan points.</exception>
    /// <exception cref="InvalidOperationException">Axis/circle could not be derived from the inputs.</exception>
    public FeatureInspectionReport Inspect(
        FeatureCutter hole,
        IReadOnlyList<Point3D> scanPoints,
        double nominalDiameterMm,
        double diameterToleranceMm = DefaultDiameterToleranceMm)
    {
        ArgumentNullException.ThrowIfNull(hole);
        ArgumentNullException.ThrowIfNull(scanPoints);
        if (nominalDiameterMm <= 0 || double.IsNaN(nominalDiameterMm))
            throw new ArgumentOutOfRangeException(nameof(nominalDiameterMm), nominalDiameterMm,
                "Nominal diameter must be a positive length in millimetres.");
        if (scanPoints.Count < 3)
            throw new ArgumentException("A hole diameter fit needs at least 3 scan points.", nameof(scanPoints));

        CylinderAxis axis = CutterAxis.FromCutter(hole.Cutter);
        (Vec3 uHat, Vec3 vHat) = OrthonormalBasis(axis.Direction);

        int n = scanPoints.Count;
        var u = new double[n];
        var v = new double[n];
        for (int i = 0; i < n; i++)
        {
            Point3D p = scanPoints[i];
            Vec3 w = new Vec3(p.X, p.Y, p.Z) - axis.Point;
            u[i] = Vec3.Dot(w, uHat);
            v[i] = Vec3.Dot(w, vHat);
        }

        CircleFitResult fit = CircleFit.Fit(u, v);
        double measuredDiameterMm = 2.0 * fit.Radius;

        var diameter = FeatureParameter.Judge(
            "Diameter", nominalDiameterMm, measuredDiameterMm, diameterToleranceMm);

        return new FeatureInspectionReport
        {
            Feature = hole.Descriptor,
            Parameters = new[] { diameter },
            PointCount = n,
        };
    }

    /// <summary>
    /// The nominal bore diameter encoded in an INTC (hole) macro's parameters. INTC01 builds the bore as a
    /// cylinder of radius <c>C/2</c> (or <c>F/2</c> on round profiles), so the diameter is the <c>C</c>
    /// parameter (falling back to <c>F</c>). This is the design value the scan is checked against.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="macro"/> is null.</exception>
    /// <exception cref="ArgumentException">The macro is not a hole macro or carries no diameter parameter.</exception>
    public static double NominalDiameterFromMacro(MacroSpec macro)
    {
        ArgumentNullException.ThrowIfNull(macro);
        if (FeatureKinds.FromMacroClassName(macro.MacroClassName) != FeatureKind.Hole)
            throw new ArgumentException(
                $"Macro '{macro.MacroClassName}' is not a hole macro.", nameof(macro));

        double c = macro.Parameters.GetValueOrDefault("C");
        double f = macro.Parameters.GetValueOrDefault("F");
        double diameter = c > 0 ? c : f;
        if (diameter <= 0)
            throw new ArgumentException(
                $"Hole macro '{macro.MacroClassName}' carries no positive diameter parameter (C or F).",
                nameof(macro));
        return diameter;
    }

    /// <summary>Builds a right-handed orthonormal basis (u, v) spanning the plane perpendicular to <paramref name="axis"/>.</summary>
    private static (Vec3 U, Vec3 V) OrthonormalBasis(Vec3 axis)
    {
        Vec3 a = axis.Normalized();
        Vec3 seed = Math.Abs(a.X) < 0.9 ? new Vec3(1, 0, 0) : new Vec3(0, 1, 0);
        Vec3 u = Vec3.Cross(a, seed).Normalized();
        Vec3 v = Vec3.Cross(a, u).Normalized();
        return (u, v);
    }
}

namespace Ficep.QualityControl.Core.Features;

/// <summary>
/// One dimensional parameter of a feature judged against its nominal: e.g. a hole's diameter. The
/// <see cref="MeasuredMm"/> value is fitted from the scan, the <see cref="NominalMm"/> value comes from
/// the design (macro parameter / cutter geometry), and the verdict is a symmetric ±<see cref="ToleranceMm"/>
/// band around the nominal.
/// </summary>
/// <param name="Name">Parameter name, e.g. "Diameter".</param>
/// <param name="NominalMm">Design value (mm).</param>
/// <param name="MeasuredMm">Value fitted from the scan (mm).</param>
/// <param name="ToleranceMm">Symmetric tolerance half-width (mm), always non-negative.</param>
/// <param name="InTolerance">True when <c>|MeasuredMm − NominalMm| ≤ ToleranceMm</c>.</param>
public readonly record struct FeatureParameter(
    string Name, double NominalMm, double MeasuredMm, double ToleranceMm, bool InTolerance)
{
    /// <summary>Signed measured−nominal deviation (mm).</summary>
    public double DeviationMm => MeasuredMm - NominalMm;

    /// <summary>
    /// Builds a parameter from a nominal/measured pair and a symmetric ±<paramref name="toleranceMm"/>
    /// band, computing the verdict. The sign of <paramref name="toleranceMm"/> is ignored.
    /// </summary>
    public static FeatureParameter Judge(string name, double nominalMm, double measuredMm, double toleranceMm)
    {
        double tol = Math.Abs(toleranceMm);
        bool inTol = Math.Abs(measuredMm - nominalMm) <= tol;
        return new FeatureParameter(name, nominalMm, measuredMm, tol, inTol);
    }
}

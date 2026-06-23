namespace Ficep.QualityControl.Core.Measurement;

/// <summary>
/// A signed tolerance band (mm) against which a point's deviation is judged conform. The sign follows
/// the nominal's outward normal: a <b>positive</b> deviation is material in excess (outside the nominal
/// surface), a <b>negative</b> one is material missing (inside it). A point conforms when its signed
/// deviation lies within <c>[LowerMm, UpperMm]</c>.
/// </summary>
/// <param name="LowerMm">Lower (most negative) admissible deviation, mm.</param>
/// <param name="UpperMm">Upper (most positive) admissible deviation, mm.</param>
public readonly record struct ToleranceBand(double LowerMm, double UpperMm)
{
    /// <summary>A symmetric band ±<paramref name="halfWidthMm"/> (the sign of the argument is ignored).</summary>
    public static ToleranceBand Symmetric(double halfWidthMm)
    {
        double t = Math.Abs(halfWidthMm);
        return new ToleranceBand(-t, t);
    }

    /// <summary>True if <paramref name="signedDeviationMm"/> falls inside the band (inclusive).</summary>
    public bool Contains(double signedDeviationMm) =>
        signedDeviationMm >= LowerMm && signedDeviationMm <= UpperMm;
}

using System.Collections.Generic;
using devDept.Geometry;
using Ficep.QualityControl.Core.Registration;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Features;

/// <summary>
/// The beam's bounding datum planes, measured from the scan (mm), in the aligned/nominal frame. The beam
/// is extruded along X with its cross-section in the YZ plane (see <see cref="Model.BeamSpec"/>), so the
/// six axis-aligned faces are: the two end caps (±X), the flange faces (±Y) and the flange-edge faces
/// (±Z). The face-to-face spans give the measured length / height / width.
/// <para>
/// These are the <b>references</b> a feature-relative measurement is expressed against: because both the
/// feature walls and these datum planes are fitted from the <i>same</i> cloud, their difference (a
/// wall→datum distance) is invariant to residual registration error — a translation/rotation bias shifts
/// feature and datum together. See <c>docs/research/notch-parameter-extraction.md</c> §C.
/// </para>
/// </summary>
/// <param name="XMinMm">Coordinate (mm) of the start end cap (X-min), where Vx="I" features anchor.</param>
/// <param name="XMaxMm">Coordinate (mm) of the far end cap (X-max), where Vx="F" features anchor.</param>
/// <param name="YMinMm">Coordinate (mm) of the bottom flange face (Y-min).</param>
/// <param name="YMaxMm">Coordinate (mm) of the top flange face (Y-max).</param>
/// <param name="ZMinMm">Coordinate (mm) of the flange-edge face at flange A (Z-min).</param>
/// <param name="ZMaxMm">Coordinate (mm) of the flange-edge face at flange B (Z-max).</param>
public readonly record struct BeamDatumFrame(
    double XMinMm, double XMaxMm,
    double YMinMm, double YMaxMm,
    double ZMinMm, double ZMaxMm)
{
    /// <summary>Measured beam length along X (end-cap to end-cap), mm.</summary>
    public double MeasuredLengthMm => XMaxMm - XMinMm;

    /// <summary>Measured section height along Y (flange face to flange face), mm.</summary>
    public double MeasuredHeightMm => YMaxMm - YMinMm;

    /// <summary>Measured flange width along Z (edge face to edge face), mm.</summary>
    public double MeasuredWidthMm => ZMaxMm - ZMinMm;
}

/// <summary>
/// Estimates the beam's <see cref="BeamDatumFrame"/> from the scan's <b>base bucket</b> (the unmachined
/// body points — those the segmentation does not assign to any feature cutter).
/// <para>
/// After registration the beam axis is aligned to the nominal, so every bounding face has a <b>known
/// normal</b> (a ±unit axis). That is exactly the condition under which the offset-only robust fit (median
/// of the coordinate along the axis, reusing <see cref="PlaneFit"/> technique A3) is both cheaper and more
/// accurate than re-estimating an unknown normal — no RANSAC/PCA/OBB needed. Each face's points are
/// selected by their <see cref="SurfaceSample.Normal"/> direction (dot against the outward face normal),
/// then the face coordinate is the median of those points' projection on the axis.
/// </para>
/// </summary>
public sealed class BeamDatums
{
    /// <summary>Default minimum cosine between a sample normal and a face normal to claim the sample for that face (~25°).</summary>
    public const double DefaultNormalCosThreshold = 0.9;

    /// <summary>Default minimum number of points a datum face cluster must hold to be trusted.</summary>
    public const int DefaultMinPointsPerFace = 10;

    /// <summary>Default thickness (mm) of the band that groups one face's points into a cluster.</summary>
    public const double DefaultFaceBandMm = 2.0;

    private static readonly Vec3 AxisX = new(1, 0, 0);
    private static readonly Vec3 AxisY = new(0, 1, 0);
    private static readonly Vec3 AxisZ = new(0, 0, 1);

    /// <summary>
    /// Fits the six bounding datum planes from <paramref name="baseSamples"/> (base-bucket scan samples,
    /// in the aligned/nominal frame, carrying their surface normals).
    /// </summary>
    /// <param name="baseSamples">The unmachined-body samples (positions + normals), already aligned.</param>
    /// <param name="normalCosThreshold">Min cosine to claim a sample for a face; see <see cref="DefaultNormalCosThreshold"/>.</param>
    /// <param name="minPointsPerFace">Min points per face cluster; see <see cref="DefaultMinPointsPerFace"/>.</param>
    /// <param name="faceBandMm">Cluster band thickness; see <see cref="DefaultFaceBandMm"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="baseSamples"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A datum face found no cluster of <paramref name="minPointsPerFace"/> points.</exception>
    public static BeamDatumFrame Estimate(
        IReadOnlyList<SurfaceSample> baseSamples,
        double normalCosThreshold = DefaultNormalCosThreshold,
        int minPointsPerFace = DefaultMinPointsPerFace,
        double faceBandMm = DefaultFaceBandMm)
    {
        ArgumentNullException.ThrowIfNull(baseSamples);

        double xMin = FitFaceCoordinate(baseSamples, AxisX, -1, normalCosThreshold, minPointsPerFace, faceBandMm, "X-min (start end)");
        double xMax = FitFaceCoordinate(baseSamples, AxisX, +1, normalCosThreshold, minPointsPerFace, faceBandMm, "X-max (far end)");
        double yMin = FitFaceCoordinate(baseSamples, AxisY, -1, normalCosThreshold, minPointsPerFace, faceBandMm, "Y-min (bottom flange)");
        double yMax = FitFaceCoordinate(baseSamples, AxisY, +1, normalCosThreshold, minPointsPerFace, faceBandMm, "Y-max (top flange)");
        double zMin = FitFaceCoordinate(baseSamples, AxisZ, -1, normalCosThreshold, minPointsPerFace, faceBandMm, "Z-min (flange A edge)");
        double zMax = FitFaceCoordinate(baseSamples, AxisZ, +1, normalCosThreshold, minPointsPerFace, faceBandMm, "Z-max (flange B edge)");

        return new BeamDatumFrame(xMin, xMax, yMin, yMax, zMin, zMax);
    }

    /// <summary>
    /// The robust coordinate (mm) of the datum face whose outward normal is
    /// <paramref name="faceSign"/>·<paramref name="posAxis"/>. Collects the base samples whose normal aligns
    /// with that outward direction, then takes the <b>outermost</b> cluster (in the outward direction) of at
    /// least <paramref name="minPoints"/> points within a <paramref name="faceBandMm"/> band, and returns its
    /// median coordinate. Taking the outer cluster — not the global median — is what separates the wanted
    /// bounding face from any inner parallel face that shares the same normal (e.g. the web's side face has
    /// far more points than, but sits inside, the flange-edge face along ±Z).
    /// </summary>
    private static double FitFaceCoordinate(
        IReadOnlyList<SurfaceSample> samples, Vec3 posAxis, int faceSign,
        double cosThreshold, int minPoints, double faceBandMm, string label)
    {
        Vec3 outward = posAxis * faceSign;
        var coords = new List<double>();
        foreach (SurfaceSample s in samples)
        {
            var nrm = new Vec3(s.Normal.X, s.Normal.Y, s.Normal.Z);
            if (Vec3.Dot(nrm, outward) >= cosThreshold)
                coords.Add(Vec3.Dot(new Vec3(s.Position.X, s.Position.Y, s.Position.Z), posAxis));
        }
        coords.Sort();

        // Walk inward from the outer extreme, gathering each band-thick cluster; return the first (outermost)
        // one that is populated enough to be a real face (a sparse cluster is a stray-point artefact, skipped).
        if (faceSign > 0)
        {
            for (int i = coords.Count - 1; i >= 0;)
            {
                double hi = coords[i];
                int j = i;
                while (j >= 0 && coords[j] >= hi - faceBandMm) j--;
                int count = i - j; // indices (j+1 .. i)
                if (count >= minPoints)
                    return PlaneFit.Median(coords.GetRange(j + 1, count));
                i = j;
            }
        }
        else
        {
            for (int i = 0; i < coords.Count;)
            {
                double lo = coords[i];
                int j = i;
                while (j < coords.Count && coords[j] <= lo + faceBandMm) j++;
                int count = j - i; // indices (i .. j-1)
                if (count >= minPoints)
                    return PlaneFit.Median(coords.GetRange(i, count));
                i = j;
            }
        }

        throw new InvalidOperationException(
            $"Datum face '{label}' is under-sampled (no cluster of {minPoints} points within {faceBandMm} mm).");
    }
}

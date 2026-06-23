using devDept.Geometry;

namespace Ficep.QualityControl.Core.Measurement;

/// <summary>
/// One scan point's deviation from the nominal surface: the point's position (in the aligned frame)
/// and its <b>signed</b> distance to the nominal. Positive = outside the nominal (material in excess),
/// negative = inside it (material missing); the sign is taken along the nominal's outward normal at the
/// closest point.
/// </summary>
/// <param name="Point">The scan point after alignment (mm).</param>
/// <param name="SignedDistanceMm">Signed point-to-surface distance (mm); see the class summary for sign.</param>
public readonly record struct PointDeviation(Point3D Point, double SignedDistanceMm);

using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.QualityControl.Core.Registration;

namespace Ficep.QualityControl.Core.Features;

/// <summary>An infinite line: a point on the axis and a unit direction.</summary>
internal readonly record struct CylinderAxis(Vec3 Point, Vec3 Direction);

/// <summary>
/// Derives the axis of a cylindrical hole cutter from its geometry (no macro letters involved). The
/// cutter is a circle extruded into a solid, so it has two planar circular caps whose normal is the
/// cylinder axis and whose centre lies on it. We read that axis off the first planar face: the analytic
/// plane gives both a unit normal (axis direction) and an origin coincident with the cap centre (a point
/// on the axis). With the axis known the diameter measurement reduces to a 2D circle fit.
/// </summary>
internal static class CutterAxis
{
    /// <summary>Returns the cylinder axis of <paramref name="cutter"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="cutter"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No planar cap face was found.</exception>
    public static CylinderAxis FromCutter(Brep cutter)
    {
        ArgumentNullException.ThrowIfNull(cutter);

        Brep.Face[]? faces = cutter.Faces;
        if (faces is not null)
        {
            foreach (Brep.Face face in faces)
            {
                Plane? plane = face.IsPlanar();
                if (plane is null)
                    continue;

                Vector3D n = plane.AxisZ;
                Vec3 dir = new Vec3(n.X, n.Y, n.Z).Normalized();
                if (dir.LengthSquared <= 0)
                    continue;

                Point3D o = plane.Origin;
                return new CylinderAxis(new Vec3(o.X, o.Y, o.Z), dir);
            }
        }

        throw new InvalidOperationException(
            "Cannot derive the cylinder axis: the cutter has no planar cap face.");
    }
}

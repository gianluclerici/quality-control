using devDept.Eyeshot.Entities;

namespace Ficep.QualityControl.Core.Sampling;

/// <summary>
/// Tessellates a <see cref="Brep"/> into a triangle <see cref="Mesh"/> for surface
/// sampling, via <c>Brep.ConvertToMesh</c>. Uses the <see cref="Mesh.natureType.Plain"/>
/// nature (one normal per triangle), which matches the flat / developable faces of a
/// machined steel beam and the geometric per-triangle normals the sampler computes.
/// </summary>
public static class BrepTessellator
{
    /// <summary>
    /// Converts <paramref name="brep"/> to a triangle mesh.
    /// </summary>
    /// <param name="brep">The solid to tessellate.</param>
    /// <param name="chordDeviation">
    /// Maximum chordal deviation (mm) between the mesh and the exact surface — the
    /// tessellation tolerance. Smaller ⇒ finer mesh on curved faces. Typically the same
    /// Brep tolerance used to build the solid (<c>EyeParam.Tol.Brep</c>).
    /// </param>
    /// <param name="angleDeviation">Maximum angular deflection (radians); 0 to ignore.</param>
    /// <returns>The triangulated mesh.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="brep"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="chordDeviation"/> is not positive.</exception>
    public static Mesh ToMesh(Brep brep, double chordDeviation, double angleDeviation = 0.0)
    {
        ArgumentNullException.ThrowIfNull(brep);
        if (chordDeviation <= 0 || double.IsNaN(chordDeviation))
            throw new ArgumentOutOfRangeException(nameof(chordDeviation), chordDeviation,
                "Chord deviation must be a positive tolerance in millimetres.");

        return brep.ConvertToMesh(chordDeviation, angleDeviation, Mesh.natureType.Plain, weld: false);
    }
}

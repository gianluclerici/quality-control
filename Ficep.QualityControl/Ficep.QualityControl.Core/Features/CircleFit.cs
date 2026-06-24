using Ficep.QualityControl.Core.Registration;

namespace Ficep.QualityControl.Core.Features;

/// <summary>Result of a 2D circle fit: centre and radius in the fit plane (mm).</summary>
internal readonly record struct CircleFitResult(double CenterU, double CenterV, double Radius);

/// <summary>
/// Algebraic (Kåsa) least-squares circle fit in 2D. Fits <c>u² + v² + D·u + E·v + F = 0</c> by solving
/// the 3×3 normal equations for (D, E, F) — a single linear solve, no initial guess and no iteration,
/// which is exactly what we want when the cylinder axis is already known (the hole's axis comes from the
/// cutter geometry, so the only unknowns left are the in-plane centre and radius). Reuses the project's
/// <see cref="Cholesky"/> solver since the normal matrix is the symmetric positive-definite Gram matrix
/// of the basis [u, v, 1].
/// </summary>
internal static class CircleFit
{
    /// <summary>
    /// Fits a circle to the planar points (<paramref name="u"/>, <paramref name="v"/>).
    /// </summary>
    /// <exception cref="ArgumentNullException">A coordinate array is null.</exception>
    /// <exception cref="ArgumentException">Fewer than 3 points, or the arrays differ in length.</exception>
    /// <exception cref="InvalidOperationException">The points are degenerate (collinear / coincident).</exception>
    public static CircleFitResult Fit(IReadOnlyList<double> u, IReadOnlyList<double> v)
    {
        ArgumentNullException.ThrowIfNull(u);
        ArgumentNullException.ThrowIfNull(v);
        if (u.Count != v.Count)
            throw new ArgumentException("Coordinate arrays must have the same length.");
        int n = u.Count;
        if (n < 3)
            throw new ArgumentException("A circle fit needs at least 3 points.", nameof(u));

        double sUU = 0, sUV = 0, sVV = 0, sU = 0, sV = 0;
        double sZU = 0, sZV = 0, sZ = 0; // z = u² + v²
        for (int i = 0; i < n; i++)
        {
            double ui = u[i], vi = v[i], zi = ui * ui + vi * vi;
            sUU += ui * ui; sUV += ui * vi; sVV += vi * vi; sU += ui; sV += vi;
            sZU += zi * ui; sZV += zi * vi; sZ += zi;
        }

        // Normal equations A·[D,E,F] = -[sZU, sZV, sZ].
        var a = new double[3, 3] { { sUU, sUV, sU }, { sUV, sVV, sV }, { sU, sV, n } };
        var b = new double[] { -sZU, -sZV, -sZ };
        var x = new double[3];
        if (!Cholesky.Solve(a, b, x, 3))
            throw new InvalidOperationException("Circle fit failed: the points are collinear or coincident.");

        double cu = -x[0] / 2.0;
        double cv = -x[1] / 2.0;
        double r2 = cu * cu + cv * cv - x[2];
        if (r2 <= 0 || double.IsNaN(r2))
            throw new InvalidOperationException("Circle fit failed: non-positive radius.");

        return new CircleFitResult(cu, cv, Math.Sqrt(r2));
    }
}

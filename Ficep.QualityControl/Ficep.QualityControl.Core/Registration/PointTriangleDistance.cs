namespace Ficep.QualityControl.Core.Registration;

/// <summary>
/// Closest point from a point to a triangle, via Ericson's "Real-Time Collision Detection"
/// Voronoi-region method (Ch. 5.1.5): a branch sequence that returns whichever vertex, edge, or
/// face region of the triangle contains the projection — exact and allocation-free.
/// </summary>
internal static class PointTriangleDistance
{
    public static Vec3 ClosestPoint(Vec3 p, Vec3 a, Vec3 b, Vec3 c)
    {
        Vec3 ab = b - a;
        Vec3 ac = c - a;
        Vec3 ap = p - a;

        double d1 = Vec3.Dot(ab, ap);
        double d2 = Vec3.Dot(ac, ap);
        if (d1 <= 0 && d2 <= 0)
            return a; // vertex region A

        Vec3 bp = p - b;
        double d3 = Vec3.Dot(ab, bp);
        double d4 = Vec3.Dot(ac, bp);
        if (d3 >= 0 && d4 <= d3)
            return b; // vertex region B

        double vc = d1 * d4 - d3 * d2;
        if (vc <= 0 && d1 >= 0 && d3 <= 0)
            return a + ab * (d1 / (d1 - d3)); // edge region AB

        Vec3 cp = p - c;
        double d5 = Vec3.Dot(ab, cp);
        double d6 = Vec3.Dot(ac, cp);
        if (d6 >= 0 && d5 <= d6)
            return c; // vertex region C

        double vb = d5 * d2 - d1 * d6;
        if (vb <= 0 && d2 >= 0 && d6 <= 0)
            return a + ac * (d2 / (d2 - d6)); // edge region AC

        double va = d3 * d6 - d5 * d4;
        if (va <= 0 && d4 - d3 >= 0 && d5 - d6 >= 0)
            return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6))); // edge region BC

        // Inside the face: barycentric combination.
        double denom = 1.0 / (va + vb + vc);
        double v = vb * denom;
        double w = vc * denom;
        return a + ab * v + ac * w;
    }
}

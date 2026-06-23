namespace Ficep.QualityControl.Core.Registration;

/// <summary>
/// Bounding-volume hierarchy (AABB tree) over a triangle set, giving an <b>exact</b> closest-point
/// query via branch-and-bound: at each node the nearer child is searched first and a child is skipped
/// once its box can't possibly hold a closer point than the best found so far. Unlike a centroid
/// k-nearest heuristic, this is correct for coarse meshes with large triangles (where a point's own
/// triangle centroid can be far away), which is exactly the case for the flat faces of a steel beam.
/// </summary>
internal sealed class TriangleBvh
{
    private const int LeafSize = 4;

    private readonly Vec3[] _a;
    private readonly Vec3[] _b;
    private readonly Vec3[] _c;
    private readonly int[] _tri; // triangle indices, reordered by the build

    // Node arrays (parallel). Internal nodes use _childA/_childB; leaves use _start/_count (_childA = -1).
    private readonly double[] _minX, _minY, _minZ, _maxX, _maxY, _maxZ;
    private readonly int[] _childA, _childB, _start, _count;
    private int _nodeCount;

    private TriangleBvh(Vec3[] a, Vec3[] b, Vec3[] c)
    {
        _a = a;
        _b = b;
        _c = c;
        int n = a.Length;
        _tri = new int[n];
        for (int i = 0; i < n; i++)
            _tri[i] = i;

        int maxNodes = Math.Max(1, 2 * n);
        _minX = new double[maxNodes]; _minY = new double[maxNodes]; _minZ = new double[maxNodes];
        _maxX = new double[maxNodes]; _maxY = new double[maxNodes]; _maxZ = new double[maxNodes];
        _childA = new int[maxNodes]; _childB = new int[maxNodes];
        _start = new int[maxNodes]; _count = new int[maxNodes];
    }

    public static TriangleBvh Build(Vec3[] a, Vec3[] b, Vec3[] c)
    {
        var bvh = new TriangleBvh(a, b, c);
        bvh.BuildNode(0, a.Length);
        return bvh;
    }

    /// <summary>Returns the index of the triangle closest to <paramref name="q"/> and the closest point on it.</summary>
    public int ClosestTriangle(Vec3 q, out Vec3 closestPoint)
    {
        double bestDistSq = double.MaxValue;
        int bestTri = 0;
        Vec3 bestPoint = default;
        Search(0, q, ref bestDistSq, ref bestTri, ref bestPoint);
        closestPoint = bestPoint;
        return bestTri;
    }

    private int BuildNode(int start, int count)
    {
        int node = _nodeCount++;
        ComputeBounds(start, count, out double miX, out double miY, out double miZ, out double maX, out double maY, out double maZ);
        _minX[node] = miX; _minY[node] = miY; _minZ[node] = miZ;
        _maxX[node] = maX; _maxY[node] = maY; _maxZ[node] = maZ;

        if (count <= LeafSize)
        {
            _childA[node] = -1;
            _start[node] = start;
            _count[node] = count;
            return node;
        }

        // Split along the widest axis of the triangle centroids, at the spatial midpoint.
        int axis = LongestCentroidAxis(start, count, out double mid);
        int split = Partition(start, count, axis, mid);
        if (split == start || split == start + count)
            split = start + count / 2; // midpoint split degenerated; fall back to a median index split

        int left = BuildNode(start, split - start);
        int right = BuildNode(split, start + count - split);
        _childA[node] = left;
        _childB[node] = right;
        return node;
    }

    private void Search(int node, Vec3 q, ref double bestDistSq, ref int bestTri, ref Vec3 bestPoint)
    {
        if (_childA[node] == -1)
        {
            int s = _start[node], e = s + _count[node];
            for (int k = s; k < e; k++)
            {
                int t = _tri[k];
                Vec3 cp = PointTriangleDistance.ClosestPoint(q, _a[t], _b[t], _c[t]);
                double d2 = (q - cp).LengthSquared;
                if (d2 < bestDistSq)
                {
                    bestDistSq = d2;
                    bestTri = t;
                    bestPoint = cp;
                }
            }
            return;
        }

        int a = _childA[node], b = _childB[node];
        double da = BoxDistSq(a, q);
        double db = BoxDistSq(b, q);

        // Visit the nearer child first; prune a child whose box can't beat the current best.
        if (da <= db)
        {
            if (da < bestDistSq) Search(a, q, ref bestDistSq, ref bestTri, ref bestPoint);
            if (db < bestDistSq) Search(b, q, ref bestDistSq, ref bestTri, ref bestPoint);
        }
        else
        {
            if (db < bestDistSq) Search(b, q, ref bestDistSq, ref bestTri, ref bestPoint);
            if (da < bestDistSq) Search(a, q, ref bestDistSq, ref bestTri, ref bestPoint);
        }
    }

    private double BoxDistSq(int node, Vec3 q)
    {
        double dx = q.X < _minX[node] ? _minX[node] - q.X : q.X > _maxX[node] ? q.X - _maxX[node] : 0;
        double dy = q.Y < _minY[node] ? _minY[node] - q.Y : q.Y > _maxY[node] ? q.Y - _maxY[node] : 0;
        double dz = q.Z < _minZ[node] ? _minZ[node] - q.Z : q.Z > _maxZ[node] ? q.Z - _maxZ[node] : 0;
        return dx * dx + dy * dy + dz * dz;
    }

    private void ComputeBounds(int start, int count, out double miX, out double miY, out double miZ, out double maX, out double maY, out double maZ)
    {
        miX = miY = miZ = double.MaxValue;
        maX = maY = maZ = double.MinValue;
        for (int k = start; k < start + count; k++)
        {
            int t = _tri[k];
            Accumulate(_a[t], ref miX, ref miY, ref miZ, ref maX, ref maY, ref maZ);
            Accumulate(_b[t], ref miX, ref miY, ref miZ, ref maX, ref maY, ref maZ);
            Accumulate(_c[t], ref miX, ref miY, ref miZ, ref maX, ref maY, ref maZ);
        }
    }

    private static void Accumulate(Vec3 p, ref double miX, ref double miY, ref double miZ, ref double maX, ref double maY, ref double maZ)
    {
        if (p.X < miX) miX = p.X; if (p.X > maX) maX = p.X;
        if (p.Y < miY) miY = p.Y; if (p.Y > maY) maY = p.Y;
        if (p.Z < miZ) miZ = p.Z; if (p.Z > maZ) maZ = p.Z;
    }

    private int LongestCentroidAxis(int start, int count, out double mid)
    {
        double miX = double.MaxValue, miY = double.MaxValue, miZ = double.MaxValue;
        double maX = double.MinValue, maY = double.MinValue, maZ = double.MinValue;
        for (int k = start; k < start + count; k++)
        {
            Vec3 c = Centroid(_tri[k]);
            if (c.X < miX) miX = c.X; if (c.X > maX) maX = c.X;
            if (c.Y < miY) miY = c.Y; if (c.Y > maY) maY = c.Y;
            if (c.Z < miZ) miZ = c.Z; if (c.Z > maZ) maZ = c.Z;
        }

        double ex = maX - miX, ey = maY - miY, ez = maZ - miZ;
        if (ex >= ey && ex >= ez) { mid = 0.5 * (miX + maX); return 0; }
        if (ey >= ez) { mid = 0.5 * (miY + maY); return 1; }
        mid = 0.5 * (miZ + maZ);
        return 2;
    }

    private int Partition(int start, int count, int axis, double mid)
    {
        int i = start, j = start + count - 1;
        while (i <= j)
        {
            double ci = Component(Centroid(_tri[i]), axis);
            if (ci < mid)
            {
                i++;
            }
            else
            {
                (_tri[i], _tri[j]) = (_tri[j], _tri[i]);
                j--;
            }
        }
        return i;
    }

    private Vec3 Centroid(int t) => (_a[t] + _b[t] + _c[t]) * (1.0 / 3.0);

    private static double Component(Vec3 v, int axis) => axis switch { 0 => v.X, 1 => v.Y, _ => v.Z };
}

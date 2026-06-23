using System.Runtime.CompilerServices;

// The test project exercises internal registration primitives (Vec3, PointTriangleDistance, KdTree3,
// and RigidTransform.FromRotationVector) directly, in addition to the public API.
[assembly: InternalsVisibleTo("Ficep.QualityControl.Core.Tests")]

using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.QualityControl.Core.Generation;
using Ficep.QualityControl.Core.Model;
using Ficep.QualityControl.Core.Registration;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Tests;

public class RegistrationTests
{
    // A small nominal + a low-density on-surface cloud, shared by the ICP tests.
    private static (NominalSurface surface, IReadOnlyList<SurfaceSample> cloud) BuildBeam()
    {
        var factory = new BeamFactory();
        Brep brep = factory.BuildRaw(BeamSpec.Ipe300(1000.0));
        Mesh mesh = BrepTessellator.ToMesh(brep, factory.BrepTolerance);
        NominalSurface surface = NominalSurface.FromMesh(mesh);
        IReadOnlyList<SurfaceSample> cloud = new MeshSurfaceSampler(densityPerMm2: 0.05, seed: 2024).Sample(mesh);
        return (surface, cloud);
    }

    [Fact]
    public void ClosestPoint_OnSurfaceSample_IsEssentiallyZero()
    {
        var (surface, cloud) = BuildBeam();
        SurfaceProjection proj = surface.ClosestPoint(cloud[0].Position);
        Assert.True(proj.Distance < 1e-6, $"on-surface point should project to ~0, was {proj.Distance}");
    }

    [Fact]
    public void ClosestPoint_OffsetAlongNormal_RecoversTheOffset()
    {
        var (surface, cloud) = BuildBeam();
        SurfaceSample s = cloud[0];
        const double offset = 0.5;
        var moved = new Point3D(
            s.Position.X + s.Normal.X * offset,
            s.Position.Y + s.Normal.Y * offset,
            s.Position.Z + s.Normal.Z * offset);

        SurfaceProjection proj = surface.ClosestPoint(moved);
        Assert.Equal(offset, proj.Distance, 3);
    }

    [Fact]
    public void Register_RecoversAKnownMisalignment()
    {
        var (surface, cloud) = BuildBeam();

        // Perturb the on-surface cloud by a small known rigid transform: ~2.3° about a tilted axis
        // plus a few mm of translation. ICP must pull it back onto the surface.
        var misalign = RigidTransform.FromRotationVector(0.03, -0.02, 0.015, 3.0, -2.0, 1.5);
        var misaligned = new List<SurfaceSample>(cloud.Count);
        foreach (SurfaceSample s in cloud)
            misaligned.Add(new SurfaceSample(misalign.Apply(s.Position), s.Normal));

        double preRms = Rms(surface, misaligned);
        RegistrationResult result = new IcpRegistration().Register(misaligned, surface);

        Assert.True(preRms > 1.0, $"misalignment should move points well off the surface (pre-RMS {preRms:F3})");
        Assert.True(result.RmsErrorMm < 0.05,
            $"post-alignment RMS should be ~0, was {result.RmsErrorMm:F4} after {result.Iterations} iters (converged={result.Converged})");
    }

    [Fact]
    public void RigidTransform_ToTransformation_MatchesApply()
    {
        // Validates the Eyeshot 4x4 matrix layout: transforming via the produced Transformation must
        // match our own Apply for a non-trivial rotation+translation.
        var rt = RigidTransform.FromRotationVector(0.3, 0.1, -0.2, 5.0, -3.0, 2.0);
        var p = new Point3D(7.0, -4.0, 11.0);

        Point3D mine = rt.Apply(p);
        var viaEyeshot = (Point3D)p.Clone();
        viaEyeshot.TransformBy(rt.ToTransformation());

        Assert.Equal(mine.X, viaEyeshot.X, 9);
        Assert.Equal(mine.Y, viaEyeshot.Y, 9);
        Assert.Equal(mine.Z, viaEyeshot.Z, 9);
    }

    private static double Rms(NominalSurface surface, IReadOnlyList<SurfaceSample> cloud)
    {
        double sumSq = 0;
        foreach (SurfaceSample s in cloud)
        {
            double d = surface.ClosestPoint(s.Position).Distance;
            sumSq += d * d;
        }
        return Math.Sqrt(sumSq / cloud.Count);
    }
}

# Research — Mesh Surface Sampling for Synthetic 3D-Camera Scans

**Context.** Step 1 of the quality-control pipeline turns a nominal `Brep` (a steel beam, raw or
machined) into a point cloud that *simulates* a 3D-camera scan. The first stage of that is
**surface sampling**: drawing points on the triangulated surface of the beam. The downstream uses
of these points are (a) registration/ICP against the nominal CAD model and (b) measuring how well
the realized cut parameters match the nominal ones. Both want **even, isotropic surface coverage**
with **correct per-point normals** — gaps or clusters bias plane/edge fitting.

This note records the techniques considered, the choice made for Step 1, and why.

---

## Techniques considered

### 1. Per-triangle area-weighted uniform random sampling  ✅ chosen for Step 1
Pick a triangle with probability proportional to its area, then pick a uniformly random point
inside it.

- **Triangle selection.** Build the cumulative-area table `C[i] = Σ_{k≤i} area(k)`, draw
  `u ~ U(0, totalArea)`, binary-search for the triangle. This makes the *expected* density of
  points constant per unit area regardless of how the tessellation distributes triangle sizes.
- **Uniform point in a triangle (the √ trick).** For a triangle `A,B,C`, draw `r1, r2 ~ U(0,1)` and
  set `s = √r1`. Then
  `P = (1 − s)·A + s·(1 − r2)·B + s·r2·C`.
  The `√` reshapes the unit square so the barycentric coordinates are uniform over the triangle
  area (a naïve `(1−r1−r2, r1, r2)` over-samples one corner).
- **Normal.** Use the triangle's geometric normal `n = normalize((B−A) × (C−A))`, oriented outward.
  For a tessellated CAD solid this is exact per face (flat triangles), which is what we want — we
  are reconstructing planar/cylindrical machined faces, not smooth organic surfaces.

**Why this is appropriate here.** The number of points is controlled directly by a **target density
(points/mm²)**: `nPoints ≈ density · totalArea`. It is O(n) to build the table and O(log T) per
sample, fully streaming, trivially **seedable/deterministic** (required for reproducible tests),
and produces an unbiased uniform distribution over the true surface. Real scanners do not produce
blue-noise point sets anyway — they produce a depth grid whose surface density falls roughly with
distance and incidence angle — so the marginal benefit of a more even point distribution does not
justify the cost for Step 1.

### 2. Poisson-disk / blue-noise surface sampling  ⏸ deferred
Enforces a minimum distance `r` between samples (dart-throwing, hierarchical, or relaxation such as
Capacity-Constrained Surface Triangulation). Produces **blue-noise** spectra: even spacing, no
clumping, no large gaps.

- **Pros.** Best-possible isotropic coverage; the gold standard when downstream fitting is sensitive
  to local density variation; avoids the random clusters/voids that pure i.i.d. area-weighted
  sampling leaves.
- **Cons.** More complex and slower (rejection or multi-pass relaxation); the spacing parameter `r`
  is a less natural control than density for "how fine is the scan"; benefit is largely invisible
  once realistic sensor noise and a depth grid are layered on top.

**Decision.** Documented as the upgrade path. If registration/measurement later proves sensitive to
sampling clusters, swap in a blue-noise sampler behind the same `IPointSampler` interface — a simple
and cheap approximation is **area-weighted oversampling followed by a Poisson-disk thinning pass**
(Alec Jacobson's "very simple method"), which reuses technique #1 as its first stage.

### 3. Virtual single-viewpoint scan with occlusion (hidden-point removal)  ⏸ deferred
Simulate a real camera: cast from one (or few) viewpoints, keep only the first surface hit, drop
self-occluded regions, and let density fall with `cos(incidence)/z²`. This is the most *physically
faithful* simulation.

- **Pros.** Reproduces the actual partial-view, density-gradient, shadowed-pocket character of a
  real acquisition — exactly the hard cases the QC app must survive.
- **Cons.** Substantially more machinery (ray casting / z-buffer, viewpoint planning, HPR operator)
  and not needed to validate the *generation → I/O → measurement* loop end-to-end first.

**Decision.** Step 1 samples the **full surface** (the union of all faces). Occlusion/viewpoint
realism is an explicit future refinement, layered as another `IPointSampler` (or a decorator over
the surface sampler) once the basic pipeline is proven.

---

## Decision for Step 1

| Aspect | Choice |
|---|---|
| Sampler | Per-triangle **area-weighted uniform** (`MeshSurfaceSampler`) |
| Density control | Target **points/mm²** → `nPoints = round(density · totalArea)` |
| In-triangle point | Barycentric **√-trick** for uniform distribution |
| Normal | Per-triangle geometric normal, oriented outward |
| Reproducibility | Single seedable `Random`; identical output for identical seed |
| Interface | `IPointSampler` (Strategy) so blue-noise / occlusion variants drop in later |

Tessellation of the `Brep` to a triangle `Mesh` is done with the Eyeshot kernel
(`Brep.Regen(tol)` → mesh access), with an STL round-trip as a documented fallback if direct mesh
access is unclear. The chord tolerance used for tessellation is the same `EyeParam.Tol.Brep`
already used to build the solids, keeping geometry and sampling consistent.

---

## Sources
- [A very simple method for approximate blue noise sampling on a triangle mesh — Alec Jacobson](https://www.alecjacobson.com/weblog/?p=4111)
- [A Survey of Blue-Noise Sampling and Its Applications (JCST 2015)](https://jianweiguo.net/publications/papers/2015_JCST_BNMeshSurvey_compress.pdf)
- [Efficient barycentric point sampling on meshes (arXiv:1708.07559)](https://arxiv.org/pdf/1708.07559)
- [Efficient and Flexible Sampling with Blue Noise Properties of Triangular Meshes](https://www.researchgate.net/publication/221792549_Efficient_and_Flexible_Sampling_with_Blue_Noise_Properties_of_Triangular_Meshes)
- [open3d.geometry.sample_points_poisson_disk (reference API)](https://www.open3d.org/docs/0.7.0/python_api/open3d.geometry.sample_points_poisson_disk.html)

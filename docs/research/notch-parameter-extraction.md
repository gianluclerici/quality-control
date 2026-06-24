# Research — Notch (Cope) Parameter Extraction from a Point Cloud

**Context.** Step 5.3 of the quality-control pipeline produces the dimensional verdict for a **notch**
(an end *cope*, macro `SCAI01`): from the scan points the segmentation already routed to the notch
(Step 5.1) we must measure its headline parameters — **length**, **depth**, **fillet radius** — and
judge each against the nominal within a tolerance band. This mirrors Step 5.2 (hole diameter) but the
notch is not a single primitive: it is a **2D contour extruded** through the web, so its surface is a
collection of **planar walls** plus one **cylindrical fillet**. Hence the handoff phrasing: parameters
"da fit piani + arco" (from plane fits + an arc fit).

What we know going in (the same inputs as the whole Step 5): the macro spec with the **nominal**
parameter values, the **nominal Brep**, and — crucially — the **re-derived cutter solids** for the
feature (`BeamFactory.BuildMachined`, Step 5.1). The cutter's boundary *is* the feature's surface, so
the cutter gives us the exact **orientation of every wall** (face normals) and the **extrusion axis**
(the cap-face normal). The cloud is already **ICP-aligned** to that nominal frame (Steps 3–4).

This note records the techniques considered for the two sub-problems — **plane fitting** and
**arc/circle fitting** — and how dimensions are derived from the fitted primitives, then states the
choice made for Step 5.3 and why.

What the cutter geometry looks like in practice (verified empirically on the demo IPE300 + `SCAI01`
A80 B60 C40 D60 E40 R10): the cope macro expands into **4 cutters** — two flange blocks (all planar)
and two web **contour extrusions** that carry the cope profile. The profile cutter has the **fillet**
as its only non-planar lateral face (a `TabulatedSurf`), the **back wall** plane n=(1,0,0) at x=80
(= `A`), the **depth wall** plane n=(0,1,0) at y=60 (= `B`), a slant wall, and two `PlanarSurf`
extrusion caps with n=(0,0,±1) (the **extrusion axis**). The notch cloud (~1.5k points at 0.5 pt/mm²)
lands on the back wall (median x **80.00**), the depth wall (median y **60.00**) and the fillet
(circle fit centre ≈ (70,50), radius ≈ **10**) — i.e. the three target parameters are directly
recoverable.

---

## Sub-problem A — fitting a planar wall

### A1. PCA / total-least-squares plane (full 6-DOF: normal + offset)  ⏸ not needed here
Build the 3×3 covariance of the points and take the eigenvector of the **smallest** eigenvalue as the
normal; the centroid fixes the offset. This is the orthogonal-distance (total-least-squares) plane and
is the textbook default when **the plane orientation is unknown**.

- **Pros.** No prior; one symmetric eigendecomposition; gives the best-fit normal too.
- **Cons.** Sensitive to outliers (a few stray points swing the smallest-eigenvalue direction); and it
  spends DOF estimating a normal we **already know exactly** from the cutter face. Estimating the
  normal from a small, noisy, possibly clipped wall patch is *less* accurate than reading it off the
  CAD cutter.

### A2. RANSAC plane  ⏸ deferred (overkill here)
Sample minimal triples, score inliers by distance, keep the best consensus, refit. The standard robust
choice when the **outlier fraction is high** and orientation is unknown.

- **Pros.** Very robust to gross outliers / mixed surfaces; the go-to for raw, unsegmented clouds.
- **Cons.** Non-deterministic without a fixed seed/scheme; slower; its main strength (finding an
  unknown plane amid outliers) is wasted once the cloud is already **segmented per feature** and the
  **normal is known**. Literature notes RANSAC's runtime scales with the outlier rate and it is "not
  completely free from the effect of outliers".

### A3. Robust **offset-only** fit with the known normal  ✅ chosen
Because the wall's unit normal `n̂` is taken from the cutter face, the plane has a **single** unknown —
its signed offset `d`. Each point contributes a scalar `tᵢ = n̂ · pᵢ`, and the plane offset is just a
**1D location estimate** of `{tᵢ}`. We take a **robust** location (median / trimmed mean) instead of
the mean, which rejects the scan's tail outliers for free and needs no iteration, no eigen-solver, no
RANSAC.

- **Pros.** Collapses a 6-DOF fit to a robust 1D median — trivially fast, deterministic, and *more*
  accurate than re-estimating the normal from a noisy wall patch. The known normal is the strongest
  possible prior. Naturally robust (median) without an IRLS loop.
- **Cons.** Relies on the cutter normal being correct (it is, by construction) and on the cloud being
  pre-segmented to the wall (it is, Step 5.1). Does not detect a *tilted* wall (a wrong realized
  angle) — but angle verification is out of scope for the first verdict and is the documented upgrade.

*Upgrade path:* if realized **angular** error must be checked, promote A3 to a one-step
**IRLS / Huber** re-weighting that also frees the normal (M-estimator), keeping the cutter normal as
the initial guess so it converges in 1–2 iterations and stays robust.

---

## Sub-problem B — fitting the fillet (arc / cylinder)

### B1. Full 3D cylinder fit (axis + radius, non-linear)  ⏸ not needed here
Estimate the axis from the spread of surface normals (covariance / Gauss map), then refine axis,
a point on it, and the radius with Levenberg–Marquardt. Necessary when the **axis is unknown**.

- **Pros.** General; handles arbitrary cylinder pose.
- **Cons.** Needs good initial values and per-point normals, iterative, can diverge on a thin arc
  patch. All of that to recover an axis we **already know** (the fillet is swept along the extrusion
  axis = the cutter's cap normal).

### B2. Free 2D circle fit (Kåsa, or geometric refinement) in the plane ⟂ to the known axis  ⏸ tried, rejected for the fillet
With the axis known, a fillet *could* be fit as a free **2D circle**: project the points onto the
profile plane and solve the linear Kåsa system `u²+v²+Du+Ev+F=0` (the `CircleFit` reused from the hole
in 5.2). This is what works well for the **hole** (a near-full circle).

- **But the cope fillet is only a quarter-arc**, and a short arc poorly constrains a free 2-DOF centre.
  Measured on the demo fillet: Kåsa returns **R ≈ 9.66** (the known small-arc bias toward a smaller
  radius), while a geometric (orthogonal-distance) refinement of the same points *diverges the other
  way* to **R ≈ 11.05** — the unconstrained least-squares circle is genuinely ambiguous on a coarsely
  faceted quarter-arc even though every point sits at radius ≈ 10.0–10.3 from the true centre. So a
  free circle fit is the wrong model for the fillet (it is right for the hole).

### B3. Tangent-constrained 1-parameter radius fit  ✅ chosen for the fillet
The fillet is **tangent to the two walls it rounds**, and we have already measured those walls and their
corner. For the right-angle cope corner this pins the centre to `corner + R·(û_back + û_depth)` (the û are
the inward unit normals), leaving a **single** unknown — the radius R. We minimise
`Σ(‖p − centre(R)‖ − R)²` by golden-section search.

- **Pros.** Well-conditioned (1 DOF, centre tied to the strongly-measured corner instead of floating on
  a short arc); deterministic; uses exactly the known geometry. Recovers **R ≈ 9.95** on the demo
  fillet (dev −0.05 mm), an order of magnitude better than either free fit.
- **Cons.** Assumes a right-angle filleted corner (true for the SCAI end-cope's main fillet). The
  general-angle version places the centre on the corner's **angle bisector** at distance `R/sin(θ/2)`;
  recorded as the generalisation. The hole keeps the linear Kåsa fit (full circle, well-conditioned).

### Isolating the fillet points
The fillet is tangent to the two adjacent walls, so points **on** those walls sit at the same radial
distance from the corner as the fillet — a naïve radial window grabs wall points too (confirmed in the
spike). The robust separation is **assign each point to its nearest nominal face**: points within a
small band of a wall plane belong to that wall; the remaining points near the corner (intersection of
the back and depth wall planes), off both walls, are the fillet. This "assign to nearest nominal
primitive, then fit" pipeline is the standard metrology pattern and reuses the cutter faces we already
have.

---

## Sub-problem C — turning fitted primitives into the three parameters

The cope is **open** on two sides (the beam end and the web edge), so not every dimension is an
intrinsic wall-to-wall distance. We exploit that the cloud and the cutter share the **same aligned
frame** and that the macro tells us which wall is which:

| Parameter | Nominal source | Measured from cloud |
|---|---|---|
| **Length** (`A`) | macro `A` | offset of the **back wall** (A3) along its known normal |
| **Depth** (`B`) | macro `B` | offset of the **depth wall** (A3) along its known normal |
| **Radius** (`R`) | macro `R` | radius of the **fillet** circle fit (B2) |

The two walls that bound the cope are identified as the planar lateral faces whose nominal offsets
match `A` and `B` (using the known input values to pick the right face — the same steer applied in
5.2: *"if you know the macro has radius 20, look for the radius-20 feature"*). The back and depth walls
are also exactly the two walls **tangent to the fillet** (they round into it), a frame-independent
cross-check.

**Known limitation (documented, not a defect).** Length and depth are measured as **positions in the
ICP-aligned frame**, so they inherit the registration accuracy (a translation bias shifts them);
radius is **intrinsic** (alignment-invariant). This matches Step 5.2's established convention (the hole
axis and projection also live in the aligned frame) and is adequate for a first verdict. The
alignment-invariant upgrade is to measure **feature-relative distances** — back wall → beam-end plane
for length, depth wall → flange edge for depth — by also fitting those reference planes (A3) from the
base bucket and taking plane-to-plane distances; recorded here as the future refinement.

---

## Decision for Step 5.3

| Aspect | Choice |
|---|---|
| Wall fit | **Offset-only, known normal from the cutter face** → robust 1D location (median) — A3 |
| Fillet fit | **Tangent-constrained 1-parameter radius** (centre pinned to the measured corner) — B3 |
| Point routing | **Assign each notch point to its nearest nominal face** (wall plane band; corner residual = fillet) |
| Length / Depth | Offset of the back / depth wall vs nominal `A` / `B`; walls picked by matching nominal offsets |
| Radius | Tangent-fillet radius vs nominal `R` |
| Verdict | `FeatureParameter.Judge` per parameter (symmetric band), aggregated in `FeatureInspectionReport` |
| Reuse | `Cholesky` (5.2), `FeatureParameter` / `FeatureInspectionReport` (5.2), cutter faces (5.1) |

**Rationale (one line).** Every primitive's orientation is already known *exactly* from the re-derived
cutter, so each fit collapses to its **minimal unknown** — a robust 1D offset for a wall, a linear 2D
circle for the fillet — which is cheaper, deterministic, and more accurate than the general
RANSAC/PCA/cylinder machinery the literature uses when orientation is unknown. We keep that general
machinery as the documented upgrade for the day the inputs are no longer trusted (unknown pose, gross
outliers, angular checks).

---

## Sources
- [Robust statistical approaches for local planar surface fitting in 3D laser scanning data (ISPRS J. 2014)](https://www.sciencedirect.com/science/article/pii/S0924271614001762)
- [Diagnostic-Robust PCA for plane fitting in laser data (IEEE)](https://ieeexplore.ieee.org/document/6997319/)
- [A Fast and Precise Plane Segmentation Framework for Indoor Point Clouds (Remote Sensing 2022)](https://www.mdpi.com/2072-4292/14/15/3519)
- [Tangram Vision — A Different Way To Think about Plane Fitting](https://www.tangramvision.com/blog/a-different-way-to-think-about-plane-fitting)
- [Fast Cylindrical Fitting Method Using Point Cloud's Normals Estimation (Math. Problems in Eng. 2018)](https://onlinelibrary.wiley.com/doi/10.1155/2018/8904653)
- [Estimation of Cylinder parameters from point clouds using Least Square Best Fit (IEEE)](https://ieeexplore.ieee.org/document/9826270/)
- [Robust line fitting with IRLS — D.A. Forsyth (course notes)](http://luthuli.cs.uiuc.edu/~daf/VisionCourseAssets/PDFSlideBlock/robustline.pdf)
- [Enhancing performance in the presence of outliers with redescending M-estimators (PMC)](https://pmc.ncbi.nlm.nih.gov/articles/PMC11169389/)
- [Automatic extraction of bridge dimensional information using 3D point cloud data (2025)](https://www.sciencedirect.com/science/article/pii/S1226798825004271)
- [Edge feature extraction from point clouds via plane segmentation & plane intersection (ACM 2024)](https://dl.acm.org/doi/10.1145/3705391.3705401)
- I. Kåsa, "A circle fitting procedure and its error analysis" (1976); Pratt (1987) & Taubin (1991) algebraic circle fits — standard low-bias alternatives.

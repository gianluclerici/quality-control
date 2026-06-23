# Research — 3D-Camera (Depth Sensor) Noise Modeling

**Context.** After sampling the nominal `Brep` surface (see [mesh-surface-sampling.md](mesh-surface-sampling.md)),
Step 1 perturbs the points so the synthetic cloud behaves like a **real 3D-camera scan** rather than
a perfect CAD surface. The point of the QC pipeline is to measure *with what tolerance* the machined
parameters are respected; if the synthetic input were noise-free, the measurement step would have
nothing to be robust against and the tolerance numbers would be meaningless. So the noise model must
reproduce the *dominant* error characteristics of optical depth sensors.

This note records the noise physics, the models considered, and the choice for Step 1.

---

## What real depth cameras actually do

Optical depth sensors (structured-light like Kinect v1 / RealSense, or ToF) exhibit two dominant,
well-characterized noise components, plus sparse gross errors:

- **Axial noise** — error **along the viewing ray** (the camera Z axis), i.e. in the measured
  *range*. This is the big one for surface fitting because it displaces points off the true surface.
  It grows with distance and with the surface's incidence angle to the camera.
- **Lateral noise** — error **in the image plane** (x/y), i.e. the in-plane position of each sample
  jitters. Smaller in metric terms at close range; grows with distance and strongly near grazing
  angles (edge fattening / "edge noise").
- **Outliers / dropouts** — a small fraction of points are gross errors (mixed pixels at depth
  discontinuities, multipath, specular dropout). Sparse but they break naïve least-squares fits.

### Canonical model — Nguyen, Izadi & Lovell (2012)
The reference empirical Kinect model (z in **metres**, θ = incidence angle in **radians**):

- **Axial:** `σ_z(z, θ) = 0.0012 + 0.0019·(z − 0.4)²  +  (0.0001/√z)·θ² / (π/2 − θ)²`  [metres]
  (the first two terms dominate below ~60°; the angle term blows up only near grazing).
- **Lateral:** `σ_L(θ) = 0.8 + 0.035 · θ / (π/2 − θ)`  [**pixels**], converted to metric by
  multiplying by `z / f` (focal length f).

Key takeaways that any faithful model must keep:
- **axial σ rises ~quadratically with depth**, lateral ~linearly;
- **both rise with incidence angle**, sharply near grazing;
- at a typical working range, axial σ is on the order of **~1 mm** (Nguyen reports roughly
  0.5–3 mm across distance; structured-light survey data agree).

More recent work (e.g. the 2024 "Enhancement of 3D Camera Synthetic Training Data with Noise
Models") fits per-camera degree-2 polynomials in (z, θ) for axial (mm) and lateral (px) noise, which
is the same shape with device-specific coefficients.

---

## Models considered

### 1. Isotropic Gaussian on each coordinate  ❌ rejected
Add `N(0, σ)` independently to x, y, z. Simplest possible, but **physically wrong**: real error is
*anisotropic and oriented along the viewing ray*, not isotropic in world axes. It would smear points
tangentially as much as normally, which is not what a depth sensor does and would mislead the
tolerance measurement.

### 2. Gaussian range noise along the surface normal  ✅ chosen for Step 1
Perturb each point along **its surface normal** by `N(0, σ)`. Because we sample the full surface
(no single fixed viewpoint yet), the surface normal is the best available proxy for the viewing ray:
the dominant axial error displaces points *perpendicular to the surface*, which is exactly what
distorts a fitted plane/cylinder and therefore what the measurement step must tolerate.

- **σ model.** Step 1 uses a **constant σ** (default ~0.1 mm; configurable via `--sigma`), because
  the generator samples at an effectively fixed standoff and we want a controllable, well-understood
  baseline. The interface is built so a **depth/angle-dependent σ** (the Nguyen form above) can be
  substituted without touching callers.
- **Reproducibility.** Single **seedable** RNG; Gaussian via Box–Muller or `NextDouble` transforms.
  Fixed seed ⇒ identical cloud (required by the tests: mean ≈ 0, sample σ ≈ requested σ).

**Why this over the full Nguyen model for Step 1.** The normal-aligned Gaussian captures the
*dominant, fit-relevant* (axial) error with one intuitive knob, is trivially testable, and matches
how the literature's "synthetic training data" pipelines start. The full depth/angle model adds
realism that only matters once we simulate a real viewpoint (occlusion step) — at which point the
true viewing ray and depth are available and the σ(z, θ) law becomes meaningful. Adding it now,
under full-surface sampling, would be fitting coefficients to a viewing geometry we are not yet
simulating.

### 3. Full axial + lateral + outlier sensor model  ⏸ deferred
Oriented anisotropic noise: axial `σ_z(z,θ)` along the ray + lateral `σ_L(z,θ)` in the image plane +
a small outlier fraction. This is the realistic target and pairs naturally with the
**virtual single-viewpoint scan** from the sampling note (which supplies the real ray direction,
depth z, and incidence angle θ). Deferred together with occlusion.

---

## Decision for Step 1

| Aspect | Choice |
|---|---|
| Model | **Gaussian range noise along the surface normal** (`GaussianRangeNoise`) |
| σ | Constant, default ~0.1 mm, CLI-configurable (`--sigma`) |
| Direction | Per-point surface normal (proxy for the viewing ray under full-surface sampling) |
| Outliers | None in Step 1 (documented future addition) |
| Reproducibility | Seedable RNG; fixed seed ⇒ identical output |
| Interface | `INoiseModel` (Strategy) so depth/angle σ and lateral/outlier terms drop in later |

**Planned upgrade path:** when the occlusion / single-viewpoint sampler lands, replace the constant
σ with the Nguyen σ_z(z, θ) (axial, along the true ray), add the lateral term in the image plane,
and add a small outlier fraction — all behind the existing `INoiseModel` interface.

---

## Sources
- [Nguyen, Izadi, Lovell — *Modeling Kinect Sensor Noise for Improved 3D Reconstruction and Tracking* (3DIMPVT 2012, PDF)](https://users.cecs.anu.edu.au/~nguyen/papers/conferences/Nguyen2012-ModelingKinectSensorNoise.pdf)
- [*Enhancement of 3D Camera Synthetic Training Data with Noise Models* (arXiv:2402.16514)](https://arxiv.org/html/2402.16514v1)
- [*Noise in Structured-Light Stereo Depth Cameras: Modeling and its Applications* (arXiv:1505.01936)](https://arxiv.org/abs/1505.01936)
- [*Characterizations of Noise in Kinect Depth Images: A Review*](https://www.researchgate.net/publication/261601243_Characterizations_of_Noise_in_Kinect_Depth_Images_A_Review)
- [*Improving 3D Reconstruction Through RGB-D Sensor Noise Modeling* (Sensors 2025)](https://doi.org/10.3390/s25030950)

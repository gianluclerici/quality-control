using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using devDept.Eyeshot;
using devDept.Eyeshot.Control;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.QualityControl.Core.Io;
using Ficep.QualityControl.Core.Measurement;
using Ficep.QualityControl.Core.Registration;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.App;

/// <summary>
/// The quality-control viewport shell: a thin WinForms frontend over the headless
/// <c>Ficep.QualityControl.Core</c>. It loads a nominal <see cref="Brep"/> reference (STEP) and a
/// scanned point cloud (PLY), shows them overlaid in a single Eyeshot <see cref="Design"/> viewport,
/// then drives the Core pipeline: <b>Allinea</b> registers the scan onto the nominal (ICP) and
/// <b>Misura</b> computes the signed deviation map, colours the cloud through a <see cref="Legend"/>
/// and reports the conformity verdict. All geometry/measurement logic lives in Core; this form only
/// adds results to the scene and drives the camera.
/// </summary>
public sealed class MainForm : Form
{
    private const double NominalChordToleranceMm = 0.2; // tessellation chord error for the reference surface

    private readonly Design _design;
    private readonly ToolStripStatusLabel _status;
    private readonly ToolStripTextBox _toleranceBox;

    private readonly BrepImporter _brepImporter = new();
    private readonly IPointCloudReader _plyReader = new PlyReader();

    // QC state, kept between actions.
    private readonly List<Brep> _nominalBreps = new();
    private IReadOnlyList<SurfaceSample>? _scanSamples;
    private NominalSurface? _nominal;                       // built lazily from _nominalBreps
    private RigidTransform _alignment = RigidTransform.Identity;
    private Entity? _cloudEntity;                           // the currently displayed scan cloud
    private Legend? _legend;                                // viewport colour-bar key, attached lazily

    public MainForm()
    {
        Text = "Ficep Quality Control — Viewport";
        Width = 1100;
        Height = 750;
        StartPosition = FormStartPosition.CenterScreen;

        _design = new Design
        {
            Dock = DockStyle.Fill,
            Size = new Size(ClientSize.Width, ClientSize.Height),
        };

        // Eyeshot's Design control implements ISupportInitialize. The designer normally emits the
        // BeginInit/EndInit pair; building the form in code, we must do it ourselves.
        ((ISupportInitialize)_design).BeginInit();
        SuspendLayout();

        // Built in code (not via the designer) the Viewports collection starts empty, so the first
        // paint throws ArgumentOutOfRangeException in Workspace.AdjustNearAndFarPlanes (it indexes
        // Viewports[0]). We mirror exactly what the WinForms designer serializes (see the working
        // Ficep.RobServer\Form1.Designer.cs): a real Viewport with a non-zero Size added to the
        // Viewports collection between BeginInit/EndInit. Relying on InitializeViewports() alone is
        // not enough here — an empty/zero-size viewport gets dropped during handle creation/layout,
        // leaving the collection empty again by the time the first WM_PAINT runs.
        var viewport = new Viewport
        {
            Location = new System.Drawing.Point(0, 0),
            Size = new Size(ClientSize.Width, ClientSize.Height),
        };
        _design.Viewports.Clear();
        _design.Viewports.Add(viewport);

        var toolbar = new ToolStrip();
        var loadNominal = new ToolStripButton("Carica nominale (STEP)") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        var loadScan = new ToolStripButton("Carica scan (PLY)") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        var align = new ToolStripButton("Allinea (ICP)") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        var measure = new ToolStripButton("Misura") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        var clear = new ToolStripButton("Pulisci") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _toleranceBox = new ToolStripTextBox { Text = "1.0", ToolTipText = "Tolleranza ± (mm)", Width = 50 };
        loadNominal.Click += (_, _) => LoadNominal();
        loadScan.Click += (_, _) => LoadScan();
        align.Click += (_, _) => RunAlign();
        measure.Click += (_, _) => RunMeasure();
        clear.Click += (_, _) => ClearScene();
        toolbar.Items.AddRange(new ToolStripItem[]
        {
            loadNominal, new ToolStripSeparator(), loadScan, new ToolStripSeparator(),
            align, measure, new ToolStripLabel("Toll. ±mm:"), _toleranceBox, new ToolStripSeparator(), clear,
        });

        var statusStrip = new StatusStrip();
        _status = new ToolStripStatusLabel("Pronto. Carica un nominale STEP e/o uno scan PLY.");
        statusStrip.Items.Add(_status);

        // Order matters: Fill control first, then docked top/bottom bars frame it correctly.
        Controls.Add(_design);
        Controls.Add(toolbar);
        Controls.Add(statusStrip);

        ((ISupportInitialize)_design).EndInit();
        ResumeLayout(false);
    }

    /// <summary>
    /// Belt-and-suspenders guard against the startup
    /// <see cref="System.ArgumentOutOfRangeException"/> in
    /// <c>Workspace.AdjustNearAndFarPlanes</c>. That method indexes <c>Viewports[0]</c> during the
    /// first paint; if the collection is ever empty at that point the app crashes. The constructor
    /// already adds an explicit viewport (mirroring the working designer-generated setup), but the
    /// Eyeshot guidance is that handle-touching viewport setup belongs after the native handle
    /// exists — which is here, in OnLoad, before the first WM_PAINT. We re-ensure one viewport.
    /// </summary>
    protected override void OnLoad(EventArgs e)
    {
        if (_design.Viewports.Count == 0)
            _design.InitializeViewports();

        base.OnLoad(e);
    }

    /// <summary>Loads the nominal solid(s) from a STEP file and adds them to the scene as Breps.</summary>
    private void LoadNominal()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Apri il nominale (STEP)",
            Filter = "STEP files (*.step;*.stp)|*.step;*.stp|Tutti i file (*.*)|*.*",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        RunGuarded(() =>
        {
            IReadOnlyList<Brep> breps = _brepImporter.Import(dlg.FileName);
            if (breps.Count == 0)
            {
                _status.Text = "Il file STEP non contiene solidi Brep.";
                return;
            }

            _nominalBreps.Clear();
            _nominal = null;            // invalidate the cached query surface
            _alignment = RigidTransform.Identity;

            foreach (Brep brep in breps)
            {
                brep.ColorMethod = colorMethodType.byEntity;
                brep.Color = Color.FromArgb(210, 210, 215); // neutral grey for the reference body
                _design.Entities.Add(brep);
                _nominalBreps.Add(brep);
            }

            _design.Entities.Regen(); // tessellate the freshly-imported Breps for display
            _design.ZoomFit();
            _design.Invalidate();
            _status.Text = $"Nominale caricato: {breps.Count} solido/i — {System.IO.Path.GetFileName(dlg.FileName)}";
        });
    }

    /// <summary>Loads a scanned point cloud from a PLY file and adds it as a <see cref="FastPointCloud"/>.</summary>
    private void LoadScan()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Apri lo scan (PLY)",
            Filter = "PLY point clouds (*.ply)|*.ply|Tutti i file (*.*)|*.*",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        RunGuarded(() =>
        {
            IReadOnlyList<SurfaceSample> samples = _plyReader.Read(dlg.FileName);
            if (samples.Count == 0)
            {
                _status.Text = "Lo scan PLY non contiene punti.";
                return;
            }

            _scanSamples = samples;
            _alignment = RigidTransform.Identity; // a fresh scan starts unaligned
            HideLegend();
            ShowCloud(BuildFastCloud(samples, RigidTransform.Identity, Color.DodgerBlue));

            _design.ZoomFit();
            _design.Invalidate();
            _status.Text = $"Scan caricato: {samples.Count:N0} punti — {System.IO.Path.GetFileName(dlg.FileName)}";
        });
    }

    /// <summary>Registers the scan onto the nominal (point-to-plane ICP) and shows the aligned cloud.</summary>
    private void RunAlign()
    {
        RunGuarded(() =>
        {
            if (!TryGetScanAndNominal(out IReadOnlyList<SurfaceSample> scan, out NominalSurface nominal))
                return;

            RegistrationResult result = new IcpRegistration().Register(scan, nominal);
            _alignment = result.Transform;

            HideLegend();
            ShowCloud(BuildFastCloud(scan, _alignment, Color.DodgerBlue));
            _design.Invalidate();

            _status.Text =
                $"Allineamento ICP: RMS {result.RmsErrorMm:F3} mm in {result.Iterations} iter " +
                $"(converged={result.Converged}).";
        });
    }

    /// <summary>Computes the signed deviation map against the nominal and colours the cloud through a legend.</summary>
    private void RunMeasure()
    {
        RunGuarded(() =>
        {
            if (!TryGetScanAndNominal(out IReadOnlyList<SurfaceSample> scan, out NominalSurface nominal))
                return;

            ToleranceBand band = ToleranceBand.Symmetric(ParseToleranceMm());
            DeviationReport report = new DeviationMeasurement().Measure(scan, nominal, _alignment, band);

            ShowCloud(BuildColouredCloud(report, band));
            _design.Invalidate();

            DeviationStatistics s = report.Statistics;
            string verdict = report.IsConform ? "CONFORME" : "NON CONFORME";
            _status.Text =
                $"{verdict} — entro ±{band.UpperMm:0.###} mm: {report.ConformanceRatio:P1} " +
                $"({report.InToleranceCount:N0}/{report.Deviations.Count:N0}). " +
                $"RMS {s.RmsMm:F3} · media {s.MeanMm:+0.000;-0.000} · " +
                $"min {s.MinMm:+0.000;-0.000} · max {s.MaxMm:+0.000;-0.000} · |max| {s.MaxAbsMm:F3} mm.";
        });
    }

    /// <summary>Builds (or returns the cached) queryable nominal surface from the loaded Breps.</summary>
    private NominalSurface EnsureNominalSurface()
    {
        if (_nominal is not null)
            return _nominal;

        var meshes = new List<Mesh>(_nominalBreps.Count);
        foreach (Brep brep in _nominalBreps)
            meshes.Add(BrepTessellator.ToMesh(brep, NominalChordToleranceMm));

        _nominal = NominalSurface.FromMeshes(meshes);
        return _nominal;
    }

    /// <summary>Validates that both a scan and a nominal are loaded; reports to the status bar if not.</summary>
    private bool TryGetScanAndNominal(out IReadOnlyList<SurfaceSample> scan, out NominalSurface nominal)
    {
        scan = _scanSamples!;
        nominal = null!;
        if (_scanSamples is null || _scanSamples.Count == 0)
        {
            _status.Text = "Carica prima uno scan PLY.";
            return false;
        }
        if (_nominalBreps.Count == 0)
        {
            _status.Text = "Carica prima un nominale STEP.";
            return false;
        }
        nominal = EnsureNominalSurface();
        return true;
    }

    private double ParseToleranceMm()
    {
        if (double.TryParse(_toleranceBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double t) && t > 0)
            return t;
        if (double.TryParse(_toleranceBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out t) && t > 0)
            return t;
        return 1.0; // sensible default if the field is blank/garbage
    }

    /// <summary>Builds a single-colour fast cloud from the samples, transformed by <paramref name="t"/>.</summary>
    private static FastPointCloud BuildFastCloud(IReadOnlyList<SurfaceSample> samples, RigidTransform t, Color color)
    {
        var coords = new float[samples.Count * 3];
        for (int i = 0; i < samples.Count; i++)
        {
            Point3D p = t.Apply(samples[i].Position);
            coords[i * 3] = (float)p.X;
            coords[i * 3 + 1] = (float)p.Y;
            coords[i * 3 + 2] = (float)p.Z;
        }
        return new FastPointCloud(coords)
        {
            ColorMethod = colorMethodType.byEntity,
            Color = color,
        };
    }

    /// <summary>
    /// Builds a per-point coloured cloud from the deviation report, mapping each signed deviation onto a
    /// red→blue legend centred on the tolerance band, and configures the viewport <see cref="Legend"/>
    /// to match (same idiom as Eyeshot's ComputeDistance sample).
    /// </summary>
    private PointCloud BuildColouredCloud(DeviationReport report, ToleranceBand band)
    {
        var items = Legend.RedToBlue9;
        Legend legend = EnsureLegend();
        legend.Items = items;
        legend.SetRange(band.LowerMm, band.UpperMm); // Min/Max + recompute the per-item Values
        legend.Title = "Deviazione";
        legend.Subtitle = "mm  (+ esterno / − interno)";
        legend.Visible = true; // the default FormatString already shows signed mm (+/−)

        Color[] palette = new Color[items.Length];
        for (int i = 0; i < palette.Length; i++)
            palette[i] = items[i].Color;

        IReadOnlyList<PointDeviation> dev = report.Deviations;
        var cloud = new PointCloud(dev.Count, PointCloud.natureType.Multicolor, 2);
        double min = band.LowerMm, span = band.UpperMm - band.LowerMm;
        for (int i = 0; i < dev.Count; i++)
        {
            PointDeviation d = dev[i];
            int bin = span > 0 ? (int)((d.SignedDistanceMm - min) / span * palette.Length) : 0;
            bin = Math.Clamp(bin, 0, palette.Length - 1);
            Color c = palette[bin];
            cloud.Vertices[i] = new PointRGB((float)d.Point.X, (float)d.Point.Y, (float)d.Point.Z, c.R, c.G, c.B);
        }
        return cloud;
    }

    /// <summary>
    /// Returns the viewport's deviation colour-bar, creating and attaching it on first use. The Design
    /// control is built in code, so (exactly like <c>Viewports</c>) the viewport's
    /// <see cref="Viewport.Legends"/> array starts empty: unless we add a <see cref="Legend"/>
    /// ourselves the colour map has no visible key. We attach a single legend (default position/size,
    /// as the ComputeDistance sample relies on) to the viewport, hidden until the first measurement.
    /// </summary>
    private Legend EnsureLegend()
    {
        if (_legend is not null)
            return _legend;

        _legend = new Legend { Visible = false };
        // Workspace.Legends is read-only in this Eyeshot build; the settable array is on the Viewport.
        _design.Viewports[0].Legends = new[] { _legend };
        return _legend;
    }

    private void HideLegend()
    {
        if (_legend is not null)
            _legend.Visible = false;
    }

    /// <summary>Replaces the displayed scan cloud with <paramref name="entity"/> and regenerates it.</summary>
    private void ShowCloud(Entity entity)
    {
        if (_cloudEntity is not null)
            _design.Entities.Remove(_cloudEntity);
        _cloudEntity = entity;
        _design.Entities.Add(entity);
        _design.Entities.Regen();
    }

    private void ClearScene()
    {
        _design.Entities.Clear();
        _nominalBreps.Clear();
        _nominal = null;
        _scanSamples = null;
        _cloudEntity = null;
        _alignment = RigidTransform.Identity;
        HideLegend();
        _design.Invalidate();
        _status.Text = "Scena svuotata.";
    }

    /// <summary>Runs a scene-mutating action, surfacing any failure to the user instead of crashing.</summary>
    private void RunGuarded(Action action)
    {
        try
        {
            Cursor = Cursors.WaitCursor;
            action();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = "Errore: " + ex.Message;
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }
}

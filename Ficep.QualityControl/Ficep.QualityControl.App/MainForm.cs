using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using devDept.Eyeshot;
using devDept.Eyeshot.Control;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.QualityControl.Core.Features;
using Ficep.QualityControl.Core.Generation;
using Ficep.QualityControl.Core.Io;
using Ficep.QualityControl.Core.Measurement;
using Ficep.QualityControl.Core.Model;
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
    private readonly ToolStripTextBox _densityBox;
    private readonly ToolStripTextBox _sigmaBox;
    private readonly ToolStripTextBox _seedBox;
    private readonly ListView _featureList;                 // left panel: one row per measurable feature
    private readonly ListView _paramGrid;                   // left panel: selected feature's parameters

    private readonly BrepImporter _brepImporter = new();
    private readonly IPointCloudReader _plyReader = new PlyReader();
    private readonly BeamFactory _beamFactory = new();
    private readonly PieceInspector _inspector = new();

    // QC state, kept between actions.
    private readonly List<Brep> _nominalBreps = new();
    private IReadOnlyList<SurfaceSample>? _scanSamples;
    private NominalSurface? _nominal;                       // built lazily from _nominalBreps
    private RigidTransform _alignment = RigidTransform.Identity;
    private Entity? _cloudEntity;                           // the currently displayed scan cloud
    private Legend? _legend;                                // viewport colour-bar key, attached lazily

    // Feature inspection state (populated when a macro list accompanies the loaded STEP).
    private MachinedBeam? _machined;                        // nominal solid + cutters, re-derived from the macros
    private IReadOnlyList<MacroSpec>? _macros;              // nominal macro list
    private IReadOnlyList<FeatureInspectionReport>? _nominalFeatures; // nominal parameters, shown before measuring
    private IReadOnlyDictionary<int, FeatureInspectionReport>? _measuredFeatures; // by FeatureDescriptor.Id, after Misura

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
        var genScan = new ToolStripButton("Genera scan da nominale") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        var align = new ToolStripButton("Allinea (ICP)") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        var measure = new ToolStripButton("Misura") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        var clear = new ToolStripButton("Pulisci") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _toleranceBox = new ToolStripTextBox { Text = "1.0", ToolTipText = "Tolleranza ± (mm)", Width = 45 };
        _densityBox = new ToolStripTextBox { Text = "1.0", ToolTipText = "Densità campionamento (punti/mm²)", Width = 40 };
        _sigmaBox = new ToolStripTextBox { Text = "0.1", ToolTipText = "Rumore σ lungo la normale (mm)", Width = 40 };
        _seedBox = new ToolStripTextBox { Text = "1234", ToolTipText = "Seed RNG (vuoto = casuale)", Width = 50 };
        loadNominal.Click += (_, _) => LoadNominal();
        loadScan.Click += (_, _) => LoadScan();
        genScan.Click += (_, _) => GenerateScanFromNominal();
        align.Click += (_, _) => RunAlign();
        measure.Click += (_, _) => RunMeasure();
        clear.Click += (_, _) => ClearScene();
        toolbar.Items.AddRange(new ToolStripItem[]
        {
            loadNominal, new ToolStripSeparator(),
            loadScan, genScan,
            new ToolStripLabel("dens:"), _densityBox, new ToolStripLabel("σmm:"), _sigmaBox,
            new ToolStripLabel("seed:"), _seedBox, new ToolStripSeparator(),
            align, measure, new ToolStripLabel("Toll. ±mm:"), _toleranceBox, new ToolStripSeparator(), clear,
        });

        var statusStrip = new StatusStrip();
        _status = new ToolStripStatusLabel("Pronto. Carica un nominale STEP e/o uno scan PLY.");
        statusStrip.Items.Add(_status);

        // Left dock: the feature inspector. A vertical split (Panel1 = feature list, Panel2 = the selected
        // feature's parameter table) sits left of the viewport. Both halves are filled by ListViews so the
        // panel reads like the headless `inspect` report: pick a feature, see its nominal (and, once
        // measured, the scan-fitted) parameters side by side.
        _featureList = new ListView
        {
            Dock = DockStyle.Fill,
            View = System.Windows.Forms.View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
        };
        _featureList.Columns.Add("Feature", 200);
        _featureList.Columns.Add("Esito", 90);
        _featureList.SelectedIndexChanged += (_, _) => RenderSelectedFeature();

        _paramGrid = new ListView
        {
            Dock = DockStyle.Fill,
            View = System.Windows.Forms.View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
        };
        _paramGrid.Columns.Add("Parametro", 90);
        _paramGrid.Columns.Add("Nom (mm)", 70);
        _paramGrid.Columns.Add("Mis (mm)", 70);
        _paramGrid.Columns.Add("Dev (mm)", 70);
        _paramGrid.Columns.Add("Toll ±", 60);
        _paramGrid.Columns.Add("Esito", 60);

        var featureSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        featureSplit.Panel1.Controls.Add(_featureList);
        featureSplit.Panel1.Controls.Add(new Label
        {
            Text = "Features", Dock = DockStyle.Top, Height = 20, TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font, FontStyle.Bold),
        });
        featureSplit.Panel2.Controls.Add(_paramGrid);
        featureSplit.Panel2.Controls.Add(new Label
        {
            Text = "Parametri", Dock = DockStyle.Top, Height = 20, TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font, FontStyle.Bold),
        });

        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 320,
            FixedPanel = FixedPanel.Panel1,
        };
        mainSplit.Panel1.Controls.Add(featureSplit);
        mainSplit.Panel2.Controls.Add(_design);

        // Order matters: Fill control first, then docked top/bottom bars frame it correctly.
        Controls.Add(mainSplit);
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
            ResetFeatureInspection();

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

            string macroNote = TryLoadFeatures(dlg.FileName);
            _status.Text = $"Nominale caricato: {breps.Count} solido/i — {System.IO.Path.GetFileName(dlg.FileName)}. {macroNote}";
        });
    }

    /// <summary>
    /// Looks for the macro list that accompanies the STEP (same base name, e.g. <c>lavorato.step</c> →
    /// <c>lavorato.macros.json</c>, then <c>lavorato.json</c>). When found, re-derives the machined piece
    /// and fills the feature panel with the nominal parameters; when missing, warns the user (without
    /// crashing) and leaves the panel empty — the deviation colour map still works without it.
    /// Returns a short note for the status bar.
    /// </summary>
    private string TryLoadFeatures(string stepPath)
    {
        string? macrosPath = FindMacrosFile(stepPath);
        if (macrosPath is null)
        {
            string expected = System.IO.Path.GetFileNameWithoutExtension(stepPath) + ".macros.json";
            MessageBox.Show(this,
                $"Nessun file macro trovato accanto allo STEP (atteso «{expected}»).\n\n" +
                "La mappa di deviazione resta disponibile, ma senza le macro non posso elencare le " +
                "feature né misurarne i parametri (fori, scassi).",
                "Macro non trovate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return "Macro non trovate: pannello feature non disponibile.";
        }

        try
        {
            PieceSpec piece = PieceSpecSerializer.Read(macrosPath);
            _macros = piece.Macros;
            _machined = _beamFactory.BuildMachined(piece);
            _nominalFeatures = _inspector.DescribeNominal(_machined, _macros, BuildInspectionOptions());
            PopulateFeatureList();
            return $"{_nominalFeatures.Count} feature da {System.IO.Path.GetFileName(macrosPath)}.";
        }
        catch (Exception ex)
        {
            ResetFeatureInspection();
            MessageBox.Show(this,
                $"Macro trovate ({System.IO.Path.GetFileName(macrosPath)}) ma non caricabili:\n{ex.Message}",
                "Macro non valide", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return "Macro non valide: pannello feature non disponibile.";
        }
    }

    /// <summary>The macro file beside <paramref name="stepPath"/> (<c>.macros.json</c> preferred, then <c>.json</c>), or null.</summary>
    private static string? FindMacrosFile(string stepPath)
    {
        string dir = System.IO.Path.GetDirectoryName(stepPath) ?? ".";
        string baseName = System.IO.Path.GetFileNameWithoutExtension(stepPath);
        foreach (string suffix in new[] { ".macros.json", ".json" })
        {
            string candidate = System.IO.Path.Combine(dir, baseName + suffix);
            if (System.IO.File.Exists(candidate))
                return candidate;
        }
        return null;
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
            ClearMeasuredFeatures();              // a new scan invalidates any previous feature measurements
            HideLegend();
            ShowCloud(BuildFastCloud(samples, RigidTransform.Identity, Color.DodgerBlue));

            _design.ZoomFit();
            _design.Invalidate();
            _status.Text = $"Scan caricato: {samples.Count:N0} punti — {System.IO.Path.GetFileName(dlg.FileName)}";
        });
    }

    /// <summary>
    /// Synthesises a scan point cloud from the already-loaded nominal Brep(s) using the density / σ / seed
    /// entered in the toolbar (same pipeline as the offline generator), and shows it as the current scan.
    /// </summary>
    private void GenerateScanFromNominal()
    {
        RunGuarded(() =>
        {
            if (_nominalBreps.Count == 0)
            {
                _status.Text = "Carica prima un nominale STEP da cui generare lo scan.";
                return;
            }

            GenerationOptions options = ParseGenerationOptions();
            var generator = new ScanGenerator(NominalChordToleranceMm);

            // One nominal may hold several solids; sample each with a distinct-but-reproducible seed offset.
            var samples = new List<SurfaceSample>();
            int seedOffset = 0;
            foreach (Brep brep in _nominalBreps)
            {
                samples.AddRange(generator.Sample(brep, options, seedOffset));
                seedOffset += 1000;
            }

            if (samples.Count == 0)
            {
                _status.Text = "La generazione non ha prodotto punti (nominale vuoto?).";
                return;
            }

            _scanSamples = samples;
            _alignment = RigidTransform.Identity; // generated in the nominal frame; align is a no-op refinement
            ClearMeasuredFeatures();              // a new scan invalidates any previous feature measurements
            HideLegend();
            ShowCloud(BuildFastCloud(samples, RigidTransform.Identity, Color.DodgerBlue));

            _design.ZoomFit();
            _design.Invalidate();
            string seedText = options.Seed.HasValue ? options.Seed.Value.ToString(CultureInfo.InvariantCulture) : "casuale";
            _status.Text =
                $"Scan generato: {samples.Count:N0} punti (densità {options.DensityPerMm2:0.###} pt/mm², " +
                $"σ {options.SigmaMm:0.###} mm, seed {seedText}).";
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
            string featureNote = MeasureFeatures(scan);
            _status.Text =
                $"{verdict} — entro ±{band.UpperMm:0.###} mm: {report.ConformanceRatio:P1} " +
                $"({report.InToleranceCount:N0}/{report.Deviations.Count:N0}). " +
                $"RMS {s.RmsMm:F3} · media {s.MeanMm:+0.000;-0.000} · " +
                $"min {s.MinMm:+0.000;-0.000} · max {s.MaxMm:+0.000;-0.000} · |max| {s.MaxAbsMm:F3} mm.{featureNote}";
        });
    }

    /// <summary>
    /// Runs the per-feature dimensional inspection (holes, notches) when a macro list accompanied the STEP,
    /// reusing the GUI's current ICP <see cref="_alignment"/> so the feature measurement and the deviation
    /// colour map share the same registration. Fills the parameter table with measured-vs-nominal values.
    /// A failure here is non-fatal: the colour map already succeeded, so we just note it in the status bar.
    /// Returns a short status suffix.
    /// </summary>
    private string MeasureFeatures(IReadOnlyList<SurfaceSample> scan)
    {
        if (_machined is null || _macros is null)
            return string.Empty; // no macros loaded ⇒ feature panel stays empty (deviation map still works)

        try
        {
            PieceInspectionReport piece = _inspector.Inspect(
                _machined, _macros, scan, _beamFactory.BrepTolerance, _alignment, BuildInspectionOptions());

            _measuredFeatures = piece.Features.ToDictionary(f => f.Feature.Id);
            RefreshFeatureList();
            RenderSelectedFeature();

            int ok = piece.Features.Count(f => f.InTolerance);
            return $" Feature: {ok}/{piece.Features.Count} in tolleranza" +
                   (piece.Features.Count > 0 ? (piece.InTolerance ? " (CONFORME)." : " (NON CONFORME).") : ".");
        }
        catch (Exception ex)
        {
            return $" (misura feature non riuscita: {ex.Message})";
        }
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

    // --- Feature inspection panel -------------------------------------------------

    /// <summary>The inspection knobs derived from the toolbar tolerance, shared by nominal display and measure.</summary>
    private InspectionOptions BuildInspectionOptions()
    {
        double tol = ParseToleranceMm();
        return new InspectionOptions
        {
            HoleToleranceMm = tol,
            NotchTolerance = new NotchTolerance(tol, tol, tol),
        };
    }

    /// <summary>Clears every feature-inspection state (macros, nominal list, measurements) and empties the panel.</summary>
    private void ResetFeatureInspection()
    {
        _machined = null;
        _macros = null;
        _nominalFeatures = null;
        _measuredFeatures = null;
        _featureList.Items.Clear();
        _paramGrid.Items.Clear();
    }

    /// <summary>Drops only the measured results, keeping the nominal feature list; resets verdicts to "—".</summary>
    private void ClearMeasuredFeatures()
    {
        _measuredFeatures = null;
        if (_nominalFeatures is not null)
        {
            RefreshFeatureList();
            RenderSelectedFeature();
        }
    }

    /// <summary>Fills the feature list from the nominal features (one row per measurable feature) and selects the first.</summary>
    private void PopulateFeatureList()
    {
        _featureList.Items.Clear();
        _paramGrid.Items.Clear();
        if (_nominalFeatures is null)
            return;

        foreach (FeatureInspectionReport f in _nominalFeatures)
        {
            var item = new ListViewItem(f.Feature.Label) { Tag = f.Feature.Id };
            item.SubItems.Add("—");
            _featureList.Items.Add(item);
        }
        if (_featureList.Items.Count > 0)
            _featureList.Items[0].Selected = true;
    }

    /// <summary>Updates the per-feature verdict column from the latest measurements (label column is unchanged).</summary>
    private void RefreshFeatureList()
    {
        foreach (ListViewItem item in _featureList.Items)
        {
            string verdict = "—";
            if (_measuredFeatures is not null && item.Tag is int id && _measuredFeatures.TryGetValue(id, out FeatureInspectionReport m))
                verdict = m.InTolerance ? "PASS" : "FAIL";
            item.SubItems[1].Text = verdict;
        }
    }

    /// <summary>Renders the selected feature's parameters: nominal always, measured/dev/verdict once measured.</summary>
    private void RenderSelectedFeature()
    {
        _paramGrid.Items.Clear();
        if (_nominalFeatures is null || _featureList.SelectedItems.Count == 0)
            return;

        if (_featureList.SelectedItems[0].Tag is not int id)
            return;

        FeatureInspectionReport? nominal = _nominalFeatures.FirstOrDefault(f => f.Feature.Id == id);
        if (nominal is null)
            return;

        FeatureInspectionReport? measured = null;
        if (_measuredFeatures is not null && _measuredFeatures.TryGetValue(id, out FeatureInspectionReport m))
            measured = m;

        foreach (FeatureParameter np in nominal.Parameters)
        {
            // Match the measured parameter by name (a feature's parameter names are stable).
            FeatureParameter? mp = null;
            if (measured is not null)
            {
                foreach (FeatureParameter cand in measured.Parameters)
                {
                    if (cand.Name == np.Name) { mp = cand; break; }
                }
            }

            var row = new ListViewItem(np.Name);
            row.SubItems.Add(Fmt(np.NominalMm));
            row.SubItems.Add(mp is { } v ? Fmt(v.MeasuredMm) : "—");
            row.SubItems.Add(mp is { } d ? FmtSigned(d.DeviationMm) : "—");
            row.SubItems.Add("±" + Fmt(np.ToleranceMm));
            row.SubItems.Add(mp is { } e ? (e.InTolerance ? "PASS" : "FAIL") : "—");
            _paramGrid.Items.Add(row);
        }
    }

    private static string Fmt(double mm) =>
        double.IsNaN(mm) ? "—" : mm.ToString("0.000", CultureInfo.InvariantCulture);

    private static string FmtSigned(double mm) =>
        double.IsNaN(mm) ? "—" : mm.ToString("+0.000;-0.000;0.000", CultureInfo.InvariantCulture);

    /// <summary>Reads the density / σ / seed toolbar fields into a <see cref="GenerationOptions"/>, with safe fallbacks.</summary>
    private GenerationOptions ParseGenerationOptions()
    {
        double density = ParsePositiveDouble(_densityBox.Text, GenerationOptions.DefaultDensityPerMm2);
        double sigma = ParseNonNegativeDouble(_sigmaBox.Text, GenerationOptions.Default.SigmaMm);
        int? seed = ParseOptionalInt(_seedBox.Text);
        return new GenerationOptions(density, sigma, seed);
    }

    private static double ParsePositiveDouble(string text, double fallback)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && v > 0)
            return v;
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out v) && v > 0)
            return v;
        return fallback;
    }

    private static double ParseNonNegativeDouble(string text, double fallback)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && v >= 0)
            return v;
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out v) && v >= 0)
            return v;
        return fallback;
    }

    private static int? ParseOptionalInt(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null; // blank ⇒ random (non-reproducible) cloud
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            return v;
        return null;
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
        ResetFeatureInspection();
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

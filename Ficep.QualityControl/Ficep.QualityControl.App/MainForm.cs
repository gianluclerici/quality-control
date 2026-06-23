using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using devDept.Eyeshot;
using devDept.Eyeshot.Control;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.QualityControl.Core.Io;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.App;

/// <summary>
/// The quality-control viewport shell: a thin WinForms frontend over the headless
/// <c>Ficep.QualityControl.Core</c>. It loads a nominal <see cref="Brep"/> reference (STEP) and a
/// scanned point cloud (PLY) and shows them overlaid in a single Eyeshot <see cref="Design"/>
/// viewport. All file parsing lives in Core (<see cref="BrepImporter"/>, <see cref="PlyReader"/>);
/// this form only adds the results to the scene and drives the camera.
/// </summary>
public sealed class MainForm : Form
{
    private readonly Design _design;
    private readonly ToolStripStatusLabel _status;

    private readonly BrepImporter _brepImporter = new();
    private readonly IPointCloudReader _plyReader = new PlyReader();

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
        var clear = new ToolStripButton("Pulisci") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        loadNominal.Click += (_, _) => LoadNominal();
        loadScan.Click += (_, _) => LoadScan();
        clear.Click += (_, _) => ClearScene();
        toolbar.Items.AddRange(new ToolStripItem[] { loadNominal, new ToolStripSeparator(), loadScan, new ToolStripSeparator(), clear });

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

            foreach (Brep brep in breps)
            {
                brep.ColorMethod = colorMethodType.byEntity;
                brep.Color = Color.FromArgb(210, 210, 215); // neutral grey for the reference body
                _design.Entities.Add(brep);
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

            // FastPointCloud wants a flat float[] of XYZ triples — best for million-point clouds.
            var coords = new float[samples.Count * 3];
            for (int i = 0; i < samples.Count; i++)
            {
                Point3D p = samples[i].Position;
                coords[i * 3] = (float)p.X;
                coords[i * 3 + 1] = (float)p.Y;
                coords[i * 3 + 2] = (float)p.Z;
            }

            var cloud = new FastPointCloud(coords)
            {
                ColorMethod = colorMethodType.byEntity,
                Color = Color.DodgerBlue,
            };
            _design.Entities.Add(cloud);

            _design.Entities.Regen();
            _design.ZoomFit();
            _design.Invalidate();
            _status.Text = $"Scan caricato: {samples.Count:N0} punti — {System.IO.Path.GetFileName(dlg.FileName)}";
        });
    }

    private void ClearScene()
    {
        _design.Entities.Clear();
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

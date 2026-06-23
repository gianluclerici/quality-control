using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot;
using devDept.Eyeshot.Control;
using devDept.Geometry;
using devDept.Graphics;
using devDept.Eyeshot.Translators;
using devDept.Serialization;
using devDept.CustomControls;


namespace FicepControls
{
    public partial class MeasureForm : Form
    {

        private bool _skipZoomFit = false;

        private bool _yAxisUp;
        private bool _jittering;
        private bool _insertAsBlock;
        
        private BlockReference _brJittering;

        private Brep _solid;

        public MeasureForm()
        {
            _solid = null;
            InitializeComponent();

            design1.Rendered.PlanarReflections = false;

            
        }

        public MeasureForm(Brep solid)
        {
            _solid = solid;
            InitializeComponent();

            design1.Rendered.PlanarReflections = false;
        }
        protected override void OnLoad(EventArgs e)
        {
            design1.ActiveViewport.Grid.AutoSize = true;
            design1.ActiveViewport.Grid.AutoStep = true;

            if (_solid == null)
                InitScene();
            else
                design1.Entities.Add(_solid, "Default", Color.Gray);

            design1.ActionMode = actionType.SelectVisibleByPickDynamic;
            design1.SelectionFilterMode = selectionFilterType.Face | selectionFilterType.Edge | selectionFilterType.Vertex;

            design1.MouseDown += design1_MouseDown;
            design1.WorkCompleted += design1_WorkCompleted;
            design1.WorkCancelled += design1_WorkCancelled;
            design1.WorkFailed += design1_WorkFailed;


            
            design1.SetView(viewType.Trimetric);
            design1.ZoomFit();

            design1.AssemblySelectionMode = Workspace.assemblySelectionType.Leaf;
            design1.CurrentBlock.Units = linearUnitsType.Millimeters;

            base.OnLoad(e);
        }

        #region Scene Creation
        
        private void InitScene()
        {
            Shaver(new Translation(0, 0, -40) * new Scaling(18) * new Rotation(UtilityEx.PI_2, Vector3D.AxisZ));

            SwingArm(new Translation(0, -55, 0) * new Translation(60, 0, 10) * new Rotation(UtilityEx.DegToRad(45.0), Vector3D.AxisMinusY) * new Rotation(UtilityEx.DegToRad(20.0), Vector3D.AxisX));
        }

        private void Shaver(Transformation tr)
        {
            Plane right = Plane.YZ;
            Plane pln1 = (Plane)right.Clone();
            pln1.Rotate(Utility.DegToRad(40), Vector3D.AxisY);

            Plane pln2 = (Plane)right.Clone();
            pln2.Rotate(Utility.DegToRad(25), Vector3D.AxisY);

            Plane pln3 = pln2.Offset(-.5);
            Plane pln4 = (Plane)right.Clone();
            pln4.Rotate(Utility.DegToRad(-30), Vector3D.AxisY);

            Plane pln5 = pln1.Offset(1);
            Plane pln6 = (Plane)right.Clone();
            pln6.Rotate(Utility.DegToRad(-34), Vector3D.AxisY);

            Plane pln7 = pln6.Offset(0.55);
            
            Circle sketch3 = new Circle(right, new Point2D(0, 3.9), 1.5 / 2);
            sketch3.Rotate(Utility.DegToRad(-90), sketch3.Plane.AxisZ, sketch3.Plane.Origin);
            
            Circle sketch4 = new Circle(pln3, new Point2D(0, 3.5), 1.8 / 2);
            sketch4.Rotate(Utility.DegToRad(-90), sketch4.Plane.AxisZ, sketch4.Plane.Origin);

            Ellipse sketch5 = new Ellipse(pln4, new Point2D(0, 6), 4.0 / 2, 2.0 / 2);
            sketch5.Rotate(Utility.DegToRad(-90), sketch5.Plane.AxisZ, sketch5.Plane.Origin);

            Circle sketch6 = new Circle(pln5, new Point2D(0, 3.7), 0.6 / 2);
            sketch6.Rotate(Utility.DegToRad(-90), sketch6.Plane.AxisZ, sketch6.Plane.Origin);

            Brep loft1 = Brep.Loft(new ICurve[] { sketch6, sketch4, sketch3, sketch5 }, 2);
            
            devDept.Eyeshot.Entities.Region cr = devDept.Eyeshot.Entities.Region.CreateCircle(right, new Point2D(0, 2.5), 5.6 / 2);
            
            devDept.Eyeshot.Entities.Region rr = devDept.Eyeshot.Entities.Region.CreateRectangle(right, -2, 2.5, 4, 5);
            
            devDept.Eyeshot.Entities.Region sketch2 = devDept.Eyeshot.Entities.Region.Difference(rr, cr)[0];
            loft1.ExtrudeRemove(sketch2, -5);
            
            Brep[] split1;            
            Brep[] split2;
            loft1.SplitBy(pln7, out split1, out split2);
            
            Brep body = split2[0];            
            Brep cap = split1[0];
            
            devDept.Eyeshot.Entities.Region rr1 = devDept.Eyeshot.Entities.Region.CreateRectangle(.05, .05, true);
            rr1.Translate(0, -1, 0);
            rr1.Rotate(Utility.DegToRad(-30), Vector3D.AxisY, Point3D.Origin);
            cap.ExtrudeRemovePattern(rr1, 10, Plane.XY, 1, 1, 0.1, 21);

            body.TransformBy(tr);
            cap.TransformBy(tr);

            design1.Entities.Add(body, design1.Layers[0].Name, Color.DodgerBlue);
            design1.Entities.Add(cap, design1.Layers[0].Name, Color.Gray);
        }

        private void SwingArm(Transformation tr)
        {
            Circle c1 = new Circle(Plane.XY, new Point3D(0, 0, 0), 25.0);
            
            Circle c2 = new Circle(Plane.XY, new Point3D(100, 0, 0), 12.0);
            
            Circle c1_inner = new Circle(Plane.XY, c1.Center, 18.0);
            
            Circle c2_inner = new Circle(Plane.XY, c2.Center, 6.0);
            
            Circle c3_inner = new Circle(Plane.XY, new Point3D(c2.Center.X - c2.Radius - 8.0, c2.Center.Y, c2.Center.Z), 6.0);

            Line[] tangents = UtilityEx.GetLinesTangentToTwoCircles(c1, c2);
            
            Arc a1 = new Arc(Plane.XY, c1.Center, c1.Radius, tangents[0].StartPoint, tangents[1].StartPoint, false);
            
            Arc a2 = new Arc(Plane.XY, c2.Center, c2.Radius, tangents[1].EndPoint, tangents[0].EndPoint, false);
            
            CompositeCurve cc = new CompositeCurve(new ICurve[] { a1, tangents[0], a2, tangents[1] }, true);

            devDept.Eyeshot.Entities.Region reg1 = new devDept.Eyeshot.Entities.Region(cc);

            // Circle holes            
            reg1 = devDept.Eyeshot.Entities.Region.Difference(reg1, new devDept.Eyeshot.Entities.Region(c1_inner))[0];
            
            reg1 = devDept.Eyeshot.Entities.Region.Difference(reg1, new devDept.Eyeshot.Entities.Region(c2_inner))[0];
            
            devDept.Eyeshot.Entities.Region rRect = devDept.Eyeshot.Entities.Region.CreateRectangle(-5, 0, 10, 22);
            
            rRect.Rotate(-Math.PI / 4, Vector3D.AxisZ, Point3D.Origin);
            
            reg1 = devDept.Eyeshot.Entities.Region.Difference(reg1, rRect)[0];

            // Central hole            
            Arc f1_inner, f2_inner, f3_inner;
            
            Circle c3 = new Circle(Point3D.Origin, 26);
            tangents = UtilityEx.GetLinesTangentToTwoCircles(c1_inner, c3_inner);            
            Curve.Fillet(tangents[0], tangents[1], c3_inner.Radius, true, false, true, true, out f1_inner);            
            Arc a3 = new Arc(Plane.XY, c3.Center, c3.Radius, c3.IntersectWith(tangents[0])[0], c3.IntersectWith(tangents[1])[0], true);            
            Curve.Fillet(tangents[0], a3, 5, true, true, true, true, out f2_inner);            
            Curve.Fillet(tangents[1], a3, 5, false, true, true, true, out f3_inner);
            
            CompositeCurve ccHole = new CompositeCurve(new ICurve[] { a3, f3_inner, tangents[1], f1_inner, tangents[0], f2_inner });
            
            reg1 = devDept.Eyeshot.Entities.Region.Difference(reg1, new devDept.Eyeshot.Entities.Region(ccHole))[0];

            // Main profile            
            Brep mainBody = reg1.ExtrudeAsBrep(new Interval(-40.0, 40.0));

            // Cutting shape
            
            Line ln1 = new Line(Plane.XZ, -32.5, 2, -32.5, -7);
            
            Line ln2 = new Line(Plane.XZ, -32.5, -7, 35, -7);
            
            Line ln3 = new Line(Plane.XZ, 35, -7, 75, -28);
            
            Line ln4 = new Line(Plane.XZ, 75, -28, 118, -28);
            
            Line ln5 = new Line(Plane.XZ, 118, -28, 118, -20);
            
            Line ln6 = new Line(Plane.XZ, 118, -20, 75, -20);

            Line ln7 = (Line)ln3.Offset(8, Vector3D.AxisY)[0];
            ln7.Scale(ln7.MidPoint, 1.2);
            
            Arc f1, f2, f3;
            
            Curve.Fillet(ln2, ln3, 20.0, false, false, true, true, out f1);
            
            Curve.Fillet(ln3, ln4, 20.0, false, false, true, true, out f2);
            
            Curve.Fillet(ln6, ln7, 10.0, true, false, true, true, out f3);

            Mirror mirror = new Mirror(Plane.XY);

            ICurve[] curveArray = new ICurve[] { ln1, ln2, ln3, ln4, ln5, ln6, ln7, f1, f2, f3 };
            
            ICurve[] curveArrayCopy = new ICurve[curveArray.Length];

            for (int i = 0; i < curveArray.Length; i++)
            {
                curveArrayCopy[i] = (ICurve)((Entity)curveArray[i]).Clone();                
                ((Entity)curveArrayCopy[i]).TransformBy(mirror);
            }
            
            Arc f4;            
            Curve.Fillet(ln7, curveArrayCopy[6], 10, false, true, true, true, out f4);
            
            List<ICurve> contour = new List<ICurve>(curveArray.Take(7));
            contour.AddRange(curveArrayCopy);            
            contour.AddRange(new ICurve[] { f1, f2, f3, f4 });

            CompositeCurve ccCut = new CompositeCurve(contour);
            
            devDept.Eyeshot.Entities.Region regCut = devDept.Eyeshot.Entities.Region.Difference(devDept.Eyeshot.Entities.Region.CreateRectangle(Plane.XZ, -35, -50, 160, 100), new devDept.Eyeshot.Entities.Region(ccCut))[0];

            mainBody.ExtrudeRemove(regCut, new Interval(-100, 100));

            mainBody.TransformBy(tr);

            design1.Entities.Add(mainBody, Color.LimeGreen);
        }

        #endregion

        #region Read/Write

        private void SetButtonEnabled(bool value)
        {
            openButton.Enabled = saveButton.Enabled = importButton.Enabled = value;
        }

        private void ResetControls()
        {
            design1.ResetPoints();
            design1.ResetSelection();

            if (!_insertAsBlock)
            {
                design1.Clear();
            }
        }

        private void importButton_Click(object sender, EventArgs e)
        {
            ReadFileAsync rfa = ImportExportHelper.ShowImportDialog(importFormats.SurfaceAndBrep | importFormats.Mesh | importFormats.Ifc, out _yAxisUp, out _jittering, out _insertAsBlock);
            if (rfa == null) return;
            ResetControls();
            SetButtonEnabled(false);
            design1.StartWork(rfa);
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            bool foo;
            ReadFile readFile = ImportExportHelper.ShowOpenDialog(out foo, true);
            if (readFile == null) return;
            ResetControls();
            SetButtonEnabled(false);
            design1.StartWork(readFile);

            design1.Invalidate();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            WriteFile writeFile = ImportExportHelper.ShowSaveDialog(design1);
            if (writeFile != null)
            {
                SetButtonEnabled(false);
                design1.StartWork(writeFile);
            }
        }

        #endregion

        #region Event Handlers

        private void design1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (design1.IsBusy)
                return;

            if (e.Button == MouseButtons.Left)
            {
                SelectedItem sel = design1.GetItemUnderMouseCursor(e.Location);
                design1.PtA = design1.ScreenToWorld(e.Location);
                Entity ent = null;

                bool selected = sel != null;

                if (selected)
                {
                    Brep brep = sel.Item as Brep;

                    if (brep == null)
                    {
                        MessageBox.Show($"Measurement works only on Brep entities\r\nSelected item is of type {sel.Item.GetType().Name}", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    brep.Rebuild(0, true);

                    if (sel is SelectedVertex)
                    {
                        SelectedVertex selVertex = sel as SelectedVertex;
                        ent = new devDept.Eyeshot.Entities.Point(brep.Vertices[selVertex.Index], 5.0f);
                    }
                    else if (sel is SelectedEdge)
                    {
                        SelectedEdge selEdge = sel as SelectedEdge;
                        ent = brep.Edges[selEdge.Index].Curve.GetNurbsForm();
                        ent.LineWeightMethod = colorMethodType.byEntity;
                        ent.LineWeight = 5.0f;
                    }
                    else if (sel is SelectedFace)
                    {
                        SelectedFace selFace = sel as SelectedFace;
                        ent = brep.Faces[selFace.Index].ConvertToSurface()[0];
                    }
                    else
                    {
                        ent = brep;
                    }
                    
                    if (sel.HasParents())
                    {
                        Transformation tr = new Identity();
                        // Apply parent's transformation to entity
                        for (int i = 0; i < sel.Parents.Count; i++)
                        {
                            tr = sel.Parents.ElementAt(i).GetFullTransformation(design1.Blocks) * tr;
                        }

                        ent.TransformBy(tr);
                    }

                    if (design1.Ent1 == null)
                    {
                        design1.ResetPoints();

                        design1.Ent1 = ent;
                    }
                    else
                    {
                        design1.Ent2 = ent;
                        
                        //TODO devDept 2025: Moved MinimumDistance class from devDept.Geometry to devDept.Eyeshot namespace.
                        MinimumDistance md = new MinimumDistance(design1.Ent1, design1.Ent2);

                        design1.ActionMode = actionType.None;
                        design1.StartWork(md);
                    }
                }
            }
        }

        private void design1_WorkCompleted(object sender, devDept.WorkCompletedEventArgs e)
        {
            if (e.WorkUnit is ReadFileAsync)
            {
                _brJittering = null;

                _skipZoomFit = false;
                ReadFile rf = e.WorkUnit as ReadFile;
                RegenOptions ro = new RegenOptions() { Async = true };
                if (rf != null)
                {
                    if (rf.Units != design1.CurrentBlock.Units)
                    {
                        MessageBox.Show($"Current model is set in Millimiters, scaling to {rf.Units} is not supported. Items will be imported without any scaling operations.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    _skipZoomFit = rf.Camera != null; // When ReadFile contains Camera data it's restored when calling AddTo() method.
                }

                ReadFileAsync ra = (ReadFileAsync)e.WorkUnit;
                
                if (_yAxisUp)
                    ra.RotateEverythingAroundX();

                if (_insertAsBlock)
                {
                    _brJittering = ImportExportHelper.InsertAsBlock(design1, ra, ro);

                    _skipZoomFit = false;

                    ReadFileAsyncWithBlocks rfb = e.WorkUnit as ReadFileAsyncWithBlocks;

                    if(rfb?.Blocks.RootBlockName != null)
                        design1.ManageSavedDistance(rfb.Blocks[rfb.Blocks.RootBlockName].CustomData);
                }
                else
                {
                    ra.AddTo(design1, ro);
                }

                SetButtonEnabled(true);
            }
            else if (e.WorkUnit is WriteFileAsync)
            {
                SetButtonEnabled(true);
            }
            else if (e.WorkUnit is Regeneration)
            {
                if (_jittering && _insertAsBlock) design1.RemoveJittering(_brJittering);

                else if (_jittering)
                {
                    design1.Entities.SelectAll();
                    design1.RemoveJittering();
                }

                design1.UpdateBoundingBox();

                if (!_skipZoomFit)
                    design1.ZoomFit();
                design1.Invalidate();
            }
            else if (e.WorkUnit is LazyLoading)
            {
                design1.Invalidate();
            }
            //TODO devDept 2025: Moved MinimumDistance class from devDept.Geometry to devDept.Eyeshot namespace.
            else if (e.WorkUnit is MinimumDistance)
            {
                //TODO devDept 2025: Moved MinimumDistance class from devDept.Geometry to devDept.Eyeshot namespace.
                MinimumDistance md = (MinimumDistance) e.WorkUnit;

                design1.PtA = md.Result.P0;
                design1.PtB = md.Result.P1;
                design1.Distance = md.Result.Length;

                measurePanel1.SetMeasure(design1.Distance, design1.PtA, design1.PtB);

                design1.ZoomToPoints();
                design1.ResetSelection();

                design1.ActionMode = actionType.SelectVisibleByPickDynamic;
            }
        }

        private void design1_WorkFailed(object sender, devDept.WorkFailedEventArgs e)
        {
            SetButtonEnabled(true);
        }

        private void design1_WorkCancelled(object sender, EventArgs e)
        {
            SetButtonEnabled(true);
        }

        private void ChkSelectWholeEntity_CheckedChanged(object sender, EventArgs e)
        {
            design1.SetWholeEntitySelection(!chkSelectWholeEntity.Checked);
        }

        #endregion
    }

    public class MyDesign : Design
    {
        private const float _scaleFactor = 1.1f;
        private const double ZoomOnPointFactor = 1.5;

        private readonly System.Drawing.Font _font;
        
        public Entity Ent1 = null;
        
        public Entity Ent2 = null;

        private Point3D _ptA = null;

        public Point3D PtA
        {
            get 
            {
                return _ptA;
            }

            set
            {
                _ptA = value;
            }
        }

        private Point3D _ptB = null;
        public Point3D PtB
        {
            get
            {
                return _ptB;
            }

            set
            {
                _ptB = value;
                Blocks[Blocks.RootBlockName].CustomData = new DistancePoints(_ptA, _ptB);
            }
        }

        private bool _componentSelection;

        private double _distance;
        private double _roundedDistance;

        public double Distance
        {
            get
            {
                return _distance;
            }

            set
            {
                _distance = value;
                _roundedDistance = Math.Round(_distance, 0);
            }
        }

        private readonly Graphics g;

        public MyDesign() : base()
        {
            Selection.LineWeightScaleFactor = 4;            
            _font = new Font(Font.FontFamily, 12.0f, FontStyle.Bold);
            g = Graphics.FromImage(new Bitmap(1,1));
            _componentSelection = true;
        }

        public void ResetSelection()
        {
            Ent1 = null;
            Ent2 = null;
            Entities.ClearSelection();
        }

        public void ResetPoints()
        {
            _ptA = null;
            _ptB = null;

            RootBlock.CustomData = null;
        }

        public void ZoomToPoints()
        {
            if (PtA != null && PtB != null)
            {
                Entity eA = Mesh.CreateSphere(1,5,5);
                Entity eB = Mesh.CreateSphere(1, 5, 5);

                Scaling s = new Scaling(new Line(PtA, PtB).MidPoint, ZoomOnPointFactor);
                eA.TransformBy(s * new Translation(PtA.AsVector));
                eB.TransformBy(s * new Translation(PtB.AsVector));

                IList<Entity> ents = new List<Entity>()
                {
                    eA,
                    eB
                };

                ZoomFit(ents, false);
            }
            else
            {
                ZoomFit();
            }
        }

        public void SetWholeEntitySelection(bool componentSelection)
        {
            _componentSelection = componentSelection;
            if (!_componentSelection)
            {
                SelectionFilterMode = selectionFilterType.Entity;
            }
            else
            {
                SelectionFilterMode = selectionFilterType.Face | selectionFilterType.Edge | selectionFilterType.Vertex;
            }
        }

        public void ManageSavedDistance(object customData)
        {
            if (customData is DistancePoints)
            {
                DistancePoints distPoints = (DistancePoints) customData;

                _ptA = distPoints.PtA;
                _ptB = distPoints.PtB;
            }
        }

        protected override void DrawOverlay(DrawSceneParams data)
        {
            if (_ptA != null && _ptB != null)
            {
                Color lineClr = ActiveViewport.Background.GetContrastColor();
                Color txtClr = ActiveViewport.Background.GetContrastColorInverted();

                float prevLineSize = data.RenderContext.CurrentLineWidth;
                float prevPointSize = data.RenderContext.CurrentPointSize;
                Color prevWireColor = data.RenderContext.CurrentWireColor;

                data.RenderContext.SetLineSize(7);
                data.RenderContext.SetPointSize(10);
                data.RenderContext.SetColorWireframe(lineClr);


                Point3D i = WorldToScreen(PtA);
                Point3D e = WorldToScreen(PtB);

                Point3D origin;
                Vector3D camX, camY, camZ;
                this.ActiveViewport.Camera.GetFrame(out origin, out camX, out camY, out camZ);

                Plane pln = new Plane(origin, camX, camY);

                Point3D mid = WorldToScreen(new Line(PtA, PtB).MidPoint);

                if (pln.DistanceTo(PtA) < 0 &&
                    pln.DistanceTo(PtB) < 0)
                {
                    data.RenderContext.DrawLine(i, e);
                    data.RenderContext.DrawPoints(new Point3D[] { i, e });
                }

                string txt = $"{_roundedDistance} mm";
                SizeF sT = g.MeasureString(txt, _font);
                sT.Width *= _scaleFactor;
                sT.Height *= _scaleFactor;

                data.RenderContext.DrawQuad(new RectangleF((float) mid.X - sT.Width / 2, (float) mid.Y - sT.Height / 2, sT.Width, sT.Height));

                DrawText((int) mid.X, (int) mid.Y, txt, _font, txtClr, ContentAlignment.MiddleCenter);

                data.RenderContext.SetLineSize(prevLineSize);
                data.RenderContext.SetPointSize(prevPointSize);
                data.RenderContext.SetColorWireframe(prevWireColor);
            }
        }
    }

    public class DistancePoints
    {
        public Point3D PtA { get; private set; }
        public Point3D PtB { get; private set; }

        public DistancePoints(Point3D ptA, Point3D ptB)
        {
            PtA = ptA;
            PtB = ptB;
        }
    }
}

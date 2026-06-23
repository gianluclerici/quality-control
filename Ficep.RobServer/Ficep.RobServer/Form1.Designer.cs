using System.Drawing;

namespace Ficep.RobServer
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
         {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            devDept.Eyeshot.Control.CancelToolBarButton cancelToolBarButton1 = new devDept.Eyeshot.Control.CancelToolBarButton("Cancel", devDept.Eyeshot.Control.ToolBarButton.styleType.ToggleButton, true, true);
            devDept.Eyeshot.Control.ProgressBar progressBar1 = new devDept.Eyeshot.Control.ProgressBar(devDept.Eyeshot.Control.ProgressBar.styleType.Speedometer, 0, "Idle", System.Drawing.Color.Black, System.Drawing.Color.Transparent, System.Drawing.Color.Green, 1D, true, cancelToolBarButton1, false, 0.1D, 0.333D, true);
            devDept.Eyeshot.Control.BackgroundSettings backgroundSettings1 = new devDept.Eyeshot.Control.BackgroundSettings(devDept.Graphics.backgroundStyleType.Solid, System.Drawing.Color.DeepSkyBlue, System.Drawing.Color.DodgerBlue, System.Drawing.Color.WhiteSmoke, 0.75D, null, devDept.Eyeshot.colorThemeType.Auto, 0.33D);
            devDept.Eyeshot.Camera camera1 = new devDept.Eyeshot.Camera(new devDept.Geometry.Point3D(0D, 0D, 45D), 380D, new devDept.Geometry.Quaternion(0.086824088833465166D, 0.15038373318043533D, 0.492403876506104D, 0.85286853195244339D), devDept.Eyeshot.projectionType.Perspective, 50D, 2.4754105010735894D, false, 0.001D);
            devDept.Eyeshot.Control.HomeToolBarButton homeToolBarButton1 = new devDept.Eyeshot.Control.HomeToolBarButton("Home", devDept.Eyeshot.Control.ToolBarButton.styleType.PushButton, true, true);
            devDept.Eyeshot.Control.MagnifyingGlassToolBarButton magnifyingGlassToolBarButton1 = new devDept.Eyeshot.Control.MagnifyingGlassToolBarButton("Magnifying Glass", devDept.Eyeshot.Control.ToolBarButton.styleType.ToggleButton, true, true);
            devDept.Eyeshot.Control.ZoomWindowToolBarButton zoomWindowToolBarButton1 = new devDept.Eyeshot.Control.ZoomWindowToolBarButton("Zoom Window", devDept.Eyeshot.Control.ToolBarButton.styleType.ToggleButton, true, true);
            devDept.Eyeshot.Control.ZoomToolBarButton zoomToolBarButton1 = new devDept.Eyeshot.Control.ZoomToolBarButton("Zoom", devDept.Eyeshot.Control.ToolBarButton.styleType.ToggleButton, true, true);
            devDept.Eyeshot.Control.PanToolBarButton panToolBarButton1 = new devDept.Eyeshot.Control.PanToolBarButton("Pan", devDept.Eyeshot.Control.ToolBarButton.styleType.ToggleButton, true, true);
            devDept.Eyeshot.Control.RotateToolBarButton rotateToolBarButton1 = new devDept.Eyeshot.Control.RotateToolBarButton("Rotate", devDept.Eyeshot.Control.ToolBarButton.styleType.ToggleButton, true, true);
            devDept.Eyeshot.Control.ZoomFitToolBarButton zoomFitToolBarButton1 = new devDept.Eyeshot.Control.ZoomFitToolBarButton("Zoom Fit", devDept.Eyeshot.Control.ToolBarButton.styleType.PushButton, true, true);
            devDept.Eyeshot.Control.ToolBar toolBar1 = new devDept.Eyeshot.Control.ToolBar(devDept.Eyeshot.Control.ToolBar.positionType.VerticalTopRight, true, new devDept.Eyeshot.Control.ToolBarButton[] {
            ((devDept.Eyeshot.Control.ToolBarButton)(homeToolBarButton1)),
            ((devDept.Eyeshot.Control.ToolBarButton)(magnifyingGlassToolBarButton1)),
            ((devDept.Eyeshot.Control.ToolBarButton)(zoomWindowToolBarButton1)),
            ((devDept.Eyeshot.Control.ToolBarButton)(zoomToolBarButton1)),
            ((devDept.Eyeshot.Control.ToolBarButton)(panToolBarButton1)),
            ((devDept.Eyeshot.Control.ToolBarButton)(rotateToolBarButton1)),
            ((devDept.Eyeshot.Control.ToolBarButton)(zoomFitToolBarButton1))}, 5, 0, System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0))))), 0D, System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0))))), 0D);
            devDept.Eyeshot.Control.Legend legend1 = new devDept.Eyeshot.Control.Legend(0D, 100D, "Title", "Subtitle", new System.Drawing.Point(24, 24), new System.Drawing.Size(10, 30), true, false, false, "{0:+0.###;-0.###;0}", System.Drawing.Color.Transparent, System.Drawing.Color.Black, System.Drawing.Color.Black, null, null, new System.Drawing.Color[] {
            System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255))))),
            System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(127)))), ((int)(((byte)(255))))),
            System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(255))))),
            System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(127))))),
            System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(0))))),
            System.Drawing.Color.FromArgb(((int)(((byte)(127)))), ((int)(((byte)(255)))), ((int)(((byte)(0))))),
            System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(0))))),
            System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(127)))), ((int)(((byte)(0))))),
            System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))))}, true, false);
            devDept.Eyeshot.Control.Histogram histogram1 = new devDept.Eyeshot.Control.Histogram(30, 80, "Title", System.Drawing.Color.Blue, System.Drawing.Color.Gray, System.Drawing.Color.Black, System.Drawing.Color.Red, System.Drawing.Color.LightYellow, false, true, false, "{0:+0.###;-0.###;0}");
            devDept.Eyeshot.Control.RotateSettings rotateSettings1 = new devDept.Eyeshot.Control.RotateSettings(new devDept.Eyeshot.Control.MouseButton(devDept.Eyeshot.Control.mouseButtonsZPR.Middle, devDept.Eyeshot.Control.modifierKeys.None), 10D, true, 1D, devDept.Eyeshot.rotationType.Trackball, devDept.Eyeshot.rotationCenterType.CursorLocation, new devDept.Geometry.Point3D(0D, 0D, 0D), false);
            devDept.Eyeshot.Control.ZoomSettings zoomSettings1 = new devDept.Eyeshot.Control.ZoomSettings(new devDept.Eyeshot.Control.MouseButton(devDept.Eyeshot.Control.mouseButtonsZPR.Middle, devDept.Eyeshot.Control.modifierKeys.Shift), 25, true, devDept.Eyeshot.zoomStyleType.AtCursorLocation, false, 1D, System.Drawing.Color.Empty, devDept.Eyeshot.Camera.perspectiveFitType.Accurate, false, 10, true);
            devDept.Eyeshot.Control.PanSettings panSettings1 = new devDept.Eyeshot.Control.PanSettings(new devDept.Eyeshot.Control.MouseButton(devDept.Eyeshot.Control.mouseButtonsZPR.Middle, devDept.Eyeshot.Control.modifierKeys.Ctrl), 25, true);
            devDept.Eyeshot.Control.NavigationSettings navigationSettings1 = new devDept.Eyeshot.Control.NavigationSettings(devDept.Eyeshot.Camera.navigationType.Examine, new devDept.Eyeshot.Control.MouseButton(devDept.Eyeshot.Control.mouseButtonsZPR.Left, devDept.Eyeshot.Control.modifierKeys.None), new devDept.Geometry.Point3D(-1000D, -1000D, -1000D), new devDept.Geometry.Point3D(1000D, 1000D, 1000D), 8D, 50D, 50D);
            devDept.Eyeshot.Control.CoordinateSystemIcon coordinateSystemIcon1 = new devDept.Eyeshot.Control.CoordinateSystemIcon(new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0))), System.Drawing.Color.Black, System.Drawing.Color.Black, System.Drawing.Color.Black, System.Drawing.Color.Black, System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80))))), System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80))))), System.Drawing.Color.OrangeRed, "Origin", "X", "Y", "Z", true, devDept.Eyeshot.Control.coordinateSystemPositionType.BottomLeft, 37, null, true);
            devDept.Eyeshot.Control.ViewCubeIcon viewCubeIcon1 = new devDept.Eyeshot.Control.ViewCubeIcon(devDept.Eyeshot.Control.coordinateSystemPositionType.BottomRight, true, System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(20)))), ((int)(((byte)(60))))), true, "RIGHT", "LEFT", "FRONT", "BACK", "TOP", "BOTTOM", System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(77)))), ((int)(((byte)(77)))), ((int)(((byte)(77))))), System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(77)))), ((int)(((byte)(77)))), ((int)(((byte)(77))))), System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(77)))), ((int)(((byte)(77)))), ((int)(((byte)(77))))), System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(77)))), ((int)(((byte)(77)))), ((int)(((byte)(77))))), System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(77)))), ((int)(((byte)(77)))), ((int)(((byte)(77))))), System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(77)))), ((int)(((byte)(77)))), ((int)(((byte)(77))))), 'S', 'N', 'W', 'E', true, null, System.Drawing.Color.White, System.Drawing.Color.Black, 120, true, true, null, null, null, null, null, null, true, new devDept.Geometry.Quaternion(0D, 0D, 0D, 1D), true);
            devDept.Eyeshot.Control.Viewport viewport1 = new devDept.Eyeshot.Control.Viewport(new System.Drawing.Point(0, 0), new System.Drawing.Size(913, 453), backgroundSettings1, camera1, new devDept.Eyeshot.Control.ToolBar[] {
            toolBar1}, new devDept.Eyeshot.Control.Legend[] {
            legend1}, histogram1, devDept.Eyeshot.displayType.Rendered, true, false, false, new devDept.Eyeshot.Control.Grid[0], new devDept.Eyeshot.Control.OriginSymbol[0], false, rotateSettings1, zoomSettings1, panSettings1, navigationSettings1, coordinateSystemIcon1, viewCubeIcon1, devDept.Eyeshot.viewType.Trimetric);
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.design1 = new devDept.Eyeshot.Control.Design();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.b3Save = new System.Windows.Forms.Button();
            this.b4Export = new System.Windows.Forms.Button();
            this.b5FNC = new System.Windows.Forms.Button();
            this.b6ShowFeatures = new System.Windows.Forms.Button();
            this.b7Measure = new System.Windows.Forms.Button();
            this.listMacroView = new System.Windows.Forms.ListView();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.nestedSplitContainer = new System.Windows.Forms.SplitContainer();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.panel = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.design1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nestedSplitContainer)).BeginInit();
            this.nestedSplitContainer.Panel1.SuspendLayout();
            this.nestedSplitContainer.Panel2.SuspendLayout();
            this.nestedSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.panel.SuspendLayout();
            this.SuspendLayout();
            // 
            // design1
            // 
            this.design1.AllowDrop = true;
            this.design1.Cursor = System.Windows.Forms.Cursors.Default;
            this.design1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.design1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.design1.Location = new System.Drawing.Point(0, 0);
            this.design1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.design1.Name = "design1";
            this.design1.ProgressBar = progressBar1;
            this.design1.Size = new System.Drawing.Size(913, 453);
            this.design1.TabIndex = 0;
            this.design1.Text = "design1";
            this.design1.Viewports.Add(viewport1);
            this.design1.DragDrop += new System.Windows.Forms.DragEventHandler(this.OnDragDropEyeControl);
            this.design1.DragEnter += new System.Windows.Forms.DragEventHandler(this.OnDragEnterEyeControl);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(9, 8);
            this.button1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(56, 28);
            this.button1.TabIndex = 1;
            this.button1.Text = "Open";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.b1Open_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(78, 8);
            this.button2.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(56, 28);
            this.button2.TabIndex = 2;
            this.button2.Text = "Import";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.b2Import_Click);
            // 
            // b3Save
            // 
            this.b3Save.Location = new System.Drawing.Point(146, 8);
            this.b3Save.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.b3Save.Name = "b3Save";
            this.b3Save.Size = new System.Drawing.Size(56, 28);
            this.b3Save.TabIndex = 3;
            this.b3Save.Text = "Save";
            this.b3Save.UseVisualStyleBackColor = true;
            this.b3Save.Click += new System.EventHandler(this.b3Save_Click);
            // 
            // b4Export
            // 
            this.b4Export.Location = new System.Drawing.Point(217, 8);
            this.b4Export.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.b4Export.Name = "b4Export";
            this.b4Export.Size = new System.Drawing.Size(56, 28);
            this.b4Export.TabIndex = 4;
            this.b4Export.Text = "Export";
            this.b4Export.UseVisualStyleBackColor = true;
            this.b4Export.Click += new System.EventHandler(this.b4Export_Click);
            // 
            // b5FNC
            // 
            this.b5FNC.Location = new System.Drawing.Point(286, 8);
            this.b5FNC.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.b5FNC.Name = "b5FNC";
            this.b5FNC.Size = new System.Drawing.Size(56, 28);
            this.b5FNC.TabIndex = 5;
            this.b5FNC.Text = "FNC";
            this.b5FNC.UseVisualStyleBackColor = true;
            this.b5FNC.Click += new System.EventHandler(this.b5FNC_Click);
            // 
            // b6ShowFeatures
            // 
            this.b6ShowFeatures.Location = new System.Drawing.Point(358, 8);
            this.b6ShowFeatures.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.b6ShowFeatures.Name = "b6ShowFeatures";
            this.b6ShowFeatures.Size = new System.Drawing.Size(98, 28);
            this.b6ShowFeatures.TabIndex = 6;
            this.b6ShowFeatures.Text = "Show Features";
            this.b6ShowFeatures.UseVisualStyleBackColor = true;
            this.b6ShowFeatures.Click += new System.EventHandler(this.b6ShowFeatures_Click);
            // 
            // b7Measure
            // 
            this.b7Measure.Location = new System.Drawing.Point(473, 8);
            this.b7Measure.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.b7Measure.Name = "b7Measure";
            this.b7Measure.Size = new System.Drawing.Size(62, 28);
            this.b7Measure.TabIndex = 7;
            this.b7Measure.Text = "Measure";
            this.b7Measure.UseVisualStyleBackColor = true;
            this.b7Measure.Click += new System.EventHandler(this.b7Measure_Click);
            // 
            // listMacroView
            // 
            this.listMacroView.CheckBoxes = true;
            this.listMacroView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listMacroView.FullRowSelect = true;
            this.listMacroView.GridLines = true;
            this.listMacroView.HideSelection = false;
            this.listMacroView.Location = new System.Drawing.Point(0, 0);
            this.listMacroView.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.listMacroView.Name = "listMacroView";
            this.listMacroView.Size = new System.Drawing.Size(666, 204);
            this.listMacroView.TabIndex = 7;
            this.listMacroView.UseCompatibleStateImageBehavior = false;
            this.listMacroView.View = System.Windows.Forms.View.Details;
            this.listMacroView.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.OnItemCheckedMacroList);
            this.listMacroView.SelectedIndexChanged += new System.EventHandler(this.OnSelChangeMacroList);
            this.listMacroView.DragDrop += new System.Windows.Forms.DragEventHandler(this.OnDragDropMacroList);
            this.listMacroView.DragEnter += new System.Windows.Forms.DragEventHandler(this.OnDragEnterMacroList);
            this.listMacroView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnKeyDownMacroLIst);
            this.listMacroView.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.OnKeyPressMacroList);
            this.listMacroView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OnClickMacroList);
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 40);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.design1);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.nestedSplitContainer);
            this.splitContainer.Size = new System.Drawing.Size(913, 659);
            this.splitContainer.SplitterDistance = 453;
            this.splitContainer.SplitterWidth = 2;
            this.splitContainer.TabIndex = 8;
            // 
            // nestedSplitContainer
            // 
            this.nestedSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nestedSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.nestedSplitContainer.Name = "nestedSplitContainer";
            // 
            // nestedSplitContainer.Panel1
            // 
            this.nestedSplitContainer.Panel1.Controls.Add(this.listMacroView);
            // 
            // nestedSplitContainer.Panel2
            // 
            this.nestedSplitContainer.Panel2.Controls.Add(this.pictureBox);
            this.nestedSplitContainer.Size = new System.Drawing.Size(913, 204);
            this.nestedSplitContainer.SplitterDistance = 666;
            this.nestedSplitContainer.TabIndex = 8;
            // 
            // pictureBox
            // 
            this.pictureBox.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.pictureBox.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox.Location = new System.Drawing.Point(0, 0);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(243, 204);
            this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox.TabIndex = 0;
            this.pictureBox.TabStop = false;
            // 
            // panel
            // 
            this.panel.Controls.Add(this.splitContainer);
            this.panel.Controls.Add(this.b7Measure);
            this.panel.Controls.Add(this.b5FNC);
            this.panel.Controls.Add(this.b6ShowFeatures);
            this.panel.Controls.Add(this.b4Export);
            this.panel.Controls.Add(this.button1);
            this.panel.Controls.Add(this.b3Save);
            this.panel.Controls.Add(this.button2);
            this.panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel.Location = new System.Drawing.Point(0, 0);
            this.panel.Name = "panel";
            this.panel.Padding = new System.Windows.Forms.Padding(0, 40, 0, 0);
            this.panel.Size = new System.Drawing.Size(913, 699);
            this.panel.TabIndex = 9;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(913, 699);
            this.Controls.Add(this.panel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "Form1";
            this.Text = "Ficep RobServer";
            ((System.ComponentModel.ISupportInitialize)(this.design1)).EndInit();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.nestedSplitContainer.Panel1.ResumeLayout(false);
            this.nestedSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nestedSplitContainer)).EndInit();
            this.nestedSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.panel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private devDept.Eyeshot.Control.Design design1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button b3Save;
        private System.Windows.Forms.Button b4Export;
        private System.Windows.Forms.Button b5FNC;
        private System.Windows.Forms.Button b6ShowFeatures;
        private System.Windows.Forms.Button b7Measure;
        private System.Windows.Forms.ListView listMacroView;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.Panel panel;
        private System.Windows.Forms.SplitContainer nestedSplitContainer;
        private System.Windows.Forms.PictureBox pictureBox;
    }
}



namespace FicepControls
{
    partial class MeasurePanel
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblDistance = new System.Windows.Forms.Label();
            this.txtDistance = new System.Windows.Forms.TextBox();
            this.txtDX = new System.Windows.Forms.TextBox();
            this.lblDX = new System.Windows.Forms.Label();
            this.txtDY = new System.Windows.Forms.TextBox();
            this.lblDY = new System.Windows.Forms.Label();
            this.txtDZ = new System.Windows.Forms.TextBox();
            this.lblDZ = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblDistance
            // 
            this.lblDistance.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblDistance.Location = new System.Drawing.Point(4, 9);
            this.lblDistance.Name = "lblDistance";
            this.lblDistance.Size = new System.Drawing.Size(49, 13);
            this.lblDistance.TabIndex = 0;
            this.lblDistance.Text = "Distance";
            // 
            // txtDistance
            // 
            this.txtDistance.BackColor = System.Drawing.Color.WhiteSmoke;
            this.txtDistance.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtDistance.Location = new System.Drawing.Point(59, 9);
            this.txtDistance.Name = "txtDistance";
            this.txtDistance.ReadOnly = true;
            this.txtDistance.Size = new System.Drawing.Size(74, 13);
            this.txtDistance.TabIndex = 1;
            this.txtDistance.Text = "-";
            this.txtDistance.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // txtDX
            // 
            this.txtDX.BackColor = System.Drawing.Color.WhiteSmoke;
            this.txtDX.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtDX.Location = new System.Drawing.Point(59, 41);
            this.txtDX.Name = "txtDX";
            this.txtDX.ReadOnly = true;
            this.txtDX.Size = new System.Drawing.Size(74, 13);
            this.txtDX.TabIndex = 3;
            this.txtDX.Text = "-";
            this.txtDX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // lblDX
            // 
            this.lblDX.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblDX.Location = new System.Drawing.Point(4, 41);
            this.lblDX.Name = "lblDX";
            this.lblDX.Size = new System.Drawing.Size(49, 13);
            this.lblDX.TabIndex = 2;
            this.lblDX.Text = "dX";
            // 
            // txtDY
            // 
            this.txtDY.BackColor = System.Drawing.Color.WhiteSmoke;
            this.txtDY.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtDY.Location = new System.Drawing.Point(59, 60);
            this.txtDY.Name = "txtDY";
            this.txtDY.ReadOnly = true;
            this.txtDY.Size = new System.Drawing.Size(74, 13);
            this.txtDY.TabIndex = 5;
            this.txtDY.Text = "-";
            this.txtDY.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // lblDY
            // 
            this.lblDY.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblDY.Location = new System.Drawing.Point(4, 60);
            this.lblDY.Name = "lblDY";
            this.lblDY.Size = new System.Drawing.Size(49, 13);
            this.lblDY.TabIndex = 4;
            this.lblDY.Text = "dY";
            // 
            // txtDZ
            // 
            this.txtDZ.BackColor = System.Drawing.Color.WhiteSmoke;
            this.txtDZ.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtDZ.Location = new System.Drawing.Point(59, 79);
            this.txtDZ.Name = "txtDZ";
            this.txtDZ.ReadOnly = true;
            this.txtDZ.Size = new System.Drawing.Size(74, 13);
            this.txtDZ.TabIndex = 7;
            this.txtDZ.Text = "-";
            this.txtDZ.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // lblDZ
            // 
            this.lblDZ.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblDZ.Location = new System.Drawing.Point(4, 79);
            this.lblDZ.Name = "lblDZ";
            this.lblDZ.Size = new System.Drawing.Size(49, 13);
            this.lblDZ.TabIndex = 6;
            this.lblDZ.Text = "dZ";
            // 
            // MeasurePanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.txtDZ);
            this.Controls.Add(this.lblDZ);
            this.Controls.Add(this.txtDY);
            this.Controls.Add(this.lblDY);
            this.Controls.Add(this.txtDX);
            this.Controls.Add(this.lblDX);
            this.Controls.Add(this.txtDistance);
            this.Controls.Add(this.lblDistance);
            this.Name = "MeasurePanel";
            this.Size = new System.Drawing.Size(136, 100);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblDistance;
        private System.Windows.Forms.TextBox txtDistance;
        private System.Windows.Forms.TextBox txtDX;
        private System.Windows.Forms.Label lblDX;
        private System.Windows.Forms.TextBox txtDY;
        private System.Windows.Forms.Label lblDY;
        private System.Windows.Forms.TextBox txtDZ;
        private System.Windows.Forms.Label lblDZ;
    }
}

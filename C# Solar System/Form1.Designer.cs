namespace W3D
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.axles3D1 = new AxlesGL.Axles3D();
            this.SuspendLayout();
            // 
            // axles3D1
            // 
            this.axles3D1.AngleX = 0D;
            this.axles3D1.AngleY = 30D;
            this.axles3D1.AngleZ = -30D;
            this.axles3D1.Azimuth = 0D;
            this.axles3D1.BackColor = System.Drawing.Color.Black;
            this.axles3D1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axles3D1.Elevation = 0D;
            this.axles3D1.Location = new System.Drawing.Point(0, 0);
            this.axles3D1.Name = "axles3D1";
            this.axles3D1.Size = new System.Drawing.Size(478, 405);
            this.axles3D1.TabIndex = 0;
            this.axles3D1.VSync = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(478, 405);
            this.Controls.Add(this.axles3D1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private AxlesGL.Axles3D axles3D1;
    }
}


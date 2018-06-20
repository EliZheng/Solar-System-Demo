namespace AxlesGL
{
	partial class Axles3D
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

		#region 组件设计器生成的代码

		/// <summary> 
		/// 设计器支持所需的方法 - 不要
		/// 使用代码编辑器修改此方法的内容。
		/// </summary>
		private void InitializeComponent()
		{
            this.SuspendLayout();
            // 
            // Axles3D
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.Name = "Axles3D";
            this.Size = new System.Drawing.Size(227, 200);
            this.Load += new System.EventHandler(this.Axles3D_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Axles3D_Paint);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Axles3D_KeyPress);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Axles3D_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Axles3D_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Axles3D_MouseUp);
            this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.axles3D1_MouseWheel);
            this.Resize += new System.EventHandler(this.Axles3D_Resize);
            this.ResumeLayout(false);

		}

		#endregion





	}
}

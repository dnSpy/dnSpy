// <file>
//     <copyright name="David Srbecký" email="dsrbecky@gmail.com"/>
//     <license name="GPL"/>
// </file>
namespace Decompiler
{
	partial class MainForm
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
			this.collapseCount = new System.Windows.Forms.NumericUpDown();
			this.reduceCount = new System.Windows.Forms.NumericUpDown();
			this.collapseBtn = new System.Windows.Forms.Button();
			this.reduceBtn = new System.Windows.Forms.Button();
			this.decompileBtn = new System.Windows.Forms.Button();
			this.sourceCodeBox = new System.Windows.Forms.RichTextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.filter = new System.Windows.Forms.TextBox();
			((System.ComponentModel.ISupportInitialize)(this.collapseCount)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.reduceCount)).BeginInit();
			this.SuspendLayout();
			// 
			// collapseCount
			// 
			this.collapseCount.Location = new System.Drawing.Point(12, 12);
			this.collapseCount.Maximum = new decimal(new int[] {
									100000,
									0,
									0,
									0});
			this.collapseCount.Name = "collapseCount";
			this.collapseCount.Size = new System.Drawing.Size(78, 26);
			this.collapseCount.TabIndex = 1;
			this.collapseCount.Value = new decimal(new int[] {
									1000,
									0,
									0,
									0});
			this.collapseCount.ValueChanged += new System.EventHandler(this.Decompile);
			// 
			// reduceCount
			// 
			this.reduceCount.Location = new System.Drawing.Point(266, 12);
			this.reduceCount.Maximum = new decimal(new int[] {
									100000,
									0,
									0,
									0});
			this.reduceCount.Name = "reduceCount";
			this.reduceCount.Size = new System.Drawing.Size(70, 26);
			this.reduceCount.TabIndex = 2;
			this.reduceCount.Value = new decimal(new int[] {
									1000,
									0,
									0,
									0});
			this.reduceCount.ValueChanged += new System.EventHandler(this.Decompile);
			// 
			// collapseBtn
			// 
			this.collapseBtn.Location = new System.Drawing.Point(96, 12);
			this.collapseBtn.Name = "collapseBtn";
			this.collapseBtn.Size = new System.Drawing.Size(164, 26);
			this.collapseBtn.TabIndex = 3;
			this.collapseBtn.Text = "Collapse expression";
			this.collapseBtn.UseVisualStyleBackColor = true;
			this.collapseBtn.Click += new System.EventHandler(this.CollapseBtnClick);
			// 
			// reduceBtn
			// 
			this.reduceBtn.Location = new System.Drawing.Point(342, 12);
			this.reduceBtn.Name = "reduceBtn";
			this.reduceBtn.Size = new System.Drawing.Size(143, 26);
			this.reduceBtn.TabIndex = 4;
			this.reduceBtn.Text = "Reduce graph";
			this.reduceBtn.UseVisualStyleBackColor = true;
			this.reduceBtn.Click += new System.EventHandler(this.ReduceBtnClick);
			// 
			// decompileBtn
			// 
			this.decompileBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.decompileBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.decompileBtn.Location = new System.Drawing.Point(799, 12);
			this.decompileBtn.Name = "decompileBtn";
			this.decompileBtn.Size = new System.Drawing.Size(109, 26);
			this.decompileBtn.TabIndex = 5;
			this.decompileBtn.Text = "Decompile";
			this.decompileBtn.UseVisualStyleBackColor = true;
			this.decompileBtn.Click += new System.EventHandler(this.Decompile);
			// 
			// sourceCodeBox
			// 
			this.sourceCodeBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
									| System.Windows.Forms.AnchorStyles.Left) 
									| System.Windows.Forms.AnchorStyles.Right)));
			this.sourceCodeBox.Font = new System.Drawing.Font("Courier New", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.sourceCodeBox.Location = new System.Drawing.Point(12, 74);
			this.sourceCodeBox.Name = "sourceCodeBox";
			this.sourceCodeBox.Size = new System.Drawing.Size(896, 603);
			this.sourceCodeBox.TabIndex = 6;
			this.sourceCodeBox.Text = "";
			this.sourceCodeBox.WordWrap = false;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(491, 15);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(48, 20);
			this.label1.TabIndex = 7;
			this.label1.Text = "Filter:";
			// 
			// filter
			// 
			this.filter.Location = new System.Drawing.Point(545, 12);
			this.filter.Name = "filter";
			this.filter.Size = new System.Drawing.Size(100, 26);
			this.filter.TabIndex = 8;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(920, 689);
			this.Controls.Add(this.filter);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.sourceCodeBox);
			this.Controls.Add(this.decompileBtn);
			this.Controls.Add(this.reduceBtn);
			this.Controls.Add(this.collapseBtn);
			this.Controls.Add(this.reduceCount);
			this.Controls.Add(this.collapseCount);
			this.Name = "MainForm";
			this.Text = "Decompiler";
			((System.ComponentModel.ISupportInitialize)(this.collapseCount)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.reduceCount)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();
		}
		private System.Windows.Forms.TextBox filter;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.RichTextBox sourceCodeBox;
		private System.Windows.Forms.Button decompileBtn;
		private System.Windows.Forms.Button reduceBtn;
		private System.Windows.Forms.Button collapseBtn;
		private System.Windows.Forms.NumericUpDown reduceCount;
		private System.Windows.Forms.NumericUpDown collapseCount;
	}
}

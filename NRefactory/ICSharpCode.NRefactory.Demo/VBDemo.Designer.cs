// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

namespace ICSharpCode.NRefactory.Demo
{
	partial class VBDemo
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the control.
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
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.codeView = new System.Windows.Forms.TextBox();
			this.generateCodeButton = new System.Windows.Forms.Button();
			this.parseButton = new System.Windows.Forms.Button();
			this.treeView = new System.Windows.Forms.TreeView();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.SuspendLayout();
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.codeView);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.generateCodeButton);
			this.splitContainer1.Panel2.Controls.Add(this.parseButton);
			this.splitContainer1.Panel2.Controls.Add(this.treeView);
			this.splitContainer1.Size = new System.Drawing.Size(462, 391);
			this.splitContainer1.SplitterDistance = 173;
			this.splitContainer1.TabIndex = 1;
			// 
			// codeView
			// 
			this.codeView.AcceptsReturn = true;
			this.codeView.AcceptsTab = true;
			this.codeView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.codeView.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.codeView.HideSelection = false;
			this.codeView.Location = new System.Drawing.Point(0, 0);
			this.codeView.Multiline = true;
			this.codeView.Name = "codeView";
			this.codeView.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.codeView.Size = new System.Drawing.Size(462, 173);
			this.codeView.TabIndex = 0;
			this.codeView.Text = "Option Explicit";
			this.codeView.WordWrap = false;
			// 
			// generateCodeButton
			// 
			this.generateCodeButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.generateCodeButton.Location = new System.Drawing.Point(225, 2);
			this.generateCodeButton.Name = "generateCodeButton";
			this.generateCodeButton.Size = new System.Drawing.Size(100, 23);
			this.generateCodeButton.TabIndex = 1;
			this.generateCodeButton.Text = "Generate";
			this.generateCodeButton.UseVisualStyleBackColor = true;
			this.generateCodeButton.Click += new System.EventHandler(this.CSharpGenerateCodeButtonClick);
			// 
			// parseButton
			// 
			this.parseButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.parseButton.Location = new System.Drawing.Point(119, 2);
			this.parseButton.Name = "parseButton";
			this.parseButton.Size = new System.Drawing.Size(100, 23);
			this.parseButton.TabIndex = 0;
			this.parseButton.Text = "Parse";
			this.parseButton.UseVisualStyleBackColor = true;
			this.parseButton.Click += new System.EventHandler(this.CSharpParseButtonClick);
			// 
			// treeView
			// 
			this.treeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
									| System.Windows.Forms.AnchorStyles.Left) 
									| System.Windows.Forms.AnchorStyles.Right)));
			this.treeView.Location = new System.Drawing.Point(3, 31);
			this.treeView.Name = "treeView";
			this.treeView.Size = new System.Drawing.Size(459, 180);
			this.treeView.TabIndex = 0;
			this.treeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.CSharpTreeViewAfterSelect);
			// 
			// VBDemo
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.splitContainer1);
			this.Name = "VBDemo";
			this.Size = new System.Drawing.Size(462, 391);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel1.PerformLayout();
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.ResumeLayout(false);
		}
		private System.Windows.Forms.TextBox codeView;
		private System.Windows.Forms.TreeView treeView;
		private System.Windows.Forms.Button generateCodeButton;
		private System.Windows.Forms.Button parseButton;
		private System.Windows.Forms.SplitContainer splitContainer1;
	}
}

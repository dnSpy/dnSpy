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
	partial class CSDemo
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
			this.csharpCodeTextBox = new System.Windows.Forms.TextBox();
			this.findReferencesButton = new System.Windows.Forms.Button();
			this.resolveButton = new System.Windows.Forms.Button();
			this.csharpTreeView = new System.Windows.Forms.TreeView();
			this.csharpGenerateCodeButton = new System.Windows.Forms.Button();
			this.csharpParseButton = new System.Windows.Forms.Button();
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
			this.splitContainer1.Panel1.Controls.Add(this.csharpCodeTextBox);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.findReferencesButton);
			this.splitContainer1.Panel2.Controls.Add(this.resolveButton);
			this.splitContainer1.Panel2.Controls.Add(this.csharpTreeView);
			this.splitContainer1.Panel2.Controls.Add(this.csharpGenerateCodeButton);
			this.splitContainer1.Panel2.Controls.Add(this.csharpParseButton);
			this.splitContainer1.Size = new System.Drawing.Size(475, 406);
			this.splitContainer1.SplitterDistance = 178;
			this.splitContainer1.TabIndex = 1;
			// 
			// csharpCodeTextBox
			// 
			this.csharpCodeTextBox.AcceptsReturn = true;
			this.csharpCodeTextBox.AcceptsTab = true;
			this.csharpCodeTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.csharpCodeTextBox.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.csharpCodeTextBox.HideSelection = false;
			this.csharpCodeTextBox.Location = new System.Drawing.Point(0, 0);
			this.csharpCodeTextBox.MaxLength = 99999999;
			this.csharpCodeTextBox.Multiline = true;
			this.csharpCodeTextBox.Name = "csharpCodeTextBox";
			this.csharpCodeTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.csharpCodeTextBox.Size = new System.Drawing.Size(475, 178);
			this.csharpCodeTextBox.TabIndex = 0;
			this.csharpCodeTextBox.Text = "using System;\r\nusing System.Linq;\r\nclass Test\r\n{\r\n    public void Main(string[] a" +
			"rgs)\r\n    {\r\n         Console.WriteLine(\"Hello, World\");\r\n    }\r\n}";
			this.csharpCodeTextBox.WordWrap = false;
			this.csharpCodeTextBox.TextChanged += new System.EventHandler(this.CsharpCodeTextBoxTextChanged);
			// 
			// findReferencesButton
			// 
			this.findReferencesButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.findReferencesButton.Enabled = false;
			this.findReferencesButton.Location = new System.Drawing.Point(344, 4);
			this.findReferencesButton.Name = "findReferencesButton";
			this.findReferencesButton.Size = new System.Drawing.Size(100, 23);
			this.findReferencesButton.TabIndex = 4;
			this.findReferencesButton.Text = "Find References";
			this.findReferencesButton.UseVisualStyleBackColor = true;
			this.findReferencesButton.Click += new System.EventHandler(this.FindReferencesButtonClick);
			// 
			// resolveButton
			// 
			this.resolveButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.resolveButton.Location = new System.Drawing.Point(132, 4);
			this.resolveButton.Name = "resolveButton";
			this.resolveButton.Size = new System.Drawing.Size(100, 23);
			this.resolveButton.TabIndex = 3;
			this.resolveButton.Text = "Resolve";
			this.resolveButton.UseVisualStyleBackColor = true;
			this.resolveButton.Click += new System.EventHandler(this.ResolveButtonClick);
			// 
			// csharpTreeView
			// 
			this.csharpTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
									| System.Windows.Forms.AnchorStyles.Left) 
									| System.Windows.Forms.AnchorStyles.Right)));
			this.csharpTreeView.HideSelection = false;
			this.csharpTreeView.Location = new System.Drawing.Point(3, 32);
			this.csharpTreeView.Name = "csharpTreeView";
			this.csharpTreeView.Size = new System.Drawing.Size(467, 189);
			this.csharpTreeView.TabIndex = 2;
			this.csharpTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.CSharpTreeViewAfterSelect);
			// 
			// csharpGenerateCodeButton
			// 
			this.csharpGenerateCodeButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.csharpGenerateCodeButton.Location = new System.Drawing.Point(238, 4);
			this.csharpGenerateCodeButton.Name = "csharpGenerateCodeButton";
			this.csharpGenerateCodeButton.Size = new System.Drawing.Size(100, 23);
			this.csharpGenerateCodeButton.TabIndex = 1;
			this.csharpGenerateCodeButton.Text = "Generate";
			this.csharpGenerateCodeButton.UseVisualStyleBackColor = true;
			this.csharpGenerateCodeButton.Click += new System.EventHandler(this.CSharpGenerateCodeButtonClick);
			// 
			// csharpParseButton
			// 
			this.csharpParseButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.csharpParseButton.Location = new System.Drawing.Point(26, 4);
			this.csharpParseButton.Name = "csharpParseButton";
			this.csharpParseButton.Size = new System.Drawing.Size(100, 23);
			this.csharpParseButton.TabIndex = 0;
			this.csharpParseButton.Text = "Parse";
			this.csharpParseButton.UseVisualStyleBackColor = true;
			this.csharpParseButton.Click += new System.EventHandler(this.CSharpParseButtonClick);
			// 
			// CSDemo
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.splitContainer1);
			this.Name = "CSDemo";
			this.Size = new System.Drawing.Size(475, 406);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel1.PerformLayout();
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.ResumeLayout(false);
		}
		private System.Windows.Forms.Button findReferencesButton;
		private System.Windows.Forms.Button csharpParseButton;
		private System.Windows.Forms.Button csharpGenerateCodeButton;
		private System.Windows.Forms.TreeView csharpTreeView;
		private System.Windows.Forms.Button resolveButton;
		private System.Windows.Forms.TextBox csharpCodeTextBox;
		private System.Windows.Forms.SplitContainer splitContainer1;
	}
}

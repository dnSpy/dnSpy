// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)
namespace ICSharpCode.NRefactory.Demo
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
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.csharpCodeTextBox = new System.Windows.Forms.TextBox();
			this.resolveButton = new System.Windows.Forms.Button();
			this.csharpTreeView = new System.Windows.Forms.TreeView();
			this.csharpGenerateCodeButton = new System.Windows.Forms.Button();
			this.csharpParseButton = new System.Windows.Forms.Button();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.miniToolStrip = new System.Windows.Forms.ToolStrip();
			this.vbDemo1 = new ICSharpCode.NRefactory.Demo.VBDemo();
			this.tabPage2.SuspendLayout();
			this.tabPage1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.tabControl1.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.vbDemo1);
			this.tabPage2.Location = new System.Drawing.Point(4, 22);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(507, 458);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "VB";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.splitContainer1);
			this.tabPage1.Location = new System.Drawing.Point(4, 22);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(507, 458);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "C#";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(3, 3);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.csharpCodeTextBox);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.resolveButton);
			this.splitContainer1.Panel2.Controls.Add(this.csharpTreeView);
			this.splitContainer1.Panel2.Controls.Add(this.csharpGenerateCodeButton);
			this.splitContainer1.Panel2.Controls.Add(this.csharpParseButton);
			this.splitContainer1.Size = new System.Drawing.Size(501, 452);
			this.splitContainer1.SplitterDistance = 201;
			this.splitContainer1.TabIndex = 0;
			// 
			// csharpCodeTextBox
			// 
			this.csharpCodeTextBox.AcceptsReturn = true;
			this.csharpCodeTextBox.AcceptsTab = true;
			this.csharpCodeTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.csharpCodeTextBox.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.csharpCodeTextBox.HideSelection = false;
			this.csharpCodeTextBox.Location = new System.Drawing.Point(0, 0);
			this.csharpCodeTextBox.Multiline = true;
			this.csharpCodeTextBox.Name = "csharpCodeTextBox";
			this.csharpCodeTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.csharpCodeTextBox.Size = new System.Drawing.Size(501, 201);
			this.csharpCodeTextBox.TabIndex = 0;
			this.csharpCodeTextBox.Text = "using System;\r\nclass Test\r\n{\r\n    public void Main(string[] args)\r\n    {\r\n       " +
			"  Console.WriteLine(\"Hello, World\");\r\n    }\r\n}";
			this.csharpCodeTextBox.WordWrap = false;
			this.csharpCodeTextBox.TextChanged += new System.EventHandler(this.CsharpCodeTextBoxTextChanged);
			this.csharpCodeTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CSharpCodeTextBoxKeyDown);
			// 
			// resolveButton
			// 
			this.resolveButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.resolveButton.Location = new System.Drawing.Point(200, 3);
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
			this.csharpTreeView.Size = new System.Drawing.Size(493, 212);
			this.csharpTreeView.TabIndex = 2;
			this.csharpTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.CSharpTreeViewAfterSelect);
			// 
			// csharpGenerateCodeButton
			// 
			this.csharpGenerateCodeButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.csharpGenerateCodeButton.Location = new System.Drawing.Point(306, 2);
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
			this.csharpParseButton.Location = new System.Drawing.Point(94, 3);
			this.csharpParseButton.Name = "csharpParseButton";
			this.csharpParseButton.Size = new System.Drawing.Size(100, 23);
			this.csharpParseButton.TabIndex = 0;
			this.csharpParseButton.Text = "Parse";
			this.csharpParseButton.UseVisualStyleBackColor = true;
			this.csharpParseButton.Click += new System.EventHandler(this.CSharpParseButtonClick);
			// 
			// tabControl1
			// 
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(515, 484);
			this.tabControl1.TabIndex = 0;
			// 
			// miniToolStrip
			// 
			this.miniToolStrip.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.miniToolStrip.AutoSize = false;
			this.miniToolStrip.CanOverflow = false;
			this.miniToolStrip.Dock = System.Windows.Forms.DockStyle.None;
			this.miniToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.miniToolStrip.Location = new System.Drawing.Point(13, 3);
			this.miniToolStrip.Name = "miniToolStrip";
			this.miniToolStrip.Size = new System.Drawing.Size(16, 25);
			this.miniToolStrip.TabIndex = 3;
			// 
			// vbDemo1
			// 
			this.vbDemo1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.vbDemo1.Location = new System.Drawing.Point(3, 3);
			this.vbDemo1.Name = "vbDemo1";
			this.vbDemo1.Size = new System.Drawing.Size(501, 452);
			this.vbDemo1.TabIndex = 0;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(515, 484);
			this.Controls.Add(this.tabControl1);
			this.Name = "MainForm";
			this.Text = "NRefactory Demo";
			this.tabPage2.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel1.PerformLayout();
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.tabControl1.ResumeLayout(false);
			this.ResumeLayout(false);
		}
		private ICSharpCode.NRefactory.Demo.VBDemo vbDemo1;
		private System.Windows.Forms.Button resolveButton;
		private System.Windows.Forms.ToolStrip miniToolStrip;
		private System.Windows.Forms.TreeView csharpTreeView;
		private System.Windows.Forms.Button csharpParseButton;
		private System.Windows.Forms.Button csharpGenerateCodeButton;
		private System.Windows.Forms.TextBox csharpCodeTextBox;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.TabControl tabControl1;
	}
}

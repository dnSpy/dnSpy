using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Mono.Cecil;

namespace Decompiler
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
		string filename;
		
		public MainForm(string filename)
		{
			this.filename = filename;
			InitializeComponent();
		}
		
		public string SourceCode {
			get {
				return sourceCodeBox.Text;
			}
			set {
				sourceCodeBox.Text = value;
			}
		}
		
		public void Decompile()
		{
			ControlFlow.Node.NextNodeID = 0;
			Options.CollapseExpression = (int)collapseCount.Value;
			Options.ReduceGraph = (int)reduceCount.Value;
			
			AssemblyDefinition assembly = AssemblyFactory.GetAssembly(filename);
			AstBuilder codeDomBuilder = new AstBuilder();
			codeDomBuilder.AddAssembly(assembly);
			SourceCode = codeDomBuilder.GenerateCode();
		}
		
		void CollapseBtnClick(object sender, EventArgs e)
		{
			collapseCount.Value++;
			Decompile();
		}
		
		void ReduceBtnClick(object sender, EventArgs e)
		{
			reduceCount.Value++;
			Decompile();
		}
		
		void DecompileBtnClick(object sender, EventArgs e)
		{
			Decompile();
		}
		
		void CollapseCountValueChanged(object sender, EventArgs e)
		{
			Decompile();
		}
		
		void ReduceCountValueChanged(object sender, EventArgs e)
		{
			Decompile();
		}
	}
}

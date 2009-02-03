using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
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
			int x = 16;
			int y = 46;
			foreach(FieldInfo _field in typeof(Options).GetFields()) {
				FieldInfo field = _field;
				if (field.FieldType == typeof(bool)) {
					CheckBox checkBox = new CheckBox();
					checkBox.Left = x;
					checkBox.Top = y;
					checkBox.AutoSize = true;
					checkBox.Text = field.Name;
					checkBox.Checked = (bool)field.GetValue(null);
					checkBox.CheckedChanged += delegate {
						field.SetValue(null, checkBox.Checked);
						Decompile();
					};
					this.Controls.Add(checkBox);
					x += checkBox.Width + 10;
				}
			}
			//filter.Text = "ReversiForm";
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
			Options.TypeFilter = filter.Text;
			
			
			AssemblyDefinition assembly = AssemblyFactory.GetAssembly(filename);
			AstBuilder codeDomBuilder = new AstBuilder();
			codeDomBuilder.AddAssembly(assembly);
			SourceCode = codeDomBuilder.GenerateCode();
			
			File.WriteAllText("output.cs", SourceCode);
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
		
		void Decompile(object sender, EventArgs e)
		{
			Decompile();
		}
	}
}

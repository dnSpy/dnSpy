using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Decompiler
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
		}
		
		public string SourceCode {
			get {
				return sourceCode.Text;
			}
			set {
				sourceCode.Text = value;
			}
		}
	}
}

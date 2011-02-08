// <file>
//     <copyright name="David Srbecký" email="dsrbecky@gmail.com"/>
//     <license name="GPL"/>
// </file>

using System;
using System.Windows.Forms;

using Mono.Cecil;

namespace Decompiler
{
	/// <summary>
	/// Class with program entry point.
	/// </summary>
	internal sealed class Program
	{
		/// <summary>
		/// Program entry point.
		/// </summary>
		[STAThread]
		private static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			
			OpenFileDialog openFile = new OpenFileDialog();
			openFile.Filter = "Executable (*.exe)|*.exe";
			if (openFile.ShowDialog() != DialogResult.OK) return;
			string sourceFilename = openFile.FileName;
			
			MainForm mainForm = new MainForm(sourceFilename);
			mainForm.Decompile();
			Application.Run(mainForm);
		}
	}
}

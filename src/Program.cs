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
			string sourceCode = Decompile(@"..\..\tests\ClassStructure\bin\Debug\ClassStructure.dll");
			
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			MainForm mainForm = new MainForm();
			mainForm.SourceCode = sourceCode;
			Application.Run(mainForm);
		}
		
		static string Decompile(string filename)
		{
			AssemblyDefinition assembly = AssemblyFactory.GetAssembly(filename);
			CodeDomBuilder codeDomBuilder = new CodeDomBuilder();
			codeDomBuilder.AddAssembly(assembly);
			return codeDomBuilder.GenerateCode();
		}
	}
}

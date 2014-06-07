// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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

#if DEBUG

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ICSharpCode.ILSpy.TextView;

namespace ICSharpCode.ILSpy
{
	[ExportMainMenuCommand(Menu = "_File", Header = "DEBUG -- Decompile All", MenuCategory = "Open", MenuOrder = 2.5)]
	sealed class DecompileAllCommand : SimpleCommand
	{
		public override bool CanExecute(object parameter)
		{
			return System.IO.Directory.Exists("c:\\temp\\decompiled");
		}

		public override void Execute(object parameter)
		{
			MainWindow.Instance.TextView.RunWithCancellation(ct => Task<AvalonEditTextOutput>.Factory.StartNew(() => {
				AvalonEditTextOutput output = new AvalonEditTextOutput();
				Parallel.ForEach(MainWindow.Instance.CurrentAssemblyList.GetAssemblies(), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = ct }, delegate(LoadedAssembly asm) {
					if (!asm.HasLoadError) {
						Stopwatch w = Stopwatch.StartNew();
						Exception exception = null;
						using (var writer = new System.IO.StreamWriter("c:\\temp\\decompiled\\" + asm.ShortName + ".cs")) {
							try {
								new CSharpLanguage().DecompileAssembly(asm, new Decompiler.PlainTextOutput(writer), new DecompilationOptions { FullDecompilation = true, CancellationToken = ct });
							}
							catch (Exception ex) {
								writer.WriteLine(ex.ToString());
								exception = ex;
							}
						}
						lock (output) {
							output.Write(asm.ShortName + " - " + w.Elapsed);
							if (exception != null) {
								output.Write(" - ");
								output.Write(exception.GetType().Name);
							}
							output.WriteLine();
						}
					}
				});
				return output;
			}, ct)).Then(output => MainWindow.Instance.TextView.ShowText(output)).HandleExceptions();
		}
	}
}

#endif
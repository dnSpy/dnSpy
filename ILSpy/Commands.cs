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

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy
{
	[ExportMainMenuCommand(Menu = "_File", Header = "E_xit", MenuOrder = 99999, MenuCategory = "Exit")]
	sealed class ExitCommand : SimpleCommand
	{
		public override void Execute(object parameter)
		{
			MainWindow.Instance.Close();
		}
	}
	
	[ExportToolbarCommand(ToolTip = "Back", ToolbarIcon = "Images/Back.png", ToolbarCategory = "Navigation", ToolbarOrder = 0)]
	sealed class BrowseBackCommand : CommandWrapper {
		public BrowseBackCommand() : base(NavigationCommands.BrowseBack) {}
	}
	
	[ExportToolbarCommand(ToolTip = "Forward", ToolbarIcon = "Images/Forward.png", ToolbarCategory = "Navigation", ToolbarOrder = 1)]
	sealed class BrowseForwardCommand : CommandWrapper {
		public BrowseForwardCommand() : base(NavigationCommands.BrowseForward) {}
	}
	
	[ExportToolbarCommand(ToolTip = "Open", ToolbarIcon = "Images/Open.png", ToolbarCategory = "Open", ToolbarOrder = 0)]
	[ExportMainMenuCommand(Menu = "_File", MenuIcon = "Images/Open.png", MenuCategory = "Open", MenuOrder = 0)]
	sealed class OpenCommand : CommandWrapper {
		public OpenCommand() : base(ApplicationCommands.Open) {}
	}
	
	[ExportMainMenuCommand(Menu = "_File", Header = "Open from _GAC", MenuCategory = "Open", MenuOrder = 1)]
	sealed class OpenFromGacCommand : SimpleCommand
	{
		public override void Execute(object parameter)
		{
			OpenFromGacDialog dlg = new OpenFromGacDialog();
			dlg.Owner = MainWindow.Instance;
			if (dlg.ShowDialog() == true) {
				MainWindow.Instance.OpenFiles(dlg.SelectedFileNames);
			}
		}
	}
	
	[ExportToolbarCommand(ToolTip = "Reload all assemblies", ToolbarIcon = "Images/Refresh.png", ToolbarCategory = "Open", ToolbarOrder = 2)]
	[ExportMainMenuCommand(Menu = "_File", Header = "Reload", MenuIcon = "Images/Refresh.png", MenuCategory = "Open", MenuOrder = 2)]
	sealed class RefreshCommand : CommandWrapper {
		public RefreshCommand() : base(NavigationCommands.Refresh) {}
	}
	
	[ExportMainMenuCommand(Menu = "_File", Header = "_Save Code...", MenuIcon = "Images/SaveFile.png", MenuCategory = "Save", MenuOrder = 0)]
	sealed class SaveCommand : CommandWrapper
	{
		public SaveCommand() : base(ApplicationCommands.Save) {}
	}
	
	#if DEBUG
	[ExportMainMenuCommand(Menu = "_File", Header = "DEBUG -- Decompile All", MenuCategory = "Open", MenuOrder = 2.5)]
	sealed class DecompileAllCommand : SimpleCommand
	{
		public override bool CanExecute(object parameter)
		{
			return System.IO.Directory.Exists("c:\\temp\\decompiled");
		}
		
		public override void Execute(object parameter)
		{
			MainWindow.Instance.TextView.RunWithCancellation(
				ct => Task<AvalonEditTextOutput>.Factory.StartNew(
					() => {
						AvalonEditTextOutput output = new AvalonEditTextOutput();
						Parallel.ForEach(
							MainWindow.Instance.CurrentAssemblyList.GetAssemblies(),
							new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = ct },
							delegate (LoadedAssembly asm) {
								if (!asm.HasLoadError) {
									Stopwatch w = Stopwatch.StartNew();
									Exception exception = null;
									using (var writer = new System.IO.StreamWriter("c:\\temp\\decompiled\\" + asm.ShortName + ".cs")) {
										try {
											new CSharpLanguage().DecompileAssembly(
												asm, new Decompiler.PlainTextOutput(writer),
												new DecompilationOptions { FullDecompilation = true, CancellationToken = ct });
										} catch (Exception ex) {
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
					}
				),
				task => MainWindow.Instance.TextView.ShowText(task.Result));
		}
	}
	#endif
	
	class CommandWrapper : ICommand
	{
		ICommand wrappedCommand;
		
		public CommandWrapper(ICommand wrappedCommand)
		{
			this.wrappedCommand = wrappedCommand;
		}
		
		public static ICommand Unwrap(ICommand command)
		{
			CommandWrapper w = command as CommandWrapper;
			if (w != null)
				return w.wrappedCommand;
			else
				return command;
		}
		
		public event EventHandler CanExecuteChanged {
			add { wrappedCommand.CanExecuteChanged += value; }
			remove { wrappedCommand.CanExecuteChanged -= value; }
		}
		
		public void Execute(object parameter)
		{
			wrappedCommand.Execute(parameter);
		}
		
		public bool CanExecute(object parameter)
		{
			return wrappedCommand.CanExecute(parameter);
		}
	}
	
	public abstract class SimpleCommand : ICommand
	{
		public event EventHandler CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}
		
		public abstract void Execute(object parameter);
		
		public virtual bool CanExecute(object parameter)
		{
			return true;
		}
	}
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.AsmEditor
{
	[ExportMainMenuCommand(Menu = "_File", MenuCategory = "Save", MenuOrder = 1010)]
	sealed class SaveModuleCommand : TreeNodeCommand, IMainMenuCommandInitialize
	{
		public SaveModuleCommand()
		{
			MainWindow.Instance.SetMenuAlwaysRegenerate("_File");
		}

		HashSet<LoadedAssembly> GetAssemblyNodes(ILSpyTreeNode[] nodes)
		{
			var hash = new HashSet<LoadedAssembly>();
			foreach (var node in nodes) {
				var asmNode = ILSpyTreeNode.GetAssemblyTreeNode(node);
				if (asmNode != null && asmNode.LoadedAssembly.ModuleDefinition != null)
					hash.Add(asmNode.LoadedAssembly);
			}
			return hash;
		}

		protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes)
		{
			return GetAssemblyNodes(nodes).Count > 0;
		}

		protected override void ExecuteInternal(ILSpyTreeNode[] nodes)
		{
			var asmNodes = GetAssemblyNodes(nodes);
			SaveAssemblies(asmNodes);
		}

		public void Initialize(MenuItem menuItem)
		{
			menuItem.Header = GetAssemblyNodes(GetSelectedNodes()).Count <= 1 ? "Save _Module..." : "Save _Modules...";
		}

		public static void SaveAssemblies(IEnumerable<LoadedAssembly> asms)
		{
			var asmsAry = asms.ToArray();
			if (asmsAry.Length == 0)
				return;

			if (asmsAry.Length == 1) {
				var optsData = new SaveModuleOptionsVM(asmsAry[0].ModuleDefinition);
				var optsWin = new SaveModuleOptions();
				optsWin.Owner = MainWindow.Instance;
				optsWin.DataContext = optsData;
				var res = optsWin.ShowDialog();
				if (res != true)
					return;

				var data = new SaveMultiModuleVM(optsData);
				var win = new SaveSingleModule();
				win.Owner = MainWindow.Instance;
				win.DataContext = data;
				data.Save();
				win.ShowDialog();
				MarkAsSaved(data, asmsAry);
			}
			else {
				var data = new SaveMultiModuleVM(asmsAry.Select(a => a.ModuleDefinition));
				var win = new SaveMultiModule();
				win.Owner = MainWindow.Instance;
				win.DataContext = data;
				win.ShowDialog();
				MarkAsSaved(data, asmsAry);
			}
		}

		static void MarkAsSaved(SaveMultiModuleVM vm, LoadedAssembly[] nodes)
		{
			foreach (var asm in nodes) {
				if (vm.WasSaved(asm.ModuleDefinition))
					MainWindow.Instance.UndoCommandManager.MarkAsSaved(asm);
			}
		}
	}

	[ExportToolbarCommand(ToolTip = "Save All (Ctrl+Shift+S)",
						  ToolbarIcon = "Images/SaveAll.png",
						  ToolbarCategory = "Open",
						  ToolbarOrder = 2010)]
	sealed class SaveAllToolbarCommand : ICommand
	{
		public SaveAllToolbarCommand()
		{
			MainWindow.Instance.SetMenuAlwaysRegenerate("_File");
			var keyBinding = new KeyBinding(this, Key.S, ModifierKeys.Control | ModifierKeys.Shift);
			keyBinding.Command = this;
			MainWindow.Instance.InputBindings.Add(keyBinding);
		}

		static LoadedAssembly[] GetDirtyAssemblies()
		{
			var list = new List<LoadedAssembly>();
			foreach (var asmNode in MainWindow.Instance.UndoCommandManager.GetModifiedAssemblyTreeNodes())
				list.Add(asmNode.LoadedAssembly);
			return list.ToArray();
		}

		public event EventHandler CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public bool CanExecute(object parameter)
		{
			return CanExecute();
		}

		public void Execute(object parameter)
		{
			Execute();
		}

		public static bool CanExecute()
		{
			return GetDirtyAssemblies().Length > 0;
		}

		public static void Execute()
		{
			SaveModuleCommand.SaveAssemblies(GetDirtyAssemblies());
		}
	}

	[ExportMainMenuCommand(Menu = "_File", Header = "Save A_ll...", InputGestureText = "Ctrl+Shift+S", MenuCategory = "Save", MenuOrder = 1020, MenuIcon = "Images/SaveAll.png")]
	sealed class SaveAllCommand : ICommand
	{
		public event EventHandler CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public bool CanExecute(object parameter)
		{
			return SaveAllToolbarCommand.CanExecute();
		}

		public void Execute(object parameter)
		{
			SaveAllToolbarCommand.Execute();
		}
	}
}


using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

		HashSet<AssemblyTreeNode> GetAssemblyNodes(ILSpyTreeNode[] nodes)
		{
			var hash = new HashSet<AssemblyTreeNode>(nodes.Select(a => ILSpyTreeNode.GetAssemblyTreeNode(a)));
			foreach (var asmNode in hash.ToArray()) {
				if (asmNode == null || asmNode.LoadedAssembly.ModuleDefinition == null)
					hash.Remove(asmNode);
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

		static void SaveAssemblies(IEnumerable<AssemblyTreeNode> asmNodes)
		{
			var nodes = asmNodes.ToArray();
			if (nodes.Length == 0)
				return;

			if (nodes.Length == 1) {
				var optsData = new SaveModuleOptionsVM(nodes[0].LoadedAssembly.ModuleDefinition);
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
				MarkAsSaved(data, nodes);
			}
			else {
				var data = new SaveMultiModuleVM(nodes.Select(a => a.LoadedAssembly.ModuleDefinition));
				var win = new SaveMultiModule();
				win.Owner = MainWindow.Instance;
				win.DataContext = data;
				win.ShowDialog();
				MarkAsSaved(data, nodes);
			}
		}

		static void MarkAsSaved(SaveMultiModuleVM vm, AssemblyTreeNode[] nodes)
		{
			foreach (var node in nodes) {
				if (vm.WasSaved(node.LoadedAssembly.ModuleDefinition)) {
					//TODO: Mark as non dirty
				}
			}
		}
	}
}

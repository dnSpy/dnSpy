/*
    Copyright (C) 2014-2015 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.AsmEditor.SaveModule
{
	static class Saver
	{
		public static bool AskUserToSaveIfModified(AssemblyTreeNode asmNode)
		{
			return AskUserToSaveIfModified(new[] { asmNode.LoadedAssembly });
		}

		public static bool AskUserToSaveIfModified(IEnumerable<AssemblyTreeNode> asmNodes)
		{
			return AskUserToSaveIfModified(asmNodes.Select(n => n.LoadedAssembly));
		}

		public static bool AskUserToSaveIfModified(IEnumerable<LoadedAssembly> asmNodes)
		{
			var modifiedAsms = asmNodes.Where(n => UndoCommandManager.Instance.IsModified(n)).ToArray();
			if (modifiedAsms.Length == 0)
				return true;

			var msg = modifiedAsms.Length == 1 ?
				"The file hasn't been saved yet. Do you want to save it before continuing?" :
				"The files haven't been saved yet. Do you want to save them before continuing?";
			var res = MainWindow.Instance.ShowMessageBox(msg, System.Windows.MessageBoxButton.YesNo);
			if (res == MsgBoxButton.No)
				return true;
			return SaveAssemblies(modifiedAsms);
		}

		/// <summary>
		/// Saves all assemblies and returns true if all assemblies were saved to disk
		/// </summary>
		/// <param name="asms">All assemblies to save</param>
		/// <returns></returns>
		public static bool SaveAssemblies(IEnumerable<LoadedAssembly> asms)
		{
			var asmsAry = asms.ToArray();
			if (asmsAry.Length == 0)
				return true;

			if (asmsAry.Length == 1) {
				var optsData = new SaveModuleOptionsVM(asmsAry[0].ModuleDefinition);
				var optsWin = new SaveModuleOptions();
				optsWin.Owner = MainWindow.Instance;
				optsWin.DataContext = optsData;
				var res = optsWin.ShowDialog();
				if (res != true)
					return false;

				var data = new SaveMultiModuleVM(optsData);
				var win = new SaveSingleModule();
				win.Owner = MainWindow.Instance;
				win.DataContext = data;
				data.Save();
				win.ShowDialog();
				return MarkAsSaved(data, asmsAry);
			}
			else {
				var data = new SaveMultiModuleVM(asmsAry.Select(a => a.ModuleDefinition));
				var win = new SaveMultiModule();
				win.Owner = MainWindow.Instance;
				win.DataContext = data;
				win.ShowDialog();
				return MarkAsSaved(data, asmsAry);
			}
		}

		static bool MarkAsSaved(SaveMultiModuleVM vm, LoadedAssembly[] asms)
		{
			bool setNewFileName = false;
			bool allSaved = true;
			foreach (var asm in asms) {
				if (!vm.WasSaved(asm.ModuleDefinition))
					allSaved = false;
				else {
					UndoCommandManager.Instance.MarkAsSaved(asm);
					if (string.IsNullOrEmpty(asm.FileName)) {
						var filename = vm.GetSavedFileName(asm.ModuleDefinition);
						if (!string.IsNullOrWhiteSpace(filename)) {
							asm.ModuleDefinition.Location = filename;
							asm.FileName = filename;
							setNewFileName = true;
							var asmNode = MainWindow.Instance.FindTreeNode(asm.ModuleDefinition) as AssemblyTreeNode;
							Debug.Assert(asmNode != null);
							if (asmNode != null) {
								asmNode.OnFileNameChanged();
								Utils.InvalidateDecompilationCache(asm);
							}
						}
					}
				}
			}
			if (setNewFileName)
				MainWindow.Instance.CurrentAssemblyList.RefreshSave();
			return allSaved;
		}
	}
}

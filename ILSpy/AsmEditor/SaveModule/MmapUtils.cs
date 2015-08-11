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

using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.PE;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor.SaveModule {
	static class MmapUtils {
		public static void DisableMemoryMappedIO(IEnumerable<string> filenames) {
			var hash = new HashSet<string>(filenames, StringComparer.OrdinalIgnoreCase);

			foreach (var asm in GetAssemblyTreeNodes()) {
				DisableMemoryMappedIO(hash, asm.LoadedAssembly.TheLoadedFile.ModuleDef as ModuleDefMD);
				DisableMemoryMappedIO(hash, asm.LoadedAssembly.TheLoadedFile.PEImage);
			}

			foreach (var mod in MainWindow.Instance.CurrentAssemblyList.GetAllModules())
				DisableMemoryMappedIO(hash, mod as ModuleDefMD);

			foreach (var asm in UndoCommandManager.Instance.GetAssemblies())
				DisableMemoryMappedIO(hash, asm.ModuleDefinition as ModuleDefMD);
		}

		static IEnumerable<AssemblyTreeNode> GetAssemblyTreeNodes() {
			foreach (AssemblyTreeNode asmNode in MainWindow.Instance.treeView.Root.Children) {
				if (asmNode.Children.Count == 0 || !(asmNode.Children[0] is AssemblyTreeNode))
					yield return asmNode;
				else {
					foreach (AssemblyTreeNode child in asmNode.Children)
						yield return child;
				}
			}
		}

		static void DisableMemoryMappedIO(HashSet<string> filenames, ModuleDefMD mod) {
			if (mod != null)
				DisableMemoryMappedIO(filenames, mod.MetaData.PEImage);
		}

		static void DisableMemoryMappedIO(HashSet<string> filenames, IPEImage peImage) {
			if (peImage != null && filenames.Contains(peImage.FileName))
				peImage.UnsafeDisableMemoryMappedIO();
		}
	}
}

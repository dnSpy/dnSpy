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

namespace dnSpy.AsmEditor.SaveModule {
	static class MmapUtils {
		public static void DisableMemoryMappedIO(IEnumerable<string> filenames) {
			var hash = new HashSet<string>(filenames, StringComparer.OrdinalIgnoreCase);

			foreach (var asm in MainWindow.Instance.GetAllDnSpyFileInstances()) {
				DisableMemoryMappedIO(hash, asm.ModuleDef as ModuleDefMD);
				DisableMemoryMappedIO(hash, asm.PEImage);
			}

			foreach (var mod in MainWindow.Instance.DnSpyFileList.GetAllModules())
				DisableMemoryMappedIO(hash, mod as ModuleDefMD);

			foreach (var asm in UndoCommandManager.Instance.GetAssemblies())
				DisableMemoryMappedIO(hash, asm.ModuleDef as ModuleDefMD);
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

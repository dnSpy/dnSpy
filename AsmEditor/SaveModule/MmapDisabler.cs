/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using dnlib.PE;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.AsmEditor.SaveModule {
	interface IMmapDisabler {
		void Disable(IEnumerable<string> filenames);
	}

	[Export, Export(typeof(IMmapDisabler)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class MmapDisabler : IMmapDisabler {
		readonly IFileTreeView fileTreeView;
		readonly Lazy<IUndoCommandManager> undoCommandManager;

		[ImportingConstructor]
		MmapDisabler(IFileTreeView fileTreeView, Lazy<IUndoCommandManager> undoCommandManager) {
			this.fileTreeView = fileTreeView;
			this.undoCommandManager = undoCommandManager;
		}

		public void Disable(IEnumerable<string> filenames) {
			var hash = new HashSet<string>(filenames, StringComparer.OrdinalIgnoreCase);

			var filesHash = new HashSet<IDnSpyFile>(GetFiles());
			foreach (var f in filesHash)
				DisableMemoryMappedIO(hash, f.PEImage);
		}

		IEnumerable<IDnSpyFile> GetFiles() {
			foreach (var n in fileTreeView.GetAllCreatedDnSpyFileNodes())
				yield return n.DnSpyFile;
			foreach (var f in fileTreeView.FileManager.GetFiles()) {
				foreach (var c in f.GetAllChildrenAndSelf())
					yield return c;
			}
			foreach (var uo in undoCommandManager.Value.GetAllObjects()) {
				var f = DnSpyFileUndoableDocumentsProvider.TryGetDnSpyFile(uo);
				if (f != null)
					yield return f;
			}
		}

		static void DisableMemoryMappedIO(HashSet<string> filenames, IPEImage peImage) {
			if (peImage != null && filenames.Contains(peImage.FileName))
				peImage.UnsafeDisableMemoryMappedIO();
		}
	}
}

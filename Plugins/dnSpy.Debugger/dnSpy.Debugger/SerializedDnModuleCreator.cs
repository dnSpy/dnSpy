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

using System.ComponentModel.Composition;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.Debugger {
	interface ISerializedDnModuleCreator {
		SerializedDnModule Create(ModuleDef module);
	}

	[Export(typeof(ISerializedDnModuleCreator))]
	sealed class SerializedDnModuleCreator : ISerializedDnModuleCreator {
		readonly IFileTreeView fileTreeView;

		[ImportingConstructor]
		public SerializedDnModuleCreator(IFileTreeView fileTreeView) {
			this.fileTreeView = fileTreeView;
		}

		public SerializedDnModule Create(ModuleDef module) => Create(fileTreeView, module);

		internal static SerializedDnModule Create(IFileTreeView fileTreeView, ModuleDef module) {
			if (module == null)
				return new SerializedDnModule();
			var modNode = fileTreeView.FindNode(module);
			if (modNode == null)
				return SerializedDnModule.CreateFromFile(module);
			return modNode.DnSpyFile.ToSerializedDnModule();
		}
	}
}

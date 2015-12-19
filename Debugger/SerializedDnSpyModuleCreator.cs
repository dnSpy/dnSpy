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

using System.ComponentModel.Composition;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.Debugger {
	interface ISerializedDnSpyModuleCreator {
		SerializedDnSpyModule Create(ModuleDef module);
	}

	[Export, Export(typeof(ISerializedDnSpyModuleCreator)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class SerializedDnSpyModuleCreator : ISerializedDnSpyModuleCreator {
		readonly IFileTreeView fileTreeView;

		[ImportingConstructor]
		public SerializedDnSpyModuleCreator(IFileTreeView fileTreeView) {
			this.fileTreeView = fileTreeView;
		}

		public SerializedDnSpyModule Create(ModuleDef module) {
			return Create(fileTreeView, module);
		}

		internal static SerializedDnSpyModule Create(IFileTreeView fileTreeView, ModuleDef module) {
			if (module == null)
				return new SerializedDnSpyModule();
			var modNode = fileTreeView.FindNode(module);
			if (modNode == null)
				return SerializedDnSpyModule.CreateFromFile(module);
			return modNode.DnSpyFile.SerializedDnSpyModule ?? SerializedDnSpyModule.CreateFromFile(module);
		}
	}
}

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
using dnlib.DotNet;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Debugger {
	//[ExportModuleIdFactoryProvider(ModuleIdFactoryProviderConstants.OrderDebugger)]
	sealed class DebuggerModuleIdFactoryProvider : IModuleIdFactoryProvider {
		readonly IDocumentTreeView documentTreeView;

		[ImportingConstructor]
		DebuggerModuleIdFactoryProvider(IDocumentTreeView documentTreeView) {
			this.documentTreeView = documentTreeView;
		}

		public IModuleIdFactory Create() => new ModuleIdFactory(documentTreeView);

		sealed class ModuleIdFactory : IModuleIdFactory {
			readonly IDocumentTreeView documentTreeView;

			public ModuleIdFactory(IDocumentTreeView documentTreeView) {
				this.documentTreeView = documentTreeView;
			}

			public ModuleId? Create(ModuleDef module) {
				var midHolder = documentTreeView.FindNode(module)?.Document as IModuleIdHolder;
				if (midHolder != null)
					return midHolder.ModuleId;
				return null;
			}
		}
	}
}

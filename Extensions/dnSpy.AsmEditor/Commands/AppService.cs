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
using System.Windows;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.AsmEditor.Commands {
	interface IAppService {
		Window MainWindow { get; }
		IDocumentTabService DocumentTabService { get; }
		IDocumentTreeView DocumentTreeView { get; }
		IDecompilerService DecompilerService { get; }
	}

	[Export(typeof(IAppService))]
	sealed class AppService : IAppService {
		IAppWindow AppWindow { get; }
		public Window MainWindow => AppWindow.MainWindow;
		public IDocumentTabService DocumentTabService { get; }
		public IDocumentTreeView DocumentTreeView { get; }
		public IDecompilerService DecompilerService { get; }

		[ImportingConstructor]
		AppService(IAppWindow appWindow, IDocumentTabService documentTabService, IDocumentTreeView documentTreeView, IDecompilerService decompilerService) {
			AppWindow = appWindow;
			DocumentTabService = documentTabService;
			DocumentTreeView = documentTreeView;
			DecompilerService = decompilerService;
		}
	}
}

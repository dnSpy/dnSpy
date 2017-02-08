/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Text;
using dnSpy.Decompiler;
using dnSpy.Documents.Tabs.DocViewer;
using dnSpy.Properties;
using Microsoft.Win32;

namespace dnSpy.Documents.Tabs {
	[ExportTabSaverProvider(Order = TabConstants.ORDER_DEFAULTTABSAVERPROVIDER)]
	sealed class NodeTabSaverProvider : ITabSaverProvider {
		readonly IDocumentTreeNodeDecompiler documentTreeNodeDecompiler;
		readonly IMessageBoxService messageBoxService;

		[ImportingConstructor]
		NodeTabSaverProvider(IDocumentTreeNodeDecompiler documentTreeNodeDecompiler, IMessageBoxService messageBoxService) {
			this.documentTreeNodeDecompiler = documentTreeNodeDecompiler;
			this.messageBoxService = messageBoxService;
		}

		public ITabSaver Create(IDocumentTab tab) => NodeTabSaver.TryCreate(documentTreeNodeDecompiler, tab, messageBoxService);
	}

	sealed class NodeTabSaver : ITabSaver {
		readonly IMessageBoxService messageBoxService;
		readonly IDocumentTab tab;
		readonly IDocumentTreeNodeDecompiler documentTreeNodeDecompiler;
		readonly IDecompiler decompiler;
		readonly DocumentTreeNodeData[] nodes;
		readonly IDocumentViewer documentViewer;

		public static NodeTabSaver TryCreate(IDocumentTreeNodeDecompiler documentTreeNodeDecompiler, IDocumentTab tab, IMessageBoxService messageBoxService) {
			if (tab.IsAsyncExecInProgress)
				return null;
			var uiContext = tab.UIContext as IDocumentViewer;
			if (uiContext == null)
				return null;
			var decompiler = (tab.Content as IDecompilerTabContent)?.Decompiler;
			if (decompiler == null)
				return null;
			var nodes = tab.Content.Nodes.ToArray();
			if (nodes.Length == 0)
				return null;
			return new NodeTabSaver(messageBoxService, tab, documentTreeNodeDecompiler, decompiler, uiContext, nodes);
		}

		NodeTabSaver(IMessageBoxService messageBoxService, IDocumentTab tab, IDocumentTreeNodeDecompiler documentTreeNodeDecompiler, IDecompiler decompiler, IDocumentViewer documentViewer, DocumentTreeNodeData[] nodes) {
			this.messageBoxService = messageBoxService;
			this.tab = tab;
			this.documentTreeNodeDecompiler = documentTreeNodeDecompiler;
			this.decompiler = decompiler;
			this.documentViewer = documentViewer;
			this.nodes = nodes;
		}

		public bool CanSave => !tab.IsAsyncExecInProgress;
		public string MenuHeader => dnSpy_Resources.Button_SaveCode;

		sealed class DecompileContext : IDisposable {
			public DecompileNodeContext DecompileNodeContext;
			public TextWriter Writer;
			public void Dispose() => Writer?.Dispose();
		}

		DecompileContext CreateDecompileContext(string filename) {
			var decompileContext = new DecompileContext();
			try {
				var decompilationContext = new DecompilationContext();
				decompileContext.Writer = new StreamWriter(filename);
				var output = new TextWriterDecompilerOutput(decompileContext.Writer);
				var dispatcher = Dispatcher.CurrentDispatcher;
				decompileContext.DecompileNodeContext = new DecompileNodeContext(decompilationContext, decompiler, output, NullDocumentWriterService.Instance, dispatcher);
				return decompileContext;
			}
			catch {
				decompileContext.Dispose();
				throw;
			}
		}

		sealed class NullDocumentWriterService : IDocumentWriterService {
			public static readonly NullDocumentWriterService Instance = new NullDocumentWriterService();
			public void Write(IDecompilerOutput output, string text, string contentType) =>
				output.Write(text, BoxedTextColor.Text);
		}

		DecompileContext CreateDecompileContext() {
			var saveDlg = new SaveFileDialog {
				FileName = FilenameUtils.CleanName(nodes[0].ToString(decompiler, DocumentNodeWriteOptions.Title)) + decompiler.FileExtension,
				DefaultExt = decompiler.FileExtension,
				Filter = string.Format("{0}|*{1}|{2}|*.*", decompiler.GenericNameUI, decompiler.FileExtension, dnSpy_Resources.AllFiles),
			};
			if (saveDlg.ShowDialog() != true)
				return null;
			return CreateDecompileContext(saveDlg.FileName);
		}

		public void Save() {
			if (!CanSave)
				return;

			var ctx = CreateDecompileContext();
			if (ctx == null)
				return;

			tab.AsyncExec(cs => {
				ctx.DecompileNodeContext.DecompilationContext.CancellationToken = cs.Token;
				documentViewer.ShowCancelButton(dnSpy_Resources.SavingCode, () => cs.Cancel());
			}, () => {
				documentTreeNodeDecompiler.Decompile(ctx.DecompileNodeContext, nodes);
			}, result => {
				ctx.Dispose();
				documentViewer.HideCancelButton();
				if (result.Exception != null)
					messageBoxService.Show(result.Exception);
			});
		}
	}
}

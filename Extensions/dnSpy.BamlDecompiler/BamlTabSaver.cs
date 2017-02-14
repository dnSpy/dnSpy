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
using System.Threading;
using dnSpy.BamlDecompiler.Properties;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Decompiler;
using Microsoft.Win32;

namespace dnSpy.BamlDecompiler {
	[ExportTabSaverProvider(Order = TabConstants.ORDER_BAMLTABSAVERPROVIDER)]
	sealed class BamlTabSaverProvider : ITabSaverProvider {
		readonly IMessageBoxService messageBoxService;

		[ImportingConstructor]
		BamlTabSaverProvider(IMessageBoxService messageBoxService) => this.messageBoxService = messageBoxService;

		public ITabSaver Create(IDocumentTab tab) => BamlTabSaver.TryCreate(tab, messageBoxService);
	}

	sealed class BamlTabSaver : ITabSaver {
		public bool CanSave => true;
		public string MenuHeader => bamlNode.DisassembleBaml ? dnSpy_BamlDecompiler_Resources.SaveBAML : dnSpy_BamlDecompiler_Resources.SaveXAML;

		public static BamlTabSaver TryCreate(IDocumentTab tab, IMessageBoxService messageBoxService) {
			var uiContext = tab.UIContext as IDocumentViewer;
			if (uiContext == null)
				return null;
			var nodes = tab.Content.Nodes.ToArray();
			if (nodes.Length != 1)
				return null;
			var bamlNode = nodes[0] as BamlResourceElementNode;
			if (bamlNode == null)
				return null;

			return new BamlTabSaver(tab, bamlNode, uiContext, messageBoxService);
		}

		readonly IDocumentTab tab;
		readonly BamlResourceElementNode bamlNode;
		readonly IDocumentViewer documentViewer;
		readonly IMessageBoxService messageBoxService;

		BamlTabSaver(IDocumentTab tab, BamlResourceElementNode bamlNode, IDocumentViewer documentViewer, IMessageBoxService messageBoxService) {
			this.tab = tab;
			this.bamlNode = bamlNode;
			this.documentViewer = documentViewer;
			this.messageBoxService = messageBoxService;
		}

		sealed class DecompileContext : IDisposable {
			public TextWriter Writer;
			public TextWriterDecompilerOutput Output;
			public CancellationToken Token;

			public void Dispose() {
				if (Writer != null)
					Writer.Dispose();
			}
		}

		DecompileContext CreateDecompileContext(string filename) {
			var decompileContext = new DecompileContext();
			try {
				decompileContext.Writer = new StreamWriter(filename);
				decompileContext.Output = new TextWriterDecompilerOutput(decompileContext.Writer);
				return decompileContext;
			}
			catch {
				decompileContext.Dispose();
				throw;
			}
		}

		DecompileContext CreateDecompileContext() {
			string ext, name;
			if (bamlNode.DisassembleBaml) {
				ext = ".baml";
				name = dnSpy_BamlDecompiler_Resources.BAMLFile;
			}
			else {
				ext = ".xaml";
				name = dnSpy_BamlDecompiler_Resources.XAMLFile;
			}
			var saveDlg = new SaveFileDialog {
				FileName = FilenameUtils.CleanName(RemovePath(bamlNode.GetFilename())),
				DefaultExt = ext,
				Filter = $"{name}|*{ext}|{dnSpy_BamlDecompiler_Resources.AllFiles}|*.*",
			};
			if (saveDlg.ShowDialog() != true)
				return null;
			return CreateDecompileContext(saveDlg.FileName);
		}

		string RemovePath(string s) {
			int i = s.LastIndexOf('/');
			if (i < 0)
				return s;
			return s.Substring(i + 1);
		}

		public void Save() {
			if (!CanSave)
				return;

			var ctx = CreateDecompileContext();
			if (ctx == null)
				return;

			tab.AsyncExec(cs => {
				ctx.Token = cs.Token;
				documentViewer.ShowCancelButton(dnSpy_BamlDecompiler_Resources.Saving, () => cs.Cancel());
			}, () => {
				bamlNode.Decompile(ctx.Output, ctx.Token);
			}, result => {
				ctx.Dispose();
				documentViewer.HideCancelButton();
				if (result.Exception != null)
					messageBoxService.Show(result.Exception);
			});
		}
	}
}

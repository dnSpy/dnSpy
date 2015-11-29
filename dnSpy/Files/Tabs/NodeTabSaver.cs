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
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Languages;
using dnSpy.Files.Tabs.TextEditor;
using dnSpy.Languages;
using ICSharpCode.Decompiler;
using Microsoft.Win32;

namespace dnSpy.Files.Tabs {
	[ExportTabSaverCreator(Order = TabsConstants.ORDER_DEFAULTTABSAVERCREATOR)]
	sealed class NodeTabSaverCreator : ITabSaverCreator {
		readonly IFileTreeNodeDecompiler fileTreeNodeDecompiler;
		readonly DecompilerSettings decompilerSettings;

		[ImportingConstructor]
		NodeTabSaverCreator(IFileTreeNodeDecompiler fileTreeNodeDecompiler, DecompilerSettings decompilerSettings) {
			this.fileTreeNodeDecompiler = fileTreeNodeDecompiler;
			this.decompilerSettings = decompilerSettings;
		}

		public ITabSaver Create(IFileTab tab) {
			return NodeTabSaver.TryCreate(fileTreeNodeDecompiler, tab, decompilerSettings);
		}
	}

	sealed class NodeTabSaver : ITabSaver {
		readonly IFileTab tab;
		readonly IFileTreeNodeDecompiler fileTreeNodeDecompiler;
		readonly ILanguage language;
		readonly IFileTreeNodeData[] nodes;
		readonly ITextEditorUIContext uiContext;
		readonly DecompilerSettings _global_decompilerSettings;

		public static NodeTabSaver TryCreate(IFileTreeNodeDecompiler fileTreeNodeDecompiler, IFileTab tab, DecompilerSettings decompilerSettings) {
			if (tab.IsAsyncExecInProgress)
				return null;
			var uiContext = tab.UIContext as ITextEditorUIContext;
			if (uiContext == null)
				return null;
			var langContent = tab.Content as ILanguageTabContent;
			var lang = langContent == null ? null : langContent.Language;
			if (lang == null)
				return null;
			var nodes = tab.Content.Nodes.ToArray();
			if (nodes.Length == 0)
				return null;
			return new NodeTabSaver(tab, fileTreeNodeDecompiler, lang, uiContext, nodes, decompilerSettings);
		}

		NodeTabSaver(IFileTab tab, IFileTreeNodeDecompiler fileTreeNodeDecompiler, ILanguage language, ITextEditorUIContext uiContext, IFileTreeNodeData[] nodes, DecompilerSettings decompilerSettings) {
			this.tab = tab;
			this.fileTreeNodeDecompiler = fileTreeNodeDecompiler;
			this.language = language;
			this.uiContext = uiContext;
			this.nodes = nodes;
			this._global_decompilerSettings = decompilerSettings;
		}

		public bool CanSave {
			get { return !tab.IsAsyncExecInProgress; }
		}

		public string MenuHeader {
			get { return "_Save Code..."; }
		}

		sealed class DecompileContext : IDisposable {
			public DecompileNodeContext DecompileNodeContext;
			public TextWriter Writer;

			public void Dispose() {
				if (Writer != null)
					Writer.Dispose();
			}
		}

		DecompileContext CreateDecompileContext(string filename) {
			var decompileContext = new DecompileContext();
			try {
				var decompilationOptions = new DecompilationOptions();
				decompilationOptions.DecompilerSettings = _global_decompilerSettings.Clone();
				decompilationOptions.DontShowCreateMethodBodyExceptions = true;
				decompileContext.Writer = new StreamWriter(filename);
				var output = new PlainTextOutput(decompileContext.Writer);
				var dispatcher = Dispatcher.CurrentDispatcher;
				decompileContext.DecompileNodeContext = new DecompileNodeContext(decompilationOptions, language, output, dispatcher);
				return decompileContext;
			}
			catch {
				decompileContext.Dispose();
				throw;
			}
		}

		DecompileContext CreateDecompileContext() {
			if (nodes.Length == 1 && (nodes[0] is IAssemblyFileNode || nodes[0] is IModuleFileNode)) {
				var dnSpyFile = ((IDnSpyFileNode)nodes[0]).DnSpyFile;
				var saveDlg = new SaveFileDialog {
					FileName = FilenameUtils.CleanUpName(dnSpyFile.GetShortName()) + language.ProjectFileExtension,
					DefaultExt = language.FileExtension,
					Filter = string.Format("{0} project|*{1}|{0} single file|*{2}|All files|*.*", language.NameUI, language.ProjectFileExtension, language.FileExtension),
				};
				if (saveDlg.ShowDialog() != true)
					return null;
				var ctx = CreateDecompileContext(saveDlg.FileName);
				if (saveDlg.FilterIndex == 1)
					ctx.DecompileNodeContext.DecompilationOptions.ProjectOptions.Directory = Path.GetDirectoryName(saveDlg.FileName);
				return ctx;
			}
			else {
				var saveDlg = new SaveFileDialog {
					FileName = FilenameUtils.CleanUpName(nodes[0].ToString(language)) + language.FileExtension,
					DefaultExt = language.FileExtension,
					Filter = string.Format("{0}|*{1}|All Files|*.*", language.NameUI, language.FileExtension),
				};
				if (saveDlg.ShowDialog() != true)
					return null;
				return CreateDecompileContext(saveDlg.FileName);
			}
		}

		public void Save() {
			if (!CanSave)
				return;

			var ctx = CreateDecompileContext();
			if (ctx == null)
				return;

			tab.AsyncExec(cs => {
				ctx.DecompileNodeContext.DecompilationOptions.CancellationToken = cs.Token;
				uiContext.ShowCancelButton(() => cs.Cancel(), "Saving...");
			}, () => {
				fileTreeNodeDecompiler.Decompile(ctx.DecompileNodeContext, nodes);
			}, result => {
				ctx.Dispose();
				uiContext.HideCancelButton();
				if (result.Exception != null) {
					//TODO: Show exception to user
				}
			});
		}
	}
}

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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Languages;
using dnSpy.Shared.UI.Decompiler;
using ICSharpCode.Decompiler;

namespace dnSpy.Files.Tabs.TextEditor {
	[ExportFileTabContentFactory(Order = TabsConstants.ORDER_DECOMPILEFILETABCONTENTFACTORY)]
	sealed class DecompileFileTabContentFactory : IFileTabContentFactory {
		public FileTreeNodeDecompiler FileTreeNodeDecompiler {
			get { return fileTreeNodeDecompiler; }
		}
		readonly FileTreeNodeDecompiler fileTreeNodeDecompiler;

		public ILanguageManager LanguageManager {
			get { return languageManager; }
		}
		readonly ILanguageManager languageManager;

		[ImportingConstructor]
		DecompileFileTabContentFactory(FileTreeNodeDecompiler fileTreeNodeDecompiler, ILanguageManager languageManager) {
			this.fileTreeNodeDecompiler = fileTreeNodeDecompiler;
			this.languageManager = languageManager;
		}

		public IFileTabContent Create(IFileTabContentFactoryContext context) {
			return new DecompileFileTabContent(this, context.Nodes);
		}
	}

	sealed class DecompileFileTabContent : IAsyncFileTabContent {
		readonly DecompileFileTabContentFactory decompileFileTabContentFactory;
		readonly IFileTreeNodeData[] nodes;

		public DecompileFileTabContent(DecompileFileTabContentFactory decompileFileTabContentFactory, IFileTreeNodeData[] nodes) {
			this.decompileFileTabContentFactory = decompileFileTabContentFactory;
			this.nodes = nodes;
		}

		public IFileTabContent Clone() {
			return new DecompileFileTabContent(decompileFileTabContentFactory, nodes);
		}

		public IFileTabUIContext CreateUIContext(IFileTabUIContextLocator locator) {
			return locator.Get<ITextEditorUIContext>();
		}

		public string Title {
			get {
				if (nodes.Length == 0)
					return "<empty>";
				if (nodes.Length == 1)
					return nodes[0].ToString();
				var sb = new StringBuilder();
				foreach (var node in nodes) {
					if (sb.Length > 0)
						sb.Append(", ");
					sb.Append(node.ToString());
				}
				return sb.ToString();
			}
		}

		public object ToolTip {
			get {
				if (nodes.Length == 0)
					return null;
				return Title;
			}
		}

		public void OnHide() {
		}

		public IFileTab FileTab {
			get { return fileTab; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				if (fileTab == null)
					fileTab = value;
				else if (fileTab != value)
					throw new InvalidOperationException();
			}
		}
		IFileTab fileTab;

		public IEnumerable<IFileTreeNodeData> Nodes {
			get { return nodes; }
		}

		sealed class DecompileContext {
			public DecompileNodeContext DecompileNodeContext;
			public CancellationTokenSource CancellationTokenSource;
		}
		DecompileContext decompileContext;

		DecompileContext CreateDecompileContext(ILanguage language) {
			var decompileContext = new DecompileContext();
			decompileContext.CancellationTokenSource = new CancellationTokenSource();
			var decompilationOptions = new DecompilationOptions();
			decompilationOptions.CancellationToken = decompileContext.CancellationTokenSource.Token;
			decompilationOptions.DecompilerSettings = new DecompilerSettings();//TODO: Init from settings
			decompilationOptions.DontShowCreateMethodBodyExceptions = true;
			var output = new AvalonEditTextOutput();
			var dispatcher = Dispatcher.CurrentDispatcher;
			decompileContext.DecompileNodeContext = new DecompileNodeContext(decompilationOptions, language, output, dispatcher);
			return decompileContext;
		}

		public void OnShow(IFileTabUIContext uiContext) {
			Debug.Assert(decompileContext == null);
			decompileContext = CreateDecompileContext(decompileFileTabContentFactory.LanguageManager.SelectedLanguage);
		}

		public void AsyncWorker(IFileTabUIContext uiContext) {
			Debug.Assert(decompileContext != null);
			decompileFileTabContentFactory.FileTreeNodeDecompiler.Decompile(decompileContext.DecompileNodeContext, nodes);
		}

		public void EndAsyncShow(IFileTabUIContext uiContext) {
			Debug.Assert(decompileContext != null);
			var oldDecompileContext = decompileContext;
			decompileContext = null;

			var uiCtx = (ITextEditorUIContext)uiContext;
			uiCtx.SetOutput(oldDecompileContext.DecompileNodeContext.Output, oldDecompileContext.DecompileNodeContext.Language.GetHighlightingDefinition());
		}

		public bool CanStartAsyncWorker(IFileTabUIContext uiContext) {
			return true;//TODO: Return false if it was in the cache
		}
	}
}

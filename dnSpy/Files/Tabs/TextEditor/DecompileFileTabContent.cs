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
using System.Text;
using System.Threading;
using System.Windows.Threading;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Settings;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.Decompiler;
using ICSharpCode.Decompiler;

namespace dnSpy.Files.Tabs.TextEditor {
	[Export, ExportFileTabContentFactory(Order = TabsConstants.ORDER_DECOMPILEFILETABCONTENTFACTORY)]
	sealed class DecompileFileTabContentFactory : IFileTabContentFactory {
		public IFileTreeNodeDecompiler FileTreeNodeDecompiler {
			get { return fileTreeNodeDecompiler; }
		}
		readonly IFileTreeNodeDecompiler fileTreeNodeDecompiler;

		public ILanguageManager LanguageManager {
			get { return languageManager; }
		}
		readonly ILanguageManager languageManager;

		public IDecompilationCache DecompilationCache {
			get { return decompilationCache; }
		}
		readonly IDecompilationCache decompilationCache;

		public DecompilerSettings CreateDecompilerSettings() {
			return _global_decompilerSettings.Clone();
		}
		readonly DecompilerSettings _global_decompilerSettings;

		[ImportingConstructor]
		DecompileFileTabContentFactory(IFileTreeNodeDecompiler fileTreeNodeDecompiler, ILanguageManager languageManager, IDecompilationCache decompilationCache, DecompilerSettings decompilerSettings) {
			this.fileTreeNodeDecompiler = fileTreeNodeDecompiler;
			this.languageManager = languageManager;
			this.decompilationCache = decompilationCache;
			this._global_decompilerSettings = decompilerSettings;
		}

		public IFileTabContent Create(IFileTabContentFactoryContext context) {
			return new DecompileFileTabContent(this, context.Nodes, languageManager.SelectedLanguage);
		}

		public IFileTabContent Create(IFileTreeNodeData[] nodes) {
			return new DecompileFileTabContent(this, nodes, languageManager.SelectedLanguage);
		}

		static readonly Guid GUID_SerializedContent = new Guid("DE0390B0-747C-4F53-9CFF-1D10B93DD5DD");

		public Guid? Serialize(IFileTabContent content, ISettingsSection section) {
			var dc = content as DecompileFileTabContent;
			if (dc == null)
				return null;

			section.Attribute("Language", dc.Language.NameUI);
			return GUID_SerializedContent;
		}

		public IFileTabContent Deserialize(Guid guid, ISettingsSection section, IFileTabContentFactoryContext context) {
			if (guid != GUID_SerializedContent)
				return null;

			var langName = section.Attribute<string>("Language") ?? "C#";
			var language = languageManager.FindOrDefault(langName);
			return new DecompileFileTabContent(this, context.Nodes, language);
		}
	}

	sealed class DecompileFileTabContent : IAsyncFileTabContent, ILanguageTabContent {
		readonly DecompileFileTabContentFactory decompileFileTabContentFactory;
		readonly IFileTreeNodeData[] nodes;

		public ILanguage Language {
			get { return language; }
		}
		ILanguage language;

		public DecompileFileTabContent(DecompileFileTabContentFactory decompileFileTabContentFactory, IFileTreeNodeData[] nodes, ILanguage language) {
			this.decompileFileTabContentFactory = decompileFileTabContentFactory;
			this.nodes = nodes;
			this.language = language;
		}

		public IFileTabContent Clone() {
			return new DecompileFileTabContent(decompileFileTabContentFactory, nodes, language);
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
			public AvalonEditTextOutput CachedOutput;
			public CancellationTokenSource CancellationTokenSource;
		}

		DecompileContext CreateDecompileContext() {
			var decompileContext = new DecompileContext();
			var decompilationOptions = new DecompilationOptions();
			decompilationOptions.DecompilerSettings = decompileFileTabContentFactory.CreateDecompilerSettings();
			decompilationOptions.DontShowCreateMethodBodyExceptions = true;
			var output = new AvalonEditTextOutput();
			var dispatcher = Dispatcher.CurrentDispatcher;
			decompileContext.DecompileNodeContext = new DecompileNodeContext(decompilationOptions, language, output, dispatcher);
			return decompileContext;
		}

		void UpdateLanguage(bool force = false) {
			if (force || FileTab.IsActiveTab)
				decompileFileTabContentFactory.LanguageManager.SelectedLanguage = language;
		}

		public void OnSelected() {
			UpdateLanguage(true);
		}

		public void OnUnselected() {
		}

		public void OnHide() {
		}

		public object OnShow(IFileTabUIContext uiContext) {
			UpdateLanguage();
			var decompileContext = CreateDecompileContext();
			decompileContext.CachedOutput = decompileFileTabContentFactory.DecompilationCache.Lookup(decompileContext.DecompileNodeContext.Language, nodes, decompileContext.DecompileNodeContext.DecompilationOptions);
			return decompileContext;
		}

		public void AsyncWorker(IFileTabUIContext uiContext, object userData, CancellationTokenSource source) {
			var decompileContext = (DecompileContext)userData;
			decompileContext.CancellationTokenSource = source;
			decompileContext.DecompileNodeContext.DecompilationOptions.CancellationToken = source.Token;
			decompileFileTabContentFactory.FileTreeNodeDecompiler.Decompile(decompileContext.DecompileNodeContext, nodes);
		}

		public void EndAsyncShow(IFileTabUIContext uiContext, object userData, IAsyncShowResult result) {
			var decompileContext = (DecompileContext)userData;

			var uiCtx = (ITextEditorUIContext)uiContext;

			AvalonEditTextOutput output;

			if (result.IsCanceled) {
				output = new AvalonEditTextOutput();
				output.Write("The operation was canceled", TextTokenType.Error);
			}
			else if (result.Exception != null) {
				output = new AvalonEditTextOutput();
				output.Write("An error occurred", TextTokenType.Error);
				output.WriteLine();
				output.Write(result.Exception.ToString(), TextTokenType.Text);
			}
			else {
				output = decompileContext.CachedOutput;
				if (output == null) {
					output = (AvalonEditTextOutput)decompileContext.DecompileNodeContext.Output;
					decompileFileTabContentFactory.DecompilationCache.Cache(decompileContext.DecompileNodeContext.Language, nodes, decompileContext.DecompileNodeContext.DecompilationOptions, output);
				}
			}

			if (result.CanShowOutput)
				uiCtx.SetOutput(output, decompileContext.DecompileNodeContext.Language.GetHighlightingDefinition());
		}

		public bool CanStartAsyncWorker(IFileTabUIContext uiContext, object userData) {
			var decompileContext = (DecompileContext)userData;
			if (decompileContext.CachedOutput != null)
				return false;

			var uiCtx = (ITextEditorUIContext)uiContext;
			uiCtx.ShowCancelButton(() => decompileContext.CancellationTokenSource.Cancel(), "Decompiling...");
			return true;
		}

		public bool NeedRefresh() {
			bool needRefresh = false;

			if (language != decompileFileTabContentFactory.LanguageManager.SelectedLanguage) {
				needRefresh = true;
				language = decompileFileTabContentFactory.LanguageManager.SelectedLanguage;
			}

			return needRefresh;
		}
	}
}

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
using System.Text;
using System.Threading;
using System.Windows.Threading;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Settings;
using dnSpy.Decompiler.Shared;
using dnSpy.Properties;
using dnSpy.Shared.Decompiler;
using ICSharpCode.AvalonEdit.Highlighting;

namespace dnSpy.Files.Tabs.TextEditor {
	[Export, ExportFileTabContentFactory(Order = TabConstants.ORDER_DECOMPILEFILETABCONTENTFACTORY)]
	sealed class DecompileFileTabContentFactory : IFileTabContentFactory {
		public IFileManager FileManager {
			get { return fileManager; }
		}
		readonly IFileManager fileManager;

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

		public IMethodAnnotations MethodAnnotations {
			get { return methodAnnotations; }
		}
		readonly IMethodAnnotations methodAnnotations;

		[ImportingConstructor]
		DecompileFileTabContentFactory(IFileManager fileManager, IFileTreeNodeDecompiler fileTreeNodeDecompiler, ILanguageManager languageManager, IDecompilationCache decompilationCache, IMethodAnnotations methodAnnotations) {
			this.fileManager = fileManager;
			this.fileTreeNodeDecompiler = fileTreeNodeDecompiler;
			this.languageManager = languageManager;
			this.decompilationCache = decompilationCache;
			this.methodAnnotations = methodAnnotations;
		}

		public IFileTabContent Create(IFileTabContentFactoryContext context) {
			return new DecompileFileTabContent(this, context.Nodes, languageManager.Language);
		}

		public IFileTabContent Create(IFileTreeNodeData[] nodes) {
			return new DecompileFileTabContent(this, nodes, languageManager.Language);
		}

		static readonly Guid GUID_SerializedContent = new Guid("DE0390B0-747C-4F53-9CFF-1D10B93DD5DD");

		public Guid? Serialize(IFileTabContent content, ISettingsSection section) {
			var dc = content as DecompileFileTabContent;
			if (dc == null)
				return null;

			section.Attribute("Language", dc.Language.UniqueGuid);
			return GUID_SerializedContent;
		}

		public IFileTabContent Deserialize(Guid guid, ISettingsSection section, IFileTabContentFactoryContext context) {
			if (guid != GUID_SerializedContent)
				return null;

			var langGuid = section.Attribute<Guid?>("Language") ?? LanguageConstants.LANGUAGE_CSHARP;
			var language = languageManager.FindOrDefault(langGuid);
			return new DecompileFileTabContent(this, context.Nodes, language);
		}
	}

	sealed class DecompileFileTabContent : IAsyncFileTabContent, ILanguageTabContent {
		readonly DecompileFileTabContentFactory decompileFileTabContentFactory;
		readonly IFileTreeNodeData[] nodes;

		public ILanguage Language {
			get { return language; }
			set { language = value; }
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
					return dnSpy_Resources.EmptyTabTitle;
				if (nodes.Length == 1)
					return nodes[0].ToString(language);
				var sb = new StringBuilder();
				foreach (var node in nodes) {
					if (sb.Length > 0)
						sb.Append(", ");
					sb.Append(node.ToString(language));
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
			public object SavedRefPos;
		}

		DecompileContext CreateDecompileContext(IShowContext ctx) {
			var decompileContext = new DecompileContext();
			var decompilationContext = new DecompilationContext();
			decompilationContext.GetDisableAssemblyLoad = () => decompileFileTabContentFactory.FileManager.DisableAssemblyLoad();
			decompilationContext.IsBodyModified = m => decompileFileTabContentFactory.MethodAnnotations.IsBodyModified(m);
			var output = new AvalonEditTextOutput();
			var dispatcher = Dispatcher.CurrentDispatcher;
			decompileContext.DecompileNodeContext = new DecompileNodeContext(decompilationContext, language, output, dispatcher);
			if (ctx.IsRefresh) {
				decompileContext.SavedRefPos = ((ITextEditorUIContext)ctx.UIContext).SaveReferencePosition();
				if (decompileContext.SavedRefPos != null) {
					ctx.OnShown = e => {
						if (e.Success && !e.HasMovedCaret) {
							e.HasMovedCaret = ((ITextEditorUIContext)ctx.UIContext).RestoreReferencePosition(decompileContext.SavedRefPos);
							if (!e.HasMovedCaret) {
								((ITextEditorUIContext)ctx.UIContext).ScrollAndMoveCaretTo(1, 1);
								e.HasMovedCaret = true;
							}
						}
					};
				}
			}
			return decompileContext;
		}

		void UpdateLanguage() {
			if (FileTab.IsActiveTab)
				decompileFileTabContentFactory.LanguageManager.Language = language;
		}

		public void OnSelected() {
			UpdateLanguage();
		}

		public void OnUnselected() {
		}

		public void OnHide() {
		}

		public void OnShow(IShowContext ctx) {
			UpdateLanguage();
			var decompileContext = CreateDecompileContext(ctx);
			IHighlightingDefinition highlighting;
			decompileContext.CachedOutput = decompileFileTabContentFactory.DecompilationCache.Lookup(decompileContext.DecompileNodeContext.Language, nodes, out highlighting);
			decompileContext.DecompileNodeContext.HighlightingDefinition = highlighting;
			ctx.UserData = decompileContext;
		}

		public void AsyncWorker(IShowContext ctx, CancellationTokenSource source) {
			var decompileContext = (DecompileContext)ctx.UserData;
			decompileContext.CancellationTokenSource = source;
			decompileContext.DecompileNodeContext.DecompilationContext.CancellationToken = source.Token;
			decompileFileTabContentFactory.FileTreeNodeDecompiler.Decompile(decompileContext.DecompileNodeContext, nodes);
		}

		public void EndAsyncShow(IShowContext ctx, IAsyncShowResult result) {
			var decompileContext = (DecompileContext)ctx.UserData;
			var uiCtx = (ITextEditorUIContext)ctx.UIContext;

			IHighlightingDefinition highlighting;
			if (decompileContext.DecompileNodeContext.HighlightingDefinition != null)
				highlighting = decompileContext.DecompileNodeContext.HighlightingDefinition;
			else if (decompileContext.DecompileNodeContext.HighlightingExtension != null)
				highlighting = HighlightingManager.Instance.GetDefinitionByExtension(decompileContext.DecompileNodeContext.HighlightingExtension);
			else
				highlighting = decompileContext.DecompileNodeContext.Language.GetHighlightingDefinition();

			AvalonEditTextOutput output;
			if (result.IsCanceled) {
				output = new AvalonEditTextOutput();
				output.Write(dnSpy_Resources.DecompilationCanceled, TextTokenKind.Error);
			}
			else if (result.Exception != null) {
				output = new AvalonEditTextOutput();
				output.Write(dnSpy_Resources.DecompilationException, TextTokenKind.Error);
				output.WriteLine();
				output.Write(result.Exception.ToString(), TextTokenKind.Text);
			}
			else {
				output = decompileContext.CachedOutput;
				if (output == null) {
					output = (AvalonEditTextOutput)decompileContext.DecompileNodeContext.Output;
					decompileFileTabContentFactory.DecompilationCache.Cache(decompileContext.DecompileNodeContext.Language, nodes, output, highlighting);
				}
			}

			if (result.CanShowOutput)
				uiCtx.SetOutput(output, highlighting);
		}

		public bool CanStartAsyncWorker(IShowContext ctx) {
			var decompileContext = (DecompileContext)ctx.UserData;
			if (decompileContext.CachedOutput != null)
				return false;

			var uiCtx = (ITextEditorUIContext)ctx.UIContext;
			uiCtx.ShowCancelButton(() => decompileContext.CancellationTokenSource.Cancel(), dnSpy_Resources.Decompiling);
			return true;
		}
	}
}

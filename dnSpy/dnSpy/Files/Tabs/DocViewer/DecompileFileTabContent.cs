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
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Text;
using dnSpy.Properties;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Files.Tabs.DocViewer {
	[Export, ExportFileTabContentFactory(Order = TabConstants.ORDER_DECOMPILEFILETABCONTENTFACTORY)]
	sealed class DecompileFileTabContentFactory : IFileTabContentFactory {
		public IFileManager FileManager { get; }
		public IFileTreeNodeDecompiler FileTreeNodeDecompiler { get; }
		public ILanguageManager LanguageManager { get; }
		public IDecompilationCache DecompilationCache { get; }
		public IMethodAnnotations MethodAnnotations { get; }
		public IContentTypeRegistryService ContentTypeRegistryService { get; }

		[ImportingConstructor]
		DecompileFileTabContentFactory(IFileManager fileManager, IFileTreeNodeDecompiler fileTreeNodeDecompiler, ILanguageManager languageManager, IDecompilationCache decompilationCache, IMethodAnnotations methodAnnotations, IContentTypeRegistryService contentTypeRegistryService) {
			this.FileManager = fileManager;
			this.FileTreeNodeDecompiler = fileTreeNodeDecompiler;
			this.LanguageManager = languageManager;
			this.DecompilationCache = decompilationCache;
			this.MethodAnnotations = methodAnnotations;
			this.ContentTypeRegistryService = contentTypeRegistryService;
		}

		public IFileTabContent Create(IFileTabContentFactoryContext context) =>
			new DecompileFileTabContent(this, context.Nodes, LanguageManager.Language);

		public IFileTabContent Create(IFileTreeNodeData[] nodes) =>
			new DecompileFileTabContent(this, nodes, LanguageManager.Language);

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
			var language = LanguageManager.FindOrDefault(langGuid);
			return new DecompileFileTabContent(this, context.Nodes, language);
		}
	}

	sealed class DecompileFileTabContent : IAsyncFileTabContent, ILanguageTabContent {
		readonly DecompileFileTabContentFactory decompileFileTabContentFactory;
		readonly IFileTreeNodeData[] nodes;

		public ILanguage Language { get; set; }

		public DecompileFileTabContent(DecompileFileTabContentFactory decompileFileTabContentFactory, IFileTreeNodeData[] nodes, ILanguage language) {
			this.decompileFileTabContentFactory = decompileFileTabContentFactory;
			this.nodes = nodes;
			this.Language = language;
		}

		public IFileTabContent Clone() =>
			new DecompileFileTabContent(decompileFileTabContentFactory, nodes, Language);
		public IFileTabUIContext CreateUIContext(IFileTabUIContextLocator locator) =>
			locator.Get<ITextEditorUIContext>();

		public string Title {
			get {
				if (nodes.Length == 0)
					return dnSpy_Resources.EmptyTabTitle;
				if (nodes.Length == 1)
					return nodes[0].ToString(Language);
				var sb = new StringBuilder();
				foreach (var node in nodes) {
					if (sb.Length > 0)
						sb.Append(", ");
					sb.Append(node.ToString(Language));
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

		public IEnumerable<IFileTreeNodeData> Nodes => nodes;

		sealed class DecompileContext {
			public DecompileNodeContext DecompileNodeContext;
			public DnSpyTextOutputResult CachedOutputResult;
			public CancellationTokenSource CancellationTokenSource;
			public object SavedRefPos;
		}

		DecompileContext CreateDecompileContext(IShowContext ctx) {
			var decompileContext = new DecompileContext();
			var decompilationContext = new DecompilationContext();
			decompilationContext.CalculateILRanges = true;
			decompilationContext.GetDisableAssemblyLoad = () => decompileFileTabContentFactory.FileManager.DisableAssemblyLoad();
			decompilationContext.IsBodyModified = m => decompileFileTabContentFactory.MethodAnnotations.IsBodyModified(m);
			var output = new DnSpyTextOutput();
			var dispatcher = Dispatcher.CurrentDispatcher;
			decompileContext.DecompileNodeContext = new DecompileNodeContext(decompilationContext, Language, output, dispatcher);
			if (ctx.IsRefresh) {
				decompileContext.SavedRefPos = ((ITextEditorUIContext)ctx.UIContext).SaveReferencePosition();
				if (decompileContext.SavedRefPos != null) {
					ctx.OnShown = e => {
						if (e.Success && !e.HasMovedCaret) {
							e.HasMovedCaret = ((ITextEditorUIContext)ctx.UIContext).RestoreReferencePosition(decompileContext.SavedRefPos);
							if (!e.HasMovedCaret) {
								((ITextEditorUIContext)ctx.UIContext).ScrollAndMoveCaretTo(0, 0);
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
				decompileFileTabContentFactory.LanguageManager.Language = Language;
		}

		public void OnSelected() => UpdateLanguage();
		public void OnUnselected() { }
		public void OnHide() { }

		public void OnShow(IShowContext ctx) {
			UpdateLanguage();
			var decompileContext = CreateDecompileContext(ctx);
			IContentType contentType;
			decompileContext.CachedOutputResult = decompileFileTabContentFactory.DecompilationCache.Lookup(decompileContext.DecompileNodeContext.Language, nodes, out contentType);
			decompileContext.DecompileNodeContext.ContentType = contentType;
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

			var contentType = decompileContext.DecompileNodeContext.ContentType;
			if (contentType == null) {
				var contentTypeString = decompileContext.DecompileNodeContext.ContentTypeString;
				if (contentTypeString == null)
					contentTypeString = ContentTypes.TryGetContentTypeStringByExtension(decompileContext.DecompileNodeContext.Language.FileExtension) ?? ContentTypes.PLAIN_TEXT;
				contentType = decompileFileTabContentFactory.ContentTypeRegistryService.GetContentType(contentTypeString);
				Debug.Assert(contentType != null);
			}

			DnSpyTextOutputResult outputResult;
			if (result.IsCanceled) {
				var dnSpyOutput = new DnSpyTextOutput();
				dnSpyOutput.Write(dnSpy_Resources.DecompilationCanceled, BoxedOutputColor.Error);
				outputResult = dnSpyOutput.CreateResult();
			}
			else if (result.Exception != null) {
				var dnSpyOutput = new DnSpyTextOutput();
				dnSpyOutput.Write(dnSpy_Resources.DecompilationException, BoxedOutputColor.Error);
				dnSpyOutput.WriteLine();
				dnSpyOutput.Write(result.Exception.ToString(), BoxedOutputColor.Text);
				outputResult = dnSpyOutput.CreateResult();
			}
			else {
				outputResult = decompileContext.CachedOutputResult;
				if (outputResult == null) {
					var dnSpyOutput = (DnSpyTextOutput)decompileContext.DecompileNodeContext.Output;
					outputResult = dnSpyOutput.CreateResult();
					if (dnSpyOutput.CanBeCached)
						decompileFileTabContentFactory.DecompilationCache.Cache(decompileContext.DecompileNodeContext.Language, nodes, outputResult, contentType);
				}
			}

			if (result.CanShowOutput)
				uiCtx.SetOutput(outputResult, contentType);
		}

		public bool CanStartAsyncWorker(IShowContext ctx) {
			var decompileContext = (DecompileContext)ctx.UserData;
			if (decompileContext.CachedOutputResult != null)
				return false;

			var uiCtx = (ITextEditorUIContext)ctx.UIContext;
			uiCtx.ShowCancelButton(() => decompileContext.CancellationTokenSource.Cancel(), dnSpy_Resources.Decompiling);
			return true;
		}
	}
}

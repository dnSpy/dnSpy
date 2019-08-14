/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Threading.Tasks;
using System.Windows.Threading;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Properties;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Documents.Tabs.DocViewer {
	[Export(typeof(DecompilerTabContentContext))]
	sealed class DecompilerTabContentContext {
		public DecompilerTabContentTextElementProvider DecompilerTabContentTextElementProvider { get; }
		public IDocumentTreeViewSettings DocumentTreeViewSettings { get; }

		[ImportingConstructor]
		DecompilerTabContentContext(DecompilerTabContentTextElementProvider decompilerTabContentTextElementProvider, IDocumentTreeViewSettings documentTreeViewSettings) {
			DecompilerTabContentTextElementProvider = decompilerTabContentTextElementProvider;
			DocumentTreeViewSettings = documentTreeViewSettings;
		}
	}

	[Export, ExportDocumentTabContentFactory(Order = TabConstants.ORDER_DECOMPILEDOCUMENTTABCONTENTFACTORY)]
	sealed class DecompileDocumentTabContentFactory : IDocumentTabContentFactory {
		public IDsDocumentService DocumentService { get; }
		public IDocumentTreeNodeDecompiler DocumentTreeNodeDecompiler { get; }
		public IDecompilerService DecompilerService { get; }
		public IDecompilationCache DecompilationCache { get; }
		public IMethodAnnotations MethodAnnotations { get; }
		public IContentTypeRegistryService ContentTypeRegistryService { get; }
		public IDocumentViewerContentFactoryProvider DocumentViewerContentFactoryProvider { get; }
		public IDocumentWriterService DocumentWriterService { get; }
		readonly DecompilerTabContentContext decompilerTabContentContext;

		[ImportingConstructor]
		DecompileDocumentTabContentFactory(IDsDocumentService documentService, IDocumentTreeNodeDecompiler documentTreeNodeDecompiler, IDecompilerService decompilerService, IDecompilationCache decompilationCache, IMethodAnnotations methodAnnotations, IContentTypeRegistryService contentTypeRegistryService, IDocumentViewerContentFactoryProvider documentViewerContentFactoryProvider, IDocumentWriterService documentWriterService, DecompilerTabContentContext decompilerTabContentContext) {
			DocumentService = documentService;
			DocumentTreeNodeDecompiler = documentTreeNodeDecompiler;
			DecompilerService = decompilerService;
			DecompilationCache = decompilationCache;
			MethodAnnotations = methodAnnotations;
			ContentTypeRegistryService = contentTypeRegistryService;
			DocumentViewerContentFactoryProvider = documentViewerContentFactoryProvider;
			DocumentWriterService = documentWriterService;
			this.decompilerTabContentContext = decompilerTabContentContext;
		}

		public DocumentTabContent? Create(IDocumentTabContentFactoryContext context) =>
			new DecompileDocumentTabContent(this, context.Nodes, DecompilerService.Decompiler, decompilerTabContentContext);

		public DecompileDocumentTabContent Create(DocumentTreeNodeData[] nodes) =>
			new DecompileDocumentTabContent(this, nodes, DecompilerService.Decompiler, decompilerTabContentContext);

		static readonly Guid GUID_SerializedContent = new Guid("DE0390B0-747C-4F53-9CFF-1D10B93DD5DD");

		public Guid? Serialize(DocumentTabContent content, ISettingsSection section) {
			var dc = content as DecompileDocumentTabContent;
			if (dc is null)
				return null;

			section.Attribute("Language", dc.Decompiler.UniqueGuid);
			return GUID_SerializedContent;
		}

		public DocumentTabContent? Deserialize(Guid guid, ISettingsSection section, IDocumentTabContentFactoryContext context) {
			if (guid != GUID_SerializedContent)
				return null;

			var langGuid = section.Attribute<Guid?>("Language") ?? DecompilerConstants.LANGUAGE_CSHARP;
			var language = DecompilerService.FindOrDefault(langGuid);
			return new DecompileDocumentTabContent(this, context.Nodes, language, decompilerTabContentContext);
		}
	}

	sealed class DecompileDocumentTabContent : AsyncDocumentTabContent, IDecompilerTabContent {
		readonly DecompileDocumentTabContentFactory decompileDocumentTabContentFactory;
		readonly DocumentTreeNodeData[] nodes;
		readonly DecompilerTabContentContext context;

		public IDecompiler Decompiler { get; set; }

		public DecompileDocumentTabContent(DecompileDocumentTabContentFactory decompileDocumentTabContentFactory, DocumentTreeNodeData[] nodes, IDecompiler decompiler, DecompilerTabContentContext context) {
			this.decompileDocumentTabContentFactory = decompileDocumentTabContentFactory;
			this.nodes = nodes;
			Decompiler = decompiler;
			this.context = context;
		}

		public override DocumentTabContent Clone() =>
			new DecompileDocumentTabContent(decompileDocumentTabContentFactory, nodes, Decompiler, context);
		public override DocumentTabUIContext CreateUIContext(IDocumentTabUIContextLocator locator) =>
			(DocumentTabUIContext)locator.Get<IDocumentViewer>();

		public override string Title {
			get {
				var writer = new TextClassifierTextColorWriter();
				WriteTo(writer, isToolTip: false);
				return writer.ToString();
			}
		}

		void WriteTo(ITextColorWriter writer, bool isToolTip) {
			if (nodes.Length == 0) {
				writer.Write(dnSpy_Resources.EmptyTabTitle);
				return;
			}
			var options = DocumentNodeWriteOptions.Title;
			if (isToolTip)
				options |= DocumentNodeWriteOptions.ToolTip;
			bool needSep = false;
			foreach (var node in nodes) {
				if (needSep) {
					if (isToolTip) {
						writer.WriteLine();
						writer.WriteLine();
					}
					else
						writer.Write(", ");
				}
				node.Write(writer, Decompiler, options);
				needSep = true;
			}
		}

		public override object? ToolTip {
			get {
				if (nodes.Length == 0)
					return null;
				var writer = new TextClassifierTextColorWriter();
				WriteTo(writer, isToolTip: true);
				var classifierContext = new DecompilerTabContentClassifierContext(writer.Text, isToolTip: false, colorize: context.DocumentTreeViewSettings.SyntaxHighlight, colors: writer.Colors);
				var elem = context.DecompilerTabContentTextElementProvider.CreateTextElement(classifierContext, DecompilerTabContentContentTypes.DecompilerTabContentToolTip, TextElementFlags.None);
				return elem;
			}
		}

		public override IEnumerable<DocumentTreeNodeData> Nodes => nodes;

		internal bool WasNewContent { get; private set; }

		sealed class DecompileContext {
			public IDocumentViewerContentFactory? DocumentViewerContentFactory;
			public DecompileNodeContext? DecompileNodeContext;
			public DocumentViewerContent? CachedContent;
			public IAsyncShowContext? AsyncShowContext;
			public object? SavedRefPos;
		}

		DecompileContext CreateDecompileContext(IShowContext ctx) {
			var decompileContext = new DecompileContext();
			var decompilationContext = new DecompilationContext();
			decompilationContext.CalculateILSpans = true;
			decompilationContext.GetDisableAssemblyLoad = () => decompileDocumentTabContentFactory.DocumentService.DisableAssemblyLoad();
			decompilationContext.IsBodyModified = m => decompileDocumentTabContentFactory.MethodAnnotations.IsBodyModified(m);
			var dispatcher = Dispatcher.CurrentDispatcher;
			decompileContext.DocumentViewerContentFactory = decompileDocumentTabContentFactory.DocumentViewerContentFactoryProvider.Create();
			decompileContext.DecompileNodeContext = new DecompileNodeContext(decompilationContext, Decompiler, decompileContext.DocumentViewerContentFactory.Output, decompileDocumentTabContentFactory.DocumentWriterService, dispatcher);
			if (ctx.IsRefresh) {
				decompileContext.SavedRefPos = ((IDocumentViewer)ctx.UIContext).SaveReferencePosition();
				if (!(decompileContext.SavedRefPos is null)) {
					ctx.OnShown = e => {
						if (e.Success && !e.HasMovedCaret) {
							e.HasMovedCaret = ((IDocumentViewer)ctx.UIContext).RestoreReferencePosition(decompileContext.SavedRefPos);
							if (!e.HasMovedCaret) {
								((IDocumentViewer)ctx.UIContext).MoveCaretToPosition(0);
								e.HasMovedCaret = true;
							}
						}
					};
				}
			}
			return decompileContext;
		}

		void UpdateLanguage() {
			if (DocumentTab?.IsActiveTab == true)
				decompileDocumentTabContentFactory.DecompilerService.Decompiler = Decompiler;
		}

		public override void OnSelected() => UpdateLanguage();

		public override void OnShow(IShowContext ctx) {
			UpdateLanguage();
			var decompileContext = CreateDecompileContext(ctx);
			Debug2.Assert(!(decompileContext.DecompileNodeContext is null));
			decompileContext.CachedContent = decompileDocumentTabContentFactory.DecompilationCache.Lookup(decompileContext.DecompileNodeContext.Decompiler, nodes, out var contentType);
			decompileContext.DecompileNodeContext.ContentType = contentType;
			ctx.Tag = decompileContext;
		}

		public override Task CreateContentAsync(IAsyncShowContext ctx) {
			var decompileContext = (DecompileContext)ctx.Tag!;
			decompileContext.AsyncShowContext = ctx;
			Debug2.Assert(!(decompileContext.DecompileNodeContext is null));
			decompileContext.DecompileNodeContext.DecompilationContext.CancellationToken = ctx.CancellationToken;
			decompileDocumentTabContentFactory.DocumentTreeNodeDecompiler.Decompile(decompileContext.DecompileNodeContext, nodes);
			return Task.CompletedTask;
		}

		public override void OnShowAsync(IShowContext ctx, IAsyncShowResult result) {
			var decompileContext = (DecompileContext)ctx.Tag!;
			var documentViewer = (IDocumentViewer)ctx.UIContext;

			Debug2.Assert(!(decompileContext.DecompileNodeContext is null));
			Debug2.Assert(!(decompileContext.DocumentViewerContentFactory is null));
			var contentType = decompileContext.DecompileNodeContext.ContentType;
			if (contentType is null) {
				var contentTypeString = decompileContext.DecompileNodeContext.ContentTypeString;
				if (contentTypeString is null)
					contentTypeString = ContentTypesHelper.TryGetContentTypeStringByExtension(decompileContext.DecompileNodeContext.Decompiler.FileExtension) ?? ContentTypes.PlainText;
				contentType = decompileDocumentTabContentFactory.ContentTypeRegistryService.GetContentType(contentTypeString) ??
					decompileDocumentTabContentFactory.ContentTypeRegistryService.GetContentType(ContentTypes.Text) ??
					decompileDocumentTabContentFactory.ContentTypeRegistryService.UnknownContentType;
			}

			DocumentViewerContent? content;
			if (result.IsCanceled) {
				var docViewContentFactory = decompileDocumentTabContentFactory.DocumentViewerContentFactoryProvider.Create();
				docViewContentFactory.Output.Write(dnSpy_Resources.DecompilationCanceled, BoxedTextColor.Error);
				content = docViewContentFactory.CreateContent(documentViewer, contentType);
			}
			else if (!(result.Exception is null)) {
				var docViewContentFactory = decompileDocumentTabContentFactory.DocumentViewerContentFactoryProvider.Create();
				docViewContentFactory.Output.Write(dnSpy_Resources.DecompilationException, BoxedTextColor.Error);
				docViewContentFactory.Output.WriteLine();
				docViewContentFactory.Output.Write(result.Exception.ToString(), BoxedTextColor.Text);
				content = docViewContentFactory.CreateContent(documentViewer, contentType);
			}
			else {
				content = decompileContext.CachedContent;
				if (content is null) {
					bool canBeCached = decompileContext.DocumentViewerContentFactory.Output.CanBeCached;
					content = decompileContext.DocumentViewerContentFactory.CreateContent(documentViewer, contentType);
					if (canBeCached)
						decompileDocumentTabContentFactory.DecompilationCache.Cache(decompileContext.DecompileNodeContext.Decompiler, nodes, content, contentType);
				}
			}

			if (result.CanShowOutput)
				WasNewContent = documentViewer.SetContent(content, contentType);
			else
				WasNewContent = false;
		}

		public override bool NeedAsyncWork(IShowContext ctx) {
			var decompileContext = (DecompileContext)ctx.Tag!;
			if (!(decompileContext.CachedContent is null))
				return false;

			var uiCtx = (IDocumentViewer)ctx.UIContext;
			uiCtx.ShowCancelButton(dnSpy_Resources.Decompiling, () => decompileContext.AsyncShowContext!.Cancel());
			return true;
		}
	}
}

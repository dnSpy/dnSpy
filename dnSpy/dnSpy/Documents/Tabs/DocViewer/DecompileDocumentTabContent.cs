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
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Text;
using dnSpy.Properties;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Documents.Tabs.DocViewer {
	[Export, ExportDocumentTabContentFactory(Order = TabConstants.ORDER_DECOMPILEDOCUMENTTABCONTENTFACTORY)]
	sealed class DecompileDocumentTabContentFactory : IDocumentTabContentFactory {
		public IDsDocumentService DocumentService { get; }
		public IDocumentTreeNodeDecompiler DocumentTreeNodeDecompiler { get; }
		public IDecompilerService DecompilerManager { get; }
		public IDecompilationCache DecompilationCache { get; }
		public IMethodAnnotations MethodAnnotations { get; }
		public IContentTypeRegistryService ContentTypeRegistryService { get; }
		public Lazy<IDocumentViewerCustomDataProvider, IDocumentViewerCustomDataProviderMetadata>[] DocumentViewerCustomDataProviders { get; }

		[ImportingConstructor]
		DecompileDocumentTabContentFactory(IDsDocumentService documentService, IDocumentTreeNodeDecompiler documentTreeNodeDecompiler, IDecompilerService decompilerService, IDecompilationCache decompilationCache, IMethodAnnotations methodAnnotations, IContentTypeRegistryService contentTypeRegistryService, [ImportMany] IEnumerable<Lazy<IDocumentViewerCustomDataProvider, IDocumentViewerCustomDataProviderMetadata>> documentViewerCustomDataProviders) {
			this.DocumentService = documentService;
			this.DocumentTreeNodeDecompiler = documentTreeNodeDecompiler;
			this.DecompilerManager = decompilerService;
			this.DecompilationCache = decompilationCache;
			this.MethodAnnotations = methodAnnotations;
			this.ContentTypeRegistryService = contentTypeRegistryService;
			this.DocumentViewerCustomDataProviders = documentViewerCustomDataProviders.OrderBy(a => a.Metadata.Order).ToArray();
		}

		public IDocumentTabContent Create(IDocumentTabContentFactoryContext context) =>
			new DecompileDocumentTabContent(this, context.Nodes, DecompilerManager.Decompiler);

		public IDocumentTabContent Create(IDocumentTreeNodeData[] nodes) =>
			new DecompileDocumentTabContent(this, nodes, DecompilerManager.Decompiler);

		static readonly Guid GUID_SerializedContent = new Guid("DE0390B0-747C-4F53-9CFF-1D10B93DD5DD");

		public Guid? Serialize(IDocumentTabContent content, ISettingsSection section) {
			var dc = content as DecompileDocumentTabContent;
			if (dc == null)
				return null;

			section.Attribute("Language", dc.Decompiler.UniqueGuid);
			return GUID_SerializedContent;
		}

		public IDocumentTabContent Deserialize(Guid guid, ISettingsSection section, IDocumentTabContentFactoryContext context) {
			if (guid != GUID_SerializedContent)
				return null;

			var langGuid = section.Attribute<Guid?>("Language") ?? DecompilerConstants.LANGUAGE_CSHARP;
			var language = DecompilerManager.FindOrDefault(langGuid);
			return new DecompileDocumentTabContent(this, context.Nodes, language);
		}
	}

	sealed class DecompileDocumentTabContent : IAsyncDocumentTabContent, IDecompilerTabContent {
		readonly DecompileDocumentTabContentFactory decompileDocumentTabContentFactory;
		readonly IDocumentTreeNodeData[] nodes;

		public IDecompiler Decompiler { get; set; }

		public DecompileDocumentTabContent(DecompileDocumentTabContentFactory decompileDocumentTabContentFactory, IDocumentTreeNodeData[] nodes, IDecompiler decompiler) {
			this.decompileDocumentTabContentFactory = decompileDocumentTabContentFactory;
			this.nodes = nodes;
			this.Decompiler = decompiler;
		}

		public IDocumentTabContent Clone() =>
			new DecompileDocumentTabContent(decompileDocumentTabContentFactory, nodes, Decompiler);
		public IDocumentTabUIContext CreateUIContext(IDocumentTabUIContextLocator locator) =>
			locator.Get<IDocumentViewer>();

		public string Title {
			get {
				if (nodes.Length == 0)
					return dnSpy_Resources.EmptyTabTitle;
				if (nodes.Length == 1)
					return nodes[0].ToString(Decompiler);
				var sb = new StringBuilder();
				foreach (var node in nodes) {
					if (sb.Length > 0)
						sb.Append(", ");
					sb.Append(node.ToString(Decompiler));
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

		public IDocumentTab DocumentTab {
			get { return documentTab; }
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (documentTab == null)
					documentTab = value;
				else if (documentTab != value)
					throw new InvalidOperationException();
			}
		}
		IDocumentTab documentTab;

		public IEnumerable<IDocumentTreeNodeData> Nodes => nodes;

		sealed class DecompileContext {
			public DecompileNodeContext DecompileNodeContext;
			public DocumentViewerContent CachedContent;
			public CancellationTokenSource CancellationTokenSource;
			public object SavedRefPos;
		}

		DecompileContext CreateDecompileContext(IShowContext ctx) {
			var decompileContext = new DecompileContext();
			var decompilationContext = new DecompilationContext();
			decompilationContext.CalculateBinSpans = true;
			decompilationContext.GetDisableAssemblyLoad = () => decompileDocumentTabContentFactory.DocumentService.DisableAssemblyLoad();
			decompilationContext.IsBodyModified = m => decompileDocumentTabContentFactory.MethodAnnotations.IsBodyModified(m);
			var output = new DocumentViewerOutput();
			var dispatcher = Dispatcher.CurrentDispatcher;
			decompileContext.DecompileNodeContext = new DecompileNodeContext(decompilationContext, Decompiler, output, dispatcher);
			if (ctx.IsRefresh) {
				decompileContext.SavedRefPos = ((IDocumentViewer)ctx.UIContext).SaveReferencePosition();
				if (decompileContext.SavedRefPos != null) {
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
			if (DocumentTab.IsActiveTab)
				decompileDocumentTabContentFactory.DecompilerManager.Decompiler = Decompiler;
		}

		public void OnSelected() => UpdateLanguage();
		public void OnUnselected() { }
		public void OnHide() { }

		public void OnShow(IShowContext ctx) {
			UpdateLanguage();
			var decompileContext = CreateDecompileContext(ctx);
			IContentType contentType;
			decompileContext.CachedContent = decompileDocumentTabContentFactory.DecompilationCache.Lookup(decompileContext.DecompileNodeContext.Decompiler, nodes, out contentType);
			decompileContext.DecompileNodeContext.ContentType = contentType;
			ctx.UserData = decompileContext;
		}

		public void AsyncWorker(IShowContext ctx, CancellationTokenSource source) {
			var decompileContext = (DecompileContext)ctx.UserData;
			decompileContext.CancellationTokenSource = source;
			decompileContext.DecompileNodeContext.DecompilationContext.CancellationToken = source.Token;
			decompileDocumentTabContentFactory.DocumentTreeNodeDecompiler.Decompile(decompileContext.DecompileNodeContext, nodes);
		}

		public void EndAsyncShow(IShowContext ctx, IAsyncShowResult result) {
			var decompileContext = (DecompileContext)ctx.UserData;
			var documentViewer = (IDocumentViewer)ctx.UIContext;

			var contentType = decompileContext.DecompileNodeContext.ContentType;
			if (contentType == null) {
				var contentTypeString = decompileContext.DecompileNodeContext.ContentTypeString;
				if (contentTypeString == null)
					contentTypeString = ContentTypes.TryGetContentTypeStringByExtension(decompileContext.DecompileNodeContext.Decompiler.FileExtension) ?? ContentTypes.PlainText;
				contentType = decompileDocumentTabContentFactory.ContentTypeRegistryService.GetContentType(contentTypeString);
				Debug.Assert(contentType != null);
			}

			DocumentViewerContent content;
			if (result.IsCanceled) {
				var docViewerOutput = new DocumentViewerOutput();
				docViewerOutput.Write(dnSpy_Resources.DecompilationCanceled, BoxedTextColor.Error);
				content = CreateContent(documentViewer, docViewerOutput);
			}
			else if (result.Exception != null) {
				var docViewerOutput = new DocumentViewerOutput();
				docViewerOutput.Write(dnSpy_Resources.DecompilationException, BoxedTextColor.Error);
				docViewerOutput.WriteLine();
				docViewerOutput.Write(result.Exception.ToString(), BoxedTextColor.Text);
				content = CreateContent(documentViewer, docViewerOutput);
			}
			else {
				content = decompileContext.CachedContent;
				if (content == null) {
					var docViewerOutput = (DocumentViewerOutput)decompileContext.DecompileNodeContext.Output;
					content = CreateContent(documentViewer, docViewerOutput);
					if (docViewerOutput.CanBeCached)
						decompileDocumentTabContentFactory.DecompilationCache.Cache(decompileContext.DecompileNodeContext.Decompiler, nodes, content, contentType);
				}
			}

			if (result.CanShowOutput)
				documentViewer.SetContent(content, contentType);
		}

		sealed class DocumentViewerCustomDataContext : IDocumentViewerCustomDataContext, IDisposable {
			public IDocumentViewer DocumentViewer { get; private set; }
			Dictionary<string, object> customDataDict;
			Dictionary<string, object> resultDict;

			public string Text { get; }

			public DocumentViewerCustomDataContext(IDocumentViewer documentViewer, string text, Dictionary<string, object> customDataDict) {
				DocumentViewer = documentViewer;
				Text = text;
				this.customDataDict = customDataDict;
				this.resultDict = new Dictionary<string, object>(StringComparer.Ordinal);
			}

			internal Dictionary<string, object> GetResultDictionary() => resultDict;

			public void AddCustomData(string id, object data) {
				if (customDataDict == null)
					throw new ObjectDisposedException(nameof(IDocumentViewerCustomDataContext));
				if (id == null)
					throw new ArgumentNullException(nameof(id));
				if (resultDict.ContainsKey(id))
					throw new InvalidOperationException(nameof(AddCustomData) + "() can only be called once with the same " + nameof(id));
				resultDict.Add(id, data);
			}

			public TData[] GetData<TData>(string id) {
				if (customDataDict == null)
					throw new ObjectDisposedException(nameof(IDocumentViewerCustomDataContext));
				if (id == null)
					throw new ArgumentNullException(nameof(id));

				object listObj;
				if (!customDataDict.TryGetValue(id, out listObj))
					return Array.Empty<TData>();
				var list = (List<TData>)listObj;
				return list.ToArray();
			}

			public void Dispose() {
				customDataDict = null;
				DocumentViewer = null;
				resultDict = null;
			}
		}

		DocumentViewerContent CreateContent(IDocumentViewer documentViewer, DocumentViewerOutput docViewerOutput) {
			using (var context = new DocumentViewerCustomDataContext(documentViewer, docViewerOutput.GetCachedText(), docViewerOutput.GetCustomDataDictionary())) {
				foreach (var lazy in decompileDocumentTabContentFactory.DocumentViewerCustomDataProviders)
					lazy.Value.OnCustomData(context);
				return docViewerOutput.CreateResult(context.GetResultDictionary());
			}
		}

		public bool CanStartAsyncWorker(IShowContext ctx) {
			var decompileContext = (DecompileContext)ctx.UserData;
			if (decompileContext.CachedContent != null)
				return false;

			var uiCtx = (IDocumentViewer)ctx.UIContext;
			uiCtx.ShowCancelButton(dnSpy_Resources.Decompiling, () => decompileContext.CancellationTokenSource.Cancel());
			return true;
		}
	}
}

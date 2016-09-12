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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Files.Tabs.DocViewer.ToolTips;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Files.Tabs.DocViewer.ToolTips {
	[Export(typeof(DocumentViewerToolTipServiceProvider))]
	sealed class DocumentViewerToolTipServiceProvider {
		readonly IImageManager imageManager;
		readonly IDotNetImageManager dotNetImageManager;
		readonly ICodeToolTipSettings codeToolTipSettings;
		readonly Lazy<IDocumentViewerToolTipProvider, IDocumentViewerToolTipProviderMetadata>[] documentViewerToolTipProviders;

		[ImportingConstructor]
		DocumentViewerToolTipServiceProvider(IImageManager imageManager, IDotNetImageManager dotNetImageManager, ICodeToolTipSettings codeToolTipSettings, [ImportMany] IEnumerable<Lazy<IDocumentViewerToolTipProvider, IDocumentViewerToolTipProviderMetadata>> documentViewerToolTipProviders) {
			this.imageManager = imageManager;
			this.dotNetImageManager = dotNetImageManager;
			this.codeToolTipSettings = codeToolTipSettings;
			this.documentViewerToolTipProviders = documentViewerToolTipProviders.OrderBy(a => a.Metadata.Order).ToArray();
		}

		public DocumentViewerToolTipService GetService(IDocumentViewer documentViewer) =>
			documentViewer.TextView.Properties.GetOrCreateSingletonProperty(typeof(DocumentViewerToolTipService), () => new DocumentViewerToolTipService(imageManager, dotNetImageManager, codeToolTipSettings, documentViewerToolTipProviders, documentViewer));
	}

	[Export(typeof(IQuickInfoSourceProvider))]
	[Name(PredefinedDnSpyQuickInfoSourceProviders.DocumentViewer)]
	[ContentType(ContentTypes.Any)]
	sealed class DocumentViewerToolTipServiceQuickInfoSourceProvider : IQuickInfoSourceProvider {
		readonly DocumentViewerToolTipServiceProvider documentViewerToolTipServiceProvider;

		[ImportingConstructor]
		DocumentViewerToolTipServiceQuickInfoSourceProvider(DocumentViewerToolTipServiceProvider documentViewerToolTipServiceProvider) {
			this.documentViewerToolTipServiceProvider = documentViewerToolTipServiceProvider;
		}

		public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer) {
			var docViewer = textBuffer.TryGetDocumentViewer();
			if (docViewer == null)
				return null;
			return new DocumentViewerToolTipServiceQuickInfoSource(documentViewerToolTipServiceProvider.GetService(docViewer));
		}
	}

	sealed class DocumentViewerToolTipServiceQuickInfoSource : IQuickInfoSource {
		readonly DocumentViewerToolTipService documentViewerToolTipService;

		public DocumentViewerToolTipServiceQuickInfoSource(DocumentViewerToolTipService documentViewerToolTipService) {
			if (documentViewerToolTipService == null)
				throw new ArgumentNullException(nameof(documentViewerToolTipService));
			this.documentViewerToolTipService = documentViewerToolTipService;
		}

		public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan) =>
			documentViewerToolTipService.AugmentQuickInfoSession(session, quickInfoContent, out applicableToSpan);

		public void Dispose() { }
	}

	sealed class DocumentViewerToolTipService {
		readonly IImageManager imageManager;
		readonly IDotNetImageManager dotNetImageManager;
		readonly ICodeToolTipSettings codeToolTipSettings;
		readonly Lazy<IDocumentViewerToolTipProvider, IDocumentViewerToolTipProviderMetadata>[] documentViewerToolTipProviders;
		readonly IDocumentViewer documentViewer;

		public DocumentViewerToolTipService(IImageManager imageManager, IDotNetImageManager dotNetImageManager, ICodeToolTipSettings codeToolTipSettings, Lazy<IDocumentViewerToolTipProvider, IDocumentViewerToolTipProviderMetadata>[] documentViewerToolTipProviders, IDocumentViewer documentViewer) {
			if (imageManager == null)
				throw new ArgumentNullException(nameof(imageManager));
			if (dotNetImageManager == null)
				throw new ArgumentNullException(nameof(dotNetImageManager));
			if (codeToolTipSettings == null)
				throw new ArgumentNullException(nameof(codeToolTipSettings));
			if (documentViewerToolTipProviders == null)
				throw new ArgumentNullException(nameof(documentViewerToolTipProviders));
			if (documentViewer == null)
				throw new ArgumentNullException(nameof(documentViewer));
			this.imageManager = imageManager;
			this.dotNetImageManager = dotNetImageManager;
			this.codeToolTipSettings = codeToolTipSettings;
			this.documentViewerToolTipProviders = documentViewerToolTipProviders;
			this.documentViewer = documentViewer;
		}

		public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan) {
			applicableToSpan = null;
			Debug.Assert(session.TextView == documentViewer.TextView);
			if (session.TextView != documentViewer.TextView)
				return;
			var snapshot = session.TextView.TextSnapshot;
			var point = session.GetTriggerPoint(snapshot);
			if (point == null)
				return;
			var spanData = GetReference(point.Value.Position);
			if (spanData == null)
				return;
			var info = spanData.Value;
			Debug.Assert(info.Span.End <= snapshot.Length);
			if (info.Span.End > snapshot.Length)
				return;

			var toolTipContent = CreateToolTipContent(GetDecompiler(), info.Data.Reference);
			if (toolTipContent == null)
				return;

			quickInfoContent.Add(toolTipContent);
			applicableToSpan = snapshot.CreateTrackingSpan(info.Span, SpanTrackingMode.EdgeInclusive);
		}

		SpanData<ReferenceInfo>? GetReference(int position) => documentViewer.Content.ReferenceCollection.Find(position, false);

		IDecompiler GetDecompiler() {
			var content = documentViewer.FileTab.Content as IDecompilerTabContent;
			return content == null ? null : content.Decompiler;
		}

		object CreateToolTipContent(IDecompiler decompiler, object @ref) {
			if (decompiler == null)
				return null;
			if (@ref == null)
				return null;

			var ctx = new ToolTipProviderContext(imageManager, dotNetImageManager, decompiler, codeToolTipSettings, documentViewer);
			foreach (var provider in documentViewerToolTipProviders) {
				var toolTipContent = provider.Value.Create(ctx, @ref);
				if (toolTipContent != null)
					return toolTipContent;
			}

			return null;
		}
	}
}

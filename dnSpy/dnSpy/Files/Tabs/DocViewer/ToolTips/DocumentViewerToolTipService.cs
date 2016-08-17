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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Files.Tabs.DocViewer.ToolTips;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;
using dnSpy.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Files.Tabs.DocViewer.ToolTips {
	[ExportDocumentViewerListener(DocumentViewerListenerConstants.ORDER_TOOLTIPSERVICE)]
	sealed class DocumentViewerToolTipServiceListener : IDocumentViewerListener {
		readonly DocumentViewerToolTipServiceProvider documentViewerToolTipServiceProvider;

		[ImportingConstructor]
		DocumentViewerToolTipServiceListener(DocumentViewerToolTipServiceProvider documentViewerToolTipServiceProvider) {
			this.documentViewerToolTipServiceProvider = documentViewerToolTipServiceProvider;
		}

		public void OnEvent(DocumentViewerEventArgs e) {
			if (e.EventType == DocumentViewerEvent.Added)
				documentViewerToolTipServiceProvider.GetService(e.DocumentViewer);
		}
	}

	[Export(typeof(DocumentViewerToolTipServiceProvider))]
	sealed class DocumentViewerToolTipServiceProvider {
		readonly IImageManager imageManager;
		readonly IDotNetImageManager dotNetImageManager;
		readonly ICodeToolTipSettings codeToolTipSettings;
		readonly Lazy<IToolTipProvider, IToolTipProviderMetadata>[] toolTipProviders;

		[ImportingConstructor]
		DocumentViewerToolTipServiceProvider(IImageManager imageManager, IDotNetImageManager dotNetImageManager, ICodeToolTipSettings codeToolTipSettings, [ImportMany] IEnumerable<Lazy<IToolTipProvider, IToolTipProviderMetadata>> toolTipProviders) {
			this.imageManager = imageManager;
			this.dotNetImageManager = dotNetImageManager;
			this.codeToolTipSettings = codeToolTipSettings;
			this.toolTipProviders = toolTipProviders.OrderBy(a => a.Metadata.Order).ToArray();
		}

		public DocumentViewerToolTipService GetService(IDocumentViewer documentViewer) =>
			documentViewer.TextView.Properties.GetOrCreateSingletonProperty(typeof(DocumentViewerToolTipService), () => new DocumentViewerToolTipService(imageManager, dotNetImageManager, codeToolTipSettings, toolTipProviders, documentViewer));
	}

	sealed class DocumentViewerToolTipService {
		readonly IImageManager imageManager;
		readonly IDotNetImageManager dotNetImageManager;
		readonly ICodeToolTipSettings codeToolTipSettings;
		readonly Lazy<IToolTipProvider, IToolTipProviderMetadata>[] toolTipProviders;
		readonly IDocumentViewer documentViewer;
		ToolTip toolTip;
		SpanData<ReferenceInfo>? currentReference;

		public DocumentViewerToolTipService(IImageManager imageManager, IDotNetImageManager dotNetImageManager, ICodeToolTipSettings codeToolTipSettings, Lazy<IToolTipProvider, IToolTipProviderMetadata>[] toolTipProviders, IDocumentViewer documentViewer) {
			if (imageManager == null)
				throw new ArgumentNullException(nameof(imageManager));
			if (dotNetImageManager == null)
				throw new ArgumentNullException(nameof(dotNetImageManager));
			if (codeToolTipSettings == null)
				throw new ArgumentNullException(nameof(codeToolTipSettings));
			if (toolTipProviders == null)
				throw new ArgumentNullException(nameof(toolTipProviders));
			if (documentViewer == null)
				throw new ArgumentNullException(nameof(documentViewer));
			this.imageManager = imageManager;
			this.dotNetImageManager = dotNetImageManager;
			this.codeToolTipSettings = codeToolTipSettings;
			this.toolTipProviders = toolTipProviders;
			this.documentViewer = documentViewer;
			documentViewer.TextView.Closed += TextView_Closed;
			documentViewer.TextView.MouseHover += TextView_MouseHover;
		}

		void VisualElement_MouseLeave(object sender, MouseEventArgs e) => CloseToolTip();
		void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) => CloseToolTip();
		void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e) => CloseToolTip();

		SpanData<ReferenceInfo>? GetReference(int position) => documentViewer.Content.ReferenceCollection.Find(position, false);

		void TextView_MouseHover(object sender, MouseHoverEventArgs e) {
			var spanData = GetReference(e.Position);
			if (spanData != null && currentReference != null && SameReferences(currentReference.Value, spanData.Value))
				return;

			CloseToolTip();
			if (spanData != null)
				ShowToolTip(spanData.Value);
		}

		static bool SameReferences(SpanData<ReferenceInfo> a, SpanData<ReferenceInfo> b) =>
			a.Span == b.Span;

		bool ShowToolTip(SpanData<ReferenceInfo> info) {
			var toolTipContent = CreateToolTipContent(GetLanguage(), info.Data.Reference);
			if (toolTipContent == null)
				return false;
			Debug.Assert(toolTip == null);
			CloseToolTip();
			currentReference = info;
			toolTip = new ToolTip();
			toolTip.SetResourceReference(FrameworkElement.StyleProperty, "CodeToolTipStyle");
			SetScaleTransform(toolTip);
			toolTip.Content = toolTipContent;
			toolTip.IsOpen = true;

			documentViewer.TextView.VisualElement.MouseLeave += VisualElement_MouseLeave;
			documentViewer.TextView.VisualElement.MouseMove += VisualElement_MouseMove;
			documentViewer.TextView.Caret.PositionChanged += Caret_PositionChanged;
			documentViewer.TextView.LayoutChanged += TextView_LayoutChanged;
			return true;
		}

		void SetScaleTransform(ToolTip toolTip) {
			// Part of the text (eg. bottom of g and j) are sometimes clipped if we use Display
			// instead of Ideal; don't change the default settings if it's 100% zoom.
			if (documentViewer.TextView.ZoomLevel == 100)
				return;
			ToolTipHelper.SetScaleTransform(documentViewer.TextView, toolTip);
		}

		void VisualElement_MouseMove(object sender, MouseEventArgs e) {
			var info = GetReference(e);
			if (info == null || currentReference == null || !SameReferences(currentReference.Value, info.Value))
				CloseToolTip();
		}

		SpanData<ReferenceInfo>? GetReference(MouseEventArgs e) {
			var loc = MouseLocation.TryCreateTextOnly(documentViewer.TextView, e);
			if (loc == null)
				return null;
			if (loc.Position.IsInVirtualSpace)
				return null;
			return GetReference(loc.Position.Position.Position);
		}

		ILanguage GetLanguage() {
			var content = documentViewer.FileTab.Content as ILanguageTabContent;
			return content == null ? null : content.Language;
		}

		object CreateToolTipContent(ILanguage language, object @ref) {
			if (language == null)
				return null;
			if (@ref == null)
				return null;

			var ctx = new ToolTipProviderContext(imageManager, dotNetImageManager, language, codeToolTipSettings, documentViewer);
			foreach (var provider in toolTipProviders) {
				var toolTipContent = provider.Value.Create(ctx, @ref);
				if (toolTipContent != null)
					return toolTipContent;
			}

			return null;
		}

		public bool IsToolTipOpen => toolTip != null;

		public void CloseToolTip() {
			if (toolTip != null) {
				toolTip.IsOpen = false;
				toolTip = null;
				documentViewer.TextView.VisualElement.MouseLeave -= VisualElement_MouseLeave;
				documentViewer.TextView.VisualElement.MouseMove -= VisualElement_MouseMove;
				documentViewer.TextView.Caret.PositionChanged -= Caret_PositionChanged;
				documentViewer.TextView.LayoutChanged -= TextView_LayoutChanged;
			}
			currentReference = null;
		}

		void TextView_Closed(object sender, EventArgs e) {
			CloseToolTip();
			documentViewer.TextView.Closed -= TextView_Closed;
			documentViewer.TextView.MouseHover -= TextView_MouseHover;
		}
	}
}

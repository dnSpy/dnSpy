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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using dnlib.PE;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.AsmEditor.Hex.Nodes {
	[ExportReferenceDocumentTabContentProvider(Order = TabConstants.ORDER_CONTENTPROVIDER_HEXADDRREF)]
	sealed class HexAddressReferenceFileTabContentCreator : IReferenceDocumentTabContentProvider {
		readonly Lazy<IHexViewDocumentTabContentCreator> hexViewDocumentTabContentCreator;

		[ImportingConstructor]
		HexAddressReferenceFileTabContentCreator(Lazy<IHexViewDocumentTabContentCreator> hexViewDocumentTabContentCreator, IDocumentTreeView documentTreeView) => this.hexViewDocumentTabContentCreator = hexViewDocumentTabContentCreator;

		public DocumentTabReferenceResult Create(IDocumentTabService documentTabService, DocumentTabContent sourceContent, object @ref) {
			var addrRef = @ref as AddressReference;
			if (addrRef == null)
				addrRef = (@ref as TextReference)?.Reference as AddressReference;
			if (addrRef != null)
				return Create(addrRef, documentTabService.DocumentTreeView);
			return null;
		}

		DocumentTabReferenceResult Create(AddressReference addrRef, IDocumentTreeView documentTreeView) {
			var content = hexViewDocumentTabContentCreator.Value.TryCreate(addrRef.Filename);
			if (content == null)
				return null;
			var fileOffset = GetFileOffset(addrRef, documentTreeView);
			return new DocumentTabReferenceResult(content, null, e => CreateHandler(e, content, fileOffset, addrRef));
		}

		void CreateHandler(ShowTabContentEventArgs e, HexViewDocumentTabContent content, HexPosition? fileOffset, AddressReference addrRef) {
			if (!e.Success)
				return;

			Debug.Assert(e.Tab.Content == content);
			var uiContext = e.Tab.UIContext as HexViewDocumentTabUIContext;
			Debug.Assert(uiContext != null);
			if (uiContext == null || fileOffset == null)
				return;

			var start = fileOffset.Value;
			var end = HexPosition.Min(start + addrRef.Length, HexPosition.MaxEndPosition);
			if (!IsVisible(uiContext.HexView, start, end)) {
				uiContext.HexView.Options.SetOptionValue(DefaultHexViewOptions.StartPositionId, uiContext.HexView.Buffer.Span.Start);
				uiContext.HexView.Options.SetOptionValue(DefaultHexViewOptions.EndPositionId, uiContext.HexView.Buffer.Span.End);
				RedisplayHexLines(uiContext.HexView);
				if (!IsVisible(uiContext.HexView, start, end))
					return;
			}
			if (e.HasMovedCaret)
				return;

			if (!uiContext.HexView.VisualElement.IsLoaded) {
				RoutedEventHandler loaded = null;
				loaded = (s, e2) => {
					uiContext.HexView.VisualElement.Loaded -= loaded;
					InitializeHexView(uiContext.HexView, start, end);
				};
				uiContext.HexView.VisualElement.Loaded += loaded;
			}
			else
				InitializeHexView(uiContext.HexView, start, end);
			e.HasMovedCaret = true;
		}

		static void InitializeHexView(HexView hexView, HexPosition start, HexPosition end) {
			if (!IsVisible(hexView, start, end))
				return;
			var span = new HexBufferSpan(new HexBufferPoint(hexView.Buffer, start), new HexBufferPoint(hexView.Buffer, end));
			hexView.Selection.Select(span.Start, span.End, alignPoints: false);
			var column = hexView.Caret.IsValuesCaretPresent ? HexColumnType.Values : HexColumnType.Ascii;
			hexView.Caret.MoveTo(column, span.Start);
			var flags = column == HexColumnType.Values ? HexSpanSelectionFlags.Values : HexSpanSelectionFlags.Ascii;
			hexView.ViewScroller.EnsureSpanVisible(span, flags, VSTE.EnsureSpanVisibleOptions.ShowStart);
		}

		static void RedisplayHexLines(HexView hexView) {
			var line = hexView.HexViewLines.FirstVisibleLine;
			var verticalDistance = line.Top - hexView.ViewportTop;
			var bufferPosition = line.BufferStart;
			hexView.DisplayHexLineContainingBufferPosition(bufferPosition, verticalDistance, VSTE.ViewRelativePosition.Top, null, null, DisplayHexLineOptions.CanRecreateBufferLines);
		}

		static bool IsVisible(HexView hexView, HexPosition start, HexPosition end) =>
			start < HexPosition.MaxEndPosition && start >= hexView.BufferLines.StartPosition && end <= hexView.BufferLines.EndPosition;

		HexPosition? GetFileOffset(AddressReference addrRef, IDocumentTreeView documentTreeView) {
			if (!addrRef.IsRVA)
				return addrRef.Address;
			if (string.IsNullOrEmpty(addrRef.Filename))
				return null;

			var file = documentTreeView.GetAllCreatedDocumentNodes().FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a.Document.Filename, addrRef.Filename));
			if (file == null)
				return null;
			var pe = file.Document.PEImage;
			if (pe == null)
				return null;
			return (ulong)pe.ToFileOffset((RVA)addrRef.Address);
		}
	}
}

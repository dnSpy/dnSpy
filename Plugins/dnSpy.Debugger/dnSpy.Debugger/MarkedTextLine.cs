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
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Images;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Debugger {
	abstract class MarkedTextLine : IMarkedTextLine {
		readonly IMarkedTextLine senderObj;

		protected MarkedTextLine(SerializedDnToken methodKey, uint ilOffset, IMarkedTextLine senderObj = null) {
			this.methodKey = methodKey;
			this.ILOffset = ilOffset;
			this.senderObj = senderObj ?? this;
		}

		public SerializedDnToken SerializedDnToken => methodKey;
		SerializedDnToken methodKey;

		public uint ILOffset { get; }
		public event EventHandler<TextLineObjectEventArgs> ObjPropertyChanged;
		protected void OnObjPropertyChanged(string propName) => ObjPropertyChanged?.Invoke(senderObj, new TextLineObjectEventArgs(propName));

		public int GetLineNumber(IDocumentViewer documentViewer) {
			return -1;
		}

		public bool GetLocation(IDocumentViewer documentViewer, out TextSpan textSpan) {
			textSpan = default(TextSpan);
			return false;
		}

		public abstract bool IsVisible(IDocumentViewer documentViewer);
		protected abstract void Initialize(IDocumentViewer documentViewer, ITextMarkerService markerService, ITextMarker marker);

		public ITextMarker CreateMarker(IDocumentViewer documentViewer, ITextMarkerService markerService) {
			var marker = CreateMarkerInternal(markerService, documentViewer);
			var methodDebugService = documentViewer.GetMethodDebugService();
			marker.ZOrder = ZOrder;
			marker.IsVisible = b => methodDebugService.TryGetMethodDebugInfo(SerializedDnToken) != null;
			marker.TextMarkerObject = this;
			Initialize(documentViewer, markerService, marker);
			return marker;
		}

		ITextMarker CreateMarkerInternal(ITextMarkerService markerService, IDocumentViewer documentViewer) {
			return markerService.Create(0, 0);
		}

		public abstract bool HasImage { get; }
		public abstract double ZOrder { get; }
		public abstract ImageReference? ImageReference { get; }
		protected void Redraw() => OnObjPropertyChanged(TextLineObjectEventArgs.RedrawProperty);
	}
}

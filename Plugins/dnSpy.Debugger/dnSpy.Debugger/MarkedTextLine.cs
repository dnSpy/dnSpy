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
using dnSpy.Contracts.Files.Tabs.TextEditor;
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

		public int GetLineNumber(ITextEditorUIContext uiContext) {
			TextPosition location, endLocation;
			if (GetLocation(uiContext, out location, out endLocation))
				return location.Line;
			return -1;
		}

		public bool GetLocation(ITextEditorUIContext uiContext, out TextPosition location, out TextPosition endLocation) {
			var cm = uiContext.GetCodeMappings();
			var mapping = cm.TryGetMapping(methodKey);
			if (mapping == null) {
				location = endLocation = new TextPosition();
				return false;
			}

			bool isMatch;
			var map = mapping.GetInstructionByOffset(ILOffset, out isMatch);
			if (map == null) {
				location = endLocation = new TextPosition();
				return false;
			}

			location = map.StartPosition;
			endLocation = map.EndPosition;
			return true;
		}

		public abstract bool IsVisible(ITextEditorUIContext uiContext);
		protected abstract void Initialize(ITextEditorUIContext uiContext, ITextMarkerService markerService, ITextMarker marker);

		public ITextMarker CreateMarker(ITextEditorUIContext uiContext, ITextMarkerService markerService) {
			var marker = CreateMarkerInternal(markerService, uiContext);
			var cm = uiContext.GetCodeMappings();
			marker.ZOrder = ZOrder;
			marker.IsVisible = b => cm.TryGetMapping(SerializedDnToken) != null;
			marker.TextMarkerObject = this;
			Initialize(uiContext, markerService, marker);
			return marker;
		}

		ITextMarker CreateMarkerInternal(ITextMarkerService markerService, ITextEditorUIContext uiContext) {
			TextPosition location, endLocation;
			if (!GetLocation(uiContext, out location, out endLocation))
				throw new InvalidOperationException();

			var line = markerService.TextView.Document.GetLineByNumber(location.Line + 1);
			var endLine = markerService.TextView.Document.GetLineByNumber(endLocation.Line + 1);
			int startOffset = line.Offset + location.Column;
			int endOffset = endLine.Offset + endLocation.Column;

			return markerService.Create(startOffset, endOffset - startOffset);
		}

		public abstract bool HasImage { get; }
		public abstract double ZOrder { get; }
		public abstract ImageReference? ImageReference { get; }
		protected void Redraw() => OnObjPropertyChanged(TextLineObjectEventArgs.RedrawProperty);
	}
}

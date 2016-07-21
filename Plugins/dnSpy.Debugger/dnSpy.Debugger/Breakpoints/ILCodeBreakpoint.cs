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

namespace dnSpy.Debugger.Breakpoints {
	sealed class ILCodeBreakpoint : Breakpoint, IMarkedTextLine {
		sealed class MyMarkedTextLine : MarkedTextLine {
			readonly ILCodeBreakpoint ilbp;

			public MyMarkedTextLine(ILCodeBreakpoint ilbp, SerializedDnToken methodKey, uint ilOffset)
				: base(methodKey, ilOffset, ilbp) {
				this.ilbp = ilbp;
			}

			protected override void Initialize(ITextEditorUIContext uiContext, ITextMarkerService markerService, ITextMarker marker) =>
				marker.HighlightingColor = () => ilbp.IsEnabled ? DebuggerColors.CodeBreakpointHighlightingColor : DebuggerColors.CodeBreakpointDisabledHighlightingColor;

			public override bool IsVisible(ITextEditorUIContext uiContext) {
				TextPosition location, endLocation;
				var cm = uiContext.GetCodeMappings();
				var mm = cm.TryGetMapping(SerializedDnToken);
				if (mm == null)
					return false;
				if (!mm.GetInstructionByTokenAndOffset(ILOffset, out location, out endLocation))
					return false;

				return true;
			}

			public override bool HasImage => true;
			public override double ZOrder => TextEditorConstants.ZORDER_BREAKPOINT;

			public override ImageReference? ImageReference {
				get {
					return ilbp.IsEnabled ?
						new ImageReference(GetType().Assembly, "Breakpoint") :
						new ImageReference(GetType().Assembly, "DisabledBreakpoint");
				}
			}

			internal new void Redraw() => base.Redraw();
		}

		event EventHandler<TextLineObjectEventArgs> ITextLineObject.ObjPropertyChanged {
			add { myMarkedTextLine.ObjPropertyChanged += value; }
			remove { myMarkedTextLine.ObjPropertyChanged -= value; }
		}

		public override BreakpointKind Kind => BreakpointKind.ILCode;
		public SerializedDnToken SerializedDnToken => myMarkedTextLine.SerializedDnToken;
		public uint ILOffset => myMarkedTextLine.ILOffset;
		public double ZOrder => myMarkedTextLine.ZOrder;
		public bool HasImage => myMarkedTextLine.HasImage;
		public ImageReference? ImageReference => myMarkedTextLine.ImageReference;
		readonly MyMarkedTextLine myMarkedTextLine;

		public ILCodeBreakpoint(SerializedDnToken methodKey, uint ilOffset, bool isEnabled = true)
			: base(isEnabled) {
			this.myMarkedTextLine = new MyMarkedTextLine(this, methodKey, ilOffset);
		}

		public int GetLineNumber(ITextEditorUIContext uiContext) => myMarkedTextLine.GetLineNumber(uiContext);
		public bool GetLocation(ITextEditorUIContext uiContext, out TextPosition location, out TextPosition endLocation) =>
			myMarkedTextLine.GetLocation(uiContext, out location, out endLocation);
		ITextMarker ITextMarkerObject.CreateMarker(ITextEditorUIContext uiContext, ITextMarkerService markerService) =>
			myMarkedTextLine.CreateMarker(uiContext, markerService);
		bool ITextLineObject.IsVisible(ITextEditorUIContext uiContext) => myMarkedTextLine.IsVisible(uiContext);

		protected override void OnIsEnabledChanged() {
			OnPropertyChanged("Image");
			myMarkedTextLine.Redraw();
		}
	}
}

/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Windows.Media;
using dnSpy.AvalonEdit;
using dnSpy.Images;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.NRefactory;

namespace dnSpy.Debugger.Breakpoints {
	public sealed class ILCodeBreakpoint : Breakpoint, IMarkedTextLine {
		sealed class MyMarkedTextLine : MarkedTextLine {
			readonly ILCodeBreakpoint ilbp;

			public MyMarkedTextLine(ILCodeBreakpoint ilbp, MethodKey methodKey, uint ilOffset)
				: base(methodKey, ilOffset, ilbp) {
				this.ilbp = ilbp;
			}

			protected override void Initialize(DecompilerTextView textView, ITextMarkerService markerService, ITextMarker marker) {
				marker.HighlightingColor = () => ilbp.IsEnabled ? DebuggerColors.CodeBreakpointHighlightingColor : DebuggerColors.CodeBreakpointDisabledHighlightingColor;
			}

			public override bool IsVisible(DecompilerTextView textView) {
				TextLocation location, endLocation;
				var cm = textView == null ? null : textView.CodeMappings;
				if (cm == null || !cm.ContainsKey(MethodKey))
					return false;
				if (!cm[MethodKey].GetInstructionByTokenAndOffset(ILOffset, out location, out endLocation))
					return false;

				return true;
			}

			public override bool HasImage {
				get { return true; }
			}

			public override double ZOrder {
				get { return (int)TextLineObjectZOrder.Breakpoint; }
			}

			public override ImageSource GetImage(Color bgColor) {
				return ilbp.IsEnabled ?
					ImageCache.Instance.GetImage("Breakpoint", bgColor) :
					ImageCache.Instance.GetImage("DisabledBreakpoint", bgColor);
			}

			internal new void Redraw() {
				base.Redraw();
			}
		}

		event EventHandler<TextLineObjectEventArgs> ITextLineObject.ObjPropertyChanged {
			add { myMarkedTextLine.ObjPropertyChanged += value; }
			remove { myMarkedTextLine.ObjPropertyChanged -= value; }
		}

		public override BreakpointType Type {
			get { return BreakpointType.ILCode; }
		}

		public MethodKey MethodKey {
			get { return myMarkedTextLine.MethodKey; }
		}

		public uint ILOffset {
			get { return myMarkedTextLine.ILOffset; }
		}

		public double ZOrder {
			get { return myMarkedTextLine.ZOrder; }
		}

		public bool HasImage {
			get { return myMarkedTextLine.HasImage; }
		}

		public ImageSource GetImage(Color bgColor) {
			return myMarkedTextLine.GetImage(bgColor);
		}

		readonly MyMarkedTextLine myMarkedTextLine;

		public string Assembly {
			get { return asmName; }
		}
		readonly string asmName;

		public ILCodeBreakpoint(string asmName, MethodKey methodKey, uint ilOffset, bool isEnabled = true)
			: base(isEnabled) {
			this.asmName = asmName;
			this.myMarkedTextLine = new MyMarkedTextLine(this, methodKey, ilOffset);
		}

		public int GetLineNumber(DecompilerTextView textView) {
			return myMarkedTextLine.GetLineNumber(textView);
		}

		public bool GetLocation(DecompilerTextView textView, out TextLocation location, out TextLocation endLocation) {
			return myMarkedTextLine.GetLocation(textView, out location, out endLocation);
		}

		ITextMarker ITextMarkerObject.CreateMarker(DecompilerTextView textView, ITextMarkerService markerService) {
			return myMarkedTextLine.CreateMarker(textView, markerService);
		}

		bool ITextLineObject.IsVisible(DecompilerTextView textView) {
			return myMarkedTextLine.IsVisible(textView);
		}

		protected override void OnIsEnabledChanged() {
			OnPropertyChanged("Image");
			myMarkedTextLine.Redraw();
		}
	}
}

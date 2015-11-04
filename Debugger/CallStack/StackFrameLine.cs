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
using dnSpy.Files;
using dnSpy.Images;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.TextView;

namespace dnSpy.Debugger.CallStack {
	enum StackFrameLineType {
		/// <summary>
		/// This is the statement that will be executed next
		/// </summary>
		CurrentStatement,

		/// <summary>
		/// One of the return statements
		/// </summary>
		ReturnStatement,

		/// <summary>
		/// A selected return statement
		/// </summary>
		SelectedReturnStatement,
	}

	sealed class StackFrameLine : MarkedTextLine {
		public override bool HasImage {
			get { return GetImageName() != null; }
		}

		public override double ZOrder {
			get {
				switch (type) {
				case StackFrameLineType.CurrentStatement:
					return (int)TextLineObjectZOrder.CurrentStatement;
				case StackFrameLineType.SelectedReturnStatement:
					return (int)TextLineObjectZOrder.SelectedReturnStatement;
				case StackFrameLineType.ReturnStatement:
					return (int)TextLineObjectZOrder.ReturnStatement;
				default:
					throw new InvalidOperationException();
				}
			}
		}

		readonly StackFrameLineType type;

		internal DecompilerTextView TextView {
			get { return decompilerTextView; }
		}
		readonly DecompilerTextView decompilerTextView;

		public StackFrameLine(StackFrameLineType type, DecompilerTextView decompilerTextView, SerializedDnSpyToken methodKey, uint ilOffset)
			: base(methodKey, ilOffset) {
			this.type = type;
			this.decompilerTextView = decompilerTextView;
		}

		protected override void Initialize(DecompilerTextView textView, ITextMarkerService markerService, ITextMarker marker) {
			marker.HighlightingColor = () => {
				switch (type) {
				case StackFrameLineType.CurrentStatement:
					return DebuggerColors.StackFrameCurrentHighlightingColor;
				case StackFrameLineType.SelectedReturnStatement:
					return DebuggerColors.StackFrameSelectedHighlightingColor;
				case StackFrameLineType.ReturnStatement:
					return DebuggerColors.StackFrameReturnHighlightingColor;
				default:
					throw new InvalidOperationException();
				}
			};
		}

		public override ImageSource GetImage(Color bgColor) {
			var name = GetImageName();
			if (name != null)
				return ImageCache.Instance.GetImage(GetType().Assembly, name, bgColor);
			return null;
		}

		string GetImageName() {
			switch (type) {
			case StackFrameLineType.CurrentStatement:
				return "CurrentLine";
			case StackFrameLineType.SelectedReturnStatement:
				return "SelectedReturnLine";
			case StackFrameLineType.ReturnStatement:
				return null;
			default:
				throw new InvalidOperationException();
			}
		}

		public override bool IsVisible(DecompilerTextView textView) {
			return decompilerTextView == textView;
		}
	}
}

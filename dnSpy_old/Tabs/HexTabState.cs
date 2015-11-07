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

using System.IO;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Hex;
using dnSpy.Shared.UI.HexEditor;

namespace dnSpy.Tabs {
	public sealed class HexTabState : TabState {
		public readonly DnHexBox HexBox;

		public override UIElement FocusedElement {
			get { return HexBox; }
		}

		public override string Header {
			get {
				var doc = HexBox.Document;
				if (doc == null)
					return "<NO DOC>";
				var filename = HexBox.Document.Name;
				try {
					return Path.GetFileName(filename);
				}
				catch {
				}
				return filename;
			}
		}

		public override string ToolTip {
			get {
				var doc = HexBox.Document;
				if (doc == null)
					return null;
				return doc.Name;
			}
		}

		public override FrameworkElement ScaleElement {
			get { return HexBox; }
		}

		public override TabStateType Type {
			get { return TabStateType.HexEditor; }
		}

		public override string FileName {
			get { return HexBox.Document == null ? null : HexBox.Document.Name; }
		}

		public override string Name {
			get { return HexBox.Document == null ? null : Path.GetFileName(HexBox.Document.Name); }
		}

		public HexTabState() {
			this.HexBox = new DnHexBox();
			this.HexBox.Tag = this;
			var scroller = new ScrollViewer();
			scroller.Content = HexBox;
			scroller.CanContentScroll = true;
			scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
			scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			this.TabItem.Content = scroller;

			InstallMouseWheelZoomHandler(HexBox);
		}

		public void Restore(SavedHexTabState state) {
			HexBox.BytesGroupCount = state.BytesGroupCount;
			HexBox.BytesPerLine = state.BytesPerLine;
			HexBox.UseHexPrefix = state.UseHexPrefix;
			HexBox.ShowAscii = state.ShowAscii;
			HexBox.LowerCaseHex = state.LowerCaseHex;
			HexBox.AsciiEncoding = state.AsciiEncoding;

			HexBox.HexOffsetSize = state.HexOffsetSize;
			HexBox.UseRelativeOffsets = state.UseRelativeOffsets;
			HexBox.BaseOffset = state.BaseOffset;

			if (HexBox.IsLoaded)
				HexBox.State = state.HexBoxState;
			else
				new StateRestorer(HexBox, state.HexBoxState);
		}

		sealed class StateRestorer {
			readonly HexBox hexBox;
			readonly HexBoxState state;

			public StateRestorer(HexBox hexBox, HexBoxState state) {
				this.hexBox = hexBox;
				this.state = state;
				this.hexBox.Loaded += HexBox_Loaded;
			}

			private void HexBox_Loaded(object sender, RoutedEventArgs e) {
				this.hexBox.Loaded -= HexBox_Loaded;
				hexBox.UpdateLayout();
				hexBox.State = state;
			}
		}

		public override SavedTabState CreateSavedTabState() {
			var state = new SavedHexTabState();
			state.BytesGroupCount = HexBox.BytesGroupCount;
			state.BytesPerLine = HexBox.BytesPerLine;
			state.UseHexPrefix = HexBox.UseHexPrefix;
			state.ShowAscii = HexBox.ShowAscii;
			state.LowerCaseHex = HexBox.LowerCaseHex;
			state.AsciiEncoding = HexBox.AsciiEncoding;

			state.HexOffsetSize = HexBox.HexOffsetSize;
			state.UseRelativeOffsets = HexBox.UseRelativeOffsets;
			state.BaseOffset = HexBox.BaseOffset;
			state.HexBoxState = HexBox.State;
			state.FileName = HexBox.Document == null ? string.Empty : HexBox.Document.Name;
			return state;
		}

		public void SetDocument(HexDocument doc) {
			this.HexBox.Document = doc;
			UpdateHeader();
		}
	}
}

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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using dnSpy.AsmEditor.Hex;
using dnSpy.dntheme;
using dnSpy.HexEditor;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.ILSpy;

namespace dnSpy.Tabs {
	public sealed class HexTabState : TabState {
		internal readonly HexBox HexBox;

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
			this.HexBox = new HexBox();
			this.HexBox.Tag = this;
			var scroller = new ScrollViewer();
			scroller.Content = HexBox;
			scroller.CanContentScroll = true;
			scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
			scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			this.TabItem.Content = scroller;

			this.HexBox.SetBinding(Control.FontFamilyProperty, new Binding("FontFamily") { Source = HexSettings.Instance });
			this.HexBox.SetBinding(Control.FontSizeProperty, new Binding("FontSize") { Source = HexSettings.Instance });
			this.HexBox.SetResourceReference(Control.BackgroundProperty, GetBackgroundResourceKey(ColorType.HexText));
			this.HexBox.SetResourceReference(Control.ForegroundProperty, GetForegroundResourceKey(ColorType.HexText));
			this.HexBox.SetResourceReference(HexBox.OffsetForegroundProperty, GetForegroundResourceKey(ColorType.HexOffset));
			this.HexBox.SetResourceReference(HexBox.Byte0ForegroundProperty, GetForegroundResourceKey(ColorType.HexByte0));
			this.HexBox.SetResourceReference(HexBox.Byte1ForegroundProperty, GetForegroundResourceKey(ColorType.HexByte1));
			this.HexBox.SetResourceReference(HexBox.ByteErrorForegroundProperty, GetForegroundResourceKey(ColorType.HexByteError));
			this.HexBox.SetResourceReference(HexBox.AsciiForegroundProperty, GetForegroundResourceKey(ColorType.HexAscii));
			this.HexBox.SetResourceReference(HexBox.CaretForegroundProperty, GetBackgroundResourceKey(ColorType.HexCaret));
			this.HexBox.SetResourceReference(HexBox.InactiveCaretForegroundProperty, GetBackgroundResourceKey(ColorType.HexInactiveCaret));
			this.HexBox.SetResourceReference(HexBox.SelectionBackgroundProperty, GetBackgroundResourceKey(ColorType.HexSelection));
			this.HexBox.SetResourceReference(Control.FontStyleProperty, GetFontStyleResourceKey(ColorType.HexText));
			this.HexBox.SetResourceReference(Control.FontWeightProperty, GetFontWeightResourceKey(ColorType.HexText));

			ContextMenuProvider.Add(this.HexBox);

			InstallMouseWheelZoomHandler(HexBox);

			BytesGroupCount = null;
			BytesPerLine = null;
			UseHexPrefix = null;
			ShowAscii = null;
			LowerCaseHex = null;
			AsciiEncoding = null;
		}

		internal static void OnThemeUpdatedStatic() {
			var theme = Themes.Theme;

			var color = theme.GetColor(ColorType.HexText).InheritedColor;
			App.Current.Resources[GetBackgroundResourceKey(ColorType.HexText)] = GetBrush(color.Background);
			App.Current.Resources[GetForegroundResourceKey(ColorType.HexText)] = GetBrush(color.Foreground);
			App.Current.Resources[GetFontStyleResourceKey(ColorType.HexText)] = color.FontStyle ?? FontStyles.Normal;
			App.Current.Resources[GetFontWeightResourceKey(ColorType.HexText)] = color.FontWeight ?? FontWeights.Normal;

			UpdateForeground(theme, ColorType.HexOffset);
			UpdateForeground(theme, ColorType.HexByte0);
			UpdateForeground(theme, ColorType.HexByte1);
			UpdateForeground(theme, ColorType.HexByteError);
			UpdateForeground(theme, ColorType.HexAscii);
			UpdateBackground(theme, ColorType.HexCaret);
			UpdateBackground(theme, ColorType.HexInactiveCaret);
			UpdateBackground(theme, ColorType.HexSelection);
		}

		static void UpdateForeground(Theme theme, ColorType colorType) {
			var color = theme.GetColor(colorType).TextInheritedColor;
			App.Current.Resources[GetForegroundResourceKey(colorType)] = GetBrush(color.Foreground);
		}

		static void UpdateBackground(Theme theme, ColorType colorType) {
			var color = theme.GetColor(colorType).TextInheritedColor;
			App.Current.Resources[GetBackgroundResourceKey(colorType)] = GetBrush(color.Background);
		}

		static Brush GetBrush(HighlightingBrush b) {
			return b == null ? Brushes.Transparent : b.GetBrush(null);
		}

		static string GetBackgroundResourceKey(ColorType colorType) {
			return string.Format("HB_{0}_Background", Enum.GetName(typeof(ColorType), colorType));
		}

		static string GetForegroundResourceKey(ColorType colorType) {
			return string.Format("HB_{0}_Foreground", Enum.GetName(typeof(ColorType), colorType));
		}

		static string GetFontStyleResourceKey(ColorType colorType) {
			return string.Format("HB_{0}_FontStyle", Enum.GetName(typeof(ColorType), colorType));
		}

		static string GetFontWeightResourceKey(ColorType colorType) {
			return string.Format("HB_{0}_FontWeight", Enum.GetName(typeof(ColorType), colorType));
		}

		public void Restore(SavedHexTabState state) {
			BytesGroupCount = state.BytesGroupCount;
			BytesPerLine = state.BytesPerLine;
			UseHexPrefix = state.UseHexPrefix;
			ShowAscii = state.ShowAscii;
			LowerCaseHex = state.LowerCaseHex;
			AsciiEncoding = state.AsciiEncoding;

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
			state.BytesGroupCount = BytesGroupCount;
			state.BytesPerLine = BytesPerLine;
			state.UseHexPrefix = UseHexPrefix;
			state.ShowAscii = ShowAscii;
			state.LowerCaseHex = LowerCaseHex;
			state.AsciiEncoding = AsciiEncoding;

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

		public void InitializeStartEndOffset() {
			HexBox.StartOffset = DocumentStartOffset;
			HexBox.EndOffset = DocumentEndOffset;
		}

		public ulong DocumentStartOffset {
			get {
				var doc = HexBox.Document;
				return doc == null ? 0 : doc.StartOffset;
			}
		}

		public ulong DocumentEndOffset {
			get {
				var doc = HexBox.Document;
				return doc == null ? 0 : doc.EndOffset;
			}
		}

		public int? BytesGroupCount {
			get { return useDefault_BytesGroupCount ? (int?)null : HexBox.BytesGroupCount; }
			set {
				if (value == null) {
					useDefault_BytesGroupCount = true;
					HexBox.ClearValue(HexBox.BytesGroupCountProperty);
					HexBox.SetBinding(HexBox.BytesGroupCountProperty, new Binding("BytesGroupCount") { Source = HexSettings.Instance });
				}
				else {
					useDefault_BytesGroupCount = false;
					HexBox.BytesGroupCount = value.Value;
				}
			}
		}
		bool useDefault_BytesGroupCount;

		public int? BytesPerLine {
			get { return useDefault_BytesPerLine ? (int?)null : HexBox.BytesPerLine; }
			set {
				if (value == null) {
					useDefault_BytesPerLine = true;
					HexBox.ClearValue(HexBox.BytesPerLineProperty);
					HexBox.SetBinding(HexBox.BytesPerLineProperty, new Binding("BytesPerLine") { Source = HexSettings.Instance });
				}
				else {
					useDefault_BytesPerLine = false;
					HexBox.BytesPerLine = Math.Min(HexSettings.MAX_BYTES_PER_LINE, value.Value);
				}
			}
		}
		bool useDefault_BytesPerLine;

		public bool? UseHexPrefix {
			get { return useDefault_UseHexPrefix ? (bool?)null : HexBox.UseHexPrefix; }
			set {
				if (value == null) {
					useDefault_UseHexPrefix = true;
					HexBox.ClearValue(HexBox.UseHexPrefixProperty);
					HexBox.SetBinding(HexBox.UseHexPrefixProperty, new Binding("UseHexPrefix") { Source = HexSettings.Instance });
				}
				else {
					useDefault_UseHexPrefix = false;
					HexBox.UseHexPrefix = value.Value;
				}
			}
		}
		bool useDefault_UseHexPrefix;

		public bool? ShowAscii {
			get { return useDefault_ShowAscii ? (bool?)null : HexBox.ShowAscii; }
			set {
				if (value == null) {
					useDefault_ShowAscii = true;
					HexBox.ClearValue(HexBox.ShowAsciiProperty);
					HexBox.SetBinding(HexBox.ShowAsciiProperty, new Binding("ShowAscii") { Source = HexSettings.Instance });
				}
				else {
					useDefault_ShowAscii = false;
					HexBox.ShowAscii = value.Value;
				}
			}
		}
		bool useDefault_ShowAscii;

		public bool? LowerCaseHex {
			get { return useDefault_LowerCaseHex ? (bool?)null : HexBox.LowerCaseHex; }
			set {
				if (value == null) {
					useDefault_LowerCaseHex = true;
					HexBox.ClearValue(HexBox.LowerCaseHexProperty);
					HexBox.SetBinding(HexBox.LowerCaseHexProperty, new Binding("LowerCaseHex") { Source = HexSettings.Instance });
				}
				else {
					useDefault_LowerCaseHex = false;
					HexBox.LowerCaseHex = value.Value;
				}
			}
		}
		bool useDefault_LowerCaseHex;

		public AsciiEncoding? AsciiEncoding {
			get { return useDefault_AsciiEncoding ? (AsciiEncoding?)null : HexBox.AsciiEncoding; }
			set {
				if (value == null) {
					useDefault_AsciiEncoding = true;
					HexBox.ClearValue(HexBox.AsciiEncodingProperty);
					HexBox.SetBinding(HexBox.AsciiEncodingProperty, new Binding("AsciiEncoding") { Source = HexSettings.Instance });
				}
				else {
					useDefault_AsciiEncoding = false;
					HexBox.AsciiEncoding = value.Value;
				}
			}
		}
		bool useDefault_AsciiEncoding;

		public void SelectAndMoveCaret(ulong fileOffset, ulong length) {
			ulong end = length == 0 ? fileOffset : fileOffset + length - 1 < fileOffset ? ulong.MaxValue : fileOffset + length - 1;
			if (length == 0)
				HexBox.Selection = null;
			else
				HexBox.Selection = new HexSelection(end, fileOffset);
			SetCaretPositionAndMakeVisible(fileOffset, end, true);
		}

		public void SetCaretPositionAndMakeVisible(ulong start, ulong end, bool resetKindPos = false) {
			// Make sure end address is also visible
			var kindPos = HexBox.CaretPosition.KindPosition;
			if (resetKindPos) {
				if (HexBox.CaretPosition.Kind != HexBoxPositionKind.HexByte)
					kindPos = 0;
				else
					kindPos = start <= end ? HexBoxPosition.INDEX_HEXBYTE_FIRST : HexBoxPosition.INDEX_HEXBYTE_LAST;
			}
			HexBox.CaretPosition = new HexBoxPosition(end, HexBox.CaretPosition.Kind, kindPos);
			HexBox.CaretPosition = new HexBoxPosition(start, HexBox.CaretPosition.Kind, kindPos);
		}
	}
}

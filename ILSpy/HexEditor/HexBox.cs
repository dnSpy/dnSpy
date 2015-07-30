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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.ILSpy.dntheme;

namespace dnSpy.HexEditor {
	/// <summary>
	/// Hex editor. Should only be used with a monospaced font! Variable sized fonts are not supported.
	/// It only uses ASCII characters 0x20-0x7E so those characters must have the same width and height.
	/// </summary>
	public sealed class HexBox : Control, IScrollInfo {
		TextFormatter textFormatter;
		ulong topOffset;
		readonly SelectionLayer selectionLayer;
		readonly HexLineLayer hexLineLayer;
		readonly HexCaret hexCaret;
		List<IHexLayer> layers = new List<IHexLayer>();
		readonly Canvas bgCanvas;

		List<HexLine> hexLines = new List<HexLine>();
		Dictionary<ulong, HexLine> offsetToLine = new Dictionary<ulong, HexLine>();

		public static readonly DependencyProperty DocumentProperty =
			DependencyProperty.Register("Document", typeof(HexDocument), typeof(HexBox),
			new FrameworkPropertyMetadata(null, OnDocumentChanged));
		public static readonly DependencyProperty StartOffsetProperty =
			DependencyProperty.Register("StartOffset", typeof(ulong), typeof(HexBox),
			new FrameworkPropertyMetadata(0UL, OnStartOffsetChanged));
		public static readonly DependencyProperty EndOffsetProperty =
			DependencyProperty.Register("EndOffset", typeof(ulong), typeof(HexBox),
			new FrameworkPropertyMetadata(0UL, OnEndOffsetChanged));
		public static readonly DependencyProperty BytesGroupCountProperty =
			DependencyProperty.Register("BytesGroupCount", typeof(int), typeof(HexBox),
			new FrameworkPropertyMetadata(8, OnBytesGroupCountChanged));
		public static readonly DependencyProperty BytesPerLineProperty =
			DependencyProperty.Register("BytesPerLine", typeof(int), typeof(HexBox),
			new FrameworkPropertyMetadata(0, OnBytesPerLineChanged));
		public static readonly DependencyProperty HexOffsetSizeProperty =
			DependencyProperty.Register("HexOffsetSize", typeof(int), typeof(HexBox),
			new FrameworkPropertyMetadata(0, OnHexOffsetSizeChanged));
		public static readonly DependencyProperty UseRelativeOffsetsProperty =
			DependencyProperty.Register("UseRelativeOffsets", typeof(bool), typeof(HexBox),
			new FrameworkPropertyMetadata(false, OnUseRelativeOffsetsChanged));
		public static readonly DependencyProperty UseHexPrefixProperty =
			DependencyProperty.Register("UseHexPrefix", typeof(bool), typeof(HexBox),
			new FrameworkPropertyMetadata(false, OnUseHexPrefixChanged));
		public static readonly DependencyProperty PrintAsciiProperty =
			DependencyProperty.Register("PrintAscii", typeof(bool), typeof(HexBox),
			new FrameworkPropertyMetadata(true, OnPrintAsciiChanged));
		public static readonly DependencyProperty BaseOffsetProperty =
			DependencyProperty.Register("BaseOffset", typeof(ulong), typeof(HexBox),
			new FrameworkPropertyMetadata(0UL, OnBaseOffsetChanged));
		public static readonly DependencyProperty CaretForegroundProperty =
			DependencyProperty.Register("CaretForeground", typeof(Brush), typeof(HexBox),
			new FrameworkPropertyMetadata(Brushes.Black));
		public static readonly DependencyProperty SelectionBackgroundProperty =
			DependencyProperty.Register("SelectionBackground", typeof(Brush), typeof(HexBox),
			new FrameworkPropertyMetadata(Brushes.Blue));

		public HexDocument Document {
			get { return (HexDocument)GetValue(DocumentProperty); }
			set { SetValue(DocumentProperty, value); }
		}

		public ulong StartOffset {
			get { return (ulong)GetValue(StartOffsetProperty); }
			set { SetValue(StartOffsetProperty, value); }
		}

		public ulong EndOffset {
			get { return (ulong)GetValue(EndOffsetProperty); }
			set { SetValue(EndOffsetProperty, value); }
		}

		public int BytesGroupCount {
			get { return (int)GetValue(BytesGroupCountProperty); }
			set { SetValue(BytesGroupCountProperty, value); }
		}

		public int BytesPerLine {
			get { return (int)GetValue(BytesPerLineProperty); }
			set { SetValue(BytesPerLineProperty, value); }
		}

		public int HexOffsetSize {
			get { return (int)GetValue(HexOffsetSizeProperty); }
			set { SetValue(HexOffsetSizeProperty, value); }
		}

		public bool UseRelativeOffsets {
			get { return (bool)GetValue(UseRelativeOffsetsProperty); }
			set { SetValue(UseRelativeOffsetsProperty, value); }
		}

		public bool UseHexPrefix {
			get { return (bool)GetValue(UseHexPrefixProperty); }
			set { SetValue(UseHexPrefixProperty, value); }
		}

		public bool PrintAscii {
			get { return (bool)GetValue(PrintAsciiProperty); }
			set { SetValue(PrintAsciiProperty, value); }
		}

		public ulong BaseOffset {
			get { return (ulong)GetValue(BaseOffsetProperty); }
			set { SetValue(BaseOffsetProperty, value); }
		}

		public Brush CaretForeground {
			get { return (Brush)GetValue(CaretForegroundProperty); }
			set { SetValue(CaretForegroundProperty, value); }
		}

		public Brush SelectionBackground {
			get { return (Brush)GetValue(SelectionBackgroundProperty); }
			set { SetValue(SelectionBackgroundProperty, value); }
		}

		static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var self = (HexBox)d;
			//TODO: hook events, unhook events from old doc, invalidate layers, etc
			self.InvalidateCachedLinesAndRefresh();
		}

		static void OnStartOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var self = (HexBox)d;
			self.InitializeHexOffsetSizeData();
			self.SetTopOffset(self.topOffset);
			self.UpdateCaretOffset();
			self.UpdateSelection();
			self.InvalidateCachedLinesAndRefresh();
		}

		static void OnEndOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var self = (HexBox)d;
			self.InitializeHexOffsetSizeData();
			self.SetTopOffset(self.topOffset);
			self.UpdateCaretOffset();
			self.UpdateSelection();
			self.InvalidateCachedLinesAndRefresh();
		}

		static void OnBytesGroupCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var self = (HexBox)d;
			self.InvalidateCachedLinesAndRefresh();
		}

		static void OnBytesPerLineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var self = (HexBox)d;
			self.InitializeVisibleBytesPerLine(true);
		}

		static void OnHexOffsetSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var self = (HexBox)d;
			self.InitializeHexOffsetSizeData();
			self.InvalidateCachedLinesAndRefresh();
		}

		static void OnUseRelativeOffsetsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var self = (HexBox)d;
			self.InitializeHexOffsetSizeData();
			self.InvalidateCachedLinesAndRefresh();
		}

		static void OnUseHexPrefixChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var self = (HexBox)d;
			self.RepaintLayers();
			self.InvalidateCachedLinesAndRefresh();
		}

		static void OnPrintAsciiChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var self = (HexBox)d;
			if (!self.PrintAscii)
				self.SwitchCaretToHexColumn();
			self.RepaintLayers();
			self.InvalidateCachedLinesAndRefresh();
		}

		static void OnBaseOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var self = (HexBox)d;
			self.InitializeHexOffsetSizeData();
			self.InvalidateCachedLinesAndRefresh();
		}

		public HexBox() {
			this.bgCanvas = new Canvas();
			AddVisualChild(this.bgCanvas);
			Add(this.selectionLayer = new SelectionLayer(this));
			Add(this.hexLineLayer = new HexLineLayer());
			Add(this.hexCaret = new HexCaret());

			this.selectionLayer.SetBinding(BackgroundProperty, new Binding("SelectionBackground") { Source = this });
			this.hexCaret.SetBinding(ForegroundProperty, new Binding("CaretForeground") { Source = this });

			// Since we don't use a ControlTemplate, the Background property isn't used by WPF. Use
			// a Canvas to show the background color.
			this.bgCanvas.SetBinding(Panel.BackgroundProperty, new Binding("Background") { Source = this });
			this.FocusVisualStyle = null;

			InitializeHexOffsetSizeData();
			this.Loaded += HexBox_Loaded;

			Add(EditingCommands.MoveDownByLine, ModifierKeys.None, Key.Down, (s, e) => UnselectText(() => MoveCaretDown()));
			Add(EditingCommands.MoveUpByLine, ModifierKeys.None, Key.Up, (s, e) => UnselectText(() => MoveCaretUp()));
			Add(EditingCommands.MoveDownByPage, ModifierKeys.None, Key.PageDown, (s, e) => UnselectText(() => MoveCaretPageDown()));
			Add(EditingCommands.MoveUpByPage, ModifierKeys.None, Key.PageUp, (s, e) => UnselectText(() => MoveCaretPageUp()));
			Add(EditingCommands.MoveLeftByCharacter, ModifierKeys.None, Key.Left, (s, e) => UnselectText(() => MoveCaretLeft()));
			Add(EditingCommands.MoveRightByCharacter, ModifierKeys.None, Key.Right, (s, e) => UnselectText(() => MoveCaretRight()));
			Add(EditingCommands.MoveLeftByWord, ModifierKeys.Control, Key.Left, (s, e) => UnselectText(() => MoveCaretLeftWord()));
			Add(EditingCommands.MoveRightByWord, ModifierKeys.Control, Key.Right, (s, e) => UnselectText(() => MoveCaretRightWord()));
			Add(EditingCommands.MoveToDocumentStart, ModifierKeys.Control, Key.Home, (s, e) => UnselectText(() => MoveCaretToStart()));
			Add(EditingCommands.MoveToDocumentEnd, ModifierKeys.Control, Key.End, (s, e) => UnselectText(() => MoveCaretToEnd()));
			Add(EditingCommands.MoveToLineStart, ModifierKeys.None, Key.Home, (s, e) => UnselectText(() => MoveCaretToLineStart()));
			Add(EditingCommands.MoveToLineEnd, ModifierKeys.None, Key.End, (s, e) => UnselectText(() => MoveCaretToLineEnd()));
			Add(MoveCaretToTopCommand, ModifierKeys.Control, Key.PageUp, (s, e) => UnselectText(() => MoveCaretToTop()));
			Add(MoveCaretToBottomCommand, ModifierKeys.Control, Key.PageDown, (s, e) => UnselectText(() => MoveCaretToBottom()));
			Add(ScrollMoveCaretUpCommand, ModifierKeys.Control, Key.Up, (s, e) => UnselectText(() => ScrollMoveCaretUp()));
			Add(ScrollMoveCaretDownCommand, ModifierKeys.Control, Key.Down, (s, e) => UnselectText(() => ScrollMoveCaretDown()));
			Add(SwitchCaretColumnCommand, ModifierKeys.None, Key.Tab, (s, e) => SwitchCaretColumn());
			Add(SwitchCaretToHexColumnCommand, ModifierKeys.Control | ModifierKeys.Shift, Key.H, (s, e) => SwitchCaretToHexColumn());
			Add(SwitchCaretToAsciiColumnCommand, ModifierKeys.Control | ModifierKeys.Shift, Key.A, (s, e) => SwitchCaretToAsciiColumn());

			Add(EditingCommands.SelectDownByLine, ModifierKeys.Shift, Key.Down, (s, e) => SelectText(() => MoveCaretDown()));
			Add(EditingCommands.SelectUpByLine, ModifierKeys.Shift, Key.Up, (s, e) => SelectText(() => MoveCaretUp()));
			Add(EditingCommands.SelectDownByPage, ModifierKeys.Shift, Key.PageDown, (s, e) => SelectText(() => MoveCaretPageDown()));
			Add(EditingCommands.SelectUpByPage, ModifierKeys.Shift, Key.PageUp, (s, e) => SelectText(() => MoveCaretPageUp()));
			Add(EditingCommands.SelectLeftByCharacter, ModifierKeys.Shift, Key.Left, (s, e) => SelectText(() => MoveCaretLeft()));
			Add(EditingCommands.SelectRightByCharacter, ModifierKeys.Shift, Key.Right, (s, e) => SelectText(() => MoveCaretRight()));
			Add(EditingCommands.SelectLeftByWord, ModifierKeys.Control | ModifierKeys.Shift, Key.Left, (s, e) => SelectText(() => MoveCaretLeftWord()));
			Add(EditingCommands.SelectRightByWord, ModifierKeys.Control | ModifierKeys.Shift, Key.Right, (s, e) => SelectText(() => MoveCaretRightWord()));
			Add(EditingCommands.SelectToDocumentStart, ModifierKeys.Control | ModifierKeys.Shift, Key.Home, (s, e) => SelectText(() => MoveCaretToStart()));
			Add(EditingCommands.SelectToDocumentEnd, ModifierKeys.Control | ModifierKeys.Shift, Key.End, (s, e) => SelectText(() => MoveCaretToEnd()));
			Add(EditingCommands.SelectToLineStart, ModifierKeys.Shift, Key.Home, (s, e) => SelectText(() => MoveCaretToLineStart()));
			Add(EditingCommands.SelectToLineEnd, ModifierKeys.Shift, Key.End, (s, e) => SelectText(() => MoveCaretToLineEnd()));
			Add(SelectCaretToTopCommand, ModifierKeys.Control | ModifierKeys.Shift, Key.PageUp, (s, e) => SelectText(() => MoveCaretToTop()));
			Add(SelectCaretToBottomCommand, ModifierKeys.Control | ModifierKeys.Shift, Key.PageDown, (s, e) => SelectText(() => MoveCaretToBottom()));
			Add(SelectScrollMoveCaretUpCommand, ModifierKeys.Control | ModifierKeys.Shift, Key.Up, (s, e) => SelectText(() => ScrollMoveCaretUp()));
			Add(SelectScrollMoveCaretDownCommand, ModifierKeys.Control | ModifierKeys.Shift, Key.Down, (s, e) => SelectText(() => ScrollMoveCaretDown()));
		}

		public static readonly RoutedUICommand MoveCaretToTopCommand = new RoutedUICommand("Move Caret To Top", "Move Caret To Top", typeof(HexBox));
		public static readonly RoutedUICommand MoveCaretToBottomCommand = new RoutedUICommand("Move Caret To Bottom", "Move Caret To Bottom", typeof(HexBox));
		public static readonly RoutedUICommand ScrollMoveCaretUpCommand = new RoutedUICommand("Scroll Move Caret Up", "Scroll Move Caret Up", typeof(HexBox));
		public static readonly RoutedUICommand ScrollMoveCaretDownCommand = new RoutedUICommand("Scroll Move Caret Down", "Scroll Move Caret Down", typeof(HexBox));
		public static readonly RoutedUICommand SelectCaretToTopCommand = new RoutedUICommand("Select Caret To Top", "Select Caret To Top", typeof(HexBox));
		public static readonly RoutedUICommand SelectCaretToBottomCommand = new RoutedUICommand("Select Caret To Bottom", "Select Caret To Bottom", typeof(HexBox));
		public static readonly RoutedUICommand SelectScrollMoveCaretUpCommand = new RoutedUICommand("Select Scroll Move Caret Up", "Select Scroll Move Caret Up", typeof(HexBox));
		public static readonly RoutedUICommand SelectScrollMoveCaretDownCommand = new RoutedUICommand("Select Scroll Move Caret Down", "Select Scroll Move Caret Down", typeof(HexBox));
		public static readonly RoutedUICommand SwitchCaretColumnCommand = new RoutedUICommand("Switch Caret Column", "Switch Caret Column", typeof(HexBox));
		public static readonly RoutedUICommand SwitchCaretToHexColumnCommand = new RoutedUICommand("Switch Caret To Hex Column", "Switch Caret To Hex Column", typeof(HexBox));
		public static readonly RoutedUICommand SwitchCaretToAsciiColumnCommand = new RoutedUICommand("Switch Caret To Ascii Column", "Switch Caret To Ascii Column", typeof(HexBox));

		void Add(ICommand command, ModifierKeys modifiers, Key key, ExecutedRoutedEventHandler del) {
			this.CommandBindings.Add(new CommandBinding(command, del));
			this.InputBindings.Add(new KeyBinding(command, key, modifiers));
		}

		bool InitializeHexOffsetSizeData() {
			int bits = GetCalculatedHexOffsetSize();
			bits = (bits + 3) / 4 * 4;
			int newNumOffsetNibbles = bits / 4;
			if (numOffsetNibbles == newNumOffsetNibbles)
				return false;
			numOffsetNibbles = newNumOffsetNibbles;
			offsetFormatString = string.Format("{{0}}{{1:X{0}}}", numOffsetNibbles);
			if (bits >= 64)
				offsetMask = ulong.MaxValue;
			else
				offsetMask = (1UL << bits) - 1;
			RepaintLayers();
			return true;
		}
		int numOffsetNibbles;
		string offsetFormatString;
		ulong offsetMask;

		int GetCalculatedHexOffsetSize() {
			int size = HexOffsetSize;
			if (1 <= size && size <= 64)
				return size;
			if (size > 64)
				return 64;

			ulong end = PhysicalToVisibleOffset(EndOffset);

			if (end <= byte.MaxValue) return 8;
			if (end <= ushort.MaxValue) return 16;
			if (end <= uint.MaxValue) return 32;
			return 64;
		}

		void InvalidateScrollInfo() {
			if (scrollOwner != null)
				scrollOwner.InvalidateScrollInfo();
		}

		void HexBox_Loaded(object sender, RoutedEventArgs e) {
			Loaded -= HexBox_Loaded;
			if (textFormatter == null)
				InitializeAll();
		}

		void InitializeAll() {
			textFormatter = TextFormatter.Create(TextOptions.GetTextFormattingMode(this));
			InitializeFontProperties();
			InitializeSizeProperties(false);
			RepaintLayers();
			InvalidateScrollInfo();
		}

		void InitializeSizeProperties(bool callInvalidateMethods) {
			InitializeVisibleBytesPerLine(callInvalidateMethods);
		}

		void InitializeFontProperties() {
			var textRunProps = CreateHexTextRunProperties();
			var paraProps = CreateHexTextParagraphProperties(textRunProps);
			var hexLine = new HexLine(0, "O", new HexLinePart[1] { new HexLinePart(0, 1, textRunProps) });
			var hexLineTextSource = new HexLineTextSource(hexLine);
			using (var textLine = textFormatter.FormatLine(hexLineTextSource, 0, 10000, paraProps, null)) {
				characterWidth = textLine.Width;
				characterHeight = textLine.Height;
			}
		}
		double characterWidth;
		double characterHeight;

		void InitializeVisibleBytesPerLine(bool callInvalidateMethods) {
			InitializeVisibleBytesPerLine(ActualWidth, callInvalidateMethods);
		}

		bool InitializeVisibleBytesPerLine(double width, bool callInvalidateMethods) {
			const int MAX_BYTES_PER_LINE = 1024 * 4;
			int newValue = Math.Max(1, Math.Min(MAX_BYTES_PER_LINE, CalculateBytesPerLine(width)));
			if (newValue == visibleBytesPerLine)
				return false;

			visibleBytesPerLine = newValue;
			InvalidateCachedLines();
			RepaintLayers();
			if (callInvalidateMethods)
				InvalidateMeasure();

			return true;
		}
		int visibleBytesPerLine;

		public int VisibleBytesPerLine {
			get { return visibleBytesPerLine; }
		}

		int CalculateBytesPerLine(double width) {
			if (BytesPerLine > 0)
				return BytesPerLine;
			if (characterWidth == 0)
				return 0;

			int numCharsPerLine = (int)(width / characterWidth);

			int num = (UseHexPrefix ? 2/*0x*/ : 0) + numOffsetNibbles/*offset*/;
			if (PrintAscii)
				num++;	// space between hex + ASCII
			int charsLeft = numCharsPerLine - num;
			int bytes = charsLeft / (1/*space*/ + 2/*hex*/ + (PrintAscii ? 1/*ASCII char*/ : 0));
			return Math.Max(1, bytes);
		}

		ulong CalculateCharactersPerLine() {
			return (ulong)(UseHexPrefix ? 2/*0x*/ : 0) +
				(ulong)numOffsetNibbles/*offset*/ +
				(ulong)visibleBytesPerLine * 3/*space + hex*/ +
				(ulong)(PrintAscii ? 1/*space between hex + ASCII*/ : 0) +
				(ulong)(PrintAscii ? visibleBytesPerLine/*ASCII*/ : 0);
		}

		internal ulong GetHexByteColumnIndex() {
			return (ulong)(UseHexPrefix ? 2/*0x*/ : 0) +
				(ulong)numOffsetNibbles/*offset*/;
		}

		internal ulong GetAsciiColumnIndex() {
			return (ulong)(UseHexPrefix ? 2/*0x*/ : 0) +
				(ulong)numOffsetNibbles/*offset*/ +
				(ulong)visibleBytesPerLine * 3/*space + hex*/ +
				(ulong)(PrintAscii ? 1/*space between hex + ASCII*/ : 0);
		}

		ulong? TryGetHexByteLineIndex(ulong col, out int hexByteCharIndex) {
			ulong start = GetHexByteColumnIndex();
			ulong end = NumberUtils.SubUInt64(NumberUtils.AddUInt64(start, NumberUtils.MulUInt64((ulong)visibleBytesPerLine, 3)), 1);
			if (start <= col && col <= end) {
				ulong diff = col - start;
				hexByteCharIndex = (diff % 3) <= 1 ? 0 : 1;
				return diff / 3;
			}
			hexByteCharIndex = 0;
			return null;
		}

		ulong? TryGetAsciiLineIndex(ulong col) {
			ulong start = GetAsciiColumnIndex();
			ulong end = NumberUtils.AddUInt64(start, (ulong)(visibleBytesPerLine - 1));
			if (start <= col && col <= end)
				return col - start;
			// Check if the user clicked the space character before the ASCII text
			return start != 0 && start - 1 == col ? 0 : (ulong?)null;
		}

		int ToColumn(HexBoxPosition position) {
			int byteIndex = (int)GetLineByteIndex(position.Offset);
			switch (position.Kind) {
			case HexBoxPositionKind.Ascii:
				return (int)GetAsciiColumnIndex() + byteIndex;

			case HexBoxPositionKind.HexByte:
				return (int)GetHexByteColumnIndex() + byteIndex * 3 + position.KindPosition + 1;

			default: throw new InvalidOperationException();
			}
		}

		void Add(IHexLayer layer) {
			int i;
			for (i = 0; i < layers.Count; i++) {
				if (layer.Order < layers[i].Order)
					break;
			}
			layers.Insert(i, layer);
			AddVisualChild((Visual)layer);
		}

		ulong PhysicalToVisibleOffset(ulong offset) {
			return BaseOffset + (UseRelativeOffsets ? offset - StartOffset : offset);
		}

		HexTextRunProperties CreateHexTextRunProperties() {
			var textRunProps = new HexTextRunProperties();
			textRunProps._Typeface = new Typeface((FontFamily)GetValue(FontFamilyProperty),
								(FontStyle)GetValue(FontStyleProperty),
								(FontWeight)GetValue(FontWeightProperty),
								(FontStretch)GetValue(FontStretchProperty));
			textRunProps._FontRenderingEmSize = (double)GetValue(FontSizeProperty);
			textRunProps._FontHintingEmSize = (double)GetValue(FontSizeProperty);
			textRunProps._ForegroundBrush = (Brush)GetValue(ForegroundProperty);
			textRunProps._CultureInfo = CultureInfo.CurrentUICulture;
			return textRunProps;
		}

		HexTextParagraphProperties CreateHexTextParagraphProperties(TextRunProperties textRunProps) {
			var paraProps = new HexTextParagraphProperties();
			paraProps._DefaultTextRunProperties = textRunProps;
			paraProps._TextWrapping = TextWrapping.NoWrap;
			return paraProps;
		}

		void InvalidateCachedLinesAndRefresh() {
			InvalidateCachedLines();
			RepaintLayers();
			InvalidateMeasure();
		}

		void InvalidateCachedLines() {
			offsetToLine.Clear();
			// Don't call InvalidateMeasure(). Callers should do it if necessary
		}

		protected override Size MeasureOverride(Size constraint) {
			if (textFormatter == null)
				InitializeAll();

			bool invalidateScrollInfo = false;
			if (textFormatter != null) {
				if (BytesPerLine <= 0)
					invalidateScrollInfo |= InitializeVisibleBytesPerLine(constraint.Width, false);
				InitializeHexLines(constraint.Width, constraint.Height);
			}
			else
				hexLines.Clear();
			hexLineLayer.Initialize(hexLines);
			InitializeCaret(hexCaret.Position);

			for (int i = 0; i < VisualChildrenCount; i++)
				((UIElement)this.GetVisualChild(i)).Measure(constraint);

			double maxWidth = hexLines.Count == 0 ? 0 : hexLines.Max(a => a.Width);
			double maxHeight;
			if (double.IsInfinity(constraint.Height))
				maxHeight = hexLines.Count == 0 ? 0 : hexLines.Sum(a => a.Height);
			else
				maxHeight = constraint.Height;
			var newSize = new Size(Math.Min(constraint.Width, maxWidth), Math.Min(constraint.Height, maxHeight));

			invalidateScrollInfo |= SetViewPort(constraint.Height, constraint.Width);
			if (invalidateScrollInfo)
				InvalidateScrollInfo();

			return newSize;
		}

		void InitializeCaret(HexBoxPosition position) {
			double horizPos = horizCol * characterWidth;
			if (!IsCaretVisible(position)) {
				hexCaret.SetCaret(position, horizPos, null, null);
				return;
			}
			int index = (int)((position.Offset - topOffset) / (ulong)visibleBytesPerLine);
			if (index < 0 || index >= hexLines.Count) {
				hexCaret.SetCaret(position, horizPos, null, null);
				return;
			}
			double y = index * characterHeight;
			var hexLine = hexLines[index];
			ulong byteIndex = position.Offset - hexLine.Offset;
			Rect? rectHex = GetCharacterRect(hexLine, y, (int)(GetHexByteColumnIndex() + byteIndex * 3 + 1 + position.KindPosition));
			Rect? rectAsc = GetCharacterRect(hexLine, y, (int)(GetAsciiColumnIndex() + byteIndex));
			if (!PrintAscii)
				rectAsc = null;
			if (rectHex != null && position.Kind != HexBoxPositionKind.HexByte)
				rectHex = new Rect(rectHex.Value.X, rectHex.Value.Y, rectHex.Value.Width * 2, rectHex.Value.Height);
			hexCaret.SetCaret(position, horizPos, rectHex, rectAsc);
		}

		Rect GetCharacterRect(HexLine hexLine, double y, int column) {
			return new Rect(column * characterWidth, y, characterWidth, characterHeight);
		}

		bool IsCaretVisible(HexBoxPosition position) {
			if (hexLines.Count == 0)
				return false;
			return hexLines[0].Offset <= position.Offset && position.Offset <= NumberUtils.AddUInt64(hexLines[hexLines.Count - 1].Offset, (ulong)(visibleBytesPerLine - 1));
		}

		HexBoxPosition? GetPositionFrom(Point pos) {
			if (pos.X < 0 || pos.Y < 0 || pos.X >= ActualWidth || pos.Y >= ActualHeight)
				return null;
			if (characterHeight == 0 || characterWidth == 0)
				return null;

			ulong index = (ulong)(pos.Y / characterHeight);
			ulong col = NumberUtils.AddUInt64((ulong)(pos.X / characterWidth), (ulong)horizCol);
			ulong lineOffset = NumberUtils.AddUInt64(topOffset, NumberUtils.MulUInt64(index, (ulong)visibleBytesPerLine));

			int hexByteCharIndex;
			ulong? hexByteIndex = TryGetHexByteLineIndex(col, out hexByteCharIndex);
			if (hexByteIndex != null)
				return HexBoxPosition.CreateByte(NumberUtils.AddUInt64(lineOffset, hexByteIndex.Value), hexByteCharIndex);

			ulong? asciiIndex = TryGetAsciiLineIndex(col);
			if (asciiIndex != null)
				return HexBoxPosition.CreateAscii(NumberUtils.AddUInt64(lineOffset, asciiIndex.Value));

			return null;
        }

		void InitializeHexLines(double width, double height) {
			var textRunProps = CreateHexTextRunProperties();
			var paraProps = CreateHexTextParagraphProperties(textRunProps);

			hexLines.Clear();
			ulong offs = topOffset;
			var parts = new List<HexLinePart>();
			var sb = new StringBuilder(2/*0x*/ + 16/*offset: up to 16 hex chars*/ + visibleBytesPerLine * 3/*space + 2 hex chars*/ + 1/*space*/ + visibleBytesPerLine/*ASCII chars*/);
			var sb2 = new StringBuilder(visibleBytesPerLine);
			var bytesAry = new short[visibleBytesPerLine];
			double y = 0;
			while (offs <= EndOffset && y < height) {
				var hexLine = GetHexLine(offs, parts, textRunProps, paraProps, sb, sb2, width, bytesAry);

				hexLines.Add(hexLine);
				sb.Clear();

				var nextOffs = offs + (ulong)visibleBytesPerLine;
				if (nextOffs < offs)
					break;
				offs = nextOffs;
				y += hexLine.Height;
			}
			foreach (var line in hexLines)
				offsetToLine.Remove(line.Offset);
			foreach (var line in offsetToLine.Values)
				line.Dispose();
			offsetToLine.Clear();
			foreach (var line in hexLines)
				offsetToLine.Add(line.Offset, line);
		}

		HexLine GetHexLine(ulong offset, List<HexLinePart> parts, TextRunProperties textRunProps, TextParagraphProperties paraProps, StringBuilder sb, StringBuilder sb2, double width, short[] bytesAry) {
			HexLine hexLine;
			if (offsetToLine.TryGetValue(offset, out hexLine))
				return hexLine;

			parts.Clear();
			int bytes = (int)Math.Min(EndOffset - offset + 1, (ulong)visibleBytesPerLine);
			if (bytes == 0)
				bytes = visibleBytesPerLine;

			ulong visibleOffs = PhysicalToVisibleOffset(offset);
			Add(parts, sb, string.Format(offsetFormatString, UseHexPrefix ? "0x" : string.Empty, visibleOffs & offsetMask), textRunProps, ColorType.HexOffset);
			var doc = Document;
			for (int i = 0; i < bytes; i++, visibleOffs++) {
				int b = doc.ReadByte(offset + (ulong)i);
				bytesAry[i] = (short)b;

				// The space gets the same color for speed. Less color switching = faster rendering
				if (b < 0)
					Add(parts, sb, " ??", textRunProps, ColorType.HexByteError);
				else
					Add(parts, sb, string.Format(" {0:X2}", b), textRunProps, BytesGroupCount <= 0 || ((i / BytesGroupCount) & 1) == 0 ? ColorType.HexByte1 : ColorType.HexByte2);
			}

			if (visibleBytesPerLine != bytes)
				Add(parts, sb, new string(' ', (visibleBytesPerLine - bytes) * 3), textRunProps, ColorType.HexText);
			if (PrintAscii) {
				Add(parts, sb, " ", textRunProps, ColorType.HexAscii);
				for (int i = 0; i < bytes; i++) {
					int b = bytesAry[i];
					if (b >= 0x20 && b <= 0x7E)
						sb2.Append((char)b);
					else
						sb2.Append(b < 0 ? "?" : ".");
				}
				Add(parts, sb, sb2.ToString(), textRunProps, ColorType.HexAscii);
				sb2.Clear();
			}

			hexLine = new HexLine(offset, sb.ToString(), parts.ToArray());
			offsetToLine.Add(offset, hexLine);
			var hexLineTextSource = new HexLineTextSource(hexLine);
			var textLines = new List<TextLine>();
			int textOffset = 0;
			const int newLineLength = 1;
			TextLineBreak previousLineBreak = null;
			for (;;) {
				var textLine = textFormatter.FormatLine(hexLineTextSource, 0, width, paraProps, previousLineBreak);
				textLines.Add(textLine);
				textOffset += textLine.Length;

				if (textOffset >= hexLine.Length + newLineLength)
					break;
				previousLineBreak = textLine.GetTextLineBreak();
			}
			hexLine.TextLines = textLines.ToArray();

			return hexLine;
		}

		void Add(List<HexLinePart> parts, StringBuilder sb, string s, TextRunProperties textRunProperties, ColorType colorType) {
			//TODO: Use dep props instead of using Themes.

			var c = Themes.Theme.GetColor(colorType).TextInheritedColor;
			var trp = new HexTextRunProperties(textRunProperties);

			var fg = c.Foreground == null ? null : c.Foreground.GetBrush(null);
			if (fg != null)
				trp._ForegroundBrush = fg;

			var bg = c.Background == null ? null : c.Background.GetBrush(null);
			if (bg != null)
				trp._ForegroundBrush = bg;

			var fs = c.FontStyle;
			var fw = c.FontWeight;
			if (fs != null || fw != null)
				trp._Typeface = new Typeface(trp.Typeface.FontFamily, fs ?? FontStyles.Normal, fw ?? FontWeights.Normal, trp.Typeface.Stretch);

			// Merge if possible
			bool merged = false;
			if (parts.Count > 0) {
				var last = parts[parts.Count - 1];
				if (HexTextRunProperties.Equals(last.TextRunProperties, trp)) {
					last.Length += s.Length;
					merged = true;
				}
			}
			if (!merged)
				parts.Add(new HexLinePart(sb.Length, s.Length, trp));
			sb.Append(s);
		}

		bool SetViewPort(double height, double width) {
			var newViewportHeight = characterHeight == 0 ? 0 : height / characterHeight;
			var newViewportWidth = characterWidth == 0 ? 0 : width / characterWidth;
			bool invalidateScrollInfo = viewportHeight != newViewportHeight || viewportWidth != newViewportWidth;
			viewportHeight = newViewportHeight;
			viewportWidth = newViewportWidth;
			return invalidateScrollInfo;
		}

		protected override Size ArrangeOverride(Size arrangeBounds) {
			var size = new Rect(new Point(0, 0), arrangeBounds);
			for (int i = 0; i < VisualChildrenCount; i++)
				((UIElement)this.GetVisualChild(i)).Arrange(size);

			bool refresh = SetTopOffsetInternal(topOffset) | SetHorizontalColumnInternal(horizCol);
			if (refresh) {
				RepaintLayers();
				InvalidateMeasure();
			}

			return base.ArrangeOverride(arrangeBounds);
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
			base.OnPropertyChanged(e);
			if (e.Property == TextOptions.TextFormattingModeProperty ||
				e.Property == FontFamilyProperty ||
				e.Property == FontSizeProperty ||
				e.Property == FontStretchProperty ||
				e.Property == FontStyleProperty ||
				e.Property == FontWeightProperty ||
				e.Property == ForegroundProperty) {
				InvalidateCachedLines();
				InitializeAll();
				RepaintLayers();
				InvalidateMeasure();
			}
		}

		protected override int VisualChildrenCount {
			get { return layers.Count + 1; }
		}

		protected override Visual GetVisualChild(int index) {
			if (index == 0)
				return this.bgCanvas;
			return (Visual)layers[index - 1];
		}

		public bool CanVerticallyScroll {
			get { return canVerticallyScroll; }
			set {
				if (canVerticallyScroll != value) {
					canVerticallyScroll = value;
					RepaintLayers();
					InvalidateMeasure();
				}
			}
		}
		bool canVerticallyScroll;

		public bool CanHorizontallyScroll {
			get { return canHorizontallyScroll; }
			set {
				if (canHorizontallyScroll != value) {
					canHorizontallyScroll = value;
					RepaintLayers();
					InvalidateMeasure();
				}
			}
		}
		bool canHorizontallyScroll;

		public double ExtentWidth {
			get { return CalculateCharactersPerLine(); }
		}

		public double ExtentHeight {
			get { return Math.Ceiling(((double)EndOffset - (double)StartOffset + 1 + visibleBytesPerLine - 1) / (double)visibleBytesPerLine); }
		}

		public double ViewportWidth {
			get { return viewportWidth; }
		}
		double viewportWidth;

		public double ViewportHeight {
			get { return viewportHeight; }
		}
		double viewportHeight;

		public double HorizontalOffset {
			get { return horizCol; }
		}

		public double VerticalOffset {
			get { return visibleBytesPerLine == 0 ? 0 : (topOffset - StartOffset) / (ulong)visibleBytesPerLine; }
		}

		public ScrollViewer ScrollOwner {
			get { return scrollOwner; }
			set {
				Debug.Assert(value != null);
				scrollOwner = value;
			}
		}
		ScrollViewer scrollOwner;

		public int VisibleLinesPerPage {
			get { return (int)Math.Ceiling(ViewportHeight); }
		}

		int WholeLinesPerPage() {
			return (int)ViewportHeight;
		}

		int WholeLinesPerPageAtLeastOne() {
			int c = WholeLinesPerPage();
			return c == 0 ? 1 : c;
		}

		int VisibleCharactersPerLine() {
			return (int)ViewportWidth;
		}

		public void LineUp() {
			AddVertical(-1);
		}

		public void LineDown() {
			AddVertical(1);
		}

		public void LineLeft() {
			AddHorizontal(-1);
		}

		public void LineRight() {
			AddHorizontal(1);
		}

		public void PageUp() {
			AddVertical(-WholeLinesPerPageAtLeastOne());
		}

		public void PageDown() {
			AddVertical(WholeLinesPerPageAtLeastOne());
		}

		public void PageLeft() {
			AddHorizontal(-VisibleCharactersPerLine());
		}

		public void PageRight() {
			AddHorizontal(VisibleCharactersPerLine());
		}

		int GetScrollWheelLines() {
			if (!SystemParameters.IsMouseWheelPresent)
				return 1;
			return SystemParameters.WheelScrollLines;
		}

		public void MouseWheelUp() {
			AddVertical(-GetScrollWheelLines());
		}

		public void MouseWheelDown() {
			AddVertical(GetScrollWheelLines());
		}

		public void MouseWheelLeft() {
			AddHorizontal(-GetScrollWheelLines());
		}

		public void MouseWheelRight() {
			AddHorizontal(GetScrollWheelLines());
		}

		public void SetHorizontalOffset(double offset) {
			offset = Math.Ceiling(offset);
			int offs = (int)offset;
			if (offset < 0)
				offs = 0;
			else if (offset >= int.MaxValue)
				offs = int.MaxValue;
			SetHorizontalStartColumn(offs);
		}

		public void SetVerticalOffset(double offset) {
			offset = StartOffset + Math.Ceiling(offset) * visibleBytesPerLine;
			ulong offs = (ulong)offset;
			if (offset < 0)
				offs = 0;
			// Use '>=' since it gets rounded up when converted to a double
			else if (offset >= ulong.MaxValue)
				offs = ulong.MaxValue;
			SetTopOffset(offs);
		}

		public Rect MakeVisible(Visual visual, Rect rectangle) {
			if (visual == this)
				return Rect.Empty;

			return rectangle;//TODO:
		}

		void AddVertical(int lines) {
			var newTopOffset = topOffset + (ulong)(lines * visibleBytesPerLine);
			if (lines < 0 && newTopOffset > topOffset)
				newTopOffset = ulong.MinValue;
			else if (lines > 0 && newTopOffset < topOffset)
				newTopOffset = ulong.MaxValue;
			SetTopOffset(newTopOffset);
		}

		void AddHorizontal(long cols) {
			SetHorizontalStartColumn(horizCol + cols);
		}

		public void SetTopOffset(ulong newTopOffset) {
			if (SetTopOffsetInternal(newTopOffset)) {
				RepaintLayers();
				InvalidateMeasure();
				InvalidateScrollInfo();
			}
		}

		bool SetTopOffsetInternal(ulong newTopOffset) {
			ulong bpl = (ulong)(visibleBytesPerLine == 0 ? 1 : visibleBytesPerLine);

			bool ovfl = false;
			ulong a = (EndOffset - StartOffset) / bpl * bpl;
			ovfl |= a + bpl - 1 < a;
			a = NumberUtils.AddUInt64(a, bpl - 1);
			ovfl |= StartOffset + a < a;

			ulong end = NumberUtils.AddUInt64(StartOffset, a);
			ulong pageBytes = (ulong)WholeLinesPerPage() * (ulong)visibleBytesPerLine;
			ulong max = end > pageBytes + 1 ? end - pageBytes + 1 : 0;
			if (ovfl)
				max += (ulong)visibleBytesPerLine;
			if (newTopOffset > max)
				newTopOffset = max;
			if (newTopOffset < StartOffset)
				newTopOffset = StartOffset;
			newTopOffset -= (newTopOffset - StartOffset) % bpl;
			if (!canVerticallyScroll)
				newTopOffset = StartOffset;
			if (newTopOffset == topOffset)
				return false;

			topOffset = newTopOffset;
			return true;
		}

		ulong GetBottomOffset() {
			ulong bpl = (ulong)(visibleBytesPerLine == 0 ? 1 : visibleBytesPerLine);
			ulong bytesPerPage = bpl * NumberUtils.SubUInt64((ulong)WholeLinesPerPage(), 1);
			return NumberUtils.AddUInt64(topOffset, bytesPerPage);
		}

		void SetBottomOffset(ulong newBottomOffset) {
			ulong bpl = (ulong)(visibleBytesPerLine == 0 ? 1 : visibleBytesPerLine);
			ulong bytesPerPage = bpl * NumberUtils.SubUInt64((ulong)WholeLinesPerPage(), 1);
			ulong offset = NumberUtils.SubUInt64(newBottomOffset, bytesPerPage);
			SetTopOffset(offset);
		}

		public void SetHorizontalStartColumn(long newHorizCol) {
			if (SetHorizontalColumnInternal(newHorizCol)) {
				RepaintLayers();
				InvalidateArrange();
				InvalidateScrollInfo();
			}
		}

		void SetHorizontalEndColumn(long newHorizCol) {
			int cols = VisibleCharactersPerLine();
			newHorizCol = Math.Max(0, newHorizCol - (cols - 1));
			SetHorizontalStartColumn(newHorizCol);
		}

		bool SetHorizontalColumnInternal(long newHorizCol) {
			if (newHorizCol < 0)
				newHorizCol = 0;
			long max = Math.Max(0, (long)CalculateCharactersPerLine() - VisibleCharactersPerLine());
			if (max > int.MaxValue)
				max = int.MaxValue;
			if (newHorizCol >= max)
				newHorizCol = max;
			if (!canHorizontallyScroll)
				newHorizCol = 0;
			if (newHorizCol == horizCol)
				return false;

			horizCol = (int)newHorizCol;
			hexLineLayer.LineStart = new Vector(-horizCol * characterWidth, 0);
			InitializeCaret(hexCaret.Position);
			return true;
		}
		int horizCol;

		void UpdateCaretOffset() {
			SetCaretPosition(hexCaret.Position, false);
		}

		void UpdateCaretPosition() {
			InitializeCaret(hexCaret.Position);
		}

		public void MoveCaretLeft() {
			SetCaretPosition(MoveLeft(hexCaret.Position));
		}

		public void MoveCaretRight() {
			SetCaretPosition(MoveRight(hexCaret.Position));
		}

		public void MoveCaretUp() {
			SetCaretPosition(MoveUp(hexCaret.Position));
		}

		public void MoveCaretDown() {
			SetCaretPosition(MoveDown(hexCaret.Position));
		}

		public void MoveCaretPageUp() {
			PageUp();
			SetCaretPosition(MovePageUp(hexCaret.Position));
		}

		public void MoveCaretPageDown() {
			PageDown();
			SetCaretPosition(MovePageDown(hexCaret.Position));
		}

		public void MoveCaretToStart() {
			SetCaretPosition(MoveStart(hexCaret.Position));
		}

		public void MoveCaretToEnd() {
			SetCaretPosition(MoveEnd(hexCaret.Position));
		}

		public void MoveCaretToTop() {
			SetCaretPosition(MoveToTop(hexCaret.Position));
		}

		public void MoveCaretToBottom() {
			SetCaretPosition(MoveToBottom(hexCaret.Position));
		}

		public void MoveCaretLeftWord() {
			SetCaretPosition(MoveLeftWord(hexCaret.Position));
		}

		public void MoveCaretRightWord() {
			SetCaretPosition(MoveRightWord(hexCaret.Position));
		}

		public void MoveCaretToLineStart() {
			SetCaretPosition(MoveLineStart(hexCaret.Position));
		}

		public void MoveCaretToLineEnd() {
			SetCaretPosition(MoveLineEnd(hexCaret.Position));
		}

		public void SwitchCaretColumn() {
			if (hexCaret.Position.Kind == HexBoxPositionKind.HexByte)
				SwitchCaretToAsciiColumn();
			else
				SwitchCaretToHexColumn();
		}

		public void SwitchCaretToHexColumn() {
			if (hexCaret.Position.Kind == HexBoxPositionKind.HexByte)
				return;
			SetCaretPosition(new HexBoxPosition(hexCaret.Position.Offset, HexBoxPositionKind.HexByte, 0));
		}

		public void SwitchCaretToAsciiColumn() {
			if (!PrintAscii)
				return;
			if (hexCaret.Position.Kind == HexBoxPositionKind.Ascii)
				return;
			SetCaretPosition(new HexBoxPosition(hexCaret.Position.Offset, HexBoxPositionKind.Ascii, 0));
		}

		public void ScrollMoveCaretUp() {
			LineUp();
			SetCaretPosition(EnsureInCurrentView(hexCaret.Position));
		}

		public void ScrollMoveCaretDown() {
			LineDown();
			SetCaretPosition(EnsureInCurrentView(hexCaret.Position));
		}

		void SetCaretPosition(HexBoxPosition position, bool bringCaretIntoView = true) {
			if (position.Offset < StartOffset)
				position = MoveStart(position);
			else if (position.Offset > EndOffset)
				position = MoveEnd(position);
			if (!PrintAscii && position.Kind == HexBoxPositionKind.Ascii)
				position = new HexBoxPosition(position.Offset, HexBoxPositionKind.HexByte, 0);
			InitializeCaret(position);
			if (bringCaretIntoView)
				BringCaretIntoView();
		}

		HexBoxPosition MoveLeft(HexBoxPosition position) {
			switch (position.Kind) {
			case HexBoxPositionKind.Ascii:
				return HexBoxPosition.CreateAscii(NumberUtils.SubUInt64(position.Offset, 1));

			case HexBoxPositionKind.HexByte:
				if (position.KindPosition == HexBoxPosition.INDEX_HEXBYTE_FIRST) {
					if (position.Offset <= StartOffset)
						return HexBoxPosition.CreateByte(StartOffset, HexBoxPosition.INDEX_HEXBYTE_FIRST);
					return HexBoxPosition.CreateByte(NumberUtils.SubUInt64(position.Offset, 1), HexBoxPosition.INDEX_HEXBYTE_LAST);
				}
				return HexBoxPosition.CreateByte(position.Offset, position.KindPosition - 1);

			default: throw new InvalidOperationException();
			}
		}

		HexBoxPosition MoveRight(HexBoxPosition position) {
			switch (position.Kind) {
			case HexBoxPositionKind.Ascii:
				return HexBoxPosition.CreateAscii(NumberUtils.AddUInt64(position.Offset, 1));

			case HexBoxPositionKind.HexByte:
				if (position.KindPosition == HexBoxPosition.INDEX_HEXBYTE_LAST) {
					if (position.Offset >= EndOffset)
						return HexBoxPosition.CreateByte(EndOffset, HexBoxPosition.INDEX_HEXBYTE_LAST);
					return HexBoxPosition.CreateByte(NumberUtils.AddUInt64(position.Offset, 1), HexBoxPosition.INDEX_HEXBYTE_FIRST);
				}
				return HexBoxPosition.CreateByte(position.Offset, position.KindPosition + 1);

			default:
				throw new InvalidOperationException();
			}
		}

		HexBoxPosition MoveUp(HexBoxPosition position) {
			return SubPageBytes(position, 1);
		}

		HexBoxPosition MoveDown(HexBoxPosition position) {
			return AddPageBytes(position, 1);
		}

		HexBoxPosition MovePageUp(HexBoxPosition position) {
			return SubPageBytes(position, (ulong)WholeLinesPerPageAtLeastOne());
		}

		HexBoxPosition MovePageDown(HexBoxPosition position) {
			return AddPageBytes(position, (ulong)WholeLinesPerPageAtLeastOne());
		}

		HexBoxPosition SubPageBytes(HexBoxPosition position, ulong pages) {
			if (visibleBytesPerLine == 0)
				return position;
			ulong pageSize = (ulong)visibleBytesPerLine;
			ulong pageNo = position.Offset / pageSize;
			if (pageNo == 0)
				return position;
			if (pages > pageNo)
				pages = pageNo;
			ulong count = NumberUtils.MulUInt64(pages, pageSize);
			switch (position.Kind) {
			case HexBoxPositionKind.Ascii:
				return HexBoxPosition.CreateAscii(NumberUtils.SubUInt64(position.Offset, count));

			case HexBoxPositionKind.HexByte:
				return HexBoxPosition.CreateByte(NumberUtils.SubUInt64(position.Offset, count), position.KindPosition);

			default:
				throw new InvalidOperationException();
			}
		}

		HexBoxPosition AddPageBytes(HexBoxPosition position, ulong pages) {
			if (visibleBytesPerLine == 0)
				return position;
			ulong pageSize = (ulong)visibleBytesPerLine;
			ulong pageNo = position.Offset / pageSize;
			ulong lastPage = EndOffset / pageSize;
			ulong pagesLeftUntilLast = NumberUtils.SubUInt64(lastPage, pageNo);
			if (pagesLeftUntilLast == 0)
				return position;
			if (pages > pagesLeftUntilLast)
				pages = pagesLeftUntilLast;
			ulong count = NumberUtils.MulUInt64(pages, pageSize);
			switch (position.Kind) {
			case HexBoxPositionKind.Ascii:
				return HexBoxPosition.CreateAscii(NumberUtils.AddUInt64(position.Offset, count));

			case HexBoxPositionKind.HexByte:
				return HexBoxPosition.CreateByte(NumberUtils.AddUInt64(position.Offset, count), position.KindPosition);

			default:
				throw new InvalidOperationException();
			}
		}

		HexBoxPosition MoveStart(HexBoxPosition position) {
			switch (position.Kind) {
			case HexBoxPositionKind.Ascii:
				return HexBoxPosition.CreateAscii(StartOffset);

			case HexBoxPositionKind.HexByte:
				return HexBoxPosition.CreateByte(StartOffset, HexBoxPosition.INDEX_HEXBYTE_FIRST);

			default:
				throw new InvalidOperationException();
			}
		}

		HexBoxPosition MoveEnd(HexBoxPosition position) {
			switch (position.Kind) {
			case HexBoxPositionKind.Ascii:
				return HexBoxPosition.CreateAscii(EndOffset);

			case HexBoxPositionKind.HexByte:
				return HexBoxPosition.CreateByte(EndOffset, HexBoxPosition.INDEX_HEXBYTE_LAST);

			default:
				throw new InvalidOperationException();
			}
		}

		internal ulong GetLineByteIndex(ulong offset) {
			ulong bpl = (ulong)(visibleBytesPerLine == 0 ? 1 : visibleBytesPerLine);
			return (offset - StartOffset) % bpl;
		}

		HexBoxPosition MoveToTop(HexBoxPosition position) {
			ulong lineByteIndex = GetLineByteIndex(position.Offset);
			return new HexBoxPosition(NumberUtils.AddUInt64(topOffset, lineByteIndex), position.Kind, position.KindPosition);
		}

		HexBoxPosition MoveToBottom(HexBoxPosition position) {
			ulong lineByteIndex = GetLineByteIndex(position.Offset);
			ulong bpl = (ulong)(visibleBytesPerLine == 0 ? 1 : visibleBytesPerLine);
			int linesPerPage = WholeLinesPerPage();
			ulong end = linesPerPage == 0 ? topOffset : NumberUtils.AddUInt64(topOffset, bpl * (ulong)(linesPerPage - 1));
			return new HexBoxPosition(NumberUtils.AddUInt64(end, lineByteIndex), position.Kind, position.KindPosition);
		}

		HexBoxPosition MoveLeftWord(HexBoxPosition position) {
			switch (position.Kind) {
			case HexBoxPositionKind.Ascii:
				return HexBoxPosition.CreateAscii(NumberUtils.SubUInt64(position.Offset, 1));

			case HexBoxPositionKind.HexByte:
				return HexBoxPosition.CreateByte(NumberUtils.SubUInt64(position.Offset, 1), HexBoxPosition.INDEX_HEXBYTE_FIRST);

			default:
				throw new InvalidOperationException();
			}
		}

		HexBoxPosition MoveRightWord(HexBoxPosition position) {
			switch (position.Kind) {
			case HexBoxPositionKind.Ascii:
				return HexBoxPosition.CreateAscii(NumberUtils.AddUInt64(position.Offset, 1));

			case HexBoxPositionKind.HexByte:
				return HexBoxPosition.CreateByte(NumberUtils.AddUInt64(position.Offset, 1), HexBoxPosition.INDEX_HEXBYTE_FIRST);

			default:
				throw new InvalidOperationException();
			}
		}

		HexBoxPosition MoveLineStart(HexBoxPosition position) {
			ulong newOffset = position.Offset - GetLineByteIndex(position.Offset);
			switch (position.Kind) {
			case HexBoxPositionKind.Ascii:
				return HexBoxPosition.CreateAscii(newOffset);

			case HexBoxPositionKind.HexByte:
				return HexBoxPosition.CreateByte(newOffset, HexBoxPosition.INDEX_HEXBYTE_FIRST);

			default:
				throw new InvalidOperationException();
			}
		}

		HexBoxPosition MoveLineEnd(HexBoxPosition position) {
			ulong bpl = (ulong)(visibleBytesPerLine == 0 ? 1 : visibleBytesPerLine);
			ulong newOffset = NumberUtils.AddUInt64(position.Offset - GetLineByteIndex(position.Offset), NumberUtils.SubUInt64(bpl, 1));
			switch (position.Kind) {
			case HexBoxPositionKind.Ascii:
				return HexBoxPosition.CreateAscii(newOffset);

			case HexBoxPositionKind.HexByte:
				return HexBoxPosition.CreateByte(newOffset, HexBoxPosition.INDEX_HEXBYTE_LAST);

			default:
				throw new InvalidOperationException();
			}
		}

		void BringCaretIntoView() {
			BringIntoView(hexCaret.Position);
		}

		void BringIntoView(HexBoxPosition position) {
			if (position.Offset < topOffset)
				SetTopOffset(position.Offset);
			else {
				ulong bpl = (ulong)(visibleBytesPerLine == 0 ? 1 : visibleBytesPerLine);
				ulong bytesPerPage = NumberUtils.MulUInt64(bpl, (ulong)this.WholeLinesPerPage());
				ulong end = NumberUtils.AddUInt64(topOffset, NumberUtils.SubUInt64(bytesPerPage, 1));
				if (position.Offset > end)
					SetBottomOffset(position.Offset);
			}

			ScrollHorizontallyToCaret(position);
		}

		void ScrollHorizontallyToCaret(HexBoxPosition position) {
			int caretCol = ToColumn(position);
			if (caretCol < horizCol)
				SetHorizontalStartColumn(caretCol);
			else {
				int cols = VisibleCharactersPerLine();
				int end = horizCol + cols - 1;
				if (caretCol > end)
					SetHorizontalEndColumn(caretCol);
			}
		}

		HexBoxPosition EnsureInCurrentView(HexBoxPosition position) {
			ulong offset = position.Offset;
			if (offset < topOffset)
				offset = NumberUtils.AddUInt64(topOffset, GetLineByteIndex(offset));
			else {
				ulong end = NumberUtils.AddUInt64(GetBottomOffset(), GetLineByteIndex(offset));
				if (offset > end)
					offset = end;
			}

			// If it's a really long line, make sure the caret is visible first
			ScrollHorizontallyToCaret(position);

			return new HexBoxPosition(offset, position.Kind, position.KindPosition);
		}

		protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e) {
			base.OnGotKeyboardFocus(e);
			hexCaret.Visibility = Visibility.Visible;
		}

		protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e) {
			base.OnLostKeyboardFocus(e);
			hexCaret.Visibility = Visibility.Collapsed;
		}

		bool mouseCaptureActive;
		HexBoxPosition? mouseCaptureStartPos;

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
			var pos = GetPositionFrom(e);
			if (pos != null) {
				if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
					SelectText(() => SetCaretPosition(pos.Value));
				else {
					UnselectText(() => SetCaretPosition(pos.Value));

					if (CaptureMouse()) {
						mouseCaptureActive = true;
						mouseCaptureStartPos = pos;
					}
				}

				Focus();
				e.Handled = true;
				return;
			}

			base.OnMouseLeftButtonDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e) {
			HexBoxPosition? pos;
			if (mouseCaptureActive && (pos = GetPositionFrom(e)) != null) {
				UpdateCaptureMouseSelection(mouseCaptureStartPos, pos.Value);
				e.Handled = true;
				return;
			}

			base.OnMouseMove(e);
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) {
			var mouseCaptured = mouseCaptureActive;
			var origSel = mouseCaptureStartPos;
			mouseCaptureActive = false;
			mouseCaptureStartPos = null;

			if (mouseCaptured)
				ReleaseMouseCapture();
			HexBoxPosition? pos;
			if (mouseCaptured && (pos = GetPositionFrom(e)) != null) {
				UpdateCaptureMouseSelection(origSel, pos.Value);
				e.Handled = true;
				return;
			}

			base.OnMouseLeftButtonUp(e);
		}

		void UpdateCaptureMouseSelection(HexBoxPosition? origSel, HexBoxPosition position) {
			if (origSel == null)
				return;
			if (Selection != null || origSel.Value.Offset != position.Offset)
				Selection = new HexSelection(origSel.Value.Offset, position.Offset);
		}

		HexBoxPosition? GetPositionFrom(MouseEventArgs e) {
			return GetPositionFrom(e.GetPosition(this));
		}

		public HexBoxState State {
			get {
				var state = new HexBoxState();
				state.TopOffset = topOffset;
				state.Column = horizCol;
				state.StartOffset = StartOffset;
				state.EndOffset = EndOffset;
				state.CaretPosition = hexCaret.Position;
				state.Selection = Selection;
				return state;
			}
			set {
				StartOffset = value.StartOffset;
				EndOffset = value.EndOffset;
				SetTopOffset(value.TopOffset);
				SetHorizontalStartColumn(value.Column);
				SetCaretPosition(value.CaretPosition);
				Selection = value.Selection;
			}
		}

		public ulong TopOffset {
			get { return topOffset; }
			set { SetTopOffset(topOffset); }
		}

		public int LeftColumn {
			get { return horizCol; }
			set { SetHorizontalStartColumn(value); }
		}

		public double CharacterWidth {
			get { return characterWidth; }
		}

		public double CharacterHeight {
			get { return characterHeight; }
		}

		public HexBoxPosition CaretPosition {
			get { return hexCaret.Position; }
			set { SetCaretPosition(value); }
		}

		public HexSelection? Selection {
			get { return selection; }
			set {
				var newSel = value;
				if (newSel != null) {
					ulong start = newSel.Value.StartOffset;
					ulong end = newSel.Value.EndOffset;
					if (start < StartOffset)
						start = StartOffset;
					if (end > EndOffset)
						end = EndOffset;
					if (newSel.Value.From < newSel.Value.To)
						newSel = new HexSelection(start, end);
					else
						newSel = new HexSelection(end, start);
				}
				if (selection != newSel) {
					selection = newSel;
					RepaintSelection();
				}
			}
		}
		HexSelection? selection;

		void UpdateSelection() {
			// The prop will verify the selection and update it if necessary
			Selection = Selection;
		}

		void RepaintSelection() {
			selectionLayer.SelectionChanged();
		}

		void RepaintLayers() {
			RepaintSelection();
			UpdateCaretPosition();
		}

		void UnselectText(Action action) {
			action();
			Selection = null;
		}

		void SelectText(Action action) {
			var pos = CaretPosition;
			var oldSel = Selection;
			action();
			Debug.Assert(oldSel == Selection);
			if (CaretPosition == pos)
				return;
			if (Selection == null)
				Selection = new HexSelection(pos.Offset, CaretPosition.Offset);
			else
				Selection = new HexSelection(Selection.Value.From, CaretPosition.Offset);
		}
	}
}

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

		readonly List<HexLine> hexLines = new List<HexLine>();
		readonly Dictionary<ulong, HexLine> offsetToLine = new Dictionary<ulong, HexLine>();

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
		public static readonly DependencyProperty ShowAsciiProperty =
			DependencyProperty.Register("ShowAscii", typeof(bool), typeof(HexBox),
			new FrameworkPropertyMetadata(true, OnShowAsciiChanged));
		public static readonly DependencyProperty LowerCaseHexProperty =
			DependencyProperty.Register("LowerCaseHex", typeof(bool), typeof(HexBox),
			new FrameworkPropertyMetadata(false, OnLowerCaseHexChanged));
		public static readonly DependencyProperty BaseOffsetProperty =
			DependencyProperty.Register("BaseOffset", typeof(ulong), typeof(HexBox),
			new FrameworkPropertyMetadata(0UL, OnBaseOffsetChanged));
		public static readonly DependencyProperty OffsetForegroundProperty =
			DependencyProperty.Register("OffsetForeground", typeof(Brush), typeof(HexBox),
			new FrameworkPropertyMetadata(Brushes.Black, OnColorChanged));
		public static readonly DependencyProperty Byte0ForegroundProperty =
			DependencyProperty.Register("Byte0Foreground", typeof(Brush), typeof(HexBox),
			new FrameworkPropertyMetadata(Brushes.Black, OnColorChanged));
		public static readonly DependencyProperty Byte1ForegroundProperty =
			DependencyProperty.Register("Byte1Foreground", typeof(Brush), typeof(HexBox),
			new FrameworkPropertyMetadata(Brushes.Black, OnColorChanged));
		public static readonly DependencyProperty ByteErrorForegroundProperty =
			DependencyProperty.Register("ByteErrorForeground", typeof(Brush), typeof(HexBox),
			new FrameworkPropertyMetadata(Brushes.Black, OnColorChanged));
		public static readonly DependencyProperty AsciiForegroundProperty =
			DependencyProperty.Register("AsciiForeground", typeof(Brush), typeof(HexBox),
			new FrameworkPropertyMetadata(Brushes.Black, OnColorChanged));
		public static readonly DependencyProperty CaretForegroundProperty =
			DependencyProperty.Register("CaretForeground", typeof(Brush), typeof(HexBox),
			new FrameworkPropertyMetadata(Brushes.Black));
		public static readonly DependencyProperty InactiveCaretForegroundProperty =
			DependencyProperty.Register("InactiveCaretForeground", typeof(Brush), typeof(HexBox),
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

		public bool ShowAscii {
			get { return (bool)GetValue(ShowAsciiProperty); }
			set { SetValue(ShowAsciiProperty, value); }
		}

		public bool LowerCaseHex {
			get { return (bool)GetValue(LowerCaseHexProperty); }
			set { SetValue(LowerCaseHexProperty, value); }
		}

		public ulong BaseOffset {
			get { return (ulong)GetValue(BaseOffsetProperty); }
			set { SetValue(BaseOffsetProperty, value); }
		}

		public Brush OffsetForeground {
			get { return (Brush)GetValue(OffsetForegroundProperty); }
			set { SetValue(OffsetForegroundProperty, value); }
		}

		public Brush Byte0Foreground {
			get { return (Brush)GetValue(Byte0ForegroundProperty); }
			set { SetValue(Byte0ForegroundProperty, value); }
		}

		public Brush Byte1Foreground {
			get { return (Brush)GetValue(Byte1ForegroundProperty); }
			set { SetValue(Byte1ForegroundProperty, value); }
		}

		public Brush ByteErrorForeground {
			get { return (Brush)GetValue(ByteErrorForegroundProperty); }
			set { SetValue(ByteErrorForegroundProperty, value); }
		}

		public Brush AsciiForeground {
			get { return (Brush)GetValue(AsciiForegroundProperty); }
			set { SetValue(AsciiForegroundProperty, value); }
		}

		public Brush CaretForeground {
			get { return (Brush)GetValue(CaretForegroundProperty); }
			set { SetValue(CaretForegroundProperty, value); }
		}

		public Brush InactiveCaretForeground {
			get { return (Brush)GetValue(InactiveCaretForegroundProperty); }
			set { SetValue(InactiveCaretForegroundProperty, value); }
		}

		public Brush SelectionBackground {
			get { return (Brush)GetValue(SelectionBackgroundProperty); }
			set { SetValue(SelectionBackgroundProperty, value); }
		}

		static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var self = (HexBox)d;
			self.RemoveHooks((HexDocument)e.OldValue);
			self.AddHooks((HexDocument)e.NewValue);
		}

		void AddHooks(HexDocument doc) {
			if (doc == null)
				return;
			if (!IsLoaded)
				return;

			doc.OnDocumentModified += OnDocumentModified;
			InvalidateCachedLinesAndRefresh();
		}

		void RemoveHooks(HexDocument doc) {
			if (doc == null)
				return;

			doc.OnDocumentModified -= OnDocumentModified;
			InvalidateCachedLinesAndRefresh();
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

		static void OnShowAsciiChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var self = (HexBox)d;
			if (!self.ShowAscii)
				self.SwitchCaretToHexColumn();
			self.RepaintLayers();
			self.InvalidateCachedLinesAndRefresh();
		}

		static void OnLowerCaseHexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var self = (HexBox)d;
			self.InitializeHexOffsetSizeData(true);
			self.InvalidateCachedLinesAndRefresh();
		}

		static void OnBaseOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var self = (HexBox)d;
			self.InitializeHexOffsetSizeData();
			self.InvalidateCachedLinesAndRefresh();
		}

		static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var self = (HexBox)d;
			self.InvalidateCachedLinesAndRefresh();
		}

		public HexBox() {
			this.bgCanvas = new Canvas();
			AddVisualChild(this.bgCanvas);
			Add(this.selectionLayer = new SelectionLayer(this));
			Add(this.hexLineLayer = new HexLineLayer());
			Add(this.hexCaret = new HexCaret());
			this.hexCaret.Visibility = Visibility.Collapsed;

			this.selectionLayer.SetBinding(BackgroundProperty, new Binding("SelectionBackground") { Source = this });
			this.hexCaret.SetBinding(ForegroundProperty, new Binding("CaretForeground") { Source = this });
			this.hexCaret.SetBinding(HexCaret.InactiveCaretForegroundProperty, new Binding("InactiveCaretForeground") { Source = this });

			// Since we don't use a ControlTemplate, the Background property isn't used by WPF. Use
			// a Canvas to show the background color.
			this.bgCanvas.SetBinding(Panel.BackgroundProperty, new Binding("Background") { Source = this });
			this.FocusVisualStyle = null;

			InitializeHexOffsetSizeData();
			this.Loaded += HexBox_Loaded;
			this.Unloaded += HexBox_Unloaded;

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
			Add(ScrollMoveCaretUpCommand, ModifierKeys.Control, Key.Up, (s, e) => UnselectText(() => ScrollMoveCaretUp(), false));
			Add(ScrollMoveCaretDownCommand, ModifierKeys.Control, Key.Down, (s, e) => UnselectText(() => ScrollMoveCaretDown(), false));
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

			this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, (s, e) => Copy(), (s, e) => e.CanExecute = CanCopyToClipboard()));
			this.CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, (s, e) => SelectAll()));
			this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, (s, e) => Paste(), (s, e) => e.CanExecute = CanPaste()));
			this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, (s, e) => ClearBytes()));
			this.CommandBindings.Add(new CommandBinding(CopyHexStringCommand, (s, e) => CopyHexString(), (s, e) => e.CanExecute = CanCopyToClipboard()));
			Add(CopyUTF8StringCommand, ModifierKeys.Control | ModifierKeys.Shift, Key.D8, (s, e) => CopyUTF8String(), (s, e) => e.CanExecute = CanCopyToClipboard());
			Add(CopyUnicodeStringCommand, ModifierKeys.Control | ModifierKeys.Shift, Key.U, (s, e) => CopyUnicodeString(), (s, e) => e.CanExecute = CanCopyToClipboard());
			Add(CopyCSharpArrayCommand, ModifierKeys.Control | ModifierKeys.Shift, Key.P, (s, e) => CopyCSharpArray(), (s, e) => e.CanExecute = CanCopyToClipboard());
			Add(CopyVBArrayCommand, ModifierKeys.Control | ModifierKeys.Shift, Key.B, (s, e) => CopyVBArray(), (s, e) => e.CanExecute = CanCopyToClipboard());
			Add(CopyUIContentsCommand, ModifierKeys.Control | ModifierKeys.Shift, Key.C, (s, e) => CopyUIContents(), (s, e) => e.CanExecute = CanCopyToClipboard());
			Add(CopyOffsetCommand, ModifierKeys.Control | ModifierKeys.Alt, Key.O, (s, e) => CopyOffset());
			this.CommandBindings.Add(new CommandBinding(ToggleLowerCaseHexCommand, (s, e) => LowerCaseHex = !LowerCaseHex));
			this.CommandBindings.Add(new CommandBinding(LowerCaseHexCommand, (s, e) => LowerCaseHex = true));
			this.CommandBindings.Add(new CommandBinding(UpperCaseHexCommand, (s, e) => LowerCaseHex = false));
			Add(PasteUtf8Command, ModifierKeys.Control, Key.D8, (s, e) => PasteUtf8(), (s, e) => e.CanExecute = CanPasteUtf8());
			Add(PasteUnicodeCommand, ModifierKeys.Control, Key.U, (s, e) => PasteUnicode(), (s, e) => e.CanExecute = CanPasteUnicode());
		}

		bool CanCopyToClipboard() {
			return Selection != null && Document != null;
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
		public static readonly RoutedUICommand CopyHexStringCommand = new RoutedUICommand("Copy Hex String", "Copy Hex String", typeof(HexBox));
		public static readonly RoutedUICommand CopyUTF8StringCommand = new RoutedUICommand("Copy UTF8 String", "Copy UTF8 String", typeof(HexBox));
		public static readonly RoutedUICommand CopyUnicodeStringCommand = new RoutedUICommand("Copy Unicode String", "Copy Unicode String", typeof(HexBox));
		public static readonly RoutedUICommand CopyCSharpArrayCommand = new RoutedUICommand("Copy C# Array", "Copy C# Array", typeof(HexBox));
		public static readonly RoutedUICommand CopyVBArrayCommand = new RoutedUICommand("Copy VB Array", "Copy VB Array", typeof(HexBox));
		public static readonly RoutedUICommand CopyUIContentsCommand = new RoutedUICommand("Copy UI Contents", "Copy UI Contents", typeof(HexBox));
		public static readonly RoutedUICommand CopyOffsetCommand = new RoutedUICommand("Copy Offset", "Copy Offset", typeof(HexBox));
		public static readonly RoutedUICommand ToggleLowerCaseHexCommand = new RoutedUICommand("Toggle Lower Case Hex", "Toggle Lower Case Hex", typeof(HexBox));
		public static readonly RoutedUICommand LowerCaseHexCommand = new RoutedUICommand("Lower Case Hex", "Lower Case Hex", typeof(HexBox));
		public static readonly RoutedUICommand UpperCaseHexCommand = new RoutedUICommand("Upper Case Hex", "Upper Case Hex", typeof(HexBox));
		public static readonly RoutedUICommand PasteUtf8Command = new RoutedUICommand("Paste UTF-8", "Paste UTF-8", typeof(HexBox));
		public static readonly RoutedUICommand PasteUnicodeCommand = new RoutedUICommand("Paste Unicode", "Paste Unicode", typeof(HexBox));

		void Add(ICommand command, ModifierKeys modifiers, Key key, ExecutedRoutedEventHandler exec, CanExecuteRoutedEventHandler canExec = null) {
			this.CommandBindings.Add(new CommandBinding(command, exec, canExec));
			this.InputBindings.Add(new KeyBinding(command, key, modifiers));
		}

		bool InitializeHexOffsetSizeData(bool force = false) {
			int bits = GetCalculatedHexOffsetSize();
			bits = (bits + 3) / 4 * 4;
			int newNumOffsetNibbles = bits / 4;
			if (!force && numOffsetNibbles == newNumOffsetNibbles)
				return false;
			numOffsetNibbles = newNumOffsetNibbles;
			offsetFormatString = string.Format(LowerCaseHex ? "{{0}}{{1:x{0}}}" : "{{0}}{{1:X{0}}}", numOffsetNibbles);
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
			AddHooks(Document);

			if (textFormatter == null)
				InitializeAll();
		}

		void HexBox_Unloaded(object sender, RoutedEventArgs e) {
			RemoveHooks(Document);
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
			var hexLine = new HexLine(0, 0, "O", new HexLinePart[1] { new HexLinePart(0, 1, textRunProps) });
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
			if (ShowAscii)
				num++;	// space between hex + ASCII
			int charsLeft = numCharsPerLine - num;
			int bytes = charsLeft / (1/*space*/ + 2/*hex*/ + (ShowAscii ? 1/*ASCII char*/ : 0));
			return Math.Max(1, bytes);
		}

		ulong CalculateCharactersPerLine() {
			return (ulong)(UseHexPrefix ? 2/*0x*/ : 0) +
				(ulong)numOffsetNibbles/*offset*/ +
				(ulong)visibleBytesPerLine * 3/*space + hex*/ +
				(ulong)(ShowAscii ? 1/*space between hex + ASCII*/ : 0) +
				(ulong)(ShowAscii ? visibleBytesPerLine/*ASCII*/ : 0);
		}

		internal ulong GetHexByteColumnIndex() {
			return (ulong)(UseHexPrefix ? 2/*0x*/ : 0) +
				(ulong)numOffsetNibbles/*offset*/;
		}

		internal ulong GetAsciiColumnIndex() {
			return (ulong)(UseHexPrefix ? 2/*0x*/ : 0) +
				(ulong)numOffsetNibbles/*offset*/ +
				(ulong)visibleBytesPerLine * 3/*space + hex*/ +
				(ulong)(ShowAscii ? 1/*space between hex + ASCII*/ : 0);
		}

		ulong? TryGetHexByteLineIndex(ulong col, double xpos, out int hexByteCharIndex) {
			ulong start = GetHexByteColumnIndex();
			ulong end = NumberUtils.SubUInt64(NumberUtils.AddUInt64(start, NumberUtils.MulUInt64((ulong)visibleBytesPerLine, 3)), 1);
			if (start <= col && col <= end) {
				ulong diff = col - start;
				bool isSpace = diff % 3 == 0;
				ulong byteIndex = diff / 3;
				if (isSpace && xpos < 0.5 && byteIndex != 0) {
					hexByteCharIndex = 1;
					return byteIndex - 1;
				}
				hexByteCharIndex = (diff % 3) <= 1 ? 0 : 1;
				return byteIndex;
			}
			if (col < start) {
				hexByteCharIndex = 0;
				return 0;
			}
			if (xpos < 0.5 && ShowAscii && GetAsciiColumnIndex() == col + 1) {
				hexByteCharIndex = 1;
				return (ulong)visibleBytesPerLine - 1;
			}
			hexByteCharIndex = 0;
			return null;
		}

		ulong? TryGetAsciiLineIndex(ulong col) {
			if (!ShowAscii)
				return null;
			ulong start = GetAsciiColumnIndex();
			ulong end = NumberUtils.AddUInt64(start, (ulong)(visibleBytesPerLine - 1));
			if (start <= col && col <= end)
				return col - start;
			if (col >= end)
				return end - start;
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

		public ulong PhysicalToVisibleOffset(ulong offset) {
			return offset - (UseRelativeOffsets ? StartOffset : 0) + BaseOffset;
		}

		public ulong VisibleToPhysicalOffset(ulong offset) {
			return offset - BaseOffset + (UseRelativeOffsets ? StartOffset : 0);
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
			InitializeCaret(CaretPosition);

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
			if (!IsCaretVisible(position) || visibleBytesPerLine == 0) {
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
			ulong byteIndex = position.Offset - hexLine.StartOffset;
			Rect? rectHex = GetCharacterRect(hexLine, y, (int)(GetHexByteColumnIndex() + byteIndex * 3 + 1 + position.KindPosition));
			Rect? rectAsc = GetCharacterRect(hexLine, y, (int)(GetAsciiColumnIndex() + byteIndex));
			if (!ShowAscii)
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
			return hexLines[0].StartOffset <= position.Offset && position.Offset <= NumberUtils.AddUInt64(hexLines[hexLines.Count - 1].StartOffset, (ulong)(visibleBytesPerLine - 1));
		}

		HexPositionUI GetDocumentPosition(Point pos) {
			if (characterHeight == 0 || characterWidth == 0)
				return new HexPositionUI(0, 0);

			ulong bpl = (ulong)(visibleBytesPerLine == 0 ? 1 : visibleBytesPerLine);
			ulong topLine = (topOffset - StartOffset) / bpl;

			long relRow = (long)(pos.Y / characterHeight);
			long relCol = (long)(pos.X / characterWidth + horizCol);

			ulong row;
			if (relRow < 0)
				row = NumberUtils.SubUInt64(topLine, (ulong)-relRow);
			else
				row = NumberUtils.AddUInt64(topLine, (ulong)relRow);

			ulong col = (ulong)Math.Max(0, relCol);

			return new HexPositionUI(col, row);
		}

		HexPositionUI GetTopDocumentPos() {
			return GetDocumentPosition(new Point(0, 0));
		}

		HexPositionUI GetBottomDocumentPos() {
			return GetDocumentPosition(new Point(Math.Max(0, (int)Math.Ceiling(ViewportWidth) - 1) * CharacterWidth, Math.Max(0, VisibleLinesPerPage - 1) * CharacterHeight));
		}

		HexPositionUI GetDocumentPositionFromMousePosition(Point pos) {
			if (characterHeight == 0 || characterWidth == 0)
				return new HexPositionUI(0, 0);

			var docPos = GetDocumentPosition(pos);
			var top = GetTopDocumentPos();
			var bottom = GetBottomDocumentPos();

			ulong x = docPos.X;
			ulong y = docPos.Y;
			if (x > bottom.X)
				x = NumberUtils.AddUInt64(bottom.X, 1);
			if (y > bottom.Y)
				y = NumberUtils.AddUInt64(bottom.Y, 1);
			if (x < top.X)
				x = NumberUtils.SubUInt64(top.X, 1);
			if (y < top.Y)
				y = NumberUtils.SubUInt64(top.Y, 1);

			return new HexPositionUI(x, y);
		}

		HexBoxPosition? GetPositionFrom(Point pos) {
			return GetPositionFrom(GetDocumentPositionFromMousePosition(pos), characterWidth == 0 ? 0 : (pos.X % characterWidth) / characterWidth);
		}

		ulong GetLineOffset(HexPositionUI docPos) {
			ulong bpl = (ulong)(visibleBytesPerLine == 0 ? 1 : visibleBytesPerLine);
			return NumberUtils.AddUInt64(StartOffset, NumberUtils.MulUInt64(docPos.Y, bpl));
		}

		HexBoxPosition? GetPositionFrom(HexPositionUI docPos, double xpos) {
			ulong lineOffset = GetLineOffset(docPos);

			int hexByteCharIndex;
			ulong? hexByteIndex = TryGetHexByteLineIndex(docPos.X, xpos, out hexByteCharIndex);
			if (hexByteIndex != null)
				return HexBoxPosition.CreateByte(NumberUtils.AddUInt64(lineOffset, hexByteIndex.Value), hexByteCharIndex);

			ulong? asciiIndex = TryGetAsciiLineIndex(docPos.X);
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
				offsetToLine.Remove(line.StartOffset);
			foreach (var line in offsetToLine.Values)
				line.Dispose();
			offsetToLine.Clear();
			foreach (var line in hexLines)
				offsetToLine.Add(line.StartOffset, line);
		}

		internal List<HexLine> CreateHexLines(ulong start, ulong end) {
			var lines = new List<HexLine>();

			var textRunProps = CreateHexTextRunProperties();

			ulong offs = start;
			var parts = new List<HexLinePart>();
			var sb = new StringBuilder(2/*0x*/ + 16/*offset: up to 16 hex chars*/ + visibleBytesPerLine * 3/*space + 2 hex chars*/ + 1/*space*/ + visibleBytesPerLine/*ASCII chars*/);
			var sb2 = new StringBuilder(visibleBytesPerLine);
			var bytesAry = new short[visibleBytesPerLine];
			while (offs <= end) {
				var hexLine = CreateHexLine(offs, end, parts, textRunProps, sb, sb2, bytesAry);

				lines.Add(hexLine);
				sb.Clear();

				offs = offs - GetLineByteIndex(offs);
				var nextOffs = offs + (ulong)visibleBytesPerLine;
				if (nextOffs < offs)
					break;
				offs = nextOffs;
			}

			return lines;
		}

		HexLine CreateHexLine(ulong offset, ulong end, List<HexLinePart> parts, TextRunProperties textRunProps, StringBuilder sb, StringBuilder sb2, short[] bytesAry) {
			parts.Clear();
			int skipBytes = (int)GetLineByteIndex(offset);
			int bytes = (int)Math.Min(end - offset + 1, (ulong)(visibleBytesPerLine - skipBytes));
			if (bytes == 0)
				bytes = visibleBytesPerLine - skipBytes;

			ulong visibleOffs = PhysicalToVisibleOffset(offset - (ulong)skipBytes);
			Add(parts, sb, string.Format(offsetFormatString, UseHexPrefix ? "0x" : string.Empty, visibleOffs & offsetMask), textRunProps, OffsetForeground);

			if (skipBytes > 0)
				Add(parts, sb, new string(' ', skipBytes * 3), textRunProps, Foreground);

			var doc = Document;
			for (int i = 0; i < bytes; i++) {
				int b = doc == null ? -1 : doc.ReadByte(offset + (ulong)i);
				bytesAry[i] = (short)b;

				// The space gets the same color for speed. Less color switching = faster rendering
				if (b < 0)
					Add(parts, sb, " ??", textRunProps, ByteErrorForeground);
				else
					Add(parts, sb, string.Format(LowerCaseHex ? " {0:x2}" : " {0:X2}", b), textRunProps, BytesGroupCount <= 0 || ((i / BytesGroupCount) & 1) == 0 ? Byte0Foreground : Byte1Foreground);
			}

			if (visibleBytesPerLine - skipBytes != bytes)
				Add(parts, sb, new string(' ', (visibleBytesPerLine - skipBytes - bytes) * 3), textRunProps, Foreground);
			if (ShowAscii) {
				if (skipBytes > 0)
					Add(parts, sb, new string(' ', skipBytes), textRunProps, AsciiForeground);
				Add(parts, sb, " ", textRunProps, AsciiForeground);
				for (int i = 0; i < bytes; i++) {
					int b = bytesAry[i];
					if (b >= 0x20 && b <= 0x7E)
						sb2.Append((char)b);
					else
						sb2.Append(b < 0 ? "?" : ".");
				}
				Add(parts, sb, sb2.ToString(), textRunProps, AsciiForeground);
				sb2.Clear();
			}

			return new HexLine(offset, offset + (ulong)(bytes == 0 ? 0 : bytes - 1), sb.ToString(), parts.ToArray());
		}

		HexLine GetHexLine(ulong offset, List<HexLinePart> parts, TextRunProperties textRunProps, TextParagraphProperties paraProps, StringBuilder sb, StringBuilder sb2, double width, short[] bytesAry) {
			HexLine hexLine;
			if (offsetToLine.TryGetValue(offset, out hexLine))
				return hexLine;

			hexLine = CreateHexLine(offset, EndOffset , parts, textRunProps, sb, sb2, bytesAry);
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

		void Add(List<HexLinePart> parts, StringBuilder sb, string s, TextRunProperties textRunProperties, Brush brush) {
			var trp = new HexTextRunProperties(textRunProperties);

			if (brush != null)
				trp._ForegroundBrush = brush;

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
				e.Property == FontWeightProperty) {
				InitializeAll();
				InvalidateCachedLinesAndRefresh();
			}
			else if (e.Property == ForegroundProperty)
				OnColorChanged(this, e);
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
		bool canVerticallyScroll = true;

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
		bool canHorizontallyScroll = true;

		public double ExtentWidth {
			get { return CalculateCharactersPerLine(); }
		}

		public double ExtentHeight {
			get { return EndOffset < StartOffset ? 0 : Math.Ceiling(((double)EndOffset - (double)StartOffset + 1 + visibleBytesPerLine - 1) / (double)visibleBytesPerLine); }
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

		int WholeCharactersPerLine() {
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
			AddHorizontal(-WholeCharactersPerLine());
		}

		public void PageRight() {
			AddHorizontal(WholeCharactersPerLine());
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
			ulong a = (EndOffset < StartOffset ? 0 : EndOffset - StartOffset) / bpl * bpl;
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
			int cols = WholeCharactersPerLine();
			newHorizCol = Math.Max(0, newHorizCol - (cols - 1));
			SetHorizontalStartColumn(newHorizCol);
		}

		bool SetHorizontalColumnInternal(long newHorizCol) {
			if (newHorizCol < 0)
				newHorizCol = 0;
			long max = Math.Max(0, (long)CalculateCharactersPerLine() - WholeCharactersPerLine());
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
			InitializeCaret(CaretPosition);
			return true;
		}
		int horizCol;

		void UpdateCaretOffset() {
			SetCaretPosition(CaretPosition, false);
		}

		void UpdateCaretPosition() {
			InitializeCaret(CaretPosition);
		}

		public void MoveCaretLeft() {
			SetCaretPosition(MoveLeft(CaretPosition));
		}

		public void MoveCaretRight() {
			SetCaretPosition(MoveRight(CaretPosition));
		}

		public void MoveCaretUp() {
			SetCaretPosition(MoveUp(CaretPosition));
		}

		public void MoveCaretDown() {
			SetCaretPosition(MoveDown(CaretPosition));
		}

		public void MoveCaretPageUp() {
			PageUp();
			SetCaretPosition(MovePageUp(CaretPosition));
		}

		public void MoveCaretPageDown() {
			PageDown();
			SetCaretPosition(MovePageDown(CaretPosition));
		}

		public void MoveCaretToStart() {
			SetCaretPosition(MoveStart(CaretPosition));
		}

		public void MoveCaretToEnd() {
			SetCaretPosition(MoveEnd(CaretPosition));
		}

		public void MoveCaretToTop() {
			SetCaretPosition(MoveToTop(CaretPosition));
		}

		public void MoveCaretToBottom() {
			SetCaretPosition(MoveToBottom(CaretPosition));
		}

		public void MoveCaretLeftWord() {
			SetCaretPosition(MoveLeftWord(CaretPosition));
		}

		public void MoveCaretRightWord() {
			SetCaretPosition(MoveRightWord(CaretPosition));
		}

		public void MoveCaretToLineStart() {
			SetCaretPosition(MoveLineStart(CaretPosition));
		}

		public void MoveCaretToLineEnd() {
			SetCaretPosition(MoveLineEnd(CaretPosition));
		}

		public void SwitchCaretColumn() {
			if (CaretPosition.Kind == HexBoxPositionKind.HexByte)
				SwitchCaretToAsciiColumn();
			else
				SwitchCaretToHexColumn();
		}

		public void SwitchCaretToHexColumn() {
			if (CaretPosition.Kind == HexBoxPositionKind.HexByte)
				return;
			SetCaretPosition(new HexBoxPosition(CaretPosition.Offset, HexBoxPositionKind.HexByte, 0));
		}

		public void SwitchCaretToAsciiColumn() {
			if (!ShowAscii)
				return;
			if (CaretPosition.Kind == HexBoxPositionKind.Ascii)
				return;
			SetCaretPosition(new HexBoxPosition(CaretPosition.Offset, HexBoxPositionKind.Ascii, 0));
		}

		public void ScrollMoveCaretUp() {
			LineUp();
			SetCaretPosition(EnsureInCurrentView(CaretPosition));
		}

		public void ScrollMoveCaretDown() {
			LineDown();
			SetCaretPosition(EnsureInCurrentView(CaretPosition));
		}

		void SetCaretPosition(HexBoxPosition position, bool bringCaretIntoView = true) {
			if (position.Offset < StartOffset)
				position = MoveStart(position);
			else if (position.Offset > EndOffset)
				position = MoveEnd(position);
			if (!ShowAscii && position.Kind == HexBoxPositionKind.Ascii)
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
			ulong pageNo = (position.Offset < StartOffset ? 0 : position.Offset - StartOffset) / pageSize;
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
			ulong pageNo = (position.Offset < StartOffset ? 0 : position.Offset - StartOffset) / pageSize;
			ulong lastPage = (EndOffset < StartOffset ? 0 : EndOffset - StartOffset) / pageSize;
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

		HexBoxPosition MoveToEndOfSelection(HexBoxPosition position) {
			var sel = Selection;
			if (sel == null)
				return position;

			switch (position.Kind) {
			case HexBoxPositionKind.Ascii:
				return HexBoxPosition.CreateAscii(sel.Value.To);

			case HexBoxPositionKind.HexByte:
				if (sel.Value.From <= sel.Value.To)
					return HexBoxPosition.CreateByte(sel.Value.To, HexBoxPosition.INDEX_HEXBYTE_LAST);
				return HexBoxPosition.CreateByte(sel.Value.To, HexBoxPosition.INDEX_HEXBYTE_FIRST);

			default:
				throw new InvalidOperationException();
			}
		}

		public Rect? GetCaretWindowRect() {
			switch (CaretPosition.Kind) {
			case HexBoxPositionKind.Ascii:
				return hexCaret.AsciiRect;

			case HexBoxPositionKind.HexByte:
				return hexCaret.HexRect;

			default:
				throw new InvalidOperationException();
			}
		}

		public void BringCaretIntoView() {
			BringIntoView(CaretPosition);
		}

		void BringIntoView(HexBoxPosition position) {
			if (position.Kind == HexBoxPositionKind.HexByte && position.KindPosition == HexBoxPosition.INDEX_HEXBYTE_FIRST) {
				// To make sure left scrolling with the mouse works, make sure that the space
				// character is visible. First make it visible, and then bring the new position into
				// view, just in case the size of the control is so small that two chars won't fit.
				ScrollHorizontallyToCaret(ToColumn(position) - 1);
				BringIntoView2(position);
			}
			else
				BringIntoView2(position);
		}

		void BringIntoView2(HexBoxPosition position) {
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
			ScrollHorizontallyToCaret(ToColumn(position));
		}

		void ScrollHorizontallyToCaret(int caretCol) {
			if (caretCol < horizCol)
				SetHorizontalStartColumn(caretCol);
			else {
				int cols = WholeCharactersPerLine();
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
		ulong? mouseCaptureStartPos;
		HexPositionUI mouseCaptureStartPosUI;

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
			var pos = GetPositionFrom(e);
			if (pos != null) {
				if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
					SelectText(() => SetCaretPosition(pos.Value));
				else
					UnselectText(() => SetCaretPosition(pos.Value));
				if (CaptureMouse()) {
					mouseCaptureActive = true;
					mouseCaptureStartPos = Selection == null ? pos.Value.Offset : Selection.Value.From;
					mouseCaptureStartPosUI = GetDocumentPositionFromMousePosition(e.GetPosition(this));
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
				UpdateCaptureMouseSelection(mouseCaptureStartPos, pos.Value, GetDocumentPositionFromMousePosition(e.GetPosition(this)));
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
				UpdateCaptureMouseSelection(origSel, pos.Value, GetDocumentPositionFromMousePosition(e.GetPosition(this)));
				e.Handled = true;
				return;
			}

			base.OnMouseLeftButtonUp(e);
		}

		void UpdateCaptureMouseSelection(ulong? startOffset, HexBoxPosition position, HexPositionUI uiPos) {
			SetCaretPosition(position);
			if (startOffset == null)
				return;
			if (Selection != null || uiPos != mouseCaptureStartPosUI)
				Selection = new HexSelection(startOffset.Value, position.Offset);
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
				state.CaretPosition = CaretPosition;
				state.Selection = Selection;
				return state;
			}
			set {
				StartOffset = value.StartOffset;
				EndOffset = value.EndOffset;
				SetTopOffset(value.TopOffset);
				SetHorizontalStartColumn(value.Column);
				SetCaretPosition(value.CaretPosition, false);
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
					else if (start > EndOffset)
						start = EndOffset;
					if (end < StartOffset)
						end = StartOffset;
					else if (end > EndOffset)
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

		void UnselectText(Action action, bool forceUnselect = true) {
			var oldPos = CaretPosition;
			action();
			if (forceUnselect || oldPos != CaretPosition)
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

		public void Copy() {
			CopyHexString();
		}

		public void CopyHexString() {
			if (CanCopyToClipboard())
				CopyHexString(Selection.Value.StartOffset, Selection.Value.EndOffset);
		}

		public void CopyUTF8String() {
			if (CanCopyToClipboard())
				CopyUTF8String(Selection.Value.StartOffset, Selection.Value.EndOffset);
		}

		public void CopyUnicodeString() {
			if (CanCopyToClipboard())
				CopyUnicodeString(Selection.Value.StartOffset, Selection.Value.EndOffset);
		}

		public void CopyCSharpArray() {
			if (CanCopyToClipboard())
				CopyCSharpArray(Selection.Value.StartOffset, Selection.Value.EndOffset);
		}

		public void CopyVBArray() {
			if (CanCopyToClipboard())
				CopyVBArray(Selection.Value.StartOffset, Selection.Value.EndOffset);
		}

		public void CopyUIContents() {
			if (CanCopyToClipboard())
				CopyUIContents(Selection.Value.StartOffset, Selection.Value.EndOffset);
		}

		public void CopyOffset() {
			CopyOffset(CaretPosition.Offset);
		}

		void CopyOffset(ulong offset) {
			ulong visibleOffs = PhysicalToVisibleOffset(offset);
			var s = string.Format(offsetFormatString, UseHexPrefix ? "0x" : string.Empty, visibleOffs & offsetMask);
			Clipboard.SetText(s);
		}

		void CopyHexString(ulong start, ulong end) {
			new HexStringFormatter(this, start, end, LowerCaseHex).CopyToClipboard();
		}

		void CopyUTF8String(ulong start, ulong end) {
			new UTF8StringFormatter(this, start, end).CopyToClipboard();
		}

		void CopyUnicodeString(ulong start, ulong end) {
			new UnicodeStringFormatter(this, start, end).CopyToClipboard();
		}

		void CopyCSharpArray(ulong start, ulong end) {
			new CSharpArrayFormatter(this, start, end, LowerCaseHex).CopyToClipboard();
		}

		void CopyVBArray(ulong start, ulong end) {
			new VBArrayFormatter(this, start, end, LowerCaseHex).CopyToClipboard();
		}

		void CopyUIContents(ulong start, ulong end) {
			new UILayoutFormatter(this, start, end).CopyToClipboard();
		}

		public void SelectAll() {
			Selection = new HexSelection(StartOffset, EndOffset);
			SetCaretPosition(MoveToEndOfSelection(CaretPosition));
		}

		public void MoveTo(ulong offset, bool bringCaretIntoView = true) {
			SetCaretPosition(new HexBoxPosition(offset, CaretPosition.Kind, CaretPosition.KindPosition), bringCaretIntoView);
		}

		void OnDocumentModified(object sender, HexDocumentModifiedEventArgs e) {
			if (InvalidateLines(e.StartOffset, e.EndOffset))
				InvalidateMeasure();
		}

		bool InvalidateLines(ulong startOffset, ulong endOffset) {
			int linesCount = offsetToLine.Count;
			foreach (var kv in offsetToLine.ToArray()) {
				var line = kv.Value;
				if (startOffset <= line.EndOffset && endOffset >= line.StartOffset) {
					bool b = offsetToLine.Remove(kv.Key);
					Debug.Assert(b);
				}
			}
			return linesCount != offsetToLine.Count;
		}

		public void PasteUtf8() {
			PasteUtf8(Clipboard.GetText());
		}

		public void PasteUtf8(string s) {
			if (!CanPasteUtf8(s))
				return;
			Paste(Encoding.UTF8.GetBytes(s));
		}

		public bool CanPasteUtf8() {
			return CanPasteUtf8(Clipboard.GetText());
		}

		public bool CanPasteUtf8(string s) {
			return Document != null && s != null && s.Length != 0;
		}

		public void PasteUnicode() {
			PasteUnicode(Clipboard.GetText());
		}

		public void PasteUnicode(string s) {
			if (!CanPasteUnicode(s))
				return;
			Paste(Encoding.Unicode.GetBytes(s));
		}

		public bool CanPasteUnicode() {
			return CanPasteUnicode(Clipboard.GetText());
		}

		public bool CanPasteUnicode(string s) {
			return Document != null && s != null && s.Length != 0;
		}

		public void Paste() {
			Paste(ClipboardUtils.GetData());
		}

		public void Paste(byte[] data) {
			if (!CanPaste(data))
				return;

			NotifyBeforeWrite(data.Length);
			Document.Write(CaretPosition.Offset, data, 0, data.Length);
			SetCaretPosition(new HexBoxPosition(NumberUtils.AddUInt64(CaretPosition.Offset, (ulong)data.Length), CaretPosition.Kind, 0));
			Selection = null;
			BringCaretIntoView();
			NotifyAfterWrite(data.Length);
		}

		public bool CanPaste() {
			return CanPaste(ClipboardUtils.GetData());
		}

		public bool CanPaste(byte[] data) {
			return Document != null && data != null && data.Length != 0;
		}

		protected override void OnTextInput(TextCompositionEventArgs e) {
			if (!e.Handled && HandleTextInput(e.Text)) {
				e.Handled = true;
				return;
			}
			base.OnTextInput(e);
		}

		public bool HandleTextInput(string text) {
			if (Document == null)
				return false;

			bool unselect = false;
			switch (CaretPosition.Kind) {
			case HexBoxPositionKind.HexByte:
				foreach (var c in text) {
					if (HandleHexByteInput(c)) {
						unselect = true;
						MoveCaretRight();
					}
				}
				break;

			case HexBoxPositionKind.Ascii:
				foreach (var c in text) {
					if (HandleHexAsciiInput(c)) {
						unselect = true;
						MoveCaretRight();
					}
				}
				break;

			default:
				throw new InvalidOperationException();
			}

			if (unselect)
				Selection = null;
			return true;
		}

		bool HandleHexByteInput(char c) {
			int h = ClipboardUtils.TryParseHexChar(c);
			if (h < 0)
				return false;

			int b = Document.ReadByte(CaretPosition.Offset);
			if (b >= 0) {
				NotifyBeforeWrite(1);
				if (CaretPosition.KindPosition == HexBoxPosition.INDEX_HEXBYTE_HI)
					b = (b & 0x0F) | (h << 4);
				else
					b = (b & 0xF0) | h;
				Document.Write(CaretPosition.Offset, (byte)b);
				NotifyAfterWrite(1);
			}
			return true;
		}

		bool HandleHexAsciiInput(char c) {
			if (c > 0x7E)
				return false;

			NotifyBeforeWrite(1);
			Document.Write(CaretPosition.Offset, (byte)c);
			NotifyAfterWrite(1);
			return true;
		}

		public void ClearBytes() {
			FillBytes(0);
		}

		void FillBytes(byte b) {
			if (Selection == null) {
				FillBytes(CaretPosition.Offset, CaretPosition.Offset, b);
				SetCaretPosition(new HexBoxPosition(NumberUtils.AddUInt64(CaretPosition.Offset, 1), CaretPosition.Kind, 0));
			}
			else
				FillBytes(Selection.Value.StartOffset, Selection.Value.EndOffset, b);
			Selection = null;
		}

		void FillBytes(ulong startOffset, ulong endOffset, byte b) {
			if (Document == null)
				return;
			if (endOffset < startOffset)
				return;
			ulong count = startOffset == 0 && endOffset == ulong.MaxValue ? ulong.MaxValue : endOffset - startOffset + 1;
			if (count > int.MaxValue)
				count = int.MaxValue;

			NotifyBeforeWrite((int)count);
			ulong offs = startOffset;
			ulong end = offs + count - 1;
			while (offs <= end) {
				Document.Write(offs, b);
				if (offs++ == ulong.MaxValue)
					break;
			}
			NotifyAfterWrite((int)count);
		}

		void NotifyBeforeWrite(int count) {
			NotifyWrite(count, true);
		}

		void NotifyAfterWrite(int count) {
			NotifyWrite(count, false);
		}

		void NotifyWrite(int count, bool isBeforeWrite) {
			if (OnWrite != null)
				OnWrite(this, new HexBoxWriteEventArgs(count, isBeforeWrite));
		}

		public event EventHandler<HexBoxWriteEventArgs> OnWrite;

		public override string ToString() {
			return string.Format("HexBox: {0}", Document == null ? null : Document.Name);
		}
	}
}

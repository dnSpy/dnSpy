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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Themes;
using dnSpy.Shared.UI.HexEditor;

namespace dnSpy.Shared.UI.Hex {
	public sealed class DnHexBox : HexBox {
		sealed class ContextMenuInitializer : IContextMenuInitializer {
			public void Initialize(IMenuItemContext context, ContextMenu menu) {
				var hexBox = (HexBox)context.CreatorObject.Object;
				var rect = hexBox.GetCaretWindowRect();
				if (rect != null && context.OpenedFromKeyboard) {
					var pos = rect.Value.BottomLeft;
					menu.HorizontalOffset = pos.X;
					menu.VerticalOffset = pos.Y;
					ContextMenuService.SetPlacement(hexBox, PlacementMode.Relative);
					ContextMenuService.SetPlacementTarget(hexBox, hexBox);
					menu.Closed += (s, e2) => {
						hexBox.ClearValue(ContextMenuService.PlacementProperty);
						hexBox.ClearValue(ContextMenuService.PlacementTargetProperty);
					};
				}
				else {
					hexBox.ClearValue(ContextMenuService.PlacementProperty);
					hexBox.ClearValue(ContextMenuService.PlacementTargetProperty);
				}
			}
		}

		readonly IHexEditorSettings hexEditorSettings;

		public DnHexBox(IMenuManager menuManager, IHexEditorSettings hexEditorSettings) {
			this.hexEditorSettings = hexEditorSettings;
			SetBinding(Control.FontFamilyProperty, new Binding("FontFamily") { Source = hexEditorSettings });
			SetBinding(Control.FontSizeProperty, new Binding("FontSize") { Source = hexEditorSettings });
			SetResourceReference(Control.BackgroundProperty, HexBoxThemeHelper.GetBackgroundResourceKey(ColorType.HexText));
			SetResourceReference(Control.ForegroundProperty, HexBoxThemeHelper.GetForegroundResourceKey(ColorType.HexText));
			SetResourceReference(HexBox.OffsetForegroundProperty, HexBoxThemeHelper.GetForegroundResourceKey(ColorType.HexOffset));
			SetResourceReference(HexBox.Byte0ForegroundProperty, HexBoxThemeHelper.GetForegroundResourceKey(ColorType.HexByte0));
			SetResourceReference(HexBox.Byte1ForegroundProperty, HexBoxThemeHelper.GetForegroundResourceKey(ColorType.HexByte1));
			SetResourceReference(HexBox.ByteErrorForegroundProperty, HexBoxThemeHelper.GetForegroundResourceKey(ColorType.HexByteError));
			SetResourceReference(HexBox.AsciiForegroundProperty, HexBoxThemeHelper.GetForegroundResourceKey(ColorType.HexAscii));
			SetResourceReference(HexBox.CaretForegroundProperty, HexBoxThemeHelper.GetBackgroundResourceKey(ColorType.HexCaret));
			SetResourceReference(HexBox.InactiveCaretForegroundProperty, HexBoxThemeHelper.GetBackgroundResourceKey(ColorType.HexInactiveCaret));
			SetResourceReference(HexBox.SelectionBackgroundProperty, HexBoxThemeHelper.GetBackgroundResourceKey(ColorType.HexSelection));
			SetResourceReference(Control.FontStyleProperty, HexBoxThemeHelper.GetFontStyleResourceKey(ColorType.HexText));
			SetResourceReference(Control.FontWeightProperty, HexBoxThemeHelper.GetFontWeightResourceKey(ColorType.HexText));

			menuManager.InitializeContextMenu(this, MenuConstants.GUIDOBJ_HEXBOX_GUID, null, new ContextMenuInitializer());

			BytesGroupCount = null;
			BytesPerLine = null;
			UseHexPrefix = null;
			ShowAscii = null;
			LowerCaseHex = null;
			AsciiEncoding = null;

			InstallBindings();
		}

		public bool IsMemory {
			get { return isMemory; }
			set { isMemory = value; }
		}
		bool isMemory;

		void InstallBindings() {
			Add(new RoutedCommand("GoToOffset", typeof(GoToOffsetHexBoxCtxMenuCommand)),
				(s, e) => GoToOffsetHexBoxCtxMenuCommand.Execute2(this, Window.GetWindow(this)),
				(s, e) => e.CanExecute = GoToOffsetHexBoxCtxMenuCommand.CanExecute(this),
				ModifierKeys.Control, Key.G);
			Add(new RoutedCommand("PasteBlobData", typeof(PasteBlobDataHexBoxCtxMenuCommand)),
				(s, e) => PasteBlobDataHexBoxCtxMenuCommand.Execute2(this),
				(s, e) => e.CanExecute = PasteBlobDataHexBoxCtxMenuCommand.CanExecute(this),
				ModifierKeys.Control, Key.B);
			Add(new RoutedCommand("Select", typeof(SelectRangeHexBoxCtxMenuCommand)),
				(s, e) => SelectRangeHexBoxCtxMenuCommand.Execute2(this, Window.GetWindow(this)),
				(s, e) => e.CanExecute = SelectRangeHexBoxCtxMenuCommand.CanExecute(this),
				ModifierKeys.Control, Key.L);
			Add(new RoutedCommand("ShowOnlySelectedBytes", typeof(ShowSelectionHexBoxCtxMenuCommand)),
				(s, e) => ShowSelectionHexBoxCtxMenuCommand.Execute2(this),
				(s, e) => e.CanExecute = ShowSelectionHexBoxCtxMenuCommand.CanExecute(this),
				ModifierKeys.Control, Key.D);
			Add(new RoutedCommand("ShowAllBytes", typeof(ShowWholeDocumentHexBoxCtxMenuCommand)),
				(s, e) => ShowWholeDocumentHexBoxCtxMenuCommand.Execute2(this),
				(s, e) => e.CanExecute = ShowWholeDocumentHexBoxCtxMenuCommand.CanExecute(this),
				ModifierKeys.Control | ModifierKeys.Shift, Key.D);
			Add(new RoutedCommand("SaveSelection", typeof(SaveSelectionHexBoxCtxMenuCommand)),
				(s, e) => SaveSelectionHexBoxCtxMenuCommand.Execute2(this, Window.GetWindow(this)),
				(s, e) => e.CanExecute = SaveSelectionHexBoxCtxMenuCommand.CanExecute(this),
				ModifierKeys.Control | ModifierKeys.Alt, Key.S);
		}

		void Add(ICommand command, ExecutedRoutedEventHandler exec, CanExecuteRoutedEventHandler canExec, ModifierKeys modifiers, Key key) {
			this.CommandBindings.Add(new CommandBinding(command, exec, canExec));
			this.InputBindings.Add(new KeyBinding(command, key, modifiers));
		}

		public ulong DocumentStartOffset {
			get {
				var doc = Document;
				return doc == null ? 0 : doc.StartOffset;
			}
		}

		public ulong DocumentEndOffset {
			get {
				var doc = Document;
				return doc == null ? 0 : doc.EndOffset;
			}
		}

		public new int? BytesGroupCount {
			get { return useDefault_BytesGroupCount ? (int?)null : base.BytesGroupCount; }
			set {
				if (value == null) {
					useDefault_BytesGroupCount = true;
					ClearValue(HexBox.BytesGroupCountProperty);
					SetBinding(HexBox.BytesGroupCountProperty, new Binding("BytesGroupCount") { Source = hexEditorSettings });
				}
				else {
					useDefault_BytesGroupCount = false;
					base.BytesGroupCount = value.Value;
				}
			}
		}
		bool useDefault_BytesGroupCount;

		public new int? BytesPerLine {
			get { return useDefault_BytesPerLine ? (int?)null : base.BytesPerLine; }
			set {
				if (value == null) {
					useDefault_BytesPerLine = true;
					ClearValue(HexBox.BytesPerLineProperty);
					SetBinding(HexBox.BytesPerLineProperty, new Binding("BytesPerLine") { Source = hexEditorSettings });
				}
				else {
					useDefault_BytesPerLine = false;
					base.BytesPerLine = Math.Min(HexEditorSettings.MAX_BYTES_PER_LINE, value.Value);
				}
			}
		}
		bool useDefault_BytesPerLine;

		public new bool? UseHexPrefix {
			get { return useDefault_UseHexPrefix ? (bool?)null : base.UseHexPrefix; }
			set {
				if (value == null) {
					useDefault_UseHexPrefix = true;
					ClearValue(HexBox.UseHexPrefixProperty);
					SetBinding(HexBox.UseHexPrefixProperty, new Binding("UseHexPrefix") { Source = hexEditorSettings });
				}
				else {
					useDefault_UseHexPrefix = false;
					base.UseHexPrefix = value.Value;
				}
			}
		}
		bool useDefault_UseHexPrefix;

		public new bool? ShowAscii {
			get { return useDefault_ShowAscii ? (bool?)null : base.ShowAscii; }
			set {
				if (value == null) {
					useDefault_ShowAscii = true;
					ClearValue(HexBox.ShowAsciiProperty);
					SetBinding(HexBox.ShowAsciiProperty, new Binding("ShowAscii") { Source = hexEditorSettings });
				}
				else {
					useDefault_ShowAscii = false;
					base.ShowAscii = value.Value;
				}
			}
		}
		bool useDefault_ShowAscii;

		public new bool? LowerCaseHex {
			get { return useDefault_LowerCaseHex ? (bool?)null : base.LowerCaseHex; }
			set {
				if (value == null) {
					useDefault_LowerCaseHex = true;
					ClearValue(HexBox.LowerCaseHexProperty);
					SetBinding(HexBox.LowerCaseHexProperty, new Binding("LowerCaseHex") { Source = hexEditorSettings });
				}
				else {
					useDefault_LowerCaseHex = false;
					base.LowerCaseHex = value.Value;
				}
			}
		}
		bool useDefault_LowerCaseHex;

		public new AsciiEncoding? AsciiEncoding {
			get { return useDefault_AsciiEncoding ? (AsciiEncoding?)null : base.AsciiEncoding; }
			set {
				if (value == null) {
					useDefault_AsciiEncoding = true;
					ClearValue(HexBox.AsciiEncodingProperty);
					SetBinding(HexBox.AsciiEncodingProperty, new Binding("AsciiEncoding") { Source = hexEditorSettings });
				}
				else {
					useDefault_AsciiEncoding = false;
					base.AsciiEncoding = value.Value;
				}
			}
		}
		bool useDefault_AsciiEncoding;

		public void SelectAndMoveCaret(ulong fileOffset, ulong length) {
			ulong end = length == 0 ? fileOffset : fileOffset + length - 1 < fileOffset ? ulong.MaxValue : fileOffset + length - 1;
			if (length == 0)
				Selection = null;
			else
				Selection = new HexSelection(end, fileOffset);
			SetCaretPositionAndMakeVisible(fileOffset, end, true);
		}

		public void SetCaretPositionAndMakeVisible(ulong start, ulong end, bool resetKindPos = false) {
			// Make sure end address is also visible
			var kindPos = CaretPosition.KindPosition;
			if (resetKindPos) {
				if (CaretPosition.Kind != HexBoxPositionKind.HexByte)
					kindPos = 0;
				else
					kindPos = start <= end ? HexBoxPosition.INDEX_HEXBYTE_FIRST : HexBoxPosition.INDEX_HEXBYTE_LAST;
			}
			CaretPosition = new HexBoxPosition(end, CaretPosition.Kind, kindPos);
			CaretPosition = new HexBoxPosition(start, CaretPosition.Kind, kindPos);
		}

		public void InitializeStartEndOffsetToDocument() {
			StartOffset = DocumentStartOffset;
			EndOffset = DocumentEndOffset;
		}
	}
}

/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using dnSpy.Contracts.Hex.Formatting;
using VSTC = Microsoft.VisualStudio.Text.Classification;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSTF = Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Hex.Editor {
	sealed class HexCaretImpl : HexCaret {
		public override bool IsValuesCaretPresent => hexCaretLayer.IsValuesCaretPresent;
		public override bool IsAsciiCaretPresent => hexCaretLayer.IsAsciiCaretPresent;
		public override double ValuesTop => hexCaretLayer.ValuesTop;
		public override double ValuesBottom => hexCaretLayer.ValuesBottom;
		public override double ValuesLeft => hexCaretLayer.ValuesLeft;
		public override double ValuesRight => hexCaretLayer.ValuesRight;
		public override double ValuesWidth => hexCaretLayer.ValuesWidth;
		public override double ValuesHeight => hexCaretLayer.ValuesHeight;
		public override double AsciiTop => hexCaretLayer.AsciiTop;
		public override double AsciiBottom => hexCaretLayer.AsciiBottom;
		public override double AsciiLeft => hexCaretLayer.AsciiLeft;
		public override double AsciiRight => hexCaretLayer.AsciiRight;
		public override double AsciiWidth => hexCaretLayer.AsciiWidth;
		public override double AsciiHeight => hexCaretLayer.AsciiHeight;
		public override bool OverwriteMode => hexCaretLayer.OverwriteMode;
		public override HexViewLine ContainingHexViewLine => GetLine(currentPosition);
		internal HexColumnPosition CurrentPosition => currentPosition;

		public override bool IsHidden {
			get => hexCaretLayer.IsHidden;
			set => hexCaretLayer.IsHidden = value;
		}

		public override event EventHandler<HexCaretPositionChangedEventArgs> PositionChanged;
		public override HexCaretPosition Position => new HexCaretPosition(currentPosition);
		HexColumnPosition currentPosition;

		readonly WpfHexView hexView;
		readonly HexCaretLayer hexCaretLayer;
		readonly ImeState imeState;
		double preferredXCoordinate;

		public HexCaretImpl(WpfHexView hexView, HexAdornmentLayer caretLayer, VSTC.IClassificationFormatMap classificationFormatMap, VSTC.IClassificationTypeRegistryService classificationTypeRegistryService) {
			if (caretLayer == null)
				throw new ArgumentNullException(nameof(caretLayer));
			if (classificationFormatMap == null)
				throw new ArgumentNullException(nameof(classificationFormatMap));
			if (classificationTypeRegistryService == null)
				throw new ArgumentNullException(nameof(classificationTypeRegistryService));
			this.hexView = hexView ?? throw new ArgumentNullException(nameof(hexView));
			imeState = new ImeState();
			preferredXCoordinate = 0;
			__preferredYCoordinate = 0;
			hexView.Options.OptionChanged += Options_OptionChanged;
			hexView.VisualElement.AddHandler(UIElement.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(VisualElement_GotKeyboardFocus), true);
			hexView.VisualElement.AddHandler(UIElement.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(VisualElement_LostKeyboardFocus), true);
			hexView.LayoutChanged += HexView_LayoutChanged;
			hexView.BufferLinesChanged += HexView_BufferLinesChanged;
			hexView.ZoomLevelChanged += HexView_ZoomLevelChanged;
			hexCaretLayer = new HexCaretLayer(this, caretLayer, classificationFormatMap, classificationTypeRegistryService);
			InputMethod.SetIsInputMethodSuspended(hexView.VisualElement, true);
		}

		internal void Initialize() {
			SetPositionCore(new HexColumnPosition(HexColumnType.Values, new HexCellPosition(HexColumnType.Values, hexView.BufferLines.BufferSpan.Start, 0), new HexCellPosition(HexColumnType.Ascii, hexView.BufferLines.BufferSpan.Start, 0)));
			OnBufferLinesChanged();
		}

		void HexView_BufferLinesChanged(object sender, BufferLinesChangedEventArgs e) {
			OnBufferLinesChanged();
			savePreferredCoordinates = true;
		}

		void OnBufferLinesChanged() {
			var bufferLines = hexView.BufferLines;
			hexCaretLayer.IsValuesCaretPresent = bufferLines.ShowValues;
			hexCaretLayer.IsAsciiCaretPresent = bufferLines.ShowAscii;
			SetPositionCore(GetUpdatedCaretPosition());
		}

		HexColumnPosition GetUpdatedCaretPosition() {
			var bufferLines = hexView.BufferLines;
			if ((!IsValuesCaretPresent && !IsAsciiCaretPresent) || bufferLines.BufferSpan.Length == 0)
				return new HexColumnPosition(HexColumnType.Values, new HexCellPosition(HexColumnType.Values, hexView.BufferLines.BufferStart, 0), new HexCellPosition(HexColumnType.Ascii, hexView.BufferLines.BufferStart, 0));

			var activeColumn = currentPosition.ActiveColumn;
			var valuePosition = currentPosition.ValuePosition;
			var asciiPosition = currentPosition.AsciiPosition;

			// Use ASCII's buffer position since it's more accurate
			var bufferPosition = asciiPosition.BufferPosition;
			if (bufferPosition.IsDefault)
				bufferPosition = valuePosition.BufferPosition;
			if (bufferPosition.IsDefault)
				bufferPosition = bufferLines.BufferStart;
			if (bufferPosition < bufferLines.BufferStart)
				bufferPosition = bufferLines.BufferStart;
			else if (bufferPosition > bufferLines.BufferEnd)
				bufferPosition = bufferLines.BufferEnd;
			if (bufferPosition >= HexPosition.MaxEndPosition)
				bufferPosition = new HexBufferPoint(bufferLines.Buffer, HexPosition.MaxEndPosition - 1);

			var caretLine = bufferLines.GetLineFromPosition(bufferPosition);
			var valueCell = caretLine.ValueCells.GetCell(bufferPosition);
			var asciiCell = caretLine.AsciiCells.GetCell(bufferPosition);

			switch (activeColumn) {
			case HexColumnType.Values:
				if (valueCell == null)
					activeColumn = HexColumnType.Ascii;
				break;

			case HexColumnType.Ascii:
				if (asciiCell == null)
					activeColumn = HexColumnType.Values;
				break;

			case HexColumnType.Offset:
			default:
				throw new InvalidOperationException();
			}

			HexCellPosition newValuePosition, newAsciiPosition;
			if (valueCell == null)
				newValuePosition = new HexCellPosition(HexColumnType.Values, hexView.BufferLines.BufferStart, 0);
			else
				newValuePosition = new HexCellPosition(HexColumnType.Values, valueCell.BufferStart, 0);
			if (asciiCell == null)
				newAsciiPosition = new HexCellPosition(HexColumnType.Ascii, hexView.BufferLines.BufferStart, 0);
			else
				newAsciiPosition = new HexCellPosition(HexColumnType.Ascii, valueCell?.BufferStart ?? asciiCell.BufferStart, 0);

			bool keepPositions = CanReUse(valuePosition, newValuePosition, valueCell) &&
								 CanReUse(asciiPosition, newAsciiPosition, asciiCell);
			if (keepPositions)
				return currentPosition;
			return new HexColumnPosition(activeColumn, newValuePosition, newAsciiPosition);
		}

		static bool CanReUse(HexCellPosition oldPos, HexCellPosition newPos, HexCell cell) {
			if (oldPos.IsDefault)
				return false;
			if (oldPos.IsDefault != newPos.IsDefault)
				return false;
			if (cell == null)
				return newPos.IsDefault;
			return cell.BufferSpan.Contains(oldPos.BufferPosition);
		}

		void HexView_ZoomLevelChanged(object sender, VSTE.ZoomLevelChangedEventArgs e) =>
			savePreferredCoordinates = true;
		bool savePreferredCoordinates;

		void HexView_LayoutChanged(object sender, HexViewLayoutChangedEventArgs e) {
			hexCaretLayer.OnLayoutChanged(e);
			if (savePreferredCoordinates) {
				savePreferredCoordinates = false;
				SavePreferredXCoordinate();
				SavePreferredYCoordinate();
			}
			if (imeState.CompositionStarted)
				MoveImeCompositionWindow();
		}

		void VisualElement_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => InitializeIME();
		void VisualElement_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => StopIME(true);

		void CancelCompositionString() {
			if (imeState.Context == IntPtr.Zero)
				return;
			const int NI_COMPOSITIONSTR = 0x0015;
			const int CPS_CANCEL = 0x0004;
			ImeState.ImmNotifyIME(imeState.Context, NI_COMPOSITIONSTR, CPS_CANCEL, 0);
		}

		void InitializeIME() {
			if (imeState.HwndSource != null)
				return;
			imeState.HwndSource = PresentationSource.FromVisual(hexView.VisualElement) as HwndSource;
			if (imeState.HwndSource == null)
				return;

			Debug.Assert(imeState.Context == IntPtr.Zero);
			Debug.Assert(imeState.HWND == IntPtr.Zero);
			Debug.Assert(imeState.OldContext == IntPtr.Zero);
			if (hexView.Buffer.IsReadOnly || hexView.Options.DoesViewProhibitUserInput()) {
				imeState.Context = IntPtr.Zero;
				imeState.HWND = IntPtr.Zero;
			}
			else {
				imeState.HWND = ImeState.ImmGetDefaultIMEWnd(IntPtr.Zero);
				imeState.Context = ImeState.ImmGetContext(imeState.HWND);
			}
			imeState.OldContext = ImeState.ImmAssociateContext(imeState.HwndSource.Handle, imeState.Context);
			imeState.HwndSource.AddHook(WndProc);
			TfThreadMgrHelper.SetFocus();
		}

		void StopIME(bool cancelCompositionString) {
			if (imeState.HwndSource == null)
				return;
			if (cancelCompositionString)
				CancelCompositionString();
			ImeState.ImmAssociateContext(imeState.HwndSource.Handle, imeState.OldContext);
			ImeState.ImmReleaseContext(imeState.HWND, imeState.Context);
			imeState.HwndSource.RemoveHook(WndProc);
			imeState.Clear();
			hexCaretLayer.SetImeStarted(false);
		}

		static class TfThreadMgrHelper {
			static bool initd;
			static ITfThreadMgr tfThreadMgr;

			[DllImport("msctf")]
			static extern int TF_CreateThreadMgr(out ITfThreadMgr pptim);

			[ComImport, Guid("AA80E801-2021-11D2-93E0-0060B067B86E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
			interface ITfThreadMgr {
				void _VtblGap0_5();
				[PreserveSig]
				int SetFocus(IntPtr pdimFocus);
			}

			public static void SetFocus() {
				if (!initd) {
					initd = true;
					TF_CreateThreadMgr(out tfThreadMgr);
				}
				tfThreadMgr?.SetFocus(IntPtr.Zero);
			}
		}

		sealed class ImeState {
			[DllImport("imm32")]
			public static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);
			[DllImport("imm32")]
			public static extern IntPtr ImmGetContext(IntPtr hWnd);
			[DllImport("imm32")]
			public static extern IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hIMC);
			[DllImport("imm32")]
			public static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);
			[DllImport("imm32")]
			public static extern bool ImmNotifyIME(IntPtr hIMC, uint dwAction, uint dwIndex, uint dwValue);
			[DllImport("imm32")]
			public static extern bool ImmSetCompositionWindow(IntPtr hIMC, ref COMPOSITIONFORM lpCompForm);

			[StructLayout(LayoutKind.Sequential)]
			public struct COMPOSITIONFORM {
				public int dwStyle;
				public POINT ptCurrentPos;
				public RECT rcArea;
			}

			[StructLayout(LayoutKind.Sequential)]
			public class POINT {
				public int x;
				public int y;

				public POINT(int x, int y) {
					this.x = x;
					this.y = y;
				}
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct RECT {
				public int left;
				public int top;
				public int right;
				public int bottom;

				public RECT(int left, int top, int right, int bottom) {
					this.left = left;
					this.top = top;
					this.right = right;
					this.bottom = bottom;
				}
			}

			public HwndSource HwndSource;
			public IntPtr Context;
			public IntPtr HWND;
			public IntPtr OldContext;
			public bool CompositionStarted;

			public void Clear() {
				HwndSource = null;
				Context = IntPtr.Zero;
				HWND = IntPtr.Zero;
				OldContext = IntPtr.Zero;
				CompositionStarted = false;
			}
		}

		IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
			if (hexView.IsClosed)
				return IntPtr.Zero;
			const int WM_IME_STARTCOMPOSITION = 0x010D;
			const int WM_IME_ENDCOMPOSITION = 0x010E;
			const int WM_IME_COMPOSITION = 0x010F;
			if (msg == WM_IME_COMPOSITION) {
				MoveImeCompositionWindow();
				return IntPtr.Zero;
			}
			else if (msg == WM_IME_STARTCOMPOSITION) {
				Debug.Assert(!imeState.CompositionStarted);
				imeState.CompositionStarted = true;
				EnsureVisible();
				hexCaretLayer.SetImeStarted(true);
				MoveImeCompositionWindow();
				return IntPtr.Zero;
			}
			else if (msg == WM_IME_ENDCOMPOSITION) {
				Debug.Assert(imeState.CompositionStarted);
				imeState.CompositionStarted = false;
				hexCaretLayer.SetImeStarted(false);
				return IntPtr.Zero;
			}
			return IntPtr.Zero;
		}

		static int? GetLinePosition(HexBufferLine line, HexColumnPosition position) {
			HexCell cell;
			switch (position.ActiveColumn) {
			case HexColumnType.Values:
				cell = line.ValueCells.GetCell(position.ActivePosition.BufferPosition);
				return cell?.CellSpan.Start + position.ActivePosition.CellPosition;

			case HexColumnType.Ascii:
				cell = line.AsciiCells.GetCell(position.ActivePosition.BufferPosition);
				return cell?.CellSpan.Start + position.ActivePosition.CellPosition;

			case HexColumnType.Offset:
			default:
				throw new InvalidOperationException();
			}
		}

		void MoveImeCompositionWindow() {
			if (!IsValuesCaretPresent && !IsAsciiCaretPresent)
				return;
			if (imeState.Context == IntPtr.Zero)
				return;
			var line = ContainingHexViewLine;
			if (line.VisibilityState == VSTF.VisibilityState.Unattached)
				return;
			var linePos = GetLinePosition(line.BufferLine, currentPosition);
			if (linePos == null)
				return;
			var charBounds = line.GetExtendedCharacterBounds(linePos.Value);

			const int CFS_DEFAULT = 0x0000;
			const int CFS_FORCE_POSITION = 0x0020;

			var compForm = new ImeState.COMPOSITIONFORM();
			compForm.dwStyle = CFS_DEFAULT;

			var rootVisual = imeState.HwndSource.RootVisual;
			GeneralTransform generalTransform = null;
			if (rootVisual != null && rootVisual.IsAncestorOf(hexView.VisualElement))
				generalTransform = hexView.VisualElement.TransformToAncestor(rootVisual);

			var compTarget = imeState.HwndSource.CompositionTarget;
			if (generalTransform != null && compTarget != null) {
				var transform = compTarget.TransformToDevice;
				compForm.dwStyle = CFS_FORCE_POSITION;

				var caretPoint = transform.Transform(generalTransform.Transform(new Point(charBounds.Left - hexView.ViewportLeft, charBounds.TextTop - hexView.ViewportTop)));
				var viewPointTop = transform.Transform(generalTransform.Transform(new Point(0, 0)));
				var viewPointBottom = transform.Transform(generalTransform.Transform(new Point(hexView.ViewportWidth, hexView.ViewportHeight)));

				compForm.ptCurrentPos = new ImeState.POINT(Math.Max(0, (int)caretPoint.X), Math.Max(0, (int)caretPoint.Y));
				compForm.rcArea = new ImeState.RECT(
					Math.Max(0, (int)viewPointTop.X), Math.Max(0, (int)viewPointTop.Y),
					Math.Max(0, (int)viewPointBottom.X), Math.Max(0, (int)viewPointBottom.Y));
			}

			ImeState.ImmSetCompositionWindow(imeState.Context, ref compForm);
		}

		void Options_OptionChanged(object sender, VSTE.EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultHexViewOptions.ViewProhibitUserInputName) {
				StopIME(false);
				InitializeIME();
			}
		}

		void SetPositionCore(HexColumnPosition position) {
			position = Filter(position);
			if (currentPosition != position) {
				currentPosition = position;
				hexCaretLayer.ActiveColumn = currentPosition.ActiveColumn;
				hexCaretLayer.CaretPositionChanged();
				if (imeState.CompositionStarted)
					MoveImeCompositionWindow();
			}
		}

		HexColumnPosition Filter(HexColumnPosition position) =>
			new HexColumnPosition(position.ActiveColumn, Filter(position.ValuePosition), Filter(position.AsciiPosition));

		HexCellPosition Filter(HexCellPosition position) =>
			new HexCellPosition(position.Column, hexView.BufferLines.FilterAndVerify(position.BufferPosition), position.CellPosition);

		void SetExplicitPosition(HexColumnPosition position) {
			var oldPos = Position;
			SetPositionCore(position);
			var newPos = Position;
			if (oldPos != newPos)
				PositionChanged?.Invoke(this, new HexCaretPositionChangedEventArgs(hexView, oldPos, newPos));
		}

		public override HexCaretPosition ToggleActiveColumn() {
			if (!(IsValuesCaretPresent && IsAsciiCaretPresent))
				return Position;
			return MoveTo(new HexColumnPosition(Toggle(currentPosition.ActiveColumn), currentPosition.ValuePosition, currentPosition.AsciiPosition));
		}

		static HexColumnType Toggle(HexColumnType column) {
			switch (column) {
			case HexColumnType.Values:		return HexColumnType.Ascii;
			case HexColumnType.Ascii:		return HexColumnType.Values;
			case HexColumnType.Offset:
			default:
				throw new ArgumentOutOfRangeException(nameof(column));
			}
		}

		public override void EnsureVisible() {
			if (!IsValuesCaretPresent && !IsAsciiCaretPresent)
				return;
			var line = ContainingHexViewLine;
			if (line.VisibilityState != VSTF.VisibilityState.FullyVisible) {
				VSTE.ViewRelativePosition relativeTo;
				var firstVisibleLine = hexView.HexViewLines?.FirstVisibleLine;
				if (firstVisibleLine == null || !firstVisibleLine.IsVisible())
					relativeTo = VSTE.ViewRelativePosition.Top;
				else if (line.BufferStart <= firstVisibleLine.BufferStart)
					relativeTo = VSTE.ViewRelativePosition.Top;
				else
					relativeTo = VSTE.ViewRelativePosition.Bottom;
				hexView.DisplayHexLineContainingBufferPosition(line.BufferStart, 0, relativeTo);
			}

			double left, right, width;
			switch (currentPosition.ActiveColumn) {
			case HexColumnType.Values:
				left = hexCaretLayer.ValuesLeft;
				right = hexCaretLayer.ValuesRight;
				width = hexCaretLayer.ValuesWidth;
				break;

			case HexColumnType.Ascii:
				left = hexCaretLayer.AsciiLeft;
				right = hexCaretLayer.AsciiRight;
				width = hexCaretLayer.AsciiWidth;
				break;

			case HexColumnType.Offset:
			default:
				throw new InvalidOperationException();
			}

			double availWidth = Math.Max(0, hexView.ViewportWidth - width);
			double extraScroll;
			if (availWidth >= WpfHexViewConstants.EXTRA_HORIZONTAL_WIDTH)
				extraScroll = WpfHexViewConstants.EXTRA_HORIZONTAL_WIDTH;
			else
				extraScroll = availWidth / 2;
			if (hexView.ViewportWidth == 0) {
				// Don't do anything if there's zero width. This can happen during
				// startup when code accesses the caret before the window is shown.
			}
			else if (left < hexView.ViewportLeft)
				hexView.ViewportLeft = left - extraScroll;
			else if (right > hexView.ViewportRight)
				hexView.ViewportLeft = right + extraScroll - hexView.ViewportWidth;
		}

		public override HexCaretPosition MoveTo(HexViewLine hexLine) =>
			MoveTo(hexLine, preferredXCoordinate, HexMoveToFlags.None, true);
		public override HexCaretPosition MoveTo(HexViewLine hexLine, HexMoveToFlags flags) =>
			MoveTo(hexLine, preferredXCoordinate, flags, true);
		public override HexCaretPosition MoveTo(HexViewLine hexLine, double xCoordinate) =>
			MoveTo(hexLine, xCoordinate, HexMoveToFlags.CaptureHorizontalPosition, true);
		public override HexCaretPosition MoveTo(HexViewLine hexLine, double xCoordinate, HexMoveToFlags flags) =>
			MoveTo(hexLine, xCoordinate, flags, true);
		HexCaretPosition MoveTo(HexViewLine hexLine, double xCoordinate, HexMoveToFlags flags, bool captureVerticalPosition) {
			if (hexLine == null)
				throw new ArgumentNullException(nameof(hexLine));
			if (hexLine.BufferLine.LineProvider != hexView.BufferLines)
				throw new ArgumentException();

			var linePosition = (flags & HexMoveToFlags.InsertionPosition) != 0 ?
					hexLine.GetInsertionLinePositionFromXCoordinate(xCoordinate) :
					hexLine.GetVirtualLinePositionFromXCoordinate(xCoordinate);
			var posInfo = hexLine.BufferLine.GetLinePositionInfo(linePosition);
			if (posInfo.IsValueCellSeparator) {
				var posInfo2 = hexLine.BufferLine.GetLinePositionInfo(hexLine.GetInsertionLinePositionFromXCoordinate(xCoordinate));
				if (posInfo2.IsValueCell)
					posInfo = posInfo2;
			}
			var closestPos = hexLine.BufferLine.GetClosestCellPosition(posInfo, onlyVisibleCells: true);
			if (closestPos == null) {
				Debug.Assert(hexView.BufferLines.BufferSpan.Length == 0 || (!IsValuesCaretPresent && !IsAsciiCaretPresent));
				closestPos = new HexCellPosition(currentPosition.ActiveColumn, hexLine.BufferStart, 0);
			}
			SetExplicitPosition(CreateColumnPosition(closestPos.Value));
			if ((flags & HexMoveToFlags.CaptureHorizontalPosition) != 0)
				SavePreferredXCoordinate();
			if (captureVerticalPosition)
				SavePreferredYCoordinate();
			return Position;
		}

		HexColumnPosition CreateColumnPosition(HexCellPosition position) {
			switch (position.Column) {
			case HexColumnType.Values:
				var asciiPosition = new HexCellPosition(HexColumnType.Ascii, position.BufferPosition, 0);
				return new HexColumnPosition(position.Column, position, asciiPosition);

			case HexColumnType.Ascii:
				var valuesPosition = CreateValuesCellPosition(position.BufferPosition);
				return new HexColumnPosition(position.Column, valuesPosition, position);

			case HexColumnType.Offset:
			default:
				throw new ArgumentOutOfRangeException(nameof(position));
			}
		}

		HexCellPosition CreateValuesCellPosition(HexBufferPoint position) {
			var line = hexView.BufferLines.GetLineFromPosition(position);
			var cell = line.ValueCells.GetCell(position);
			if (cell == null)
				return new HexCellPosition(HexColumnType.Values, position, 0);
			return new HexCellPosition(HexColumnType.Values, cell.BufferStart, 0);
		}

		public override HexCaretPosition MoveTo(HexColumnType column, HexBufferPoint position, HexMoveToFlags flags) {
			if (column != HexColumnType.Values && column != HexColumnType.Ascii)
				throw new ArgumentOutOfRangeException(nameof(column));
			if (position.IsDefault)
				throw new ArgumentException();
			if (!hexView.BufferLines.IsValidPosition(position))
				throw new ArgumentOutOfRangeException(nameof(position));

			var cellPos = column == HexColumnType.Values ? CreateValuesCellPosition(position) : new HexCellPosition(column, position, 0);
			var colPos = CreateColumnPosition(cellPos);
			return MoveTo(colPos, flags);
		}

		public override HexCaretPosition MoveTo(HexCellPosition position, HexMoveToFlags flags) {
			if (position.IsDefault)
				throw new ArgumentException();
			if (!hexView.BufferLines.IsValidPosition(position.BufferPosition))
				throw new ArgumentOutOfRangeException(nameof(position));
			var colPos = CreateColumnPosition(position);
			return MoveTo(colPos, flags);
		}

		public override HexCaretPosition MoveTo(HexColumnPosition position, HexMoveToFlags flags) {
			if (position.IsDefault)
				throw new ArgumentException();
			if (!hexView.BufferLines.IsValidPosition(position.ValuePosition.BufferPosition))
				throw new ArgumentOutOfRangeException(nameof(position));
			if (!hexView.BufferLines.IsValidPosition(position.AsciiPosition.BufferPosition))
				throw new ArgumentOutOfRangeException(nameof(position));

			SetExplicitPosition(position);
			if ((flags & HexMoveToFlags.CaptureHorizontalPosition) != 0)
				SavePreferredXCoordinate();
			SavePreferredYCoordinate();
			return Position;
		}

		public override HexCaretPosition MoveToNextCaretPosition() {
			if (!IsValuesCaretPresent && !IsAsciiCaretPresent)
				return Position;

			var position = currentPosition.ActivePosition;
			var bufferPosition = position.BufferPosition;
			int cellPosition = position.CellPosition;
			var line = hexView.BufferLines.GetLineFromPosition(bufferPosition);
			HexCell cell;
			HexBufferPoint nextBufferPosition;
			switch (currentPosition.ActiveColumn) {
			case HexColumnType.Values:
				cell = line.ValueCells.GetCell(bufferPosition);
				if (cell == null)
					return Position;
				cellPosition++;
				if (cellPosition < cell.CellSpan.Length)
					return MoveTo(new HexCellPosition(position.Column, position.BufferPosition, cellPosition));
				nextBufferPosition = cell.BufferEnd;
				if (!hexView.BufferLines.IsValidPosition(nextBufferPosition) || nextBufferPosition == hexView.BufferLines.BufferEnd)
					return Position;
				return MoveTo(new HexCellPosition(position.Column, nextBufferPosition, 0));

			case HexColumnType.Ascii:
				cell = line.AsciiCells.GetCell(bufferPosition);
				if (cell == null)
					return Position;
				cellPosition++;
				if (cellPosition < cell.CellSpan.Length)
					return MoveTo(new HexCellPosition(position.Column, position.BufferPosition, cellPosition));
				nextBufferPosition = cell.BufferEnd;
				if (!hexView.BufferLines.IsValidPosition(nextBufferPosition) || nextBufferPosition == hexView.BufferLines.BufferEnd)
					return Position;
				return MoveTo(new HexCellPosition(position.Column, nextBufferPosition, 0));

			case HexColumnType.Offset:
			default:
				throw new InvalidOperationException();
			}
		}

		HexCellPosition CreateValuePositionLastCellCharacter(HexBufferPoint position) {
			var line = hexView.BufferLines.GetLineFromPosition(position);
			var cell = line.ValueCells.GetCell(position);
			if (cell == null)
				return new HexCellPosition(HexColumnType.Values, position, 0);
			return new HexCellPosition(HexColumnType.Values, position, cell.CellSpan.Length - 1);
		}

		public override HexCaretPosition MoveToPreviousCaretPosition() {
			if (!IsValuesCaretPresent && !IsAsciiCaretPresent)
				return Position;

			var position = currentPosition.ActivePosition;
			var bufferPosition = position.BufferPosition;
			int cellPosition = position.CellPosition;
			var line = hexView.BufferLines.GetLineFromPosition(bufferPosition);
			HexCell cell;
			HexBufferPoint previousBufferPosition;
			switch (currentPosition.ActiveColumn) {
			case HexColumnType.Values:
				cell = line.ValueCells.GetCell(bufferPosition);
				if (cell == null)
					return Position;
				cellPosition--;
				if (cellPosition >= 0)
					return MoveTo(new HexCellPosition(position.Column, position.BufferPosition, cellPosition));
				if (cell.BufferStart.Position == 0)
					return Position;
				previousBufferPosition = cell.BufferStart - 1;
				if (!hexView.BufferLines.IsValidPosition(previousBufferPosition))
					return Position;
				return MoveTo(CreateValuePositionLastCellCharacter(previousBufferPosition));

			case HexColumnType.Ascii:
				cell = line.AsciiCells.GetCell(bufferPosition);
				if (cell == null)
					return Position;
				cellPosition--;
				if (cellPosition >= 0)
					return MoveTo(new HexCellPosition(position.Column, position.BufferPosition, cellPosition));
				if (cell.BufferStart.Position == 0)
					return Position;
				previousBufferPosition = cell.BufferStart - 1;
				if (!hexView.BufferLines.IsValidPosition(previousBufferPosition))
					return Position;
				return MoveTo(new HexCellPosition(position.Column, previousBufferPosition, 0));

			case HexColumnType.Offset:
			default:
				throw new InvalidOperationException();
			}
		}

		double PreferredYCoordinate => Math.Min(__preferredYCoordinate, hexView.ViewportHeight) + hexView.ViewportTop;
		double __preferredYCoordinate;

		HexViewLine GetVisibleCaretLine() {
			if (hexView.HexViewLines == null)
				return null;
			var line = ContainingHexViewLine;
			if (line.IsVisible())
				return line;
			var firstVisLine = hexView.HexViewLines.FirstVisibleLine;
			// Don't return FirstVisibleLine/LastVisibleLine since they will return a hidden line if it fails to find a visible line
			if (line.BufferStart <= firstVisLine.BufferStart)
				return hexView.HexViewLines.FirstOrDefault(a => a.IsVisible());
			return hexView.HexViewLines.LastOrDefault(a => a.IsVisible());
		}

		void SavePreferredXCoordinate() {
			preferredXCoordinate = currentPosition.ActiveColumn == HexColumnType.Values ? (ValuesLeft + ValuesRight) / 2 : (AsciiLeft + AsciiRight) / 2;
			if (double.IsNaN(preferredXCoordinate) || preferredXCoordinate < 0 || preferredXCoordinate > 100000000)
				preferredXCoordinate = 0;
		}

		void SavePreferredYCoordinate() {
			var line = GetVisibleCaretLine();
			if (line != null)
				__preferredYCoordinate = (line.Top + line.Bottom) / 2 - hexView.ViewportTop;
			else
				__preferredYCoordinate = 0;
		}

		public override HexCaretPosition MoveToPreferredCoordinates() {
			var textLine = hexView.HexViewLines.GetHexViewLineContainingYCoordinate(PreferredYCoordinate);
			if (textLine == null || !textLine.IsVisible())
				textLine = PreferredYCoordinate <= hexView.ViewportTop ? hexView.HexViewLines.FirstVisibleLine : hexView.HexViewLines.LastVisibleLine;
			return MoveTo(textLine, preferredXCoordinate, HexMoveToFlags.None, false);
		}

		HexViewLine GetLine(HexColumnPosition position) =>
			hexView.GetHexViewLineContainingBufferPosition(position.ActivePosition.BufferPosition);

		internal void Dispose() {
			StopIME(true);
			hexView.Options.OptionChanged -= Options_OptionChanged;
			hexView.VisualElement.GotKeyboardFocus -= VisualElement_GotKeyboardFocus;
			hexView.VisualElement.LostKeyboardFocus -= VisualElement_LostKeyboardFocus;
			hexView.LayoutChanged -= HexView_LayoutChanged;
			hexView.BufferLinesChanged -= HexView_BufferLinesChanged;
			hexView.ZoomLevelChanged -= HexView_ZoomLevelChanged;
			hexCaretLayer.Dispose();
		}
	}
}

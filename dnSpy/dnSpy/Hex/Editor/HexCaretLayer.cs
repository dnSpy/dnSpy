/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Formatting;
using CTC = dnSpy.Contracts.Text.Classification;
using TE = dnSpy.Text.Editor;
using VSTC = Microsoft.VisualStudio.Text.Classification;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSTF = Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Hex.Editor {
	sealed class HexCaretLayer : UIElement {
		const double INACTIVE_CARET_HEIGHT = 2.0;

		public HexColumnType ActiveColumn {
			get { return activeColumn; }
			set {
				if (value != HexColumnType.Values && value != HexColumnType.Ascii)
					throw new ArgumentOutOfRangeException(nameof(value));
				if (activeColumn != value) {
					activeColumn = value;
					UpdateCaretProperties();
				}
			}
		}
		HexColumnType activeColumn;

		public bool IsValuesCaretPresent {
			get { return isValuesCaretPresent; }
			set {
				if (isValuesCaretPresent != value) {
					isValuesCaretPresent = value;
					UpdateCaretProperties();
				}
			}
		}
		bool isValuesCaretPresent;

		public bool IsAsciiCaretPresent {
			get { return isAsciiCaretPresent; }
			set {
				if (isAsciiCaretPresent != value) {
					isAsciiCaretPresent = value;
					UpdateCaretProperties();
				}
			}
		}
		bool isAsciiCaretPresent;

		readonly CaretGeometry valuesCaretGeometry;
		readonly CaretGeometry asciiCaretGeometry;

		Brush activeCaretBrush;
		Brush activeOverwriteCaretBrush;
		Brush inactiveCaretBrush;
		Brush inactiveOverwriteCaretBrush;

		bool drawCaretShape;
		bool overwriteMode;
		DispatcherTimer dispatcherTimer;

		public bool OverwriteMode {
			get { return overwriteMode; }
			set {
				if (overwriteMode != value) {
					overwriteMode = value;
					UpdateCaretProperties();
				}
			}
		}

		public double ValuesTop {
			get {
				if (!drawCaretShape)
					throw new InvalidOperationException();
				return valuesCaretGeometry.Rect.Top;
			}
		}

		public double ValuesHeight {
			get {
				if (!drawCaretShape)
					throw new InvalidOperationException();
				return valuesCaretGeometry.Rect.Height;
			}
		}

		public double ValuesLeft => valuesCaretGeometry.Rect.Left;
		public double ValuesRight => ValuesLeft + ValuesWidth;
		public double ValuesBottom => ValuesTop + ValuesHeight;
		public double ValuesWidth => valuesCaretGeometry.Rect.Width;

		public double AsciiTop {
			get {
				if (!drawCaretShape)
					throw new InvalidOperationException();
				return asciiCaretGeometry.Rect.Top;
			}
		}

		public double AsciiHeight {
			get {
				if (!drawCaretShape)
					throw new InvalidOperationException();
				return asciiCaretGeometry.Rect.Height;
			}
		}

		public double AsciiLeft => asciiCaretGeometry.Rect.Left;
		public double AsciiRight => AsciiLeft + AsciiWidth;
		public double AsciiBottom => AsciiTop + AsciiHeight;
		public double AsciiWidth => asciiCaretGeometry.Rect.Width;

		public bool IsHidden {
			get { return isHidden; }
			set {
				if (isHidden == value)
					return;
				isHidden = value;
				if (isHidden)
					RemoveAdornment();
				else {
					AddAdornment();
					UpdateCaretProperties();
				}
			}
		}
		bool isHidden;

		readonly HexCaretImpl hexCaret;
		readonly HexAdornmentLayer layer;
		readonly VSTC.IClassificationFormatMap classificationFormatMap;
		readonly VSTC.IClassificationType activeCaretClassificationType;
		readonly VSTC.IClassificationType inactiveCaretClassificationType;

		public HexCaretLayer(HexCaretImpl hexCaret, HexAdornmentLayer layer, VSTC.IClassificationFormatMap classificationFormatMap, VSTC.IClassificationTypeRegistryService classificationTypeRegistryService) {
			if (classificationTypeRegistryService == null)
				throw new ArgumentNullException(nameof(classificationTypeRegistryService));
			overwriteMode = true;
			this.hexCaret = hexCaret ?? throw new ArgumentNullException(nameof(hexCaret));
			this.layer = layer ?? throw new ArgumentNullException(nameof(layer));
			this.classificationFormatMap = classificationFormatMap ?? throw new ArgumentNullException(nameof(classificationFormatMap));
			activeCaretClassificationType = classificationTypeRegistryService.GetClassificationType(CTC.ThemeClassificationTypeNames.HexCaret);
			inactiveCaretClassificationType = classificationTypeRegistryService.GetClassificationType(CTC.ThemeClassificationTypeNames.HexInactiveCaret);
			valuesCaretGeometry = new CaretGeometry();
			asciiCaretGeometry = new CaretGeometry();
			layer.HexView.Selection.SelectionChanged += Selection_SelectionChanged;
			layer.HexView.VisualElement.AddHandler(GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(VisualElement_GotKeyboardFocus), true);
			layer.HexView.VisualElement.AddHandler(LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(VisualElement_LostKeyboardFocus), true);
			layer.HexView.VisualElement.IsVisibleChanged += VisualElement_IsVisibleChanged;
			classificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
			AddAdornment();
		}

		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) {
			activeCaretBrush = null;
			activeOverwriteCaretBrush = null;
			inactiveCaretBrush = null;
			inactiveOverwriteCaretBrush = null;
			InvalidateVisual();
		}

		void VisualElement_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			if (!layer.HexView.VisualElement.IsVisible)
				StopTimer();
			else
				UpdateCaretProperties();
		}

		void VisualElement_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
			if (!IsHidden)
				UpdateCaretProperties();
		}

		void VisualElement_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
			layer.Opacity = 0;
			StopTimer();
		}

		void RemoveAdornment() => layer.RemoveAllAdornments();
		void AddAdornment() => layer.AddAdornment(VSTE.AdornmentPositioningBehavior.OwnerControlled, (HexBufferSpan?)null, null, this, null);

		struct SelectionState {
			byte state;

			public SelectionState(HexSelection selection) {
				state = (byte)(selection.IsEmpty ? 1 : 0);
			}

			public bool Equals(SelectionState other) => state == other.state;
		}

		void Selection_SelectionChanged(object sender, EventArgs e) {
			if (!new SelectionState(layer.HexView.Selection).Equals(oldSelectionState)) {
				// Delay this because the caret's position hasn't been updated yet.
				layer.HexView.VisualElement.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(UpdateCaretProperties));
			}
		}
		SelectionState oldSelectionState;

		internal void CaretPositionChanged() => UpdateCaretProperties();
		internal void OnLayoutChanged(HexViewLayoutChangedEventArgs e) => UpdateCaretProperties();

		public void SetImeStarted(bool started) {
			if (imeStarted == started)
				return;
			imeStarted = started;
			UpdateCaretProperties(true);
		}
		bool imeStarted;

		void UpdateCaretProperties() => UpdateCaretProperties(false);
		void UpdateCaretProperties(bool forceInvalidateVisual) {
			if (inUpdateCaretPropertiesCore)
				Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => UpdateCaretProperties(forceInvalidateVisual)));
			else {
				inUpdateCaretPropertiesCore = true;
				try {
					UpdateCaretPropertiesCore(forceInvalidateVisual);
				}
				finally {
					inUpdateCaretPropertiesCore = false;
				}
			}
		}
		bool inUpdateCaretPropertiesCore;

		void UpdateCaretPropertiesCore(bool forceInvalidateVisual) {
			if (layer.HexView.IsClosed)
				return;
			StopTimer();
			var line = hexCaret.ContainingHexViewLine;
			oldSelectionState = new SelectionState(layer.HexView.Selection);
			bool oldDrawCaretShape = drawCaretShape;
			drawCaretShape = line.VisibilityState != VSTF.VisibilityState.Unattached;
			bool drawOverwriteMode = OverwriteMode;

			var valuesCaretRect = GetCaretRect(line, drawOverwriteMode, HexColumnType.Values, line.BufferLine.ValueCells.GetCell(hexCaret.CurrentPosition.ValuePosition.BufferPosition), hexCaret.CurrentPosition.ValuePosition.CellPosition);
			var asciiCaretRect = GetCaretRect(line, drawOverwriteMode, HexColumnType.Ascii, line.BufferLine.AsciiCells.GetCell(hexCaret.CurrentPosition.AsciiPosition.BufferPosition), hexCaret.CurrentPosition.AsciiPosition.CellPosition);

			if (drawCaretShape) {
				if (!imeStarted && layer.HexView.VisualElement.IsKeyboardFocused && layer.HexView.VisualElement.IsVisible)
					StartTimer();
			}
			else
				layer.Opacity = 0;

			var invalidateVisual = valuesCaretGeometry.SetProperties(valuesCaretRect, drawCaretShape, drawOverwriteMode);
			invalidateVisual |= asciiCaretGeometry.SetProperties(asciiCaretRect, drawCaretShape, drawOverwriteMode);
			invalidateVisual |= forceInvalidateVisual || oldDrawCaretShape != drawCaretShape;
			if (invalidateVisual)
				InvalidateVisual();
		}

		static Rect ToRect(Collection<VSTF.TextBounds> collection) {
			double left = double.PositiveInfinity, right = double.NegativeInfinity;
			double top = double.PositiveInfinity, bottom = double.NegativeInfinity;
			foreach (var textBounds in collection) {
				left = Math.Min(left, textBounds.Left);
				right = Math.Max(right, textBounds.Right);
				top = Math.Min(top, textBounds.TextTop);
				bottom = Math.Max(bottom, textBounds.TextBottom);
			}
			bool b = left != double.PositiveInfinity && right != double.NegativeInfinity &&
				top != double.PositiveInfinity && bottom != double.NegativeInfinity;
			if (!b)
				return Rect.Empty;
			if (left > right)
				right = left;
			if (top > bottom)
				bottom = top;
			return new Rect(left, top, right - left, bottom - top);
		}

		Rect GetCaretRect(HexViewLine line, bool drawOverwriteMode, HexColumnType column, HexCell cell, int cellPosition) {
			if (cell == null)
				return new Rect();

			int linePosition = cell.CellSpan.Start + Math.Max(0, Math.Min(cell.CellSpan.Length - 1, cellPosition));
			if (hexCaret.CurrentPosition.ActiveColumn != column) {
				var r = ToRect(line.GetNormalizedTextBounds(cell.CellSpan));
				return new Rect(r.X, r.Bottom - INACTIVE_CARET_HEIGHT, r.Width, INACTIVE_CARET_HEIGHT);
			}
			else if (drawOverwriteMode) {
				var textBounds = line.GetExtendedCharacterBounds(linePosition);
				var left = textBounds.Left;
				var top = line.TextTop;
				var width = textBounds.Width;
				var height = line.TextHeight;
				return new Rect(left, top, width, height);
			}
			else {
				double left;
				if (linePosition != 0 && linePosition <= line.BufferLine.Text.Length)
					left = line.GetExtendedCharacterBounds(linePosition - 1).Trailing;
				else
					left = line.GetExtendedCharacterBounds(linePosition).Leading;
				var top = line.TextTop;
				var width = SystemParameters.CaretWidth;
				var height = line.TextHeight;
				return new Rect(left, top, width, height);
			}
		}

		void StopTimer() {
			dispatcherTimer?.Stop();
			dispatcherTimer = null;
		}

		void StartTimer() {
			if (dispatcherTimer != null)
				throw new InvalidOperationException();
			// Make sure the caret doesn't blink when it's moved
			layer.Opacity = 1;
			var blinkTimeMs = TE.SystemTextCaret.BlinkTimeMilliSeconds;
			if (blinkTimeMs > 0)
				dispatcherTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(blinkTimeMs), DispatcherPriority.Background, OnToggleBlink, layer.HexView.VisualElement.Dispatcher);
		}

		void OnToggleBlink(object sender, EventArgs e) =>
			layer.Opacity = layer.Opacity == 0 ? 1 : 0;

		protected override void OnRender(DrawingContext drawingContext) {
			base.OnRender(drawingContext);
			Debug.Assert((activeCaretBrush == null) == (activeOverwriteCaretBrush == null));
			Debug.Assert((inactiveCaretBrush == null) == (inactiveOverwriteCaretBrush == null));
			Debug.Assert((activeCaretBrush == null) == (inactiveCaretBrush == null));
			if (activeCaretBrush == null) {
				InitializeBrushes(out activeCaretBrush, out activeOverwriteCaretBrush, activeCaretClassificationType);
				InitializeBrushes(out inactiveCaretBrush, out inactiveOverwriteCaretBrush, inactiveCaretClassificationType);
			}
			DrawCaret(drawingContext, valuesCaretGeometry, ActiveColumn == HexColumnType.Values);
			DrawCaret(drawingContext, asciiCaretGeometry, ActiveColumn == HexColumnType.Ascii);
		}

		void DrawCaret(DrawingContext drawingContext, CaretGeometry caretGeometry, bool isActive) {
			var geo = caretGeometry.Geometry;
			if (geo == null)
				return;
			var caretBrush = isActive ? activeCaretBrush : inactiveCaretBrush;
			var overwriteCaretBrush = isActive ? activeOverwriteCaretBrush : inactiveOverwriteCaretBrush;
			drawingContext.DrawGeometry(caretGeometry.IsOverwriteMode ? overwriteCaretBrush : caretBrush, null, geo);
		}

		void InitializeBrushes(out Brush brush, out Brush overwriteBrush, VSTC.IClassificationType classificationType) {
			var props = classificationFormatMap.GetTextProperties(classificationType);
			if (!props.BackgroundBrushEmpty)
				brush = props.BackgroundBrush;
			else {
				Debug.Assert(!classificationFormatMap.DefaultTextProperties.ForegroundBrushEmpty);
				brush = classificationFormatMap.DefaultTextProperties.ForegroundBrush;
				if (classificationFormatMap.DefaultTextProperties.ForegroundBrushEmpty)
					brush = Brushes.Black;
			}
			if (brush.CanFreeze)
				brush.Freeze();

			overwriteBrush = brush.Clone();
			overwriteBrush.Opacity = 0.5;
			if (overwriteBrush.CanFreeze)
				overwriteBrush.Freeze();
		}

		sealed class CaretGeometry {
			public bool IsOverwriteMode { get; private set; }
			public Rect Rect => rect;

			public Geometry Geometry {
				get {
					if (!visible) {
						Debug.Assert(geometry == null);
						return null;
					}
					if (geometry == null) {
						var geo = new RectangleGeometry(rect);
						geo.Freeze();
						geometry = geo;
					}
					return geometry;
				}
			}
			Geometry geometry;
			Rect rect;
			bool visible;

			public CaretGeometry() {
				rect = Rect.Empty;
				IsOverwriteMode = false;
			}

			public bool SetProperties(Rect rect, bool visible, bool overwriteMode) {
				var samePos = this.rect == rect && this.visible == visible;
				if (samePos && IsOverwriteMode == overwriteMode)
					return false;

				if (!samePos)
					geometry = null;
				this.rect = rect;
				this.visible = visible;
				IsOverwriteMode = overwriteMode;
				return true;
			}
		}

		public void Dispose() {
			StopTimer();
			layer.HexView.Selection.SelectionChanged -= Selection_SelectionChanged;
			layer.HexView.VisualElement.GotKeyboardFocus -= VisualElement_GotKeyboardFocus;
			layer.HexView.VisualElement.LostKeyboardFocus -= VisualElement_LostKeyboardFocus;
			layer.HexView.VisualElement.IsVisibleChanged -= VisualElement_IsVisibleChanged;
			classificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
		}
	}
}

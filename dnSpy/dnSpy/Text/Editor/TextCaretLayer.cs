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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Editor {
	sealed class TextCaretLayer : UIElement {
		public double Left => left;
		public double Right => Left + Width;
		public double Bottom => Top + Height;
		public double Width => width;
		double left, top, width, height;
		bool drawCaretShape;
		bool overwriteMode;
		DispatcherTimer? dispatcherTimer;
		readonly CaretGeometry caretGeometry;
		Brush? caretBrush;
		Brush? overwriteCaretBrush;

		public bool OverwriteMode {
			get => overwriteMode;
			set {
				if (overwriteMode != value) {
					overwriteMode = value;
					UpdateCaretProperties();
				}
			}
		}

		public double Top {
			get {
				if (!drawCaretShape)
					throw new InvalidOperationException();
				return top;
			}
		}

		public double Height {
			get {
				if (!drawCaretShape)
					throw new InvalidOperationException();
				return height;
			}
		}

		public bool IsHidden {
			get => isHidden;
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

		readonly TextCaret textCaret;
		readonly IAdornmentLayer layer;
		readonly IClassificationFormatMap classificationFormatMap;

		public TextCaretLayer(TextCaret textCaret, IAdornmentLayer layer, IClassificationFormatMap classificationFormatMap) {
			this.textCaret = textCaret ?? throw new ArgumentNullException(nameof(textCaret));
			this.layer = layer ?? throw new ArgumentNullException(nameof(layer));
			this.classificationFormatMap = classificationFormatMap ?? throw new ArgumentNullException(nameof(classificationFormatMap));
			caretGeometry = new CaretGeometry();
			layer.TextView.LayoutChanged += TextView_LayoutChanged;
			layer.TextView.Selection.SelectionChanged += Selection_SelectionChanged;
			layer.TextView.VisualElement.AddHandler(GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(VisualElement_GotKeyboardFocus), true);
			layer.TextView.VisualElement.AddHandler(LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(VisualElement_LostKeyboardFocus), true);
			layer.TextView.VisualElement.IsVisibleChanged += VisualElement_IsVisibleChanged;
			classificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
			AddAdornment();
		}

		void ClassificationFormatMap_ClassificationFormatMappingChanged(object? sender, EventArgs e) {
			caretBrush = null;
			overwriteCaretBrush = null;
			InvalidateVisual();
		}

		void VisualElement_IsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e) {
			if (!layer.TextView.VisualElement.IsVisible)
				StopTimer();
			else
				UpdateCaretProperties();
		}

		void VisualElement_GotKeyboardFocus(object? sender, KeyboardFocusChangedEventArgs e) {
			if (!IsHidden)
				UpdateCaretProperties();
		}

		void VisualElement_LostKeyboardFocus(object? sender, KeyboardFocusChangedEventArgs e) {
			layer.Opacity = 0;
			StopTimer();
		}

		void RemoveAdornment() => layer.RemoveAllAdornments();
		void AddAdornment() => layer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, null, this, null);

		readonly struct SelectionState {
			readonly byte state;

			public SelectionState(ITextSelection selection) => state = (byte)((selection.IsEmpty ? 1 : 0) | (selection.Mode == TextSelectionMode.Box ? 2 : 0));

			public bool Equals(SelectionState other) => state == other.state;
		}

		void Selection_SelectionChanged(object? sender, EventArgs e) {
			if (!new SelectionState(layer.TextView.Selection).Equals(oldSelectionState)) {
				// Delay this because the caret's position hasn't been updated yet.
				layer.TextView.VisualElement.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(UpdateCaretProperties));
			}
		}
		SelectionState oldSelectionState;

		internal void CaretPositionChanged() => UpdateCaretProperties();
		void TextView_LayoutChanged(object? sender, TextViewLayoutChangedEventArgs e) => UpdateCaretProperties();

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
			if (layer.TextView.IsClosed)
				return;
			StopTimer();
			var line = textCaret.ContainingTextViewLine;
			oldSelectionState = new SelectionState(layer.TextView.Selection);
			bool oldDrawCaretShape = drawCaretShape;
			drawCaretShape = line.VisibilityState != VisibilityState.Unattached;
			bool drawOverwriteMode = OverwriteMode && textCaret.CurrentPosition.Position < line.End && layer.TextView.Selection.IsEmpty;

			var vpos = textCaret.CurrentPosition;
			if (drawOverwriteMode) {
				var textBounds = line.GetExtendedCharacterBounds(vpos);
				left = textBounds.Left;
				width = textBounds.Width;
			}
			else {
				if (vpos.Position != line.Start && !vpos.IsInVirtualSpace)
					left = line.GetExtendedCharacterBounds(vpos.Position - 1).Trailing;
				else
					left = line.GetExtendedCharacterBounds(vpos).Leading;
				width = SystemParameters.CaretWidth;
			}

			height = line.TextHeight;
			if (drawCaretShape) {
				if (!imeStarted && layer.TextView.VisualElement.IsKeyboardFocused && layer.TextView.VisualElement.IsVisible)
					StartTimer();
				top = line.TextTop;
				Canvas.SetLeft(this, left);
				Canvas.SetTop(this, top);
			}
			else {
				layer.Opacity = 0;
				top = double.NaN;
			}

			var invalidateVisual = caretGeometry.SetProperties(width, height, drawOverwriteMode);
			invalidateVisual |= forceInvalidateVisual || oldDrawCaretShape != drawCaretShape;
			if (invalidateVisual)
				InvalidateVisual();
		}

		void StopTimer() {
			dispatcherTimer?.Stop();
			dispatcherTimer = null;
		}

		void StartTimer() {
			if (!(dispatcherTimer is null))
				throw new InvalidOperationException();
			// Make sure the caret doesn't blink when it's moved
			layer.Opacity = 1;
			var blinkTimeMs = SystemTextCaret.BlinkTimeMilliSeconds;
			if (blinkTimeMs > 0)
				dispatcherTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(blinkTimeMs), DispatcherPriority.Background, OnToggleBlink, layer.TextView.VisualElement.Dispatcher);
		}

		void OnToggleBlink(object? sender, EventArgs e) =>
			layer.Opacity = layer.Opacity == 0 ? 1 : 0;

		protected override void OnRender(DrawingContext drawingContext) {
			base.OnRender(drawingContext);
			Debug2.Assert((overwriteCaretBrush is null) == (caretBrush is null));
			if (caretBrush is null) {
				caretBrush = classificationFormatMap.DefaultTextProperties.ForegroundBrush;
				Debug.Assert(!classificationFormatMap.DefaultTextProperties.ForegroundBrushEmpty);
				if (classificationFormatMap.DefaultTextProperties.ForegroundBrushEmpty)
					caretBrush = Brushes.Black;
				caretBrush = caretBrush.Clone();
				overwriteCaretBrush = caretBrush.Clone();
				overwriteCaretBrush.Opacity = 0.5;
				if (caretBrush.CanFreeze)
					caretBrush.Freeze();
				if (overwriteCaretBrush.CanFreeze)
					overwriteCaretBrush.Freeze();
			}
			drawingContext.DrawGeometry(caretGeometry.IsOverwriteMode ? overwriteCaretBrush : caretBrush, null, caretGeometry.Geometry);
		}

		sealed class CaretGeometry {
			public bool IsOverwriteMode { get; private set; }

			public Geometry Geometry {
				get {
					if (geometry is null) {
						var geo = new RectangleGeometry(new Rect(0, 0, width, height));
						geo.Freeze();
						geometry = geo;
					}
					return geometry;
				}
			}
			Geometry? geometry;
			double width, height;

			public CaretGeometry() {
				width = double.NaN;
				height = double.NaN;
				IsOverwriteMode = false;
			}

			public bool SetProperties(double width, double height, bool overwriteMode) {
				var sameWidthHeight = this.width == width && this.height == height;
				if (sameWidthHeight && IsOverwriteMode == overwriteMode)
					return false;

				if (!sameWidthHeight)
					geometry = null;
				this.width = width;
				this.height = height;
				IsOverwriteMode = overwriteMode;
				return true;
			}
		}

		public void Dispose() {
			StopTimer();
			layer.TextView.LayoutChanged -= TextView_LayoutChanged;
			layer.TextView.Selection.SelectionChanged -= Selection_SelectionChanged;
			layer.TextView.VisualElement.GotKeyboardFocus -= VisualElement_GotKeyboardFocus;
			layer.TextView.VisualElement.LostKeyboardFocus -= VisualElement_LostKeyboardFocus;
			layer.TextView.VisualElement.IsVisibleChanged -= VisualElement_IsVisibleChanged;
			classificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
		}
	}
}

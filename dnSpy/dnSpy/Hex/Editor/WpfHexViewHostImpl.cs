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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using dnSpy.Contracts.Hex.Operations;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Hex.Editor {
	sealed class WpfHexViewHostImpl : WpfHexViewHost {
		public override bool IsClosed => isClosed;
		bool isClosed;
		public override WpfHexView HexView { get; }
		public override event EventHandler Closed;
		public override Control HostControl => contentControl;

		readonly ContentControl contentControl;
		readonly WpfHexViewMargin[] containerMargins;
		readonly Grid grid;
		readonly HexEditorOperationsFactoryService editorOperationsFactoryService;

		public WpfHexViewHostImpl(WpfHexViewMarginProviderCollectionProvider wpfHexViewMarginProviderCollectionProvider, WpfHexView wpfHexView, HexEditorOperationsFactoryService editorOperationsFactoryService, bool setFocus) {
			if (wpfHexViewMarginProviderCollectionProvider == null)
				throw new ArgumentNullException(nameof(wpfHexViewMarginProviderCollectionProvider));
			contentControl = new ContentControl();
			this.editorOperationsFactoryService = editorOperationsFactoryService ?? throw new ArgumentNullException(nameof(editorOperationsFactoryService));
			grid = CreateGrid();
			HexView = wpfHexView ?? throw new ArgumentNullException(nameof(wpfHexView));
			contentControl.Focusable = false;
			contentControl.Content = grid;
			contentControl.MouseWheel += ContentControl_MouseWheel;

			UpdateBackground();
			HexView.BackgroundBrushChanged += HexView_BackgroundBrushChanged;

			containerMargins = new WpfHexViewMargin[5];
			containerMargins[0] = CreateContainerMargin(wpfHexViewMarginProviderCollectionProvider, PredefinedHexMarginNames.Top, true, 0, 0, 3);
			containerMargins[1] = CreateContainerMargin(wpfHexViewMarginProviderCollectionProvider, PredefinedHexMarginNames.Bottom, true, 2, 0, 2);
			containerMargins[2] = CreateContainerMargin(wpfHexViewMarginProviderCollectionProvider, PredefinedHexMarginNames.BottomRightCorner, true, 2, 2, 1);
			containerMargins[3] = CreateContainerMargin(wpfHexViewMarginProviderCollectionProvider, PredefinedHexMarginNames.Left, false, 1, 0, 1);
			containerMargins[4] = CreateContainerMargin(wpfHexViewMarginProviderCollectionProvider, PredefinedHexMarginNames.Right, false, 1, 2, 1);
			Add(HexView.VisualElement, 1, 1, 1);
			Debug.Assert(!containerMargins.Any(a => a == null));

			if (setFocus) {
				contentControl.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
					if (!HexView.IsClosed)
						HexView.VisualElement.Focus();
				}));
			}
		}

		WpfHexViewMargin CreateContainerMargin(WpfHexViewMarginProviderCollectionProvider wpfHexViewMarginProviderCollectionProvider, string name, bool isHorizontal, int row, int column, int columnSpan) {
			var margin = new WpfHexViewContainerMargin(wpfHexViewMarginProviderCollectionProvider, this, name, isHorizontal);
			Add(margin.VisualElement, row, column, columnSpan);
			margin.VisualElement.AddHandler(UIElement.MouseDownEvent, new MouseButtonEventHandler(Margin_VisualElement_MouseDown), true);
			return margin;
		}

		void Margin_VisualElement_MouseDown(object sender, MouseButtonEventArgs e) {
			if (!contentControl.IsKeyboardFocusWithin)
				HexView.VisualElement.Focus();
		}

		void Add(UIElement elem, int row, int column, int columnSpan) {
			grid.Children.Add(elem);
			if (row != 0)
				Grid.SetRow(elem, row);
			if (column != 0)
				Grid.SetColumn(elem, column);
			if (columnSpan != 1)
				Grid.SetColumnSpan(elem, columnSpan);
		}

		void HexView_BackgroundBrushChanged(object sender, VSTE.BackgroundBrushChangedEventArgs e) => UpdateBackground();
		void UpdateBackground() => grid.Background = HexView.Background;

		static Grid CreateGrid() {
			var grid = new Grid();

			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength() });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength() });

			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength() });
			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength() });

			return grid;
		}

		public override void Close() {
			if (IsClosed)
				throw new InvalidOperationException();
			HexView.Close();
			isClosed = true;
			Closed?.Invoke(this, EventArgs.Empty);
			HexView.BackgroundBrushChanged -= HexView_BackgroundBrushChanged;
			foreach (var margin in containerMargins) {
				margin.VisualElement.MouseDown -= Margin_VisualElement_MouseDown;
				margin.Dispose();
			}
		}

		public override WpfHexViewMargin GetHexViewMargin(string marginName) {
			foreach (var margin in containerMargins) {
				if (margin == null)
					continue;
				var result = margin.GetHexViewMargin(marginName) as WpfHexViewMargin;
				if (result != null)
					return result;
			}
			return null;
		}

		static int GetScrollWheelLines() {
			if (!SystemParameters.IsMouseWheelPresent)
				return 1;
			return SystemParameters.WheelScrollLines;
		}

		void ContentControl_MouseWheel(object sender, MouseWheelEventArgs e) {
			if (!IsClosed && !e.Handled) {
				e.Handled = true;
				if (e.Delta == 0)
					return;

				if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && CanMouseWheelZoom) {
					var editorOperations = editorOperationsFactoryService.GetEditorOperations(HexView);
					if (e.Delta > 0)
						editorOperations.ZoomIn();
					else
						editorOperations.ZoomOut();
				}
				else {
					int lines = GetScrollWheelLines();
					var direction = e.Delta < 0 ? VSTE.ScrollDirection.Down : VSTE.ScrollDirection.Up;
					if (lines >= 0)
						HexView.ViewScroller.ScrollViewportVerticallyByLines(direction, lines);
					else
						HexView.ViewScroller.ScrollViewportVerticallyByPage(direction);
				}
			}
		}

		bool CanMouseWheelZoom =>
			HexView.Options.IsMouseWheelZoomEnabled() &&
			HexView.Roles.Contains(PredefinedHexViewRoles.Zoomable);
	}
}

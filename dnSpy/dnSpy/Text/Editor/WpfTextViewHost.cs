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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;

namespace dnSpy.Text.Editor {
	sealed class WpfTextViewHost : ContentControl, IDsWpfTextViewHost {
		public bool IsClosed { get; set; }
		IWpfTextView IWpfTextViewHost.TextView => TextView;
		public IDsWpfTextView TextView { get; }
		public event EventHandler Closed;
		public Control HostControl => this;

		readonly IWpfTextViewMargin[] containerMargins;
		readonly Grid grid;
		readonly IEditorOperationsFactoryService editorOperationsFactoryService;

		public WpfTextViewHost(IWpfTextViewMarginProviderCollectionProvider wpfTextViewMarginProviderCollectionProvider, IDsWpfTextView wpfTextView, IEditorOperationsFactoryService editorOperationsFactoryService, bool setFocus) {
			if (wpfTextViewMarginProviderCollectionProvider == null)
				throw new ArgumentNullException(nameof(wpfTextViewMarginProviderCollectionProvider));
			this.editorOperationsFactoryService = editorOperationsFactoryService ?? throw new ArgumentNullException(nameof(editorOperationsFactoryService));
			grid = CreateGrid();
			TextView = wpfTextView ?? throw new ArgumentNullException(nameof(wpfTextView));
			Focusable = false;
			Content = grid;

			UpdateBackground();
			TextView.BackgroundBrushChanged += TextView_BackgroundBrushChanged;

			containerMargins = new IWpfTextViewMargin[5];
			containerMargins[0] = CreateContainerMargin(wpfTextViewMarginProviderCollectionProvider, PredefinedMarginNames.Top, true, 0, 0, 3);
			containerMargins[1] = CreateContainerMargin(wpfTextViewMarginProviderCollectionProvider, PredefinedMarginNames.Bottom, true, 2, 0, 2);
			containerMargins[2] = CreateContainerMargin(wpfTextViewMarginProviderCollectionProvider, PredefinedMarginNames.BottomRightCorner, true, 2, 2, 1);
			containerMargins[3] = CreateContainerMargin(wpfTextViewMarginProviderCollectionProvider, PredefinedMarginNames.Left, false, 1, 0, 1);
			containerMargins[4] = CreateContainerMargin(wpfTextViewMarginProviderCollectionProvider, PredefinedMarginNames.Right, false, 1, 2, 1);
			Add(TextView.VisualElement, 1, 1, 1);
			Debug.Assert(!containerMargins.Any(a => a == null));

			if (setFocus) {
				Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
					if (!TextView.IsClosed)
						TextView.VisualElement.Focus();
				}));
			}
		}

		IWpfTextViewMargin CreateContainerMargin(IWpfTextViewMarginProviderCollectionProvider wpfTextViewMarginProviderCollectionProvider, string name, bool isHorizontal, int row, int column, int columnSpan) {
			var margin = new WpfTextViewContainerMargin(wpfTextViewMarginProviderCollectionProvider, this, name, isHorizontal);
			Add(margin.VisualElement, row, column, columnSpan);
			margin.VisualElement.AddHandler(MouseDownEvent, new MouseButtonEventHandler(Margin_VisualElement_MouseDown), true);
			return margin;
		}

		void Margin_VisualElement_MouseDown(object sender, MouseButtonEventArgs e) {
			if (!IsKeyboardFocusWithin)
				TextView.VisualElement.Focus();
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

		void TextView_BackgroundBrushChanged(object sender, BackgroundBrushChangedEventArgs e) => UpdateBackground();
		void UpdateBackground() => grid.Background = TextView.Background;

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

		public void Close() {
			if (IsClosed)
				throw new InvalidOperationException();
			TextView.Close();
			IsClosed = true;
			Closed?.Invoke(this, EventArgs.Empty);
			TextView.BackgroundBrushChanged -= TextView_BackgroundBrushChanged;
			foreach (var margin in containerMargins) {
				margin.VisualElement.MouseDown -= Margin_VisualElement_MouseDown;
				margin.Dispose();
			}
		}

		public IWpfTextViewMargin GetTextViewMargin(string marginName) {
			foreach (var margin in containerMargins) {
				if (margin == null)
					continue;
				if (margin.GetTextViewMargin(marginName) is IWpfTextViewMargin result)
					return result;
			}
			return null;
		}

		static int GetScrollWheelLines() {
			if (!SystemParameters.IsMouseWheelPresent)
				return 1;
			return SystemParameters.WheelScrollLines;
		}

		protected override void OnMouseWheel(MouseWheelEventArgs e) {
			if (!IsClosed && !e.Handled) {
				e.Handled = true;
				if (e.Delta == 0)
					return;

				if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && CanMouseWheelZoom) {
					var editorOperations = editorOperationsFactoryService.GetEditorOperations(TextView);
					if (e.Delta > 0)
						editorOperations.ZoomIn();
					else
						editorOperations.ZoomOut();
				}
				else {
					int lines = GetScrollWheelLines();
					var direction = e.Delta < 0 ? ScrollDirection.Down : ScrollDirection.Up;
					if (lines >= 0)
						TextView.ViewScroller.ScrollViewportVerticallyByLines(direction, lines);
					else
						TextView.ViewScroller.ScrollViewportVerticallyByPage(direction);
				}
			}
			else
				base.OnMouseWheel(e);
		}

		bool CanMouseWheelZoom =>
			TextView.Options.IsMouseWheelZoomEnabled() &&
			TextView.Roles.Contains(PredefinedTextViewRoles.Zoomable) &&
			TextView.TextDataModel.ContentType.IsOfType(ContentTypes.Text);
	}
}

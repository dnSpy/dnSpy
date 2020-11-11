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
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Hex.Editor {
	class WpfHexViewContainerMargin : WpfHexViewMargin {
		public override bool Enabled => true;
		public override double MarginSize => isHorizontal ? grid.ActualHeight : grid.ActualWidth;
		public override FrameworkElement VisualElement => grid;
		protected Grid Grid => grid;

		readonly Grid grid;
		readonly WpfHexViewMarginProviderCollection wpfHexViewMarginProviderCollection;
		readonly string name;
		readonly bool isHorizontal;
		WpfHexViewMarginInfo[] margins;

		public WpfHexViewContainerMargin(WpfHexViewMarginProviderCollectionProvider wpfHexViewMarginProviderCollectionProvider, WpfHexViewHost wpfHexViewHost, string name, bool isHorizontal) {
			if (wpfHexViewMarginProviderCollectionProvider is null)
				throw new ArgumentNullException(nameof(wpfHexViewMarginProviderCollectionProvider));
			if (wpfHexViewHost is null)
				throw new ArgumentNullException(nameof(wpfHexViewHost));
			grid = new Grid();
			this.name = name ?? throw new ArgumentNullException(nameof(name));
			this.isHorizontal = isHorizontal;
			margins = Array.Empty<WpfHexViewMarginInfo>();
			wpfHexViewMarginProviderCollection = wpfHexViewMarginProviderCollectionProvider.Create(wpfHexViewHost, this, name);
			wpfHexViewMarginProviderCollection.MarginsChanged += WpfHexViewMarginProviderCollection_MarginsChanged;
			UpdateMarginChildren();
		}

		void WpfHexViewMarginProviderCollection_MarginsChanged(object? sender, EventArgs e) => UpdateMarginChildren();

		void UpdateMarginChildren() {
			margins = wpfHexViewMarginProviderCollection.Margins;
			if (!isHorizontal) {
				grid.ColumnDefinitions.Clear();
				grid.Children.Clear();
				for (int i = 0; i < margins.Length; i++) {
					var info = margins[i];
					var elem = info.Margin.VisualElement;
					Grid.SetRow(elem, 0);
					Grid.SetRowSpan(elem, 1);
					Grid.SetColumn(elem, i);
					Grid.SetColumnSpan(elem, 1);
					grid.Children.Add(elem);
					grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(info.Metadata.GridCellLength, info.Metadata.GridUnitType) });
				}
			}
			else {
				grid.RowDefinitions.Clear();
				grid.Children.Clear();
				for (int i = 0; i < margins.Length; i++) {
					var info = margins[i];
					var elem = info.Margin.VisualElement;
					Grid.SetRow(elem, i);
					Grid.SetRowSpan(elem, 1);
					Grid.SetColumn(elem, 0);
					Grid.SetColumnSpan(elem, 1);
					grid.Children.Add(elem);
					grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(info.Metadata.GridCellLength, info.Metadata.GridUnitType) });
				}
			}
		}

		public override HexViewMargin? GetHexViewMargin(string marginName) {
			if (StringComparer.OrdinalIgnoreCase.Equals(marginName, name))
				return this;

			foreach (var info in margins) {
				var margin = info.Margin.GetHexViewMargin(marginName);
				if (margin is not null)
					return margin;
			}

			return null;
		}

		protected override void DisposeCore() {
			wpfHexViewMarginProviderCollection.Dispose();
			foreach (var info in margins)
				info.Margin.Dispose();
			margins = Array.Empty<WpfHexViewMarginInfo>();
			grid.ColumnDefinitions.Clear();
			grid.RowDefinitions.Clear();
			grid.Children.Clear();
		}
	}
}

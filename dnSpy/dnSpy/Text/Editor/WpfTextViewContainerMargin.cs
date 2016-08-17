/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	class WpfTextViewContainerMargin : Grid, IWpfTextViewMargin {
		public bool Enabled => true;
		public double MarginSize => isHorizontal ? ActualHeight : ActualWidth;
		public FrameworkElement VisualElement => this;

		readonly IWpfTextViewMarginProviderCollection wpfTextViewMarginProviderCollection;
		readonly string name;
		readonly bool isHorizontal;
		WpfTextViewMarginInfo[] margins;

		public WpfTextViewContainerMargin(IWpfTextViewMarginProviderCollectionProvider wpfTextViewMarginProviderCollectionProvider, IWpfTextViewHost wpfTextViewHost, string name, bool isHorizontal) {
			if (wpfTextViewMarginProviderCollectionProvider == null)
				throw new ArgumentNullException(nameof(wpfTextViewMarginProviderCollectionProvider));
			if (wpfTextViewHost == null)
				throw new ArgumentNullException(nameof(wpfTextViewHost));
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			this.name = name;
			this.isHorizontal = isHorizontal;
			this.margins = Array.Empty<WpfTextViewMarginInfo>();
			this.wpfTextViewMarginProviderCollection = wpfTextViewMarginProviderCollectionProvider.Create(wpfTextViewHost, this, name);
			this.wpfTextViewMarginProviderCollection.MarginsChanged += WpfTextViewMarginProviderCollection_MarginsChanged;
			UpdateMarginChildren();
		}

		void WpfTextViewMarginProviderCollection_MarginsChanged(object sender, EventArgs e) => UpdateMarginChildren();

		void UpdateMarginChildren() {
			margins = wpfTextViewMarginProviderCollection.Margins;
			if (!isHorizontal) {
				ColumnDefinitions.Clear();
				Children.Clear();
				for (int i = 0; i < margins.Length; i++) {
					var info = margins[i];
					var elem = info.Margin.VisualElement;
					SetRow(elem, 0);
					SetRowSpan(elem, 1);
					SetColumn(elem, i);
					SetColumnSpan(elem, 1);
					Children.Add(elem);
					ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(info.Metadata.GridCellLength, info.Metadata.GridUnitType) });
				}
			}
			else {
				RowDefinitions.Clear();
				Children.Clear();
				for (int i = 0; i < margins.Length; i++) {
					var info = margins[i];
					var elem = info.Margin.VisualElement;
					SetRow(elem, i);
					SetRowSpan(elem, 1);
					SetColumn(elem, 0);
					SetColumnSpan(elem, 1);
					Children.Add(elem);
					RowDefinitions.Add(new RowDefinition { Height = new GridLength(info.Metadata.GridCellLength, info.Metadata.GridUnitType) });
				}
			}
		}

		public ITextViewMargin GetTextViewMargin(string marginName) {
			if (StringComparer.OrdinalIgnoreCase.Equals(marginName, name))
				return this;

			foreach (var info in margins) {
				var margin = info.Margin.GetTextViewMargin(marginName);
				if (margin != null)
					return margin;
			}

			return null;
		}

		protected virtual void DisposeInternal() { }

		public void Dispose() {
			DisposeInternal();
			wpfTextViewMarginProviderCollection.Dispose();
			foreach (var info in margins)
				info.Margin.Dispose();
			margins = Array.Empty<WpfTextViewMarginInfo>();
			ColumnDefinitions.Clear();
			RowDefinitions.Clear();
			Children.Clear();
		}
	}
}

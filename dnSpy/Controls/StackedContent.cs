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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace dnSpy.Controls {
	interface IStackedContent {	// Needed by IStackedContentChild since the interface doesn't know about TChild
	}
	sealed class StackedContent<TChild> : IStackedContent, IStackedContentChild where TChild : class, IStackedContentChild {
		public TChild this[int index] {
			get { return children[index].Child; }
		}

		public int Count {
			get { return children.Count; }
		}

		sealed class ChildInfo {
			public TChild Child;
			public GridLength GridLength;

			public ChildInfo(TChild child, GridLength? gridLength) {
				this.Child = child;
				this.GridLength = gridLength ?? new GridLength(1, GridUnitType.Star);
			}
		}

		public TChild[] Children {
			get { return children.Select(a => a.Child).ToArray(); }
		}
		readonly List<ChildInfo> children;

		public bool IsHorizontal {
			get { return isHorizontal; }
			set {
				if (isHorizontal != value) {
					isHorizontal = value;
					UpdateGrid();
				}
			}
		}
		bool isHorizontal;

		IStackedContent IStackedContentChild.StackedContent { get; set; }

		public object UIObject {
			get { return grid; }
		}
		readonly Grid grid;

		public StackedContent()
			: this(true) {
		}

		public StackedContent(bool isHorizontal) {
			this.children = new List<ChildInfo>();
			this.grid = new Grid();
			this.grid.SetResourceReference(FrameworkElement.StyleProperty, "StackedContentGridStyle");
			this.isHorizontal = isHorizontal;
			UpdateGrid();
		}

		public void AddChild(TChild child, GridLength? gridLength = null, int index = -1) {
			Debug.Assert(child.StackedContent == null);
			if ((uint)index <= (uint)children.Count)
				children.Insert(index, new ChildInfo(child, gridLength));
			else
				children.Add(new ChildInfo(child, gridLength));
			child.StackedContent = this;
			UpdateGrid();
		}

		public void Remove(TChild child) {
			if (child == null)
				throw new ArgumentNullException();
			int index = IndexOf(child);
			Debug.Assert(index >= 0);
			if (index >= 0) {
				var info = children[index];
				children.RemoveAt(index);
				Debug.Assert(info.Child.StackedContent != null);
				info.Child.StackedContent = null;
				UpdateGrid();
			}
		}

		void UpdateGrid() {
			UpdateGrid(IsHorizontal);
		}

		void UpdateGrid(bool horizontal) {
			grid.Children.Clear();
			grid.ColumnDefinitions.Clear();
			grid.RowDefinitions.Clear();

			// Make sure the horizontal grid splitters can resize the content
			double d = 0.0001;
			bool needSplitter = false;
			if (!horizontal) {
				grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
				int rowCol = 0;
				foreach (var info in children) {
					if (needSplitter && !info.GridLength.IsAuto) {
						var gridSplitter = new GridSplitter();
						Panel.SetZIndex(gridSplitter, 1);
						grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(3, GridUnitType.Pixel) });
						gridSplitter.SetValue(Grid.RowProperty, rowCol);
						gridSplitter.Margin = new Thickness(0, -5, 0, -5);
						gridSplitter.BorderThickness = new Thickness(0, 5, 0, 5);
						gridSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
						gridSplitter.VerticalAlignment = VerticalAlignment.Center;
						gridSplitter.Focusable = false;
						gridSplitter.BorderBrush = Brushes.Transparent;
						grid.Children.Add(gridSplitter);
						rowCol++;
					}

					grid.RowDefinitions.Add(new RowDefinition() { Height = GetGridLength(info.GridLength, -d) });
					var uiel = GetUIElement(info.Child);
					uiel.SetValue(Grid.RowProperty, rowCol);
					uiel.ClearValue(Grid.ColumnProperty);
					grid.Children.Add(uiel);
					rowCol++;
					d = -d;
					needSplitter = !info.GridLength.IsAuto;
				}
			}
			else {
				grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
				int rowCol = 0;
				foreach (var info in children) {
					if (needSplitter && !info.GridLength.IsAuto) {
						var gridSplitter = new GridSplitter();
						Panel.SetZIndex(gridSplitter, 1);
						grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(3, GridUnitType.Pixel) });
						gridSplitter.SetValue(Grid.ColumnProperty, rowCol);
						gridSplitter.Margin = new Thickness(-5, 0, -5, 0);
						gridSplitter.BorderThickness = new Thickness(5, 0, 5, 0);
						gridSplitter.HorizontalAlignment = HorizontalAlignment.Center;
						gridSplitter.VerticalAlignment = VerticalAlignment.Stretch;
						gridSplitter.Focusable = false;
						gridSplitter.BorderBrush = Brushes.Transparent;
						grid.Children.Add(gridSplitter);
						rowCol++;
					}

					grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GetGridLength(info.GridLength, -d) });
					var uiel = GetUIElement(info.Child);
					uiel.ClearValue(Grid.RowProperty);
					uiel.SetValue(Grid.ColumnProperty, rowCol);
					grid.Children.Add(uiel);
					rowCol++;
					d = -d;
					needSplitter = !info.GridLength.IsAuto;
				}
			}
		}

		static GridLength GetGridLength(GridLength len, double d) {
			if (len.IsStar && len.Value == 1)
				return new GridLength(1 + d, GridUnitType.Star);
			return len;
		}

		static UIElement GetUIElement(TChild child) {
			var obj = child.UIObject;
			var uiel = obj as UIElement;
			if (uiel == null)
				uiel = new ContentPresenter { Content = obj };
			return uiel;
		}

		public int IndexOf(TChild child) {
			for (int i = 0; i < children.Count; i++) {
				if (children[i].Child == child)
					return i;
			}
			return -1;
		}

		public bool Contains(TChild child) {
			return IndexOf(child) >= 0;
		}

		public void SwapChildren(int index1, int index2) {
			var tmp1 = children[index1];
			children[index1] = children[index2];
			children[index2] = tmp1;

			// Reset sizes
			UpdateGrid();
		}
	}
}

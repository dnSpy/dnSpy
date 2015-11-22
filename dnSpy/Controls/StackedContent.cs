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

	struct GridChildLength {
		public GridLength? GridLength;
		public double? MinLength;
		public double? MaxLength;

		public GridChildLength(GridLength length, double? min = null, double? max = null) {
			this.GridLength = length;
			this.MinLength = min;
			this.MaxLength = max;
		}
	}

	sealed class StackedContentChildInfo {
		public GridChildLength Horizontal;
		public GridChildLength Vertical;

		public StackedContentChildInfo Clone() {
			return new StackedContentChildInfo {
				Horizontal = Horizontal,
				Vertical = Vertical,
			};
		}

		public static StackedContentChildInfo CreateVertical(GridLength length, double? min = null, double? max = null) {
			return new StackedContentChildInfo {
				Vertical = new GridChildLength { GridLength = length, MinLength = min, MaxLength = max }
			};
		}

		public static StackedContentChildInfo CreateHorizontal(GridLength length, double? min = null, double? max = null) {
			return new StackedContentChildInfo {
				Horizontal = new GridChildLength { GridLength = length, MinLength = min, MaxLength = max }
			};
		}
	}

	sealed class StackedContentChildImpl : IStackedContentChild {
		IStackedContent IStackedContentChild.StackedContent { get; set; }

		public object UIObject {
			get { return uiObject; }
		}
		readonly object uiObject;

		public StackedContentChildImpl(object uiObject) {
			this.uiObject = uiObject;
		}

		public static IStackedContentChild GetOrCreate(object uiObjectOwner, object uiObject) {
			var scc = uiObjectOwner as IStackedContentChild;
			if (scc != null)
				return scc;
			return new StackedContentChildImpl(uiObject);
		}
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
			public StackedContentChildInfo LengthInfo;

			public ChildInfo(TChild child, StackedContentChildInfo lengthInfo) {
				this.Child = child;
				this.LengthInfo = lengthInfo != null ? lengthInfo.Clone() : new StackedContentChildInfo();
				if (this.LengthInfo.Horizontal.GridLength == null)
					this.LengthInfo.Horizontal.GridLength = new GridLength(1, GridUnitType.Star);
				if (this.LengthInfo.Vertical.GridLength == null)
					this.LengthInfo.Vertical.GridLength = new GridLength(1, GridUnitType.Star);
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

		public void AddChild(TChild child, StackedContentChildInfo lengthInfo = null, int index = -1) {
			Debug.Assert(child.StackedContent == null);
			if ((uint)index <= (uint)children.Count)
				children.Insert(index, new ChildInfo(child, lengthInfo));
			else
				children.Add(new ChildInfo(child, lengthInfo));
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
					if (needSplitter && !info.LengthInfo.Vertical.GridLength.Value.IsAuto) {
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

					var rowDef = new RowDefinition() { Height = GetGridLength(info.LengthInfo.Vertical.GridLength.Value, -d) };
					if (info.LengthInfo.Vertical.MaxLength != null)
						rowDef.MaxHeight = info.LengthInfo.Vertical.MaxLength.Value;
					if (info.LengthInfo.Vertical.MinLength != null)
						rowDef.MinHeight = info.LengthInfo.Vertical.MinLength.Value;
					grid.RowDefinitions.Add(rowDef);
					var uiel = GetUIElement(info.Child);
					uiel.SetValue(Grid.RowProperty, rowCol);
					uiel.ClearValue(Grid.ColumnProperty);
					grid.Children.Add(uiel);
					rowCol++;
					d = -d;
					needSplitter = !info.LengthInfo.Vertical.GridLength.Value.IsAuto;
				}
			}
			else {
				grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
				int rowCol = 0;
				foreach (var info in children) {
					if (needSplitter && !info.LengthInfo.Horizontal.GridLength.Value.IsAuto) {
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

					var colDef = new ColumnDefinition() { Width = GetGridLength(info.LengthInfo.Horizontal.GridLength.Value, -d) };
					if (info.LengthInfo.Horizontal.MaxLength != null)
						colDef.MaxWidth = info.LengthInfo.Horizontal.MaxLength.Value;
					if (info.LengthInfo.Horizontal.MinLength != null)
						colDef.MinWidth = info.LengthInfo.Horizontal.MinLength.Value;
					this.grid.ColumnDefinitions.Add(colDef);
					var uiel = GetUIElement(info.Child);
					uiel.ClearValue(Grid.RowProperty);
					uiel.SetValue(Grid.ColumnProperty, rowCol);
					grid.Children.Add(uiel);
					rowCol++;
					d = -d;
					needSplitter = !info.LengthInfo.Horizontal.GridLength.Value.IsAuto;
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

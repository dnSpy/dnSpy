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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace dnSpy.Controls {
	struct GridChildLength {
		public GridLength? GridLength;
		public double? MinLength;
		public double? MaxLength;

		public GridChildLength(GridLength length, double? min = null, double? max = null) {
			GridLength = length;
			MinLength = min;
			MaxLength = max;
		}
	}

	sealed class StackedContentChildInfo {
		public GridChildLength Horizontal;
		public GridChildLength Vertical;

		public StackedContentChildInfo Clone() => new StackedContentChildInfo {
			Horizontal = Horizontal,
			Vertical = Vertical,
		};

		public static StackedContentChildInfo CreateVertical(GridLength length, double? min = null, double? max = null) => new StackedContentChildInfo {
			Vertical = new GridChildLength { GridLength = length, MinLength = min, MaxLength = max }
		};

		public static StackedContentChildInfo CreateHorizontal(GridLength length, double? min = null, double? max = null) => new StackedContentChildInfo {
			Horizontal = new GridChildLength { GridLength = length, MinLength = min, MaxLength = max }
		};
	}

	sealed class StackedContentChildImpl : IStackedContentChild {
		public object UIObject { get; }

		public StackedContentChildImpl(object uiObject) {
			UIObject = uiObject;
		}

		public static IStackedContentChild GetOrCreate(object uiObjectOwner, object uiObject) {
			var scc = uiObjectOwner as IStackedContentChild;
			if (scc != null)
				return scc;
			return new StackedContentChildImpl(uiObject);
		}
	}

	sealed class StackedContent<TChild> : IStackedContentChild where TChild : class, IStackedContentChild {
		public const double DEFAULT_SPLITTER_LENGTH = 6;

		public TChild this[int index] => children[index].Child;
		public int Count => children.Count;

		public double SplitterLength {
			get { return splitterLength; }
			set {
				if (splitterLength != value) {
					splitterLength = value;
					UpdateGrid();
				}
			}
		}
		double splitterLength;

		sealed class ChildInfo {
			public TChild Child;
			public StackedContentChildInfo LengthInfo;

			public ChildInfo(TChild child, StackedContentChildInfo lengthInfo) {
				Child = child;
				LengthInfo = lengthInfo?.Clone() ?? new StackedContentChildInfo();
				if (LengthInfo.Horizontal.GridLength == null)
					LengthInfo.Horizontal.GridLength = new GridLength(1, GridUnitType.Star);
				if (LengthInfo.Vertical.GridLength == null)
					LengthInfo.Vertical.GridLength = new GridLength(1, GridUnitType.Star);
			}
		}

		public TChild[] Children => children.Select(a => a.Child).ToArray();
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

		public object UIObject => grid;
		readonly Grid grid;

		public StackedContentState State {
			get {
				var state = new StackedContentState();
				state.IsHorizontal = IsHorizontal;
				if (!IsHorizontal)
					state.RowsCols.AddRange(grid.RowDefinitions.Select(a => a.Height));
				else
					state.RowsCols.AddRange(grid.ColumnDefinitions.Select(a => a.Width));
				return state;
			}
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (IsHorizontal != value.IsHorizontal)
					return;
				if (!IsHorizontal) {
					if (grid.RowDefinitions.Count != value.RowsCols.Count)
						return;
					for (int i = 0; i < value.RowsCols.Count; i++)
						grid.RowDefinitions[i].Height = value.RowsCols[i];
				}
				else {
					if (grid.ColumnDefinitions.Count != value.RowsCols.Count)
						return;
					for (int i = 0; i < value.RowsCols.Count; i++)
						grid.ColumnDefinitions[i].Width = value.RowsCols[i];
				}
			}
		}

		public StackedContent(bool isHorizontal = true, double splitterLength = DEFAULT_SPLITTER_LENGTH, Thickness? margin = null) {
			children = new List<ChildInfo>();
			grid = new Grid();
			grid.SetResourceReference(FrameworkElement.StyleProperty, "StackedContentGridStyle");
			if (margin != null)
				grid.Margin = margin.Value;
			this.isHorizontal = isHorizontal;
			this.splitterLength = splitterLength;
			UpdateGrid();
		}

		public void Clear() {
			children.Clear();
			UpdateGrid();
		}

		public void AddChild(TChild child, StackedContentChildInfo lengthInfo = null, int index = -1) {
			if ((uint)index <= (uint)children.Count)
				children.Insert(index, new ChildInfo(child, lengthInfo));
			else
				children.Add(new ChildInfo(child, lengthInfo));
			UpdateGrid();
		}

		public void Remove(TChild child) {
			if (child == null)
				throw new ArgumentNullException(nameof(child));
			int index = IndexOf(child);
			Debug.Assert(index >= 0);
			if (index >= 0) {
				var info = children[index];
				children.RemoveAt(index);
				UpdateGrid();
			}
		}

		void UpdateGrid() => UpdateGrid(IsHorizontal);

		void UpdateGrid(bool horizontal) {
			grid.Children.Clear();
			grid.ColumnDefinitions.Clear();
			grid.RowDefinitions.Clear();

			// Make sure the horizontal grid splitters can resize the content
			double d = 0.05;
			bool needSplitter = false;
			if (!horizontal) {
				grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
				int rowCol = 0;
				foreach (var info in children) {
					if (needSplitter && !info.LengthInfo.Vertical.GridLength.Value.IsAuto) {
						var gridSplitter = new GridSplitter();
						Panel.SetZIndex(gridSplitter, 1);
						grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(splitterLength, GridUnitType.Pixel) });
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
						grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(splitterLength, GridUnitType.Pixel) });
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
					grid.ColumnDefinitions.Add(colDef);
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

		public GridLength GetLength(TChild child) {
			int index = Array.IndexOf(Children, child);
			Debug.Assert(index >= 0);
			if (index < 0)
				throw new InvalidOperationException();
			if (!IsHorizontal) {
				for (int i = 0, j = 0; i < grid.RowDefinitions.Count; i++, j++) {
					var c = grid.Children[i];
					if (c is GridSplitter)
						c = grid.Children[++i];
					if (j != index)
						continue;
					return grid.RowDefinitions[i].Height;
				}
			}
			else {
				for (int i = 0, j = 0; i < grid.ColumnDefinitions.Count; i++, j++) {
					var c = grid.Children[i];
					if (c is GridSplitter)
						c = grid.Children[++i];
					if (j != index)
						continue;
					return grid.ColumnDefinitions[i].Width;
				}
			}
			Debug.Fail("Failed to find length");
			throw new InvalidOperationException();
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

		public bool Contains(TChild child) => IndexOf(child) >= 0;

		public void SwapChildren(int index1, int index2) {
			var tmp1 = children[index1];
			children[index1] = children[index2];
			children[index2] = tmp1;

			// Reset sizes
			UpdateGrid();
		}

		public void UpdateSize(TChild child, StackedContentChildInfo info) {
			foreach (var c in children) {
				if (c.Child != child)
					continue;

				c.LengthInfo = info;
				return;
			}
			Debug.Fail("Couldn't find child");
		}
	}
}

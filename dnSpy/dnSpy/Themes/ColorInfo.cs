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
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Themes;

namespace dnSpy.Themes {
	[DebuggerDisplay("{ColorType}, Children={Children.Length}")]
	abstract class ColorInfo {
		public readonly ColorType ColorType;
		public readonly string Description;
		public string? DefaultForeground;
		public string? DefaultBackground;
		public string? DefaultColor3;
		public string? DefaultColor4;
		public ColorInfo? Parent;

		public ColorInfo[] Children {
			get => children;
			set {
				children = value ?? Array.Empty<ColorInfo>();
				foreach (var child in children)
					child.Parent = this;
			}
		}
		ColorInfo[] children = Array.Empty<ColorInfo>();

		public abstract IEnumerable<(object?, object)> GetResourceKeyValues(ThemeColor hlColor);

		protected ColorInfo(ColorType colorType, string description) {
			ColorType = colorType;
			Description = description;
		}
	}

	sealed class ColorColorInfo : ColorInfo {
		public object? BackgroundResourceKey;
		public object? ForegroundResourceKey;

		public ColorColorInfo(ColorType colorType, string description)
			: base(colorType, description) => ForegroundResourceKey = null;

		public override IEnumerable<(object?, object)> GetResourceKeyValues(ThemeColor hlColor) {
			if (!(ForegroundResourceKey is null)) {
				Debug2.Assert(!(hlColor.Foreground is null));
				yield return (ForegroundResourceKey, ((SolidColorBrush)hlColor.Foreground).Color);
			}
			if (!(BackgroundResourceKey is null)) {
				Debug2.Assert(!(hlColor.Background is null));
				yield return (BackgroundResourceKey, ((SolidColorBrush)hlColor.Background).Color);
			}
		}
	}

	sealed class BrushColorInfo : ColorInfo {
		public object? BackgroundResourceKey;
		public object? ForegroundResourceKey;

		public BrushColorInfo(ColorType colorType, string description)
			: base(colorType, description) {
		}

		public override IEnumerable<(object?, object)> GetResourceKeyValues(ThemeColor hlColor) {
			if (!(ForegroundResourceKey is null)) {
				Debug2.Assert(!(hlColor.Foreground is null));
				yield return (ForegroundResourceKey, hlColor.Foreground);
			}
			if (!(BackgroundResourceKey is null)) {
				Debug2.Assert(!(hlColor.Background is null));
				yield return (BackgroundResourceKey, hlColor.Background);
			}
		}
	}

	sealed class DrawingBrushColorInfo : ColorInfo {
		public object? BackgroundResourceKey;
		public object? ForegroundResourceKey;
		public bool IsHorizontal;

		public DrawingBrushColorInfo(ColorType colorType, string description)
			: base(colorType, description) => ForegroundResourceKey = null;

		public override IEnumerable<(object?, object)> GetResourceKeyValues(ThemeColor hlColor) {
			if (!(ForegroundResourceKey is null)) {
				Debug2.Assert(!(hlColor.Foreground is null));
				var brush = hlColor.Foreground;
				yield return (ForegroundResourceKey, CreateDrawingBrush(brush));
			}
			if (!(BackgroundResourceKey is null)) {
				Debug2.Assert(!(hlColor.Background is null));
				var brush = hlColor.Background;
				yield return (BackgroundResourceKey, CreateDrawingBrush(brush));
			}
		}

		DrawingBrush CreateDrawingBrush(Brush brush) {
			DrawingBrush db = new DrawingBrush() {
				TileMode = TileMode.Tile,
				ViewboxUnits = BrushMappingMode.Absolute,
				ViewportUnits = BrushMappingMode.Absolute,
			};
			if (IsHorizontal) {
				db.Viewbox = new Rect(0, 0, 4, 5);
				db.Viewport = new Rect(0, 0, 4, 5);
				db.Drawing = new GeometryDrawing {
					Brush = brush,
					Geometry = new GeometryGroup {
						Children = {
							new RectangleGeometry(new Rect(0, 0, 1, 1)),
							new RectangleGeometry(new Rect(0, 4, 1, 1)),
							new RectangleGeometry(new Rect(2, 2, 1, 1))
						}
					}
				};
			}
			else {
				db.Viewbox = new Rect(0, 0, 5, 4);
				db.Viewport = new Rect(0, 0, 5, 4);
				db.Drawing = new GeometryDrawing {
					Brush = brush,
					Geometry = new GeometryGroup {
						Children = {
							new RectangleGeometry(new Rect(0, 0, 1, 1)),
							new RectangleGeometry(new Rect(4, 0, 1, 1)),
							new RectangleGeometry(new Rect(2, 2, 1, 1))
						}
					}
				};
			}
			db.Freeze();
			return db;
		}
	}

	sealed class LinearGradientColorInfo : ColorInfo {
		public object? ResourceKey;
		public Point StartPoint;
		public Point EndPoint;
		public double[] GradientOffsets;

		public LinearGradientColorInfo(ColorType colorType, Point endPoint, string description, params double[] gradientOffsets)
			: this(colorType, new Point(0, 0), endPoint, description, gradientOffsets) {
		}

		public LinearGradientColorInfo(ColorType colorType, Point startPoint, Point endPoint, string description, params double[] gradientOffsets)
			: base(colorType, description) {
			StartPoint = startPoint;
			EndPoint = endPoint;
			GradientOffsets = gradientOffsets;
		}

		public override IEnumerable<(object?, object)> GetResourceKeyValues(ThemeColor hlColor) {
			var br = new LinearGradientBrush() {
				StartPoint = StartPoint,
				EndPoint = EndPoint,
			};
			for (int i = 0; i < GradientOffsets.Length; i++) {
				var gs = new GradientStop(((SolidColorBrush)hlColor.GetBrushByIndex(i)!).Color, GradientOffsets[i]);
				gs.Freeze();
				br.GradientStops.Add(gs);
			}
			br.Freeze();
			yield return (ResourceKey, br);
		}
	}
}

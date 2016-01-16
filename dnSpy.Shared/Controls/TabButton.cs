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

using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace dnSpy.Shared.Controls {
	public class TabButton : ButtonBase {
		public static readonly DependencyProperty GlyphForegroundProperty =
			DependencyProperty.Register("GlyphForeground", typeof(Brush), typeof(TabButton),
			new FrameworkPropertyMetadata(null));
		public static readonly DependencyProperty HoverBackgroundProperty =
			DependencyProperty.Register("HoverBackground", typeof(Brush), typeof(TabButton),
			new FrameworkPropertyMetadata(null));
		public static readonly DependencyProperty HoverBorderBrushProperty =
			DependencyProperty.Register("HoverBorderBrush", typeof(Brush), typeof(TabButton),
			new FrameworkPropertyMetadata(null));
		public static readonly DependencyProperty HoverForegroundProperty =
			DependencyProperty.Register("HoverForeground", typeof(Brush), typeof(TabButton),
			new FrameworkPropertyMetadata(null));
		public static readonly DependencyProperty PressedBackgroundProperty =
			DependencyProperty.Register("PressedBackground", typeof(Brush), typeof(TabButton),
			new FrameworkPropertyMetadata(null));
		public static readonly DependencyProperty PressedBorderBrushProperty =
			DependencyProperty.Register("PressedBorderBrush", typeof(Brush), typeof(TabButton),
			new FrameworkPropertyMetadata(null));
		public static readonly DependencyProperty PressedForegroundProperty =
			DependencyProperty.Register("PressedForeground", typeof(Brush), typeof(TabButton),
			new FrameworkPropertyMetadata(null));
		public static readonly DependencyProperty HoverBorderThicknessProperty =
			DependencyProperty.Register("HoverBorderThickness", typeof(Thickness), typeof(TabButton),
			new FrameworkPropertyMetadata(new Thickness()));
		public static readonly DependencyProperty PressedBorderThicknessProperty =
			DependencyProperty.Register("PressedBorderThickness", typeof(Thickness), typeof(TabButton),
			new FrameworkPropertyMetadata(new Thickness()));

		public Brush GlyphForeground {
			get { return (Brush)GetValue(GlyphForegroundProperty); }
			set { SetValue(GlyphForegroundProperty, value); }
		}

		public Brush HoverBackground {
			get { return (Brush)GetValue(HoverBackgroundProperty); }
			set { SetValue(HoverBackgroundProperty, value); }
		}

		public Brush HoverBorderBrush {
			get { return (Brush)GetValue(HoverBorderBrushProperty); }
			set { SetValue(HoverBorderBrushProperty, value); }
		}

		public Brush HoverForeground {
			get { return (Brush)GetValue(HoverForegroundProperty); }
			set { SetValue(HoverForegroundProperty, value); }
		}

		public Brush PressedBackground {
			get { return (Brush)GetValue(PressedBackgroundProperty); }
			set { SetValue(PressedBackgroundProperty, value); }
		}

		public Brush PressedBorderBrush {
			get { return (Brush)GetValue(PressedBorderBrushProperty); }
			set { SetValue(PressedBorderBrushProperty, value); }
		}

		public Brush PressedForeground {
			get { return (Brush)GetValue(PressedForegroundProperty); }
			set { SetValue(PressedForegroundProperty, value); }
		}

		public Thickness HoverBorderThickness {
			get { return (Thickness)GetValue(HoverBorderThicknessProperty); }
			set { SetValue(HoverBorderThicknessProperty, value); }
		}

		public Thickness PressedBorderThickness {
			get { return (Thickness)GetValue(PressedBorderThicknessProperty); }
			set { SetValue(PressedBorderThicknessProperty, value); }
		}

		static TabButton() {
			DefaultStyleKeyProperty.OverrideMetadata(typeof(TabButton), new FrameworkPropertyMetadata(typeof(TabButton)));
		}

		public TabButton() {
		}
	}
}

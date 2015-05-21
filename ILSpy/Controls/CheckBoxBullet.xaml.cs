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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Shapes;

namespace ICSharpCode.ILSpy.Controls
{
	/// <summary>
	/// Interaction logic for CheckBoxBullet.xaml
	/// </summary>
	public partial class CheckBoxBullet : UserControl
	{
		public static readonly DependencyProperty RenderMouseOverProperty =
			DependencyProperty.Register("RenderMouseOver", typeof(bool), typeof(CheckBoxBullet),
			new FrameworkPropertyMetadata(false, new PropertyChangedCallback(RecalculateColors)));

		public static readonly DependencyProperty RenderPressedProperty =
			DependencyProperty.Register("RenderPressed", typeof(bool), typeof(CheckBoxBullet),
			new FrameworkPropertyMetadata(false, new PropertyChangedCallback(RecalculateColors)));

		public static readonly DependencyProperty IsCheckedProperty =
			DependencyProperty.Register("IsChecked", typeof(bool?), typeof(CheckBoxBullet),
			new FrameworkPropertyMetadata((bool?)false, new PropertyChangedCallback(RecalculateColors)));

		public bool RenderMouseOver {
			get { return (bool)GetValue(RenderMouseOverProperty); }
			set { SetValue(RenderMouseOverProperty, value); }
		}

		public bool RenderPressed {
			get { return (bool)GetValue(RenderPressedProperty); }
			set { SetValue(RenderPressedProperty, value); }
		}

		public bool? IsChecked {
			get { return (bool?)GetValue(IsCheckedProperty); }
			set { SetValue(IsCheckedProperty, value); }
		}

		public CheckBoxBullet()
		{
			IsEnabledChanged += (s, e) => Update();
			InitializeComponent();
			Update();
		}

		static void RecalculateColors(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			((CheckBoxBullet)o).Update();
		}

		void Update()
		{
			var name = GetOuterBorderBackground();
			if (name != null)
				outerBorder.SetResourceReference(Control.BackgroundProperty, name);
			else
				outerBorder.SetBinding(Control.BackgroundProperty, new Binding("Background") { Source = this });

			name = GetOuterBorderBorderBrush();
			if (name != null)
				outerBorder.SetResourceReference(Control.BorderBrushProperty, name);
			else
				outerBorder.SetBinding(Control.BorderBrushProperty, new Binding("BorderBrush") { Source = this });

			innerBorder.SetResourceReference(Control.BackgroundProperty, GetInnerBorderBackground() ?? "CommonDisabledBackgroundOverlay");
			innerBorder.SetResourceReference(Control.BorderBrushProperty, GetInnerBorderBorderBrush() ?? "CommonDisabledBackgroundOverlay");

			if (IsChecked == true || (IsChecked == false && RenderPressed)) {
				path1.Visibility = Visibility.Visible;
				path2.Visibility = Visibility.Visible;

				name = GetPathFill();
				if (name != null) {
					path1.SetResourceReference(Shape.FillProperty, name);
					path2.SetResourceReference(Shape.FillProperty, name);
				}
				else {
					path1.ClearValue(Shape.FillProperty);
					path2.ClearValue(Shape.FillProperty);
				}

				name = GetPathStroke();
				if (name != null) {
					path1.SetResourceReference(Shape.StrokeProperty, name);
					path2.SetResourceReference(Shape.StrokeProperty, name);
				}
				else {
					path1.ClearValue(Shape.StrokeProperty);
					path2.ClearValue(Shape.StrokeProperty);
				}
			}
			else {
				path1.Visibility = Visibility.Collapsed;
				path2.Visibility = Visibility.Collapsed;
			}
		}

		string GetOuterBorderBackground()
		{
			if (!IsEnabled)
				return "CommonDisabledBackgroundOverlay";
			if (RenderPressed)
				return "CommonPressedBackgroundOverlay";
			if (RenderMouseOver)
				return "CommonHoverBackgroundOverlay";
			return null;
		}

		string GetOuterBorderBorderBrush()
		{
			if (!IsEnabled)
				return "CommonDisabledBorderOverlay";
			if (RenderPressed)
				return "CommonPressedBorderOverlay";
			if (RenderMouseOver)
				return "CommonHoverBorderOverlay";
			return null;
		}

		string GetInnerBorderBackground()
		{
			if (!IsEnabled) {
				if (IsChecked == null)
					return "CommonIndeterminateDisabledFill";
				return null;
			}
			if (IsChecked == null) {
				if (RenderPressed)
					return "CommonIndeterminatePressedFill";
				if (RenderMouseOver)
					return "CommonIndeterminateHoverFill";
				return "CommonIndeterminateFill";
			}
			if (RenderPressed)
				return "CommonCheckBoxPressedInnerFill";
			if (RenderMouseOver)
				return "CommonCheckBoxHoverInnerFill";
			return "CommonCheckBoxInnerFill";
		}

		string GetInnerBorderBorderBrush()
		{
			if (!IsEnabled) {
				if (IsChecked == null)
					return "CommonIndeterminateDisabledInnerBorderPen";
				return "CommonCheckBoxDisabledInnerBorderPen";
			}
			if (RenderPressed) {
				if (IsChecked == null)
					return "CommonIndeterminatePressedInnerBorderPen";
				return "CommonCheckBoxPressedInnerBorderPen";
			}
			if (RenderMouseOver) {
				if (IsChecked == null)
					return "CommonIndeterminateHoverInnerBorderPen";
				return "CommonCheckBoxHoverInnerBorderPen";
			}
			if (IsChecked == null)
				return "CommonIndeterminateInnerBorderPen";
			return "CommonCheckBoxInnerBorderPen";
		}

		string GetPathFill()
		{
			if (!IsEnabled) {
				if (IsChecked == true)
					return "CommonCheckMarkDisabledFill";
				return null;
			}
			if (IsChecked == true) {
				if (RenderPressed)
					return "CommonCheckMarkPressedFill";
				return "CommonCheckMarkFill";
			}
			if (IsChecked == false && RenderPressed)
				return "CommonCheckMarkPressedFill";
			return null;
		}

		string GetPathStroke()
		{
			if (!IsEnabled)
				return null;
			if (IsChecked == true) {
				if (RenderPressed)
					return "CommonCheckMarkPressedStroke";
				return "CommonCheckMarkStroke";
			}
			if (RenderPressed)
				return "CommonCheckMarkPressedStroke";
			return null;
		}
	}
}

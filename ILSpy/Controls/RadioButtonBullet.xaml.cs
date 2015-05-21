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
using System.Windows.Media;

namespace ICSharpCode.ILSpy.Controls
{
	/// <summary>
	/// Interaction logic for RadioButtonBullet.xaml
	/// </summary>
	public partial class RadioButtonBullet : UserControl
	{
		public static readonly DependencyProperty RenderMouseOverProperty =
			DependencyProperty.Register("RenderMouseOver", typeof(bool), typeof(RadioButtonBullet),
			new FrameworkPropertyMetadata(false, new PropertyChangedCallback(RecalculateColors)));

		public static readonly DependencyProperty RenderPressedProperty =
			DependencyProperty.Register("RenderPressed", typeof(bool), typeof(RadioButtonBullet),
			new FrameworkPropertyMetadata(false, new PropertyChangedCallback(RecalculateColors)));

		public static readonly DependencyProperty IsCheckedProperty =
			DependencyProperty.Register("IsChecked", typeof(bool?), typeof(RadioButtonBullet),
			new FrameworkPropertyMetadata((bool?)false, new PropertyChangedCallback(RecalculateColors)));

		public static readonly DependencyProperty DefaultBackgroundProperty =
			DependencyProperty.Register("DefaultBackground", typeof(Brush), typeof(RadioButtonBullet),
			new FrameworkPropertyMetadata(null, new PropertyChangedCallback(RecalculateColors)));

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

		public Brush DefaultBackground {
			get { return (Brush)GetValue(DefaultBackgroundProperty); }
			set { SetValue(DefaultBackgroundProperty, value); }
		}

		public RadioButtonBullet()
		{
			IsEnabledChanged += (s, e) => Update();
			InitializeComponent();
			Update();
		}

		static void RecalculateColors(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			((RadioButtonBullet)o).Update();
		}

		void Update()
		{
			var name = GetOuterBorderBackground();
			if (name != null)
				outerBorder.SetResourceReference(Shape.FillProperty, name);
			else
				outerBorder.SetBinding(Shape.FillProperty, new Binding("DefaultBackground") { Source = this });

			name = GetOuterBorderBorderBrush();
			if (name != null)
				outerBorder.SetResourceReference(Shape.StrokeProperty, name);
			else
				outerBorder.SetBinding(Shape.StrokeProperty, new Binding("BorderBrush") { Source = this });

			innerBorder.SetResourceReference(Shape.FillProperty, GetInnerBorderBackground() ?? "CommonDisabledBackgroundOverlay");
			innerBorder.SetResourceReference(Shape.StrokeProperty, GetInnerBorderBorderBrush() ?? "CommonDisabledBackgroundOverlay");

			if (IsChecked == true || (IsChecked == false && RenderPressed)) {
				ellipse.Visibility = Visibility.Visible;

				name = GetPathFill();
				if (name != null)
					ellipse.SetResourceReference(Shape.FillProperty, name);
				else
					ellipse.ClearValue(Shape.FillProperty);

				name = GetPathStroke();
				if (name != null)
					ellipse.SetResourceReference(Shape.StrokeProperty, name);
				else
					ellipse.ClearValue(Shape.StrokeProperty);
			}
			else
				ellipse.Visibility = Visibility.Collapsed;
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
			if (RenderPressed)
				return "CommonRadioButtonPressedInnerBorderPen";
			if (RenderMouseOver)
				return "CommonRadioButtonHoverInnerBorderPen";
			return "CommonRadioButtonInnerBorderPen";
		}

		string GetPathFill()
		{
			if (!IsEnabled) {
				if (IsChecked == true)
					return "CommonRadioButtonGlyphDisabledFill";
				return null;
			}
			if (IsChecked == true) {
				if (RenderPressed)
					return "CommonRadioButtonGlyphPressedFill";
				if (RenderMouseOver)
					return "CommonRadioButtonGlyphHoverFill";
				return "CommonRadioButtonGlyphFill";
			}
			if (RenderPressed)
				return "CommonRadioButtonGlyphPressedFill";
			return null;
		}

		string GetPathStroke()
		{
			if (!IsEnabled) {
				if (IsChecked == true)
					return "CommonRadioButtonDisabledGlyphStroke";
				return null;
			}
			if (IsChecked == true || RenderPressed)
				return "CommonRadioButtonGlyphStroke";
			return null;
		}
	}
}

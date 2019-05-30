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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace dnSpy.Contracts.Images {
	/// <summary>
	/// Image using <see cref="IImageService"/> to load the correct image depending
	/// on DPI, zoom and background color
	/// </summary>
	public sealed class DsImage : Image {
		static DsImage() => DefaultStyleKeyProperty.OverrideMetadata(typeof(DsImage), new FrameworkPropertyMetadata(typeof(DsImage)));

		/// <summary>
		/// <see cref="ImageReference"/> dependency property
		/// </summary>
		public static readonly DependencyProperty ImageReferenceProperty =
			DependencyProperty.Register(nameof(ImageReference), typeof(ImageReference), typeof(DsImage),
			new FrameworkPropertyMetadata(default(ImageReference)));

		/// <summary>
		/// Gets/sets the image reference, eg. <see cref="DsImages.Assembly"/>
		/// </summary>
		public ImageReference ImageReference {
			get => (ImageReference)GetValue(ImageReferenceProperty);
			set => SetValue(ImageReferenceProperty, value);
		}

		/// <summary>
		/// Background color attached property
		/// </summary>
		public static readonly DependencyProperty BackgroundColorProperty =
			DependencyProperty.RegisterAttached("BackgroundColor", typeof(Color?), typeof(DsImage),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

		/// <summary>
		/// Gets the background color
		/// </summary>
		/// <param name="depo">Object</param>
		/// <returns></returns>
		public static Color? GetBackgroundColor(DependencyObject depo) => (Color?)depo.GetValue(BackgroundColorProperty);

		/// <summary>
		/// Sets the background color
		/// </summary>
		/// <param name="depo">Object</param>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public static void SetBackgroundColor(DependencyObject depo, Color? value) => depo.SetValue(BackgroundColorProperty, value);

		/// <summary>
		/// Background brush attached property
		/// </summary>
		public static readonly DependencyProperty BackgroundBrushProperty =
			DependencyProperty.RegisterAttached("BackgroundBrush", typeof(Brush), typeof(DsImage),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

		/// <summary>
		/// Gets the background brush
		/// </summary>
		/// <param name="depo">Object</param>
		/// <returns></returns>
		public static Brush GetBackgroundBrush(DependencyObject depo) => (Brush)depo.GetValue(BackgroundBrushProperty);

		/// <summary>
		/// Sets the background brush
		/// </summary>
		/// <param name="depo">Object</param>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public static void SetBackgroundBrush(DependencyObject depo, Brush? value) => depo.SetValue(BackgroundBrushProperty, value);

		/// <summary>
		/// Zoom attached property
		/// </summary>
		public static readonly DependencyProperty ZoomProperty =
			DependencyProperty.RegisterAttached("Zoom", typeof(double), typeof(DsImage),
			new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.Inherits));

		/// <summary>
		/// Gets the zoom (1.0 == 100%)
		/// </summary>
		/// <param name="depo">Object</param>
		/// <returns></returns>
		public static double GetZoom(DependencyObject depo) => (double)depo.GetValue(ZoomProperty);

		/// <summary>
		/// Sets the zoom (1.0 == 100%)
		/// </summary>
		/// <param name="depo">Object</param>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public static void SetZoom(DependencyObject depo, double value) => depo.SetValue(ZoomProperty, value);

		/// <summary>
		/// Dpi attached property
		/// </summary>
		public static readonly DependencyProperty DpiProperty =
			DependencyProperty.RegisterAttached("Dpi", typeof(double), typeof(DsImage),
			new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.Inherits));

		/// <summary>
		/// Gets the dpi
		/// </summary>
		/// <param name="depo">Object</param>
		/// <returns></returns>
		public static double GetDpi(DependencyObject depo) => (double)depo.GetValue(DpiProperty);

		/// <summary>
		/// Sets the dpi
		/// </summary>
		/// <param name="depo">Object</param>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public static void SetDpi(DependencyObject depo, double value) => depo.SetValue(DpiProperty, value);
	}
}

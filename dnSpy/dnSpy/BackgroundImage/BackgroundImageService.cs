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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using dnSpy.Contracts.BackgroundImage;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.BackgroundImage {
	sealed class BackgroundImageService : IImageSourceServiceListener {
		readonly IWpfTextView wpfTextView;
		readonly IImageSourceService imageSourceService;
		IAdornmentLayer adornmentLayer;
		Image currentImage;

#pragma warning disable 0169
		[Export(typeof(AdornmentLayerDefinition))]
		[Name(PredefinedDnSpyAdornmentLayers.BackgroundImage)]
		[Order(Before = PredefinedDnSpyAdornmentLayers.BottomLayer)]
		[Order(Before = PredefinedDnSpyAdornmentLayers.TopLayer)]
		static AdornmentLayerDefinition backgroundImageAdornmentLayerDefinition;
#pragma warning restore 0169

		BackgroundImageService(IWpfTextView wpfTextView, IImageSourceService imageSourceService) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			if (imageSourceService == null)
				throw new ArgumentNullException(nameof(imageSourceService));
			this.wpfTextView = wpfTextView;
			this.imageSourceService = imageSourceService;
			wpfTextView.Closed += WpfTextView_Closed;
			imageSourceService.Register(this);
		}

		void WpfTextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
			if (e.HorizontalTranslation || e.VerticalTranslation)
				UpdateImagePosition();
			else if (e.OldViewState.ViewportWidth != e.NewViewState.ViewportWidth)
				UpdateImagePosition();
			else if (e.OldViewState.ViewportHeight != e.NewViewState.ViewportHeight)
				UpdateImagePosition();
		}

		public static void InstallService(IWpfTextView wpfTextView, IImageSourceService imageSourceService) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			if (imageSourceService == null)
				throw new ArgumentNullException(nameof(imageSourceService));
			wpfTextView.Properties.GetOrCreateSingletonProperty(typeof(BackgroundImageService), () => new BackgroundImageService(wpfTextView, imageSourceService));
		}

		public void OnEnabled() {
			if (adornmentLayer == null)
				adornmentLayer = wpfTextView.GetAdornmentLayer(PredefinedDnSpyAdornmentLayers.BackgroundImage);
			wpfTextView.LayoutChanged += WpfTextView_LayoutChanged;
		}

		public void OnDisabled() {
			wpfTextView.LayoutChanged -= WpfTextView_LayoutChanged;
			if (adornmentLayer != null)
				adornmentLayer.RemoveAllAdornments();
			currentImage = null;
		}

		Image InitializeImage(Image image) {
			image.Stretch = imageSourceService.Stretch;
			image.StretchDirection = imageSourceService.StretchDirection;
			image.Opacity = imageSourceService.Opacity;
			image.Source = imageSourceService.ImageSource;
			image.MaxHeight = imageSourceService.MaxHeight <= 0 ? double.PositiveInfinity : imageSourceService.MaxHeight;
			image.MaxWidth = imageSourceService.MaxWidth <= 0 ? double.PositiveInfinity : imageSourceService.MaxWidth;
			image.ClearValue(FrameworkElement.HeightProperty);
			image.ClearValue(FrameworkElement.WidthProperty);
			if (imageSourceService.Scale == 1)
				image.LayoutTransform = Transform.Identity;
			else {
				var scaleTransform = new ScaleTransform(imageSourceService.Scale, imageSourceService.Scale);
				scaleTransform.Freeze();
				image.LayoutTransform = scaleTransform;
			}
			UpdateImagePosition(image);
			return image;
		}

		public void OnImageChanged() {
			if (currentImage == null) {
				currentImage = InitializeImage(new Image());
				AddImageToAdornmentLayer();
			}
			else
				InitializeImage(currentImage);
		}

		void UpdateImagePosition() {
			Debug.Assert(currentImage != null);
			if (currentImage == null)
				return;
			UpdateImagePosition(currentImage);
		}

		bool UsesGridColumns => imageSourceService.TotalGridColumns > 1 && (uint)imageSourceService.GridColumn < (uint)imageSourceService.TotalGridColumns && (uint)(imageSourceService.GridColumn + (imageSourceService.GridColumnSpan <= 0 ? 1 : imageSourceService.GridColumnSpan)) <= (uint)imageSourceService.TotalGridColumns;
		bool UsesGridRows => imageSourceService.TotalGridRows > 1 && (uint)imageSourceService.GridRow < (uint)imageSourceService.TotalGridRows && (uint)(imageSourceService.GridRow + (imageSourceService.GridRowSpan <= 0 ? 1 : imageSourceService.GridRowSpan)) <= (uint)imageSourceService.TotalGridRows;

		const bool resizeTooBigImages = true;
		void UpdateImagePosition(Image image) {
			double viewportWidth, viewportHeight;
			double xOffs, yOffs;
			if (UsesGridColumns) {
				viewportWidth = wpfTextView.ViewportWidth / imageSourceService.TotalGridColumns * (imageSourceService.GridColumnSpan <= 0 ? 1 : imageSourceService.GridColumnSpan);
				xOffs = (double)imageSourceService.GridColumn / imageSourceService.TotalGridColumns * wpfTextView.ViewportWidth;
			}
			else {
				viewportWidth = wpfTextView.ViewportWidth;
				xOffs = 0;
			}
			if (UsesGridRows) {
				viewportHeight = wpfTextView.ViewportHeight / imageSourceService.TotalGridRows * (imageSourceService.GridRowSpan <= 0 ? 1 : imageSourceService.GridRowSpan);
				yOffs = (double)imageSourceService.GridRow / imageSourceService.TotalGridRows * wpfTextView.ViewportHeight;
			}
			else {
				viewportHeight = wpfTextView.ViewportHeight;
				yOffs = 0;
			}

			Size size;
			image.ClearValue(FrameworkElement.HeightProperty);
			image.ClearValue(FrameworkElement.WidthProperty);
			if (imageSourceService.Stretch == Stretch.None) {
				image.Stretch = imageSourceService.Stretch;
				image.StretchDirection = imageSourceService.StretchDirection;

				image.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				size = image.DesiredSize;

				if (resizeTooBigImages && viewportWidth != 0 && viewportHeight != 0 && (size.Width > viewportWidth || size.Height > viewportHeight)) {
					image.Stretch = Stretch.Uniform;
					image.StretchDirection = StretchDirection.Both;
					image.Measure(new Size(viewportWidth, viewportHeight));
					size = image.DesiredSize;
					image.Width = size.Width;
					image.Height = size.Height;
				}
			}
			else {
				image.Measure(new Size(viewportWidth, viewportHeight));
				size = image.DesiredSize;
				image.Width = size.Width;
				image.Height = size.Height;
			}
			Debug.Assert(viewportWidth == 0 || (size.Width > 0 && size.Width != double.PositiveInfinity));
			Debug.Assert(viewportHeight == 0 || (size.Height > 0 && size.Height != double.PositiveInfinity));

			switch (imageSourceService.ImagePlacement) {
			case ImagePlacement.TopLeft:
				break;

			case ImagePlacement.TopRight:
				xOffs += viewportWidth - size.Width;
				break;

			case ImagePlacement.BottomLeft:
				yOffs += viewportHeight - size.Height;
				break;

			case ImagePlacement.BottomRight:
				xOffs += viewportWidth - size.Width;
				yOffs += viewportHeight - size.Height;
				break;

			case ImagePlacement.Top:
				xOffs += (viewportWidth - size.Width) / 2;
				break;

			case ImagePlacement.Left:
				yOffs += (viewportHeight - size.Height) / 2;
				break;

			case ImagePlacement.Right:
				xOffs += viewportWidth - size.Width;
				yOffs += (viewportHeight - size.Height) / 2;
				break;

			case ImagePlacement.Bottom:
				xOffs += (viewportWidth - size.Width) / 2;
				yOffs += viewportHeight - size.Height;
				break;

			case ImagePlacement.Center:
				xOffs += (viewportWidth - size.Width) / 2;
				yOffs += (viewportHeight - size.Height) / 2;
				break;

			default:
				Debug.Fail($"Unknown {nameof(ImagePlacement)} value: {imageSourceService.ImagePlacement}");
				break;
			}

			Canvas.SetLeft(image, wpfTextView.ViewportLeft + imageSourceService.HorizontalOffset + xOffs);
			Canvas.SetTop(image, wpfTextView.ViewportTop + imageSourceService.VerticalOffset + yOffs);
		}

		public void OnSettingsChanged() {
			if (currentImage == null) {
				currentImage = new Image();
				AddImageToAdornmentLayer();
			}
			InitializeImage(currentImage);
		}

		void AddImageToAdornmentLayer() {
			Debug.Assert(currentImage != null);
			adornmentLayer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, null, currentImage, null);
		}

		void WpfTextView_Closed(object sender, EventArgs e) {
			OnDisabled();
			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.LayoutChanged -= WpfTextView_LayoutChanged;
			imageSourceService.Unregister(this);
		}
	}
}

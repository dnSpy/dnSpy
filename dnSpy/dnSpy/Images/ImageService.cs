/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Themes;
using dnSpy.Controls;

namespace dnSpy.Images {
	[Export(typeof(IImageService))]
	sealed class ImageService : IImageService {
		readonly Dictionary<ImageKey, WeakReference> imageCache;
		readonly Dictionary<Assembly, List<Lazy<IImageSourceInfoProvider, IImageSourceInfoProviderMetadata>>> imageSourceInfoProvidersDict;
		bool isHighContrast;
		readonly IThemeService themeService;
		readonly IDpiService dpiService;

		struct InternalImageOptions : IEquatable<InternalImageOptions> {
			public Color? BackgroundColor { get; set; }
			public Size PhysicalSize { get; set; }
			public Size Dpi { get; set; }

			public bool Equals(InternalImageOptions other) =>
				Nullable.Equals(BackgroundColor, other.BackgroundColor) &&
				PhysicalSize.Equals(other.PhysicalSize) &&
				Dpi.Equals(other.Dpi);

			public override bool Equals(object obj) => obj is InternalImageOptions && Equals((InternalImageOptions)obj);

			public override int GetHashCode() =>
				(BackgroundColor?.GetHashCode() ?? 0) ^
				PhysicalSize.GetHashCode() ^
				Dpi.GetHashCode();
		}

		struct ImageKey : IEquatable<ImageKey> {
			readonly string uri;
			/*readonly*/ InternalImageOptions options;

			public ImageKey(string uri, InternalImageOptions options) {
				this.uri = uri;
				this.options = options;
			}

			public bool Equals(ImageKey other) => StringComparer.OrdinalIgnoreCase.Equals(uri, other.uri) && options.Equals(other.options);
			public override bool Equals(object obj) => obj is ImageKey && Equals((ImageKey)obj);
			public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(uri) ^ options.GetHashCode();
			public override string ToString() => uri;
		}

		[ImportingConstructor]
		ImageService(IThemeService themeService, IDpiService dpiService, [ImportMany] IEnumerable<Lazy<IImageSourceInfoProvider, IImageSourceInfoProviderMetadata>> imageSourceInfoProviders) {
			this.themeService = themeService;
			this.dpiService = dpiService;
			imageCache = new Dictionary<ImageKey, WeakReference>();
			imageSourceInfoProvidersDict = new Dictionary<Assembly, List<Lazy<IImageSourceInfoProvider, IImageSourceInfoProviderMetadata>>>();
			this.themeService.ThemeChangedHighPriority += ThemeService_ThemeChangedHighPriority;
			foreach (var lz in imageSourceInfoProviders.OrderBy(a => a.Metadata.Order)) {
				if (!imageSourceInfoProvidersDict.TryGetValue(lz.Metadata.Type.Assembly, out var list)) {
					list = new List<Lazy<IImageSourceInfoProvider, IImageSourceInfoProviderMetadata>>();
					imageSourceInfoProvidersDict.Add(lz.Metadata.Type.Assembly, list);
					list.Add(CreateDefaultProvider(lz.Metadata.Type.Assembly));
				}
				Debug.Assert(list.Count >= 1);
				// The last one is the default provider we created above
				list.Insert(list.Count - 1, lz);
			}
		}

		Lazy<IImageSourceInfoProvider, IImageSourceInfoProviderMetadata> CreateDefaultProvider(Assembly assembly) {
			if (assembly == null)
				throw new ArgumentNullException(nameof(assembly));
			var attr = new ExportImageSourceInfoProviderAttribute(double.MaxValue);
			var lz = new Lazy<IImageSourceInfoProvider, IImageSourceInfoProviderMetadata>(() => new DefaultImageSourceInfoProvider(assembly), attr, isThreadSafe: false);
			var dummy = lz.Value;
			return lz;
		}

		List<Lazy<IImageSourceInfoProvider, IImageSourceInfoProviderMetadata>> GetProviders(Assembly assembly) {
			if (assembly == null)
				throw new ArgumentNullException(nameof(assembly));
			if (imageSourceInfoProvidersDict.TryGetValue(assembly, out var list))
				return list;
			list = new List<Lazy<IImageSourceInfoProvider, IImageSourceInfoProviderMetadata>>();
			list.Add(CreateDefaultProvider(assembly));
			imageSourceInfoProvidersDict.Add(assembly, list);
			return list;
		}

		void ThemeService_ThemeChangedHighPriority(object sender, ThemeChangedEventArgs e) {
			imageCache.Clear();
			isHighContrast = themeService.Theme.IsHighContrast;
		}

		Color? GetColor(Brush brush) => (brush as SolidColorBrush)?.Color;

		Size GetDpi(DependencyObject dpiObject, Size dpi) {
			if (dpiObject != null) {
				var window = Window.GetWindow(dpiObject) as MetroWindow;
				if (window != null)
					return window.WindowDpi;
			}

			if (dpi != new Size(0, 0))
				return dpi;

			return dpiService.MainWindowDpi;
		}

		static Size Round(Size size) => new Size(Math.Round(size.Width), Math.Round(size.Height));

		public BitmapSource GetImage(ImageReference imageReference, ImageOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			if (imageReference.Name == null)
				return null;

			var internalOptions = new InternalImageOptions();
			internalOptions.BackgroundColor = options.BackgroundColor ?? GetColor(options.BackgroundBrush);
			var logicalSize = options.LogicalSize;
			if (logicalSize == new Size(0, 0))
				logicalSize = new Size(16, 16);
			internalOptions.Dpi = GetDpi(options.DpiObject, options.Dpi);
			if (options.Zoom != new Size(0, 0))
				internalOptions.Dpi = new Size(internalOptions.Dpi.Width * options.Zoom.Width, internalOptions.Dpi.Height * options.Zoom.Height);
			internalOptions.Dpi = Round(internalOptions.Dpi);
			if (internalOptions.Dpi.Width == 0 || internalOptions.Dpi.Height == 0)
				return null;
			internalOptions.PhysicalSize = Round(new Size(logicalSize.Width * internalOptions.Dpi.Width / 96, logicalSize.Height * internalOptions.Dpi.Height / 96));

			if (internalOptions.PhysicalSize.Width == 0 || internalOptions.PhysicalSize.Height == 0)
				return null;

			if (imageReference.Assembly != null) {
				var name = imageReference.Name;
				foreach (var provider in GetProviders(imageReference.Assembly)) {
					var infos = provider.Value.GetImageSourceInfos(name);
					if (infos == null)
						continue;

					var infoList = new List<ImageSourceInfo>(infos);
					infoList.Sort((a, b) => {
						if (a.Size == b.Size)
							return 0;

						// Try exact size first
						if ((a.Size == internalOptions.PhysicalSize) != (b.Size == internalOptions.PhysicalSize))
							return a.Size == internalOptions.PhysicalSize ? -1 : 1;

						// Try any-size (xaml images)
						if ((a.Size == ImageSourceInfo.AnySize) != (b.Size == ImageSourceInfo.AnySize))
							return a.Size == ImageSourceInfo.AnySize ? -1 : 1;

						// Closest size (using height)
						if (a.Size.Height >= internalOptions.PhysicalSize.Height) {
							if (b.Size.Height < internalOptions.PhysicalSize.Height)
								return -1;
							return a.Size.Height.CompareTo(b.Size.Height);
						}
						else {
							if (b.Size.Height >= internalOptions.PhysicalSize.Height)
								return 1;
							return b.Size.Height.CompareTo(a.Size.Height);
						}
					});

					foreach (var info in infoList) {
						var bitmapSource = TryGetImage(info.Uri, internalOptions);
						if (bitmapSource != null)
							return bitmapSource;
					}

					return null;
				}
				return null;
			}
			else
				return TryGetImage(imageReference.Name, internalOptions);
		}

		BitmapSource TryGetImage(string uriString, InternalImageOptions options) {
			if (uriString == null)
				return null;

			var key = new ImageKey(uriString, options);
			BitmapSource image;
			if (imageCache.TryGetValue(key, out var weakImage)) {
				image = weakImage.Target as BitmapSource;
				if (image != null)
					return image;
			}

			image = TryLoadImage(uriString, options.PhysicalSize);
			if (image == null)
				return null;

			if (options.BackgroundColor != null)
				image = ThemedImageCreator.CreateThemedBitmapSource(image, options.BackgroundColor.Value, isHighContrast);
			imageCache[key] = new WeakReference(image);
			return image;
		}

		BitmapSource TryLoadImage(string uriString, Size physicalSize) {
			try {
				var uri = new Uri(uriString, UriKind.RelativeOrAbsolute);
				var info = Application.GetResourceStream(uri);
				if (info.ContentType.Equals("application/xaml+xml", StringComparison.OrdinalIgnoreCase) || info.ContentType.Equals("application/baml+xml", StringComparison.OrdinalIgnoreCase)) {
					var component = Application.LoadComponent(uri);
					var elem = component as FrameworkElement;
					if (elem != null)
						return ResizeElement(elem, physicalSize);
					return null;
				}
				else if (info.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) {
					var decoder = BitmapDecoder.Create(info.Stream, BitmapCreateOptions.None, BitmapCacheOption.OnDemand);
					if (decoder.Frames.Count == 0)
						return null;
					return ResizeImage(decoder.Frames[0], physicalSize);
				}
				else
					return null;
			}
			catch {
				return null;
			}
		}

		static BitmapSource ResizeImage(BitmapSource bitmapImage, Size physicalSize) {
			if (bitmapImage.PixelWidth == physicalSize.Width && bitmapImage.PixelHeight == physicalSize.Height)
				return bitmapImage;
			var image = new Image { Source = bitmapImage };
			RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
			return ResizeElement(image, physicalSize);
		}

		static BitmapSource ResizeElement(FrameworkElement elem, Size physicalSize) {
			elem.Width = physicalSize.Width;
			elem.Height = physicalSize.Height;
			elem.Measure(physicalSize);
			elem.Arrange(new Rect(physicalSize));
			var dv = new DrawingVisual();
			using (var dc = dv.RenderOpen()) {
				var brush = new VisualBrush(elem) { Stretch = Stretch.Uniform };
				dc.DrawRectangle(brush, null, new Rect(physicalSize));
			}
			Debug.Assert((int)physicalSize.Width == physicalSize.Width);
			Debug.Assert((int)physicalSize.Height == physicalSize.Height);
			var renderBmp = new RenderTargetBitmap((int)physicalSize.Width, (int)physicalSize.Height, 96, 96, PixelFormats.Pbgra32);
			renderBmp.Render(dv);
			return new FormatConvertedBitmap(renderBmp, PixelFormats.Bgra32, null, 0);
		}
	}
}

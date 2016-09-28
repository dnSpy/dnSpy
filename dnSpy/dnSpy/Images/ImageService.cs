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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Themes;

namespace dnSpy.Images {
	[Export(typeof(IImageService))]
	sealed class ImageService : IImageService {
		readonly Dictionary<ImageKey, BitmapSource> imageCache;
		readonly Dictionary<Assembly, List<Lazy<IImageSourceInfoProvider, IImageSourceInfoProviderMetadata>>> imageSourceInfoProvidersDict;
		bool isHighContrast;
		readonly IThemeService themeService;

		struct InternalImageOptions : IEquatable<InternalImageOptions> {
			public Color? BackgroundColor { get; set; }
			public Size LogicalSize { get; set; }
			public Size RealPixelSize { get; set; }
			public Size Dpi { get; set; }

			public bool Equals(InternalImageOptions other) =>
				Nullable.Equals(BackgroundColor, other.BackgroundColor) &&
				LogicalSize.Equals(other.LogicalSize) &&
				RealPixelSize.Equals(other.RealPixelSize) &&
				Dpi.Equals(other.Dpi);

			public override bool Equals(object obj) => obj is InternalImageOptions && Equals((InternalImageOptions)obj);

			public override int GetHashCode() =>
				(BackgroundColor?.GetHashCode() ?? 0) ^
				LogicalSize.GetHashCode() ^
				RealPixelSize.GetHashCode() ^
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
		ImageService(IThemeService themeService, [ImportMany] IEnumerable<Lazy<IImageSourceInfoProvider, IImageSourceInfoProviderMetadata>> imageSourceInfoProviders) {
			this.themeService = themeService;
			this.imageCache = new Dictionary<ImageKey, BitmapSource>();
			this.imageSourceInfoProvidersDict = new Dictionary<Assembly, List<Lazy<IImageSourceInfoProvider, IImageSourceInfoProviderMetadata>>>();
			this.themeService.ThemeChangedHighPriority += ThemeService_ThemeChangedHighPriority;
			foreach (var lz in imageSourceInfoProviders.OrderBy(a => a.Metadata.Order)) {
				List<Lazy<IImageSourceInfoProvider, IImageSourceInfoProviderMetadata>> list;
				if (!imageSourceInfoProvidersDict.TryGetValue(lz.Metadata.Type.Assembly, out list)) {
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
			List<Lazy<IImageSourceInfoProvider, IImageSourceInfoProviderMetadata>> list;
			if (imageSourceInfoProvidersDict.TryGetValue(assembly, out list))
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

		Color? GetColor(BackgroundType? bgType) {
			if (bgType == null)
				return null;

			switch (bgType.Value) {
			case BackgroundType.Button:				return GetColorBackground(ColorType.CommonControlsButtonIconBackground);
			case BackgroundType.TextEditor:			return GetColorBackground(ColorType.DefaultText);
			case BackgroundType.DialogWindow:		return GetColorBackground(ColorType.DialogWindow);
			case BackgroundType.TextBox:			return GetColorBackground(ColorType.CommonControlsTextBox);
			case BackgroundType.TreeNode:			return GetColorBackground(ColorType.TreeView);
			case BackgroundType.Search:				return GetColorBackground(ColorType.ListBoxBackground);
			case BackgroundType.ComboBox:			return GetColorBackground(ColorType.CommonControlsComboBoxBackground);
			case BackgroundType.ToolBar:			return GetColorBackground(ColorType.ToolBarIconBackground);
			case BackgroundType.AppMenuMenuItem:	return GetColorBackground(ColorType.ToolBarIconVerticalBackground);
			case BackgroundType.ContextMenuItem:	return GetColorBackground(ColorType.ContextMenuRectangleFill);
			case BackgroundType.GridViewItem:		return GetColorBackground(ColorType.GridViewBackground);
			case BackgroundType.ListBoxItem:		return GetColorBackground(ColorType.ListBoxBackground);
			case BackgroundType.QuickInfo:			return GetColorBackground(ColorType.QuickInfo);
			case BackgroundType.SignatureHelp:		return GetColorBackground(ColorType.SignatureHelp);
			case BackgroundType.TitleAreaActive:	return GetColorBackground(ColorType.EnvironmentMainWindowActiveCaption);
			case BackgroundType.TitleAreaInactive:	return GetColorBackground(ColorType.EnvironmentMainWindowInactiveCaption);
			case BackgroundType.CommandBar:			return GetColorBackground(ColorType.EnvironmentCommandBarIcon);
			case BackgroundType.GlyphMargin:		return GetColorBackground(ColorType.GlyphMargin);
			default:
				Debug.Fail("Invalid bg type");
				return null;
			}
		}

		Color GetColorBackground(ColorType colorType) {
			var c = themeService.Theme.GetColor(colorType).Background as SolidColorBrush;
			Debug.WriteLineIf(c == null, string.Format("Background color is null: {0}", colorType));
			return c.Color;
		}

		public BitmapSource GetImage(ImageReference imageReference, BackgroundType bgType) =>
			GetImage(imageReference, new ImageOptions { BackgroundType = bgType });

		public BitmapSource GetImage(ImageReference imageReference, Color? bgColor) =>
			GetImage(imageReference, new ImageOptions { BackgroundColor = bgColor });

		public BitmapSource GetImage(ImageReference imageReference, ImageOptions options) {
			if (imageReference.Name == null)
				return null;

			var internalOptions = new InternalImageOptions();
			internalOptions.BackgroundColor = options.BackgroundColor ?? GetColor(options.BackgroundType);
			internalOptions.LogicalSize = options.LogicalSize;
			if (internalOptions.LogicalSize == new Size(0, 0))
				internalOptions.LogicalSize = new Size(16, 16);
			internalOptions.Dpi = new Size(96, 96);//TODO:
			internalOptions.RealPixelSize = new Size(internalOptions.LogicalSize.Width * (internalOptions.Dpi.Width / 96), internalOptions.LogicalSize.Height * (internalOptions.Dpi.Height / 96));

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
						if ((a.Size == internalOptions.RealPixelSize) != (b.Size == internalOptions.RealPixelSize))
							return a.Size == internalOptions.RealPixelSize ? -1 : 1;

						// Try any-size (xaml images)
						if ((a.Size == ImageSourceInfo.AnySize) != (b.Size == ImageSourceInfo.AnySize))
							return a.Size == ImageSourceInfo.AnySize ? -1 : 1;

						// Closest size (using height)
						if (a.Size.Height >= internalOptions.RealPixelSize.Height) {
							if (b.Size.Height < internalOptions.RealPixelSize.Height)
								return -1;
							return a.Size.Height.CompareTo(b.Size.Height);
						}
						else {
							if (b.Size.Height >= internalOptions.RealPixelSize.Height)
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
			if (imageCache.TryGetValue(key, out image))
				return image;

			image = TryLoadImage(uriString, options.RealPixelSize, options.Dpi);
			if (image == null)
				return null;

			if (options.BackgroundColor != null)
				image = ThemedImageCreator.CreateThemedBitmapSource(image, options.BackgroundColor.Value, isHighContrast);
			imageCache.Add(key, image);
			return image;
		}

		BitmapSource TryLoadImage(string uriString, Size realPixelSize, Size dpi) {
			try {
				var uriKind = UriKind.RelativeOrAbsolute;
				if (uriString.StartsWith("pack:") || uriString.StartsWith("file:"))
					uriKind = UriKind.Absolute;
				var uri = new Uri(uriString, uriKind);
				var info = Application.GetResourceStream(uri);
				if (info.ContentType.Equals("application/xaml+xml", StringComparison.OrdinalIgnoreCase) || info.ContentType.Equals("application/baml+xml", StringComparison.OrdinalIgnoreCase)) {
					var component = Application.LoadComponent(uri);
					var elem = component as FrameworkElement;
					if (elem != null)
						return ResizeElement(elem, realPixelSize, dpi);
					var bitmapSource = component as BitmapSource;
					if (bitmapSource != null)
						return ResizeImage(bitmapSource, realPixelSize, dpi);
					return null;
				}
				else if (info.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) {
					var decoder = BitmapDecoder.Create(info.Stream, BitmapCreateOptions.None, BitmapCacheOption.OnDemand);
					if (decoder.Frames.Count == 0)
						return null;
					return ResizeImage(decoder.Frames[0], realPixelSize, dpi);
				}
				else
					return null;
			}
			catch {
				return null;
			}
		}

		static BitmapSource ResizeImage(BitmapSource bitmapImage, Size realPixelSize, Size dpi) {
			if (bitmapImage.PixelWidth == realPixelSize.Width && bitmapImage.PixelHeight == realPixelSize.Height)
				return bitmapImage;
			var image = new Image { Source = bitmapImage };
			RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
			return ResizeElement(image, realPixelSize, dpi);
		}

		static BitmapSource ResizeElement(FrameworkElement elem, Size realPixelSize, Size dpi) {
			elem.Width = realPixelSize.Width;
			elem.Height = realPixelSize.Height;
			elem.Measure(realPixelSize);
			elem.Arrange(new Rect(realPixelSize));
			var dv = new DrawingVisual();
			using (var dc = dv.RenderOpen()) {
				var brush = new VisualBrush(elem) { Stretch = Stretch.Uniform };
				dc.DrawRectangle(brush, null, new Rect(realPixelSize));
			}
			var renderBmp = new RenderTargetBitmap((int)realPixelSize.Width, (int)realPixelSize.Height, dpi.Width, dpi.Height, PixelFormats.Pbgra32);
			renderBmp.Render(dv);
			return new FormatConvertedBitmap(renderBmp, PixelFormats.Bgra32, null, 0);
		}
	}
}

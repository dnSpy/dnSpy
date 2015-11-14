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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Themes;

namespace dnSpy.Images {
	[Export, Export(typeof(IImageManager)), PartCreationPolicy(CreationPolicy.Shared)]
	public sealed class ImageManager : IImageManager {//TODO: REMOVE public
		readonly Dictionary<Tuple<string, Color>, BitmapSource> imageCache = new Dictionary<Tuple<string, Color>, BitmapSource>();
		bool isHighContrast;
		readonly IThemeManager themeManager;

		[ImportingConstructor]
		ImageManager(IThemeManager themeManager) {
			this.themeManager = themeManager;
		}

		public void OnThemeChanged() {//TODO: Should be internal
			imageCache.Clear();
			isHighContrast = themeManager.Theme.IsHighContrast;
		}

		Color GetColor(BackgroundType bgType) {
			switch (bgType) {
			case BackgroundType.Button: return GetColorBackground(ColorType.CommonControlsButtonIconBackground);
			case BackgroundType.TextEditor: return GetColorBackground(ColorType.DefaultText);
			case BackgroundType.DialogWindow: return GetColorBackground(ColorType.DialogWindow);
			case BackgroundType.TextBox: return GetColorBackground(ColorType.CommonControlsTextBox);
			case BackgroundType.TreeNode: return GetColorBackground(ColorType.TreeView);
			case BackgroundType.Search: return GetColorBackground(ColorType.ListBoxBackground);
			case BackgroundType.ComboBox: return GetColorBackground(ColorType.CommonControlsComboBoxBackground);
			case BackgroundType.ToolBar: return GetColorBackground(ColorType.ToolBarIconBackground);
			case BackgroundType.AppMenuMenuItem: return GetColorBackground(ColorType.ToolBarIconVerticalBackground);
			case BackgroundType.ContextMenuItem: return GetColorBackground(ColorType.ContextMenuRectangleFill);
			case BackgroundType.GridViewItem: return GetColorBackground(ColorType.GridViewBackground);
			case BackgroundType.CodeToolTip: return GetColorBackground(ColorType.CodeToolTip);
			case BackgroundType.TitleAreaActive: return GetColorBackground(ColorType.EnvironmentMainWindowActiveCaption);
			case BackgroundType.TitleAreaInactive: return GetColorBackground(ColorType.EnvironmentMainWindowInactiveCaption);
			case BackgroundType.CommandBar: return GetColorBackground(ColorType.EnvironmentCommandBarIcon);
			default:
				Debug.Fail("Invalid bg type");
				return GetColorBackground(ColorType.SystemColorsWindow);
			}
		}

		Color GetColorBackground(ColorType colorType) {
			var c = themeManager.Theme.GetColor(colorType).Background as SolidColorBrush;
			Debug.WriteLineIf(c == null, string.Format("Background color is null: {0}", colorType));
			return c.Color;
		}

		public BitmapSource GetImage(Assembly asm, string icon, BackgroundType bgType) {
			return GetImage(asm, icon, GetColor(bgType));
		}

		public BitmapSource GetImage(Assembly asm, string icon, Color bgColor) {
			var name = asm.GetName();
			var uri = "pack://application:,,,/" + name.Name + ";v" + name.Version + ";component/Images/" + icon + ".png";
			return GetImageUsingUri(uri, bgColor);
		}

		BitmapSource GetImageUsingUri(string uri, Color bgColor) {
			var key = Tuple.Create(uri, bgColor);
			BitmapSource image;
			if (imageCache.TryGetValue(key, out image))
				return image;

			image = ThemedImageCreator.CreateThemedBitmapSource(new BitmapImage(new Uri(uri)), bgColor, isHighContrast);
			imageCache.Add(key, image);
			return image;
		}
	}
}

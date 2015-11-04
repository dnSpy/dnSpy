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
using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace dnSpy.Images {
	public enum BackgroundType {
		Button,
		TextEditor,
		DialogWindow,
		TextBox,
		TreeNode,
		Search,
		ComboBox,
		Toolbar,
		ToolBarButtonChecked,
		MainMenuMenuItem,
		ContextMenuItem,
		GridViewItem,
		CodeToolTip,
		TitleAreaActive,
		TitleAreaInactive,
		CommandBar,
	}

	public struct ImageInfo {
		public readonly string Name;
		public readonly BackgroundType BackgroundType;

		public ImageInfo(string name, BackgroundType bgType) {
			this.Name = name;
			this.BackgroundType = bgType;
		}
	}

	public sealed class ImageCache {
		public static readonly ImageCache Instance = new ImageCache();

		readonly object lockObj = new object();
		readonly Dictionary<Tuple<string, Color>, BitmapSource> imageCache = new Dictionary<Tuple<string, Color>, BitmapSource>();
		bool isHighContrast;

		public void OnThemeChanged() {//TODO: Should be internal
			lock (lockObj) {
				imageCache.Clear();
				isHighContrast = DnTheme.Themes.Theme.IsHighContrast;
			}
		}

		public static Color GetColor(BackgroundType bgType) {
			switch (bgType) {
			case BackgroundType.Button: return GetColorBackground(DnTheme.ColorType.CommonControlsButtonIconBackground);
			case BackgroundType.TextEditor: return GetColorBackground(DnTheme.ColorType.DefaultText);
			case BackgroundType.DialogWindow: return GetColorBackground(DnTheme.ColorType.DialogWindow);
			case BackgroundType.TextBox: return GetColorBackground(DnTheme.ColorType.CommonControlsTextBox);
			case BackgroundType.TreeNode: return GetColorBackground(DnTheme.ColorType.TreeView);
			case BackgroundType.Search: return GetColorBackground(DnTheme.ColorType.ListBoxBackground);
			case BackgroundType.ComboBox: return GetColorBackground(DnTheme.ColorType.CommonControlsComboBoxBackground);
			case BackgroundType.Toolbar: return GetColorBackground(DnTheme.ColorType.ToolBarIconBackground);
			case BackgroundType.ToolBarButtonChecked: return GetColorBackground(DnTheme.ColorType.ToolBarButtonChecked);
			case BackgroundType.MainMenuMenuItem: return GetColorBackground(DnTheme.ColorType.ToolBarIconVerticalBackground);
			case BackgroundType.ContextMenuItem: return GetColorBackground(DnTheme.ColorType.ContextMenuRectangleFill);
			case BackgroundType.GridViewItem: return GetColorBackground(DnTheme.ColorType.GridViewBackground);
			case BackgroundType.CodeToolTip: return GetColorBackground(DnTheme.ColorType.CodeToolTip);
			case BackgroundType.TitleAreaActive: return GetColorBackground(DnTheme.ColorType.EnvironmentMainWindowActiveCaption);
			case BackgroundType.TitleAreaInactive: return GetColorBackground(DnTheme.ColorType.EnvironmentMainWindowInactiveCaption);
			case BackgroundType.CommandBar: return GetColorBackground(DnTheme.ColorType.EnvironmentCommandBarIcon);
			default:
				Debug.Fail("Invalid bg type");
				return GetColorBackground(DnTheme.ColorType.SystemColorsWindow);
			}
		}

		static Color GetColorBackground(DnTheme.ColorType colorType) {
			var c = DnTheme.Themes.Theme.GetColor(colorType).InheritedColor.Background.GetColor(null);
			Debug.WriteLineIf(c == null, string.Format("Background color is null: {0}", colorType));
			return c.Value;
		}

		public BitmapSource GetImage(Assembly asm, ImageInfo info) {
			if (info.Name == null)
				return null;
			return GetImage(asm, info.Name, info.BackgroundType);
		}

		public BitmapSource GetImage(Assembly asm, string icon, BackgroundType bgType) {
			return GetImage(asm, icon, GetColor(bgType));
		}

		public BitmapSource GetImage(Assembly asm, string icon, Color bgColor) {
			var name = asm.GetName();
			var uri = "pack://application:,,,/" + name.Name + ";v" + name.Version + ";component/Images/" + icon + ".png";
			return GetImageUsingUri(uri, bgColor);
		}

		public BitmapSource GetImageUsingUri(string key, BackgroundType bgType) {
			return GetImageUsingUri(key, GetColor(bgType));
		}

		public BitmapSource GetImageUsingUri(string uri, Color bgColor) {
			var key = Tuple.Create(uri, bgColor);
			BitmapSource image;
			lock (lockObj) {
				if (imageCache.TryGetValue(key, out image))
					return image;

				image = ThemedImageCreator.CreateThemedBitmapSource(new BitmapImage(new Uri(uri)), bgColor, isHighContrast);
				imageCache.Add(key, image);
			}
			return image;
		}

		public void CreateMenuItemImage(MenuItem menuItem, Assembly asm, string icon, BackgroundType bgType, bool? enable = null) {
			var image = new Image {
				Width = 16,
				Height = 16,
				Source = GetImage(asm, icon, bgType),
			};
			menuItem.Icon = image;
			if (enable == false)
				image.Opacity = 0.3;
		}
	}
}

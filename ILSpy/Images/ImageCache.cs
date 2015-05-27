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
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace ICSharpCode.ILSpy
{
	public enum BackgroundType
	{
		Button,
		Window,
		TreeNode,
		Search,
		ComboBox,
		Toolbar,
		MainMenuMenuItem,
		ContextMenuItem,
		GridViewItem,
		DebuggerToolTip,
	}

	public struct ImageInfo
	{
		public readonly string Name;
		public readonly BackgroundType BackgroundType;

		public ImageInfo(string name, BackgroundType bgType)
		{
			this.Name = name;
			this.BackgroundType = bgType;
		}
	}

	public sealed class ImageCache
	{
		public static readonly ImageCache Instance = new ImageCache();

		readonly Dictionary<Tuple<string, Color>, BitmapSource> imageCache = new Dictionary<Tuple<string, Color>, BitmapSource>();
		bool isHighContrast;

		internal void OnThemeChanged()
		{
			imageCache.Clear();
			isHighContrast = dntheme.Themes.Theme.IsHighContrast;
		}

		public BitmapSource GetImage(ImageInfo info)
		{
			if (info.Name == null)
				return null;
			return GetImage(info.Name, info.BackgroundType);
		}

		public BitmapSource GetImage(string name, BackgroundType bgType)
		{
			return GetImage(name, GetColor(bgType));
		}

		public static Color GetColor(BackgroundType bgType)
		{
			switch (bgType) {
			case BackgroundType.Button: return GetColorBackground(dntheme.ColorType.ButtonIconBackground);
			case BackgroundType.Window: return GetColorBackground(dntheme.ColorType.SystemColorsWindow);
			case BackgroundType.TreeNode: return GetColorBackground(dntheme.ColorType.TreeViewBackground);
			case BackgroundType.Search: return GetColorBackground(dntheme.ColorType.SystemColorsWindow);
			case BackgroundType.ComboBox: return GetColorBackground(dntheme.ColorType.SystemColorsWindow);
			case BackgroundType.Toolbar: return GetColorBackground(dntheme.ColorType.ToolBarIconBackground);
			case BackgroundType.MainMenuMenuItem: return GetColorBackground(dntheme.ColorType.ToolBarIconVerticalBackground);
			case BackgroundType.ContextMenuItem: return GetColorBackground(dntheme.ColorType.ContextMenuRectangleFill1);
			case BackgroundType.GridViewItem: return GetColorBackground(dntheme.ColorType.SystemColorsWindow);
			case BackgroundType.DebuggerToolTip: return Colors.White;//TODO: Update this when the debugger tooltips have been fixed
			default:
				Debug.Fail("Invalid bg type");
				return GetColorBackground(dntheme.ColorType.SystemColorsWindow);
			}
		}

		static Color GetColorBackground(dntheme.ColorType colorType)
		{
			return dntheme.Themes.Theme.GetColor(colorType).InheritedColor.Background.GetColor(null).Value;
		}

		static string GetUri(object part, string icon)
		{
			var assembly = part.GetType().Assembly;
			var name = assembly.GetName();
			return "pack://application:,,,/" + name.Name + ";v" + name.Version + ";component/" + icon;
		}

		public BitmapSource GetImage(string name, Color bgColor)
		{
			return GetImage(this, name, bgColor);
		}

		public BitmapSource GetImage(object part, string icon, BackgroundType bgType)
		{
			return GetImage(part, icon, GetColor(bgType));
		}

		public BitmapSource GetImage(object part, string icon, Color bgColor)
		{
			var assembly = part.GetType().Assembly;
			var name = assembly.GetName();
			var uri = "pack://application:,,,/" + name.Name + ";v" + name.Version + ";component/Images/" + icon + ".png";
			return GetImageUsingUri(uri, bgColor);
		}

		public BitmapSource GetImageUsingUri(string key, BackgroundType bgType)
		{
			return GetImageUsingUri(key, GetColor(bgType));
		}

		public BitmapSource GetImageUsingUri(string uri, Color bgColor)
		{
			var key = Tuple.Create(uri, bgColor);
			BitmapSource image;
			if (imageCache.TryGetValue(key, out image))
				return image;

			image = ThemedImageCreator.CreateThemedBitmapSource(new BitmapImage(new Uri(uri)), bgColor, isHighContrast);
			imageCache.Add(key, image);
			return image;
		}

		public static ImageSource GetIcon(TypeIcon icon, BackgroundType bgType)
		{
			return TreeNodes.TypeTreeNode.GetIcon(icon, bgType);
		}

		public static ImageSource GetIcon(MemberIcon icon, BackgroundType bgType)
		{
			return TreeNodes.FieldTreeNode.GetIcon(icon, bgType);
		}
	}
}

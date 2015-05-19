// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ICSharpCode.ILSpy.Debugger.Services
{
	static class ImageService
	{
		public static ImageSource GetImage(string name, BackgroundType bgType)
		{
			try {
				return ImageCache.Instance.GetImageUsingUri("pack://application:,,,/ILSpy.Debugger.Plugin;component/Images/" + name + ".png", bgType);
			}
			catch {
				// resource not found
				return null;
			}
		}

		public static Image LoadImage(ImageSource source, int width, int height)
		{
			return new Image {
				Width = width,
				Height = height,
				Source = source
			};
		}
	}
}

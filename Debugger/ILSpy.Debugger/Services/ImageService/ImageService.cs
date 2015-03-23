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
		static BitmapImage LoadBitmap(string name)
		{
			try {
				BitmapImage image = new BitmapImage(new Uri("pack://application:,,,/ILSpy.Debugger.Plugin;component/Images/" + name + ".png"));
				if (image == null)
					return null;
				image.Freeze();
				return image;
			}
			catch {
				// resource not found
				return null;
			}
		}
		
		public static readonly BitmapImage Breakpoint = LoadBitmap("Breakpoint");
		public static readonly BitmapImage DisabledBreakpoint = LoadBitmap("DisabledBreakpoint");
		public static readonly BitmapImage CurrentLine = LoadBitmap("CurrentLine");
		public static readonly BitmapImage SelectedReturnLine = LoadBitmap("SelectedReturnLine");
		
		public static ImageSource GetImage(string imageName)
		{
			return LoadBitmap(imageName);
		}

		public static BitmapImage LoadImage(object part, string icon)
		{
			Uri uri;
			var assembly = part.GetType().Assembly;
			var name = assembly.GetName();
			uri = new Uri("pack://application:,,,/" + name.Name + ";v" + name.Version + ";component/" + icon);
			BitmapImage image = new BitmapImage(uri);
			image.Freeze();
			return image;
		}

		public static Image LoadImage(BitmapImage source, int width, int height)
		{
			return new Image {
				Width = width,
				Height = height,
				Source = source
			};
		}
	}
}

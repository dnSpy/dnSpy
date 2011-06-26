// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
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
		public static readonly BitmapImage CurrentLine = LoadBitmap("CurrentLine");
		
		public static ImageSource GetImage(string imageName)
		{
			return LoadBitmap(imageName);
		}
	}
}

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

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media;

namespace ICSharpCode.ILSpy.AsmEditor.Resources
{
	sealed class ImageListOptions
	{
		public string Name;
		public ColorDepth ColorDepth;
		public Size ImageSize;
		public System.Drawing.Color TransparentColor;
		public List<ImageSource> ImageSources = new List<ImageSource>();

		public ImageListOptions()
		{
			this.Name = string.Empty;
			this.ColorDepth = ColorDepth.Depth32Bit;
			this.ImageSize = new Size(16, 16);
			this.TransparentColor = System.Drawing.Color.Transparent;
		}

		public ImageListOptions(ImageListOptions other)
		{
			this.Name = other.Name ?? string.Empty;
			this.ColorDepth = other.ColorDepth;
			this.ImageSize = other.ImageSize;
			this.TransparentColor = other.TransparentColor;
			this.ImageSources.AddRange(other.ImageSources);
		}
	}
}

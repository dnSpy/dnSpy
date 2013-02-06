// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ICSharpCode.ILSpy.TextView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes
{
	[Export(typeof(IResourceNodeFactory))]
	sealed class ImageResourceNodeFactory : IResourceNodeFactory
	{
		static readonly string[] imageFileExtensions = { ".png", ".gif", ".bmp", ".jpg" };

		public ILSpyTreeNode CreateNode(Resource resource)
		{
			EmbeddedResource er = resource as EmbeddedResource;
			if (er != null) {
				return CreateNode(er.Name, er.GetResourceStream());
			}
			return null;
		}

		public ILSpyTreeNode CreateNode(string key, object data)
		{
			if (data is System.Drawing.Image)
			{
				MemoryStream s = new MemoryStream();
				((System.Drawing.Image)data).Save(s, System.Drawing.Imaging.ImageFormat.Bmp);
				return new ImageResourceEntryNode(key, s);
			}
			if (!(data is Stream))
			    return null;
			foreach (string fileExt in imageFileExtensions) {
				if (key.EndsWith(fileExt, StringComparison.OrdinalIgnoreCase))
					return new ImageResourceEntryNode(key, (Stream)data);
			}
			return null;
		}
	}

	sealed class ImageResourceEntryNode : ResourceEntryNode
	{
		public ImageResourceEntryNode(string key, Stream data)
			: base(key, data)
		{
		}

		public override object Icon
		{
			get { return Images.ResourceImage; }
		}

		public override bool View(DecompilerTextView textView)
		{
			try {
				AvalonEditTextOutput output = new AvalonEditTextOutput();
				Data.Position = 0;
				BitmapImage image = new BitmapImage();
				image.BeginInit();
				image.StreamSource = Data;
				image.EndInit();
				output.AddUIElement(() => new Image { Source = image });
				output.WriteLine();
				output.AddButton(Images.Save, "Save", delegate {
					Save(null);
				});
				textView.ShowNode(output, this);
				return true;
			}
			catch (Exception) {
				return false;
			}
		}
	}
}

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
	sealed class IconResourceNodeFactory : IResourceNodeFactory
	{
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
			if (data is System.Drawing.Icon) {
				MemoryStream s = new MemoryStream();
				((System.Drawing.Icon)data).Save(s);
				return new IconResourceEntryNode(key, s);
			}
			if (data is Stream && key.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
				return new IconResourceEntryNode(key, (Stream)data);
			return null;
		}
	}

	sealed class IconResourceEntryNode : ResourceEntryNode
	{
		public IconResourceEntryNode(string key, Stream data)
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
				IconBitmapDecoder decoder = new IconBitmapDecoder(Data, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
				foreach (var frame in decoder.Frames) {
					output.Write(String.Format("{0}x{1}, {2} bit: ", frame.PixelHeight, frame.PixelWidth, frame.Thumbnail.Format.BitsPerPixel));
					AddIcon(output, frame);
					output.WriteLine();
				}
				output.AddButton(Images.Save, "Save", delegate {
					Save(null);
				});
				textView.ShowNode(output, this);
				return true;
			} catch (Exception) {
				return false;
			}
		}

		private static void AddIcon(AvalonEditTextOutput output, BitmapFrame frame)
		{
			output.AddUIElement(() => new Image { Source = frame });
		}
	}
}
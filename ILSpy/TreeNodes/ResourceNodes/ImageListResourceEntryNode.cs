// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy.TreeNodes
{
	[Export(typeof(IResourceNodeFactory))]
	sealed class ImageListResourceEntryNodeFactory : IResourceNodeFactory
	{
		#region IResourceNodeFactory Members

		public ILSpyTreeNode CreateNode(Mono.Cecil.Resource resource)
		{
			return null;
		}

		public ILSpyTreeNode CreateNode(string key, object data)
		{
			if (data is ImageListStreamer)
				return new ImageListResourceEntryNode(key, (ImageListStreamer)data);
			return null;
		}

		#endregion
	}

	sealed class ImageListResourceEntryNode : ILSpyTreeNode
	{
		private readonly string key;
		private readonly ImageList data;

		public ImageListResourceEntryNode(string key, ImageListStreamer data)
		{
			this.LazyLoading = true;
			this.key = key;
			this.data = new ImageList();
			this.data.ImageStream = data;
		}

		public override object Text
		{
			get { return key; }
		}

		public override object Icon
		{
			get { return Images.ResourceImage; }
		}

		protected override void LoadChildren()
		{
			int i = 0;
			foreach (Image image in this.data.Images) {
				var node = ResourceEntryNode.Create("Image" + i.ToString(), image);
				if (node != null)
					Children.Add(node);
				++i;
			}
		}


		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			EnsureLazyChildren();
		}
	}
}

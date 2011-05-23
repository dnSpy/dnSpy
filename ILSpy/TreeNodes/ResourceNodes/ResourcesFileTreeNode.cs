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
using System.Collections;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Resources;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes
{
	[Export(typeof(IResourceNodeFactory))]
	sealed class ResourcesFileTreeNodeFactory : IResourceNodeFactory
	{
		public ILSpyTreeNode CreateNode(Resource resource)
		{
			EmbeddedResource er = resource as EmbeddedResource;
			if (er != null && er.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase)) {
				return new ResourcesFileTreeNode(er);
			}
			return null;
		}

		public ILSpyTreeNode CreateNode(string key, Stream data)
		{
			return null;
		}
	}
	
	sealed class ResourcesFileTreeNode : ResourceTreeNode
	{
		public ResourcesFileTreeNode(EmbeddedResource er)
			: base(er)
		{
			this.LazyLoading = true;
		}

		public override object Icon
		{
			get { return Images.ResourceResourcesFile; }
		}

		protected override void LoadChildren()
		{
			EmbeddedResource er = this.Resource as EmbeddedResource;
			if (er != null) {
				Stream s = er.GetResourceStream();
				s.Position = 0;
				ResourceReader reader;
				try {
					reader = new ResourceReader(s);
				}
				catch (ArgumentException) {
					return;
				}
				foreach (DictionaryEntry entry in reader.Cast<DictionaryEntry>().OrderBy(e => e.Key.ToString())) {
					if (entry.Value is Stream)
						Children.Add(ResourceEntryNode.Create(entry.Key.ToString(), (Stream)entry.Value));
					else if (entry.Value is byte[])
						Children.Add(ResourceEntryNode.Create(entry.Key.ToString(), new MemoryStream((byte[])entry.Value)));
				}
			}
		}
	}
}

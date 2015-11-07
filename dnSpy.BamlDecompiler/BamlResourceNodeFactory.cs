/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.ComponentModel.Composition;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.TreeNodes;

namespace dnSpy.BamlDecompiler {
	[Export(typeof(IResourceFactory<ResourceElement, ResourceElementTreeNode>))]
	public sealed class BamlResourceNodeFactory : IResourceFactory<ResourceElement, ResourceElementTreeNode> {
		public int Priority {
			get { return 0; }
		}

		public ResourceElementTreeNode Create(ModuleDef module, ResourceElement resInput) {
			if (resInput.ResourceData.Code != ResourceTypeCode.ByteArray && resInput.ResourceData.Code != ResourceTypeCode.Stream)
				return null;

			var data = (byte[])((BuiltInResourceData)resInput.ResourceData).Data;

			if (!resInput.Name.EndsWith(".baml", StringComparison.OrdinalIgnoreCase))
				return null;

			return new BamlResourceNode(module, resInput, new MemoryStream(data));
		}
	}
}
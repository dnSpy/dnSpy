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

using System.ComponentModel.Composition;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.BamlDecompiler.Baml;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;
using dnSpy.Contracts.TreeView;

namespace dnSpy.BamlDecompiler {
	[ExportResourceNodeProvider(Order = DocumentTreeViewConstants.ORDER_RSRCPROVIDER_BAML_NODE)]
	sealed class BamlResourceNodeProvider : IResourceNodeProvider {
		readonly BamlSettingsImpl bamlSettings;
		readonly IXamlOutputOptionsProvider xamlOutputOptionsProvider;
		readonly IDocumentWriterService documentWriterService;

		[ImportingConstructor]
		BamlResourceNodeProvider(BamlSettingsImpl bamlSettings, IXamlOutputOptionsProvider xamlOutputOptionsProvider, IDocumentWriterService documentWriterService) {
			this.bamlSettings = bamlSettings;
			this.xamlOutputOptionsProvider = xamlOutputOptionsProvider;
			this.documentWriterService = documentWriterService;
		}

		public DocumentTreeNodeData Create(ModuleDef module, Resource resource, ITreeNodeGroup treeNodeGroup) => null;

		public DocumentTreeNodeData Create(ModuleDef module, ResourceElement resourceElement, ITreeNodeGroup treeNodeGroup) {
			if (resourceElement.ResourceData.Code != ResourceTypeCode.ByteArray && resourceElement.ResourceData.Code != ResourceTypeCode.Stream)
				return null;

			var data = (byte[])((BuiltInResourceData)resourceElement.ResourceData).Data;

			if (!BamlReader.IsBamlHeader(new MemoryStream(data)))
				return null;

			return new BamlResourceElementNode(module, resourceElement, data, treeNodeGroup, bamlSettings, xamlOutputOptionsProvider, documentWriterService);
		}
	}
}

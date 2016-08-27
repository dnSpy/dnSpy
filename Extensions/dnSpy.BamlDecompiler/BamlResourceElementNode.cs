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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.BamlDecompiler.Baml;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.BamlDecompiler {
	sealed class BamlResourceElementNode : ResourceElementNode, IDecompileSelf {
		readonly ModuleDef module;
		readonly byte[] bamlData;
		readonly BamlSettings bamlSettings;

		public bool DisassembleBaml => bamlSettings.DisassembleBaml;
		public override Guid Guid => new Guid(FileTVConstants.BAML_RESOURCE_ELEMENT_NODE_GUID);
		protected override ImageReference GetIcon() => new ImageReference(GetType().Assembly, "XamlFile");

		public BamlResourceElementNode(ModuleDef module, ResourceElement resourceElement, byte[] bamlData, ITreeNodeGroup treeNodeGroup, BamlSettings bamlSettings)
			: base(treeNodeGroup, resourceElement) {
			this.module = module;
			this.bamlData = bamlData;
			this.bamlSettings = bamlSettings;
		}

		void Disassemble(ModuleDef module, BamlDocument document, IDecompiler lang,
			IDecompilerOutput output, CancellationToken token) {
			var disassembler = new BamlDisassembler(lang, output, token);
			disassembler.Disassemble(module, document);
		}

		void Decompile(ModuleDef module, BamlDocument document, IDecompiler lang,
			IDecompilerOutput output, CancellationToken token) {
			var decompiler = new XamlDecompiler();
			var xaml = decompiler.Decompile(module, document, token, BamlDecompilerOptions.Create(lang), null);

			output.Write(xaml.ToString(), BoxedTextColor.Text);
		}

		protected override IEnumerable<ResourceData> GetDeserializedData() {
			yield return new ResourceData(GetFilename(), token => GetDecompiledStream(token));
		}

		public string GetFilename() {
			string otherExt, targetExt;
			if (bamlSettings.DisassembleBaml) {
				otherExt = ".xaml";
				targetExt = ".baml";
			}
			else {
				otherExt = ".baml";
				targetExt = ".xaml";
			}

			string s = ResourceElement.Name;
			if (s.EndsWith(otherExt, StringComparison.OrdinalIgnoreCase))
				return s.Substring(0, s.Length - otherExt.Length) + targetExt;
			if (s.EndsWith(targetExt, StringComparison.OrdinalIgnoreCase))
				return s;
			return s + targetExt;
		}

		Stream GetDecompiledStream(CancellationToken token) {
			var output = new StringBuilderDecompilerOutput();
			Decompile(output, token);
			return ResourceUtilities.StringToStream(output.ToString());
		}

		public bool Decompile(IDecompileNodeContext context) {
			context.ContentTypeString = Decompile(context.Output, context.DecompilationContext.CancellationToken);
			return true;
		}

		public string Decompile(IDecompilerOutput output, CancellationToken token) {
			var lang = Context.Decompiler;
			var document = BamlReader.ReadDocument(new MemoryStream(bamlData), token);
			if (bamlSettings.DisassembleBaml) {
				Disassemble(module, document, lang, output, token);
				return ContentTypes.BamlDnSpy;
			}
			else {
				Decompile(module, document, lang, output, token);
				return ContentTypes.Xaml;
			}
		}

		public override string ToString(CancellationToken token, bool canDecompile) {
			if (!canDecompile)
				return null;
			var output = new StringBuilderDecompilerOutput();
			Decompile(output, token);
			return output.ToString();
		}
	}
}
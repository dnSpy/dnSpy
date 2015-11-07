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
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.BamlDecompiler.Baml;
using dnSpy.NRefactory;
using dnSpy.TreeNodes;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;

namespace dnSpy.BamlDecompiler {
	public class BamlResourceNode : ResourceElementTreeNode {
		ModuleDef module;
		string bamlName;
		Stream bamlData;

		public BamlResourceNode(ModuleDef module, ResourceElement resElem, Stream bamlData)
			: base(resElem) {
			this.module = module;
			bamlName = resElem.Name;
			this.bamlData = bamlData;
		}

		public override string IconName {
			get { return "XamlFile"; }
		}

		void Disassemble(ModuleDef module, BamlDocument document, Language lang,
			ITextOutput output, out IHighlightingDefinition highlight, CancellationToken token) {
			var disassembler = new BamlDisassembler(lang, output, token);
			disassembler.Disassemble(module, document);
			highlight = HighlightingManager.Instance.GetDefinitionByExtension(".cs");
		}

		void Decompile(ModuleDef module, BamlDocument document, Language lang,
			ITextOutput output, out IHighlightingDefinition highlight, CancellationToken token) {
			var decompiler = new XamlDecompiler();
			var xaml = decompiler.Decompile(module, document, token);

			output.Write(xaml.ToString(), TextTokenType.Text);
			highlight = HighlightingManager.Instance.GetDefinitionByExtension(".xml");
		}

		public override bool View(DecompilerTextView textView) {
			AvalonEditTextOutput output = new AvalonEditTextOutput();
			IHighlightingDefinition highlighting = null;
			var lang = MainWindow.Instance.CurrentLanguage;

			textView.RunWithCancellation(
				token => Task.Factory.StartNew(
					() => {
						try {
							bamlData.Position = 0;
							var document = BamlReader.ReadDocument(bamlData, token);
							if (BamlSettings.Instance.DisassembleBaml)
								Disassemble(module, document, lang, output, out highlighting, token);
							else
								Decompile(module, document, lang, output, out highlighting, token);
						}
						catch (Exception ex) {
							output.Write(ex.ToString(), TextTokenType.Text);
						}
						return output;
					}, token)
				).Then(t => textView.ShowNode(t, this, highlighting)).HandleExceptions();
			return true;
		}

		protected override IEnumerable<ResourceData> GetDeserialized() {
			yield return new ResourceData(resElem.Name, () => bamlData);
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("baml", UIUtils.CleanUpName(resElem.Name)); }
		}
	}
}
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnSpy.BamlDecompiler.Baml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.NRefactory;
using ICSharpCode.TreeView;

namespace dnSpy.BamlDecompiler {
	internal class BamlResourceNode : ResourceEntryNode {
		string bamlName;
		Stream bamlData;

		bool isDisassembly = false;

		public BamlResourceNode(string bamlName, Stream bamlData)
			: base(bamlName, bamlData) {
			this.bamlName = bamlName;
			this.bamlData = bamlData;
		}

		ModuleDef FindModule() {
			SharpTreeNode node = this;
			while (node != null && !(node is AssemblyTreeNode))
				node = node.Parent;
			if (node == null)
				return null;
			return ((AssemblyTreeNode)node).LoadedAssembly.ModuleDefinition;
		}

		// TODO: Save state in history?
		public void ToggleDisassembly() {
			isDisassembly = !isDisassembly;
			View(MainWindow.Instance.SafeActiveTextView);
		}

		public override bool View(DecompilerTextView textView) {
			AvalonEditTextOutput output = new AvalonEditTextOutput();
			IHighlightingDefinition highlighting = null;
			var lang = MainWindow.Instance.CurrentLanguage;
			var module = FindModule();

			textView.RunWithCancellation(
				token => Task.Factory.StartNew(
					() => {
						try {
							bamlData.Position = 0;
							var document = BamlReader.ReadDocument(bamlData, token);
							if (isDisassembly)
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

		void Disassemble(ModuleDef module, BamlDocument document, Language lang,
			AvalonEditTextOutput output, out IHighlightingDefinition highlight, CancellationToken token) {
			var disassembler = new BamlDisassembler(lang, output, token);
			disassembler.Disassemble(module, document);
			highlight = HighlightingManager.Instance.GetDefinitionByExtension(".cs");
		}

		void Decompile(ModuleDef module, BamlDocument document, Language lang,
			AvalonEditTextOutput output, out IHighlightingDefinition highlight, CancellationToken token) {
			var decompiler = new XamlDecompiler();
			var xaml = decompiler.Decompile(module, document, token);

			output.Write(xaml.ToString(), TextTokenType.Text);
			highlight = HighlightingManager.Instance.GetDefinitionByExtension(".xml");
		}
	}
}
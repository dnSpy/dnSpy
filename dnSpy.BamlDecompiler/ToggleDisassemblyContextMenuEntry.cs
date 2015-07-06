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

using ICSharpCode.ILSpy;

namespace dnSpy.BamlDecompiler {
	[ExportContextMenuEntry(Header = "Toggle _BAML Disassembly", Order = 130)]
	internal class ToggleDisassemblyContextMenuEntry : IContextMenuEntry {
		public bool IsVisible(TextViewContext context) {
			return context.SelectedTreeNodes != null &&
			       context.SelectedTreeNodes.Length > 0 &&
			       context.SelectedTreeNodes[0] is BamlResourceNode;
		}

		public bool IsEnabled(TextViewContext context) {
			return IsVisible(context);
		}

		public void Execute(TextViewContext context) {
			var node = (BamlResourceNode)context.SelectedTreeNodes[0];
			node.ToggleDisassembly();
		}
	}
}
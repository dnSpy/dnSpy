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


namespace ICSharpCode.ILSpy.TextView {
	[ExportContextMenuEntry(Header = "Cop_y", Icon = "Copy", Category = "Editor", InputGestureText = "Ctrl+C", Order = 1000)]
	internal sealed class CopyContextMenuEntry : IContextMenuEntry {
		public bool IsVisible(ContextMenuEntryContext context) {
			return IsEnabled(context);
		}

		public bool IsEnabled(ContextMenuEntryContext context) {
			return context.Element is DecompilerTextView && ((DecompilerTextView)context.Element).textEditor.SelectionLength > 0;
		}

		public void Execute(ContextMenuEntryContext context) {
			((DecompilerTextView)context.Element).textEditor.Copy();
		}
	}
}

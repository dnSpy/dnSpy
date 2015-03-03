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
using System.Linq;
using System.Windows.Input;

using ICSharpCode.AvalonEdit.Editing;

namespace ICSharpCode.AvalonEdit.Snippets
{
	sealed class SnippetInputHandler : TextAreaStackedInputHandler
	{
		readonly InsertionContext context;
		
		public SnippetInputHandler(InsertionContext context)
			: base(context.TextArea)
		{
			this.context = context;
		}
		
		public override void Attach()
		{
			base.Attach();
			
			SelectElement(FindNextEditableElement(-1, false));
		}
		
		public override void Detach()
		{
			base.Detach();
			context.Deactivate(new SnippetEventArgs(DeactivateReason.InputHandlerDetached));
		}
		
		public override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);
			if (e.Key == Key.Escape) {
				context.Deactivate(new SnippetEventArgs(DeactivateReason.EscapePressed));
				e.Handled = true;
			} else if (e.Key == Key.Return) {
				context.Deactivate(new SnippetEventArgs(DeactivateReason.ReturnPressed));
				e.Handled = true;
			} else if (e.Key == Key.Tab) {
				bool backwards = e.KeyboardDevice.Modifiers == ModifierKeys.Shift;
				SelectElement(FindNextEditableElement(TextArea.Caret.Offset, backwards));
				e.Handled = true;
			}
		}
		
		void SelectElement(IActiveElement element)
		{
			if (element != null) {
				TextArea.Selection = Selection.Create(TextArea, element.Segment);
				TextArea.Caret.Offset = element.Segment.EndOffset;
			}
		}
		
		IActiveElement FindNextEditableElement(int offset, bool backwards)
		{
			IEnumerable<IActiveElement> elements = context.ActiveElements.Where(e => e.IsEditable && e.Segment != null);
			if (backwards) {
				elements = elements.Reverse();
				foreach (IActiveElement element in elements) {
					if (offset > element.Segment.EndOffset)
						return element;
				}
			} else {
				foreach (IActiveElement element in elements) {
					if (offset < element.Segment.Offset)
						return element;
				}
			}
			return elements.FirstOrDefault();
		}
	}
}

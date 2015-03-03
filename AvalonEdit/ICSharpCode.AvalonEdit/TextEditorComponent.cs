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
using System.ComponentModel;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit
{
	/// <summary>
	/// Represents a text editor control (<see cref="TextEditor"/>, <see cref="TextArea"/>
	/// or <see cref="TextView"/>).
	/// </summary>
	public interface ITextEditorComponent : IServiceProvider
	{
		/// <summary>
		/// Gets the document being edited.
		/// </summary>
		TextDocument Document { get; }
		
		/// <summary>
		/// Occurs when the Document property changes (when the text editor is connected to another
		/// document - not when the document content changes).
		/// </summary>
		event EventHandler DocumentChanged;
		
		/// <summary>
		/// Gets the options of the text editor.
		/// </summary>
		TextEditorOptions Options { get; }
		
		/// <summary>
		/// Occurs when the Options property changes, or when an option inside the current option list
		/// changes.
		/// </summary>
		event PropertyChangedEventHandler OptionChanged;
	}
}

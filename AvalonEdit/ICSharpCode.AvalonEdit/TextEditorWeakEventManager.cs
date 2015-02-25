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

using ICSharpCode.AvalonEdit.Utils;
using System;

namespace ICSharpCode.AvalonEdit
{
	/// <summary>
	/// Contains weak event managers for <see cref="ITextEditorComponent"/>.
	/// </summary>
	public static class TextEditorWeakEventManager
	{
		/// <summary>
		/// Weak event manager for the <see cref="ITextEditorComponent.DocumentChanged"/> event.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public sealed class DocumentChanged : WeakEventManagerBase<DocumentChanged, ITextEditorComponent>
		{
			/// <inheritdoc/>
			protected override void StartListening(ITextEditorComponent source)
			{
				source.DocumentChanged += DeliverEvent;
			}
			
			/// <inheritdoc/>
			protected override void StopListening(ITextEditorComponent source)
			{
				source.DocumentChanged -= DeliverEvent;
			}
		}
		
		/// <summary>
		/// Weak event manager for the <see cref="ITextEditorComponent.OptionChanged"/> event.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public sealed class OptionChanged : WeakEventManagerBase<OptionChanged, ITextEditorComponent>
		{
			/// <inheritdoc/>
			protected override void StartListening(ITextEditorComponent source)
			{
				source.OptionChanged += DeliverEvent;
			}
			
			/// <inheritdoc/>
			protected override void StopListening(ITextEditorComponent source)
			{
				source.OptionChanged -= DeliverEvent;
			}
		}
	}
}

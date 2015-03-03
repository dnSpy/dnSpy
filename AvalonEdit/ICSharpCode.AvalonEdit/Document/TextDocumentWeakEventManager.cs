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
#if NREFACTORY
using ICSharpCode.NRefactory.Editor;
#endif
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// Contains weak event managers for the TextDocument events.
	/// </summary>
	public static class TextDocumentWeakEventManager
	{
		/// <summary>
		/// Weak event manager for the <see cref="TextDocument.UpdateStarted"/> event.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public sealed class UpdateStarted : WeakEventManagerBase<UpdateStarted, TextDocument>
		{
			/// <inheritdoc/>
			protected override void StartListening(TextDocument source)
			{
				source.UpdateStarted += DeliverEvent;
			}
			
			/// <inheritdoc/>
			protected override void StopListening(TextDocument source)
			{
				source.UpdateStarted -= DeliverEvent;
			}
		}
		
		/// <summary>
		/// Weak event manager for the <see cref="TextDocument.UpdateFinished"/> event.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public sealed class UpdateFinished : WeakEventManagerBase<UpdateFinished, TextDocument>
		{
			/// <inheritdoc/>
			protected override void StartListening(TextDocument source)
			{
				source.UpdateFinished += DeliverEvent;
			}
			
			/// <inheritdoc/>
			protected override void StopListening(TextDocument source)
			{
				source.UpdateFinished -= DeliverEvent;
			}
		}
		
		/// <summary>
		/// Weak event manager for the <see cref="TextDocument.Changing"/> event.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public sealed class Changing : WeakEventManagerBase<Changing, TextDocument>
		{
			/// <inheritdoc/>
			protected override void StartListening(TextDocument source)
			{
				source.Changing += DeliverEvent;
			}
			
			/// <inheritdoc/>
			protected override void StopListening(TextDocument source)
			{
				source.Changing -= DeliverEvent;
			}
		}
		
		/// <summary>
		/// Weak event manager for the <see cref="TextDocument.Changed"/> event.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public sealed class Changed : WeakEventManagerBase<Changed, TextDocument>
		{
			/// <inheritdoc/>
			protected override void StartListening(TextDocument source)
			{
				source.Changed += DeliverEvent;
			}
			
			/// <inheritdoc/>
			protected override void StopListening(TextDocument source)
			{
				source.Changed -= DeliverEvent;
			}
		}
		
		/// <summary>
		/// Weak event manager for the <see cref="TextDocument.LineCountChanged"/> event.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		[Obsolete("The TextDocument.LineCountChanged event will be removed in a future version. Use PropertyChangedEventManager instead.")]
		public sealed class LineCountChanged : WeakEventManagerBase<LineCountChanged, TextDocument>
		{
			/// <inheritdoc/>
			protected override void StartListening(TextDocument source)
			{
				source.LineCountChanged += DeliverEvent;
			}
			
			/// <inheritdoc/>
			protected override void StopListening(TextDocument source)
			{
				source.LineCountChanged -= DeliverEvent;
			}
		}
		
		/// <summary>
		/// Weak event manager for the <see cref="TextDocument.TextLengthChanged"/> event.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		[Obsolete("The TextDocument.TextLengthChanged event will be removed in a future version. Use PropertyChangedEventManager instead.")]
		public sealed class TextLengthChanged : WeakEventManagerBase<TextLengthChanged, TextDocument>
		{
			/// <inheritdoc/>
			protected override void StartListening(TextDocument source)
			{
				source.TextLengthChanged += DeliverEvent;
			}
			
			/// <inheritdoc/>
			protected override void StopListening(TextDocument source)
			{
				source.TextLengthChanged -= DeliverEvent;
			}
		}
		
		/// <summary>
		/// Weak event manager for the <see cref="TextDocument.TextChanged"/> event.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public sealed class TextChanged : WeakEventManagerBase<TextChanged, TextDocument>
		{
			/// <inheritdoc/>
			protected override void StartListening(TextDocument source)
			{
				source.TextChanged += DeliverEvent;
			}
			
			/// <inheritdoc/>
			protected override void StopListening(TextDocument source)
			{
				source.TextChanged -= DeliverEvent;
			}
		}
	}
}

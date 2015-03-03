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

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Abstract base class for generators that produce new visual line elements.
	/// </summary>
	public abstract class VisualLineElementGenerator
	{
		/// <summary>
		/// Gets the text run construction context.
		/// </summary>
		protected ITextRunConstructionContext CurrentContext { get; private set; }
		
		/// <summary>
		/// Initializes the generator for the <see cref="ITextRunConstructionContext"/>
		/// </summary>
		public virtual void StartGeneration(ITextRunConstructionContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.CurrentContext = context;
		}
		
		/// <summary>
		/// De-initializes the generator.
		/// </summary>
		public virtual void FinishGeneration()
		{
			this.CurrentContext = null;
		}
		
		/// <summary>
		/// Should only be used by VisualLine.ConstructVisualElements.
		/// </summary>
		internal int cachedInterest;
		
		/// <summary>
		/// Gets the first offset >= startOffset where the generator wants to construct an element.
		/// Return -1 to signal no interest.
		/// </summary>
		public abstract int GetFirstInterestedOffset(int startOffset);
		
		/// <summary>
		/// Constructs an element at the specified offset.
		/// May return null if no element should be constructed.
		/// </summary>
		/// <remarks>
		/// Avoid signalling interest and then building no element by returning null - doing so
		/// causes the generated <see cref="VisualLineText"/> elements to be unnecessarily split
		/// at the position where you signalled interest.
		/// </remarks>
		public abstract VisualLineElement ConstructElement(int offset);
	}
	
	internal interface IBuiltinElementGenerator
	{
		void FetchOptions(TextEditorOptions options);
	}
}

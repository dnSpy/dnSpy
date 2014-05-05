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

using System;
using System.Collections.Generic;
using System.Windows;

using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.ILSpy.TextView
{
	using Pair = KeyValuePair<int, Lazy<UIElement>>;
	
	/// <summary>
	/// Embeds UIElements in the text output.
	/// </summary>
	sealed class UIElementGenerator : VisualLineElementGenerator, IComparer<Pair>
	{
		/// <summary>
		/// The list of embedded UI elements to be displayed.
		/// We store this as a sorted list of (offset, Lazy&lt;UIElement&gt;) pairs.
		/// The "Lazy" part is used to create UIElements on demand (and thus on the UI thread, not on the decompiler thread).
		/// </summary>
		public List<Pair> UIElements;
		
		public override int GetFirstInterestedOffset(int startOffset)
		{
			if (this.UIElements == null)
				return -1;
			int r = this.UIElements.BinarySearch(new Pair(startOffset, null), this);
			// If the element isn't found, BinarySearch returns the complement of "insertion position".
			// We use this to find the next element (if there wasn't any exact match).
			if (r < 0)
				r = ~r;
			if (r < this.UIElements.Count)
				return this.UIElements[r].Key;
			else
				return -1;
		}
		
		public override VisualLineElement ConstructElement(int offset)
		{
			if (this.UIElements == null)
				return null;
			int r = UIElements.BinarySearch(new Pair(offset, null), this);
			if (r >= 0)
				return new InlineObjectElement(0, this.UIElements[r].Value.Value);
			else
				return null;
		}
		
		int IComparer<Pair>.Compare(Pair x, Pair y)
		{
			// Compare (offset,Lazy<UIElement>) pairs by the offset.
			// Used in BinarySearch()
			return x.Key.CompareTo(y.Key);
		}
	}
}

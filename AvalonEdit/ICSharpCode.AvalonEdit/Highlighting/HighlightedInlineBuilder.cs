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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Takes a series of highlighting commands and stores them.
	/// Later, it can build inline objects (for use with WPF TextBlock) from the commands.
	/// </summary>
	/// <remarks>
	/// This class is not used in AvalonEdit - but it is useful for someone who wants to put a HighlightedLine
	/// into a TextBlock.
	/// In SharpDevelop, we use it to provide syntax highlighting inside the search results pad.
	/// </remarks>
	[Obsolete("Use RichText / RichTextModel instead")]
	public sealed class HighlightedInlineBuilder
	{
		static HighlightingBrush MakeBrush(Brush b)
		{
			SolidColorBrush scb = b as SolidColorBrush;
			if (scb != null)
				return new SimpleHighlightingBrush(scb);
			else
				return null;
		}
		
		readonly string text;
		List<int> stateChangeOffsets = new List<int>();
		List<HighlightingColor> stateChanges = new List<HighlightingColor>();
		
		int GetIndexForOffset(int offset)
		{
			if (offset < 0 || offset > text.Length)
				throw new ArgumentOutOfRangeException("offset");
			int index = stateChangeOffsets.BinarySearch(offset);
			if (index < 0) {
				index = ~index;
				if (offset < text.Length) {
					stateChanges.Insert(index, stateChanges[index - 1].Clone());
					stateChangeOffsets.Insert(index, offset);
				}
			}
			return index;
		}
		
		/// <summary>
		/// Creates a new HighlightedInlineBuilder instance.
		/// </summary>
		public HighlightedInlineBuilder(string text)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			this.text = text;
			stateChangeOffsets.Add(0);
			stateChanges.Add(new HighlightingColor());
		}
		
		/// <summary>
		/// Creates a new HighlightedInlineBuilder instance.
		/// </summary>
		public HighlightedInlineBuilder(RichText text)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			this.text = text.Text;
			stateChangeOffsets.AddRange(text.stateChangeOffsets);
			stateChanges.AddRange(text.stateChanges);
		}
		
		HighlightedInlineBuilder(string text, List<int> offsets, List<HighlightingColor> states)
		{
			this.text = text;
			stateChangeOffsets = offsets;
			stateChanges = states;
		}
		
		/// <summary>
		/// Gets the text.
		/// </summary>
		public string Text {
			get { return text; }
		}
		
		/// <summary>
		/// Applies the properties from the HighlightingColor to the specified text segment.
		/// </summary>
		public void SetHighlighting(int offset, int length, HighlightingColor color)
		{
			if (color == null)
				throw new ArgumentNullException("color");
			if (color.Foreground == null && color.Background == null && color.FontStyle == null && color.FontWeight == null && color.Underline == null) {
				// Optimization: don't split the HighlightingState when we're not changing
				// any property. For example, the "Punctuation" color in C# is
				// empty by default.
				return;
			}
			int startIndex = GetIndexForOffset(offset);
			int endIndex = GetIndexForOffset(offset + length);
			for (int i = startIndex; i < endIndex; i++) {
				stateChanges[i].MergeWith(color);
			}
		}
		
		/// <summary>
		/// Sets the foreground brush on the specified text segment.
		/// </summary>
		public void SetForeground(int offset, int length, Brush brush)
		{
			int startIndex = GetIndexForOffset(offset);
			int endIndex = GetIndexForOffset(offset + length);
			var hbrush = MakeBrush(brush);
			for (int i = startIndex; i < endIndex; i++) {
				stateChanges[i].Foreground = hbrush;
			}
		}
		
		/// <summary>
		/// Sets the background brush on the specified text segment.
		/// </summary>
		public void SetBackground(int offset, int length, Brush brush)
		{
			int startIndex = GetIndexForOffset(offset);
			int endIndex = GetIndexForOffset(offset + length);
			var hbrush = MakeBrush(brush);
			for (int i = startIndex; i < endIndex; i++) {
				stateChanges[i].Background = hbrush;
			}
		}
		
		/// <summary>
		/// Sets the font weight on the specified text segment.
		/// </summary>
		public void SetFontWeight(int offset, int length, FontWeight weight)
		{
			int startIndex = GetIndexForOffset(offset);
			int endIndex = GetIndexForOffset(offset + length);
			for (int i = startIndex; i < endIndex; i++) {
				stateChanges[i].FontWeight = weight;
			}
		}
		
		/// <summary>
		/// Sets the font style on the specified text segment.
		/// </summary>
		public void SetFontStyle(int offset, int length, FontStyle style)
		{
			int startIndex = GetIndexForOffset(offset);
			int endIndex = GetIndexForOffset(offset + length);
			for (int i = startIndex; i < endIndex; i++) {
				stateChanges[i].FontStyle = style;
			}
		}
		
		/// <summary>
		/// Creates WPF Run instances that can be used for TextBlock.Inlines.
		/// </summary>
		public Run[] CreateRuns()
		{
			return ToRichText().CreateRuns();
		}
		
		/// <summary>
		/// Creates a RichText instance.
		/// </summary>
		public RichText ToRichText()
		{
			return new RichText(text, stateChangeOffsets.ToArray(), stateChanges.Select(FreezableHelper.GetFrozenClone).ToArray());
		}
		
		/// <summary>
		/// Clones this HighlightedInlineBuilder.
		/// </summary>
		public HighlightedInlineBuilder Clone()
		{
			return new HighlightedInlineBuilder(this.text,
			                                    stateChangeOffsets.ToList(),
			                                    stateChanges.Select(sc => sc.Clone()).ToList());
		}
	}
}

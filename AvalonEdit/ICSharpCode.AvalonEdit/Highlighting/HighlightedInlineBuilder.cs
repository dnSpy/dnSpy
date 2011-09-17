// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

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
	public sealed class HighlightedInlineBuilder
	{
		sealed class HighlightingState
		{
			internal Brush Foreground;
			internal FontFamily Family;
			internal FontWeight? Weight;
			internal FontStyle? Style;
			
			public HighlightingState Clone()
			{
				return new HighlightingState {
					Foreground = this.Foreground,
					Family = this.Family,
					Weight = this.Weight,
					Style = this.Style
				};
			}
		}
		
		readonly string text;
		List<int> stateChangeOffsets = new List<int>();
		List<HighlightingState> stateChanges = new List<HighlightingState>();
		
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
			stateChanges.Add(new HighlightingState());
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
			if (color.Foreground == null && color.FontStyle == null && color.FontWeight == null) {
				// Optimization: don't split the HighlightingState when we're not changing
				// any property. For example, the "Punctuation" color in C# is
				// empty by default.
				return;
			}
			int startIndex = GetIndexForOffset(offset);
			int endIndex = GetIndexForOffset(offset + length);
			for (int i = startIndex; i < endIndex; i++) {
				HighlightingState state = stateChanges[i];
				if (color.Foreground != null)
					state.Foreground = color.Foreground.GetBrush(null);
				if (color.FontStyle != null)
					state.Style = color.FontStyle;
				if (color.FontWeight != null)
					state.Weight = color.FontWeight;
			}
		}
		
		/// <summary>
		/// Sets the foreground brush on the specified text segment.
		/// </summary>
		public void SetForeground(int offset, int length, Brush brush)
		{
			int startIndex = GetIndexForOffset(offset);
			int endIndex = GetIndexForOffset(offset + length);
			for (int i = startIndex; i < endIndex; i++) {
				stateChanges[i].Foreground = brush;
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
				stateChanges[i].Weight = weight;
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
				stateChanges[i].Style = style;
			}
		}
		
		/// <summary>
		/// Sets the font family on the specified text segment.
		/// </summary>
		public void SetFontFamily(int offset, int length, FontFamily family)
		{
			int startIndex = GetIndexForOffset(offset);
			int endIndex = GetIndexForOffset(offset + length);
			for (int i = startIndex; i < endIndex; i++) {
				stateChanges[i].Family = family;
			}
		}
		
		/// <summary>
		/// Creates WPF Run instances that can be used for TextBlock.Inlines.
		/// </summary>
		public Run[] CreateRuns()
		{
			Run[] runs = new Run[stateChanges.Count];
			for (int i = 0; i < runs.Length; i++) {
				int startOffset = stateChangeOffsets[i];
				int endOffset = i + 1 < stateChangeOffsets.Count ? stateChangeOffsets[i + 1] : text.Length;
				Run r = new Run(text.Substring(startOffset, endOffset - startOffset));
				HighlightingState state = stateChanges[i];
				if (state.Foreground != null)
					r.Foreground = state.Foreground;
				if (state.Weight != null)
					r.FontWeight = state.Weight.Value;
				if (state.Family != null)
					r.FontFamily = state.Family;
				if (state.Style != null)
					r.FontStyle = state.Style.Value;
				runs[i] = r;
			}
			return runs;
		}
	}
}

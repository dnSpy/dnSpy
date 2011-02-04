// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;
using SpanStack = ICSharpCode.AvalonEdit.Utils.ImmutableStack<ICSharpCode.AvalonEdit.Highlighting.HighlightingSpan>;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// This class can syntax-highlight a document.
	/// It automatically manages invalidating the highlighting when the document changes.
	/// </summary>
	public class DocumentHighlighter : ILineTracker, IHighlighter
	{
		/// <summary>
		/// Stores the span state at the end of each line.
		/// storedSpanStacks[0] = state at beginning of document
		/// storedSpanStacks[i] = state after line i
		/// </summary>
		readonly CompressingTreeList<SpanStack> storedSpanStacks = new CompressingTreeList<SpanStack>(object.ReferenceEquals);
		readonly CompressingTreeList<bool> isValid = new CompressingTreeList<bool>((a, b) => a == b);
		readonly TextDocument document;
		readonly HighlightingRuleSet baseRuleSet;
		bool isHighlighting;
		
		/// <summary>
		/// Gets the document that this DocumentHighlighter is highlighting.
		/// </summary>
		public TextDocument Document {
			get { return document; }
		}
		
		/// <summary>
		/// Creates a new DocumentHighlighter instance.
		/// </summary>
		public DocumentHighlighter(TextDocument document, HighlightingRuleSet baseRuleSet)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			if (baseRuleSet == null)
				throw new ArgumentNullException("baseRuleSet");
			this.document = document;
			this.baseRuleSet = baseRuleSet;
			WeakLineTracker.Register(document, this);
			InvalidateHighlighting();
		}
		
		void ILineTracker.BeforeRemoveLine(DocumentLine line)
		{
			CheckIsHighlighting();
			int number = line.LineNumber;
			storedSpanStacks.RemoveAt(number);
			isValid.RemoveAt(number);
			if (number < isValid.Count) {
				isValid[number] = false;
				if (number < firstInvalidLine)
					firstInvalidLine = number;
			}
		}
		
		void ILineTracker.SetLineLength(DocumentLine line, int newTotalLength)
		{
			CheckIsHighlighting();
			int number = line.LineNumber;
			isValid[number] = false;
			if (number < firstInvalidLine)
				firstInvalidLine = number;
		}
		
		void ILineTracker.LineInserted(DocumentLine insertionPos, DocumentLine newLine)
		{
			CheckIsHighlighting();
			Debug.Assert(insertionPos.LineNumber + 1 == newLine.LineNumber);
			int lineNumber = newLine.LineNumber;
			storedSpanStacks.Insert(lineNumber, null);
			isValid.Insert(lineNumber, false);
			if (lineNumber < firstInvalidLine)
				firstInvalidLine = lineNumber;
		}
		
		void ILineTracker.RebuildDocument()
		{
			InvalidateHighlighting();
		}
		
		ImmutableStack<HighlightingSpan> initialSpanStack = SpanStack.Empty;
		
		/// <summary>
		/// Gets/sets the the initial span stack of the document. Default value is <see cref="SpanStack.Empty" />.
		/// </summary>
		public ImmutableStack<HighlightingSpan> InitialSpanStack {
			get { return initialSpanStack; }
			set {
				if (value == null)
					initialSpanStack = SpanStack.Empty;
				else
					initialSpanStack = value;
				InvalidateHighlighting();
			}
		}
		
		/// <summary>
		/// Invalidates all stored highlighting info.
		/// When the document changes, the highlighting is invalidated automatically, this method
		/// needs to be called only when there are changes to the highlighting rule set.
		/// </summary>
		public void InvalidateHighlighting()
		{
			CheckIsHighlighting();
			storedSpanStacks.Clear();
			storedSpanStacks.Add(initialSpanStack);
			storedSpanStacks.InsertRange(1, document.LineCount, null);
			isValid.Clear();
			isValid.Add(true);
			isValid.InsertRange(1, document.LineCount, false);
			firstInvalidLine = 1;
		}
		
		int firstInvalidLine;
		
		/// <summary>
		/// Highlights the specified document line.
		/// </summary>
		/// <param name="line">The line to highlight.</param>
		/// <returns>A <see cref="HighlightedLine"/> line object that represents the highlighted sections.</returns>
		[ObsoleteAttribute("Use the (int lineNumber) overload instead")]
		public HighlightedLine HighlightLine(DocumentLine line)
		{
			if (!document.Lines.Contains(line))
				throw new ArgumentException("The specified line does not belong to the document.");
			return HighlightLine(line.LineNumber);
		}
		
		/// <inheritdoc/>
		public HighlightedLine HighlightLine(int lineNumber)
		{
			ThrowUtil.CheckInRangeInclusive(lineNumber, "lineNumber", 1, document.LineCount);
			CheckIsHighlighting();
			isHighlighting = true;
			try {
				HighlightUpTo(lineNumber);
				DocumentLine line = document.GetLineByNumber(lineNumber);
				highlightedLine = new HighlightedLine(document, line);
				HighlightLineAndUpdateTreeList(line, lineNumber);
				return highlightedLine;
			} finally {
				highlightedLine = null;
				isHighlighting = false;
			}
		}
		
		/// <inheritdoc/>
		public SpanStack GetSpanStack(int lineNumber)
		{
			ThrowUtil.CheckInRangeInclusive(lineNumber, "lineNumber", 0, document.LineCount);
			if (firstInvalidLine <= lineNumber) {
				CheckIsHighlighting();
				isHighlighting = true;
				try {
					HighlightUpTo(lineNumber + 1);
				} finally {
					isHighlighting = false;
				}
			}
			return storedSpanStacks[lineNumber];
		}
		
		void CheckIsHighlighting()
		{
			if (isHighlighting) {
				throw new InvalidOperationException("Invalid call - a highlighting operation is currently running.");
			}
		}
		
		void HighlightUpTo(int targetLineNumber)
		{
			Debug.Assert(highlightedLine == null); // ensure this method is only used for
			while (firstInvalidLine < targetLineNumber) {
				HighlightLineAndUpdateTreeList(document.GetLineByNumber(firstInvalidLine), firstInvalidLine);
			}
		}
		
		void HighlightLineAndUpdateTreeList(DocumentLine line, int lineNumber)
		{
			//Debug.WriteLine("Highlight line " + lineNumber + (highlightedLine != null ? "" : " (span stack only)"));
			spanStack = storedSpanStacks[lineNumber - 1];
			HighlightLineInternal(line);
			if (!EqualSpanStacks(spanStack, storedSpanStacks[lineNumber])) {
				isValid[lineNumber] = true;
				//Debug.WriteLine("Span stack in line " + lineNumber + " changed from " + storedSpanStacks[lineNumber] + " to " + spanStack);
				storedSpanStacks[lineNumber] = spanStack;
				if (lineNumber + 1 < isValid.Count) {
					isValid[lineNumber + 1] = false;
					firstInvalidLine = lineNumber + 1;
				} else {
					firstInvalidLine = int.MaxValue;
				}
				OnHighlightStateChanged(line, lineNumber);
			} else if (firstInvalidLine == lineNumber) {
				isValid[lineNumber] = true;
				firstInvalidLine = isValid.IndexOf(false);
				if (firstInvalidLine < 0)
					firstInvalidLine = int.MaxValue;
			}
		}
		
		static bool EqualSpanStacks(SpanStack a, SpanStack b)
		{
			// We must use value equality between the stacks because TextViewDocumentHighlighter.OnHighlightStateChanged
			// depends on the fact that equal input state + unchanged line contents produce equal output state.
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			while (!a.IsEmpty && !b.IsEmpty) {
				if (a.Peek() != b.Peek())
					return false;
				a = a.Pop();
				b = b.Pop();
				if (a == b)
					return true;
			}
			return a.IsEmpty && b.IsEmpty;
		}
		
		/// <summary>
		/// Is called when the highlighting state at the end of the specified line has changed.
		/// </summary>
		/// <remarks>This callback must not call HighlightLine or InvalidateHighlighting.
		/// It may call GetSpanStack, but only for the changed line and lines above.
		/// This method must not modify the document.</remarks>
		protected virtual void OnHighlightStateChanged(DocumentLine line, int lineNumber)
		{
		}
		
		#region Highlighting Engine
		SpanStack spanStack;
		
		// local variables from HighlightLineInternal (are member because they are accessed by HighlighLine helper methods)
		string lineText;
		int lineStartOffset;
		int position;
		
		/// <summary>
		/// the HighlightedLine where highlighting output is being written to.
		/// if this variable is null, nothing is highlighted and only the span state is updated
		/// </summary>
		HighlightedLine highlightedLine;
		
		void HighlightLineInternal(DocumentLine line)
		{
			lineStartOffset = line.Offset;
			lineText = document.GetText(line.Offset, line.Length);
			position = 0;
			ResetColorStack();
			HighlightingRuleSet currentRuleSet = this.CurrentRuleSet;
			Stack<Match[]> storedMatchArrays = new Stack<Match[]>();
			Match[] matches = AllocateMatchArray(currentRuleSet.Spans.Count);
			Match endSpanMatch = null;
			
			while (true) {
				for (int i = 0; i < matches.Length; i++) {
					if (matches[i] == null || (matches[i].Success && matches[i].Index < position))
						matches[i] = currentRuleSet.Spans[i].StartExpression.Match(lineText, position);
				}
				if (endSpanMatch == null && !spanStack.IsEmpty)
					endSpanMatch = spanStack.Peek().EndExpression.Match(lineText, position);
				
				Match firstMatch = Minimum(matches, endSpanMatch);
				if (firstMatch == null)
					break;
				
				HighlightNonSpans(firstMatch.Index);
				
				Debug.Assert(position == firstMatch.Index);
				
				if (firstMatch == endSpanMatch) {
					PopColor(); // pop SpanColor
					HighlightingSpan poppedSpan = spanStack.Peek();
					PushColor(poppedSpan.EndColor);
					position = firstMatch.Index + firstMatch.Length;
					PopColor(); // pop EndColor
					spanStack = spanStack.Pop();
					currentRuleSet = this.CurrentRuleSet;
					//FreeMatchArray(matches);
					if (storedMatchArrays.Count > 0) {
						matches = storedMatchArrays.Pop();
						int index = currentRuleSet.Spans.IndexOf(poppedSpan);
						Debug.Assert(index >= 0 && index < matches.Length);
						if (matches[index].Index == position) {
							throw new InvalidOperationException(
								"A highlighting span matched 0 characters, which would cause an endless loop.\n" +
								"Change the highlighting definition so that either the start or the end regex matches at least one character.\n" +
								"Start regex: " + poppedSpan.StartExpression + "\n" +
								"End regex: " + poppedSpan.EndExpression);
						}
					} else {
						matches = AllocateMatchArray(currentRuleSet.Spans.Count);
					}
				} else {
					int index = Array.IndexOf(matches, firstMatch);
					Debug.Assert(index >= 0);
					HighlightingSpan newSpan = currentRuleSet.Spans[index];
					spanStack = spanStack.Push(newSpan);
					currentRuleSet = this.CurrentRuleSet;
					storedMatchArrays.Push(matches);
					matches = AllocateMatchArray(currentRuleSet.Spans.Count);
					PushColor(newSpan.StartColor);
					position = firstMatch.Index + firstMatch.Length;
					PopColor();
					PushColor(newSpan.SpanColor);
				}
				endSpanMatch = null;
			}
			HighlightNonSpans(line.Length);
			
			PopAllColors();
		}
		
		void HighlightNonSpans(int until)
		{
			Debug.Assert(position <= until);
			if (position == until)
				return;
			if (highlightedLine != null) {
				IList<HighlightingRule> rules = CurrentRuleSet.Rules;
				Match[] matches = AllocateMatchArray(rules.Count);
				while (true) {
					for (int i = 0; i < matches.Length; i++) {
						if (matches[i] == null || (matches[i].Success && matches[i].Index < position))
							matches[i] = rules[i].Regex.Match(lineText, position, until - position);
					}
					Match firstMatch = Minimum(matches, null);
					if (firstMatch == null)
						break;
					
					position = firstMatch.Index;
					int ruleIndex = Array.IndexOf(matches, firstMatch);
					if (firstMatch.Length == 0) {
						throw new InvalidOperationException(
							"A highlighting rule matched 0 characters, which would cause an endless loop.\n" +
							"Change the highlighting definition so that the rule matches at least one character.\n" +
							"Regex: " + rules[ruleIndex].Regex);
					}
					PushColor(rules[ruleIndex].Color);
					position = firstMatch.Index + firstMatch.Length;
					PopColor();
				}
				//FreeMatchArray(matches);
			}
			position = until;
		}
		
		static readonly HighlightingRuleSet emptyRuleSet = new HighlightingRuleSet() { Name = "EmptyRuleSet" };
		
		HighlightingRuleSet CurrentRuleSet {
			get {
				if (spanStack.IsEmpty)
					return baseRuleSet;
				else
					return spanStack.Peek().RuleSet ?? emptyRuleSet;
			}
		}
		#endregion
		
		#region Color Stack Management
		Stack<HighlightedSection> highlightedSectionStack;
		HighlightedSection lastPoppedSection;
		
		void ResetColorStack()
		{
			Debug.Assert(position == 0);
			lastPoppedSection = null;
			if (highlightedLine == null) {
				highlightedSectionStack = null;
			} else {
				highlightedSectionStack = new Stack<HighlightedSection>();
				foreach (HighlightingSpan span in spanStack.Reverse()) {
					PushColor(span.SpanColor);
				}
			}
		}
		
		void PushColor(HighlightingColor color)
		{
			if (highlightedLine == null)
				return;
			if (color == null) {
				highlightedSectionStack.Push(null);
			} else if (lastPoppedSection != null && lastPoppedSection.Color == color
			           && lastPoppedSection.Offset + lastPoppedSection.Length == position + lineStartOffset)
			{
				highlightedSectionStack.Push(lastPoppedSection);
				lastPoppedSection = null;
			} else {
				HighlightedSection hs = new HighlightedSection {
					Offset = position + lineStartOffset,
					Color = color
				};
				highlightedLine.Sections.Add(hs);
				highlightedSectionStack.Push(hs);
				lastPoppedSection = null;
			}
		}
		
		void PopColor()
		{
			if (highlightedLine == null)
				return;
			HighlightedSection s = highlightedSectionStack.Pop();
			if (s != null) {
				s.Length = (position + lineStartOffset) - s.Offset;
				if (s.Length == 0)
					highlightedLine.Sections.Remove(s);
				else
					lastPoppedSection = s;
			}
		}
		
		void PopAllColors()
		{
			if (highlightedSectionStack != null) {
				while (highlightedSectionStack.Count > 0)
					PopColor();
			}
		}
		#endregion
		
		#region Match helpers
		/// <summary>
		/// Returns the first match from the array or endSpanMatch.
		/// </summary>
		static Match Minimum(Match[] arr, Match endSpanMatch)
		{
			Match min = null;
			foreach (Match v in arr) {
				if (v.Success && (min == null || v.Index < min.Index))
					min = v;
			}
			if (endSpanMatch != null && endSpanMatch.Success && (min == null || endSpanMatch.Index < min.Index))
				return endSpanMatch;
			else
				return min;
		}
		
		static Match[] AllocateMatchArray(int count)
		{
			if (count == 0)
				return Empty<Match>.Array;
			else
				return new Match[count];
		}
		#endregion
	}
}

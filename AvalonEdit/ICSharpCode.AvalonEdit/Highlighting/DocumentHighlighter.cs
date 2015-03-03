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
using ICSharpCode.NRefactory.Editor;
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
		readonly IDocument document;
		readonly IHighlightingDefinition definition;
		readonly HighlightingEngine engine;
		readonly WeakLineTracker weakLineTracker;
		bool isHighlighting;
		bool isInHighlightingGroup;
		bool isDisposed;
		
		/// <summary>
		/// Gets the document that this DocumentHighlighter is highlighting.
		/// </summary>
		public IDocument Document {
			get { return document; }
		}
		
		/// <summary>
		/// Creates a new DocumentHighlighter instance.
		/// </summary>
		public DocumentHighlighter(TextDocument document, IHighlightingDefinition definition)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			if (definition == null)
				throw new ArgumentNullException("definition");
			this.document = document;
			this.definition = definition;
			this.engine = new HighlightingEngine(definition.MainRuleSet);
			document.VerifyAccess();
			weakLineTracker = WeakLineTracker.Register(document, this);
			InvalidateSpanStacks();
		}
		
		#if NREFACTORY
		/// <summary>
		/// Creates a new DocumentHighlighter instance.
		/// </summary>
		public DocumentHighlighter(ReadOnlyDocument document, IHighlightingDefinition definition)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			if (definition == null)
				throw new ArgumentNullException("definition");
			this.document = document;
			this.definition = definition;
			this.engine = new HighlightingEngine(definition.MainRuleSet);
			InvalidateHighlighting();
		}
		#endif
		
		/// <summary>
		/// Disposes the document highlighter.
		/// </summary>
		public void Dispose()
		{
			if (weakLineTracker != null)
				weakLineTracker.Deregister();
			isDisposed = true;
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
			InvalidateSpanStacks();
		}
		
		void ILineTracker.ChangeComplete(DocumentChangeEventArgs e)
		{
		}
		
		ImmutableStack<HighlightingSpan> initialSpanStack = SpanStack.Empty;
		
		/// <summary>
		/// Gets/sets the the initial span stack of the document. Default value is <see cref="SpanStack.Empty" />.
		/// </summary>
		public ImmutableStack<HighlightingSpan> InitialSpanStack {
			get { return initialSpanStack; }
			set {
				initialSpanStack = value ?? SpanStack.Empty;
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
			InvalidateSpanStacks();
			OnHighlightStateChanged(1, document.LineCount); // force a redraw with the new highlighting
		}
		
		/// <summary>
		/// Invalidates stored highlighting info, but does not raise the HighlightingStateChanged event.
		/// </summary>
		void InvalidateSpanStacks()
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
		
		/// <inheritdoc/>
		public HighlightedLine HighlightLine(int lineNumber)
		{
			ThrowUtil.CheckInRangeInclusive(lineNumber, "lineNumber", 1, document.LineCount);
			CheckIsHighlighting();
			isHighlighting = true;
			try {
				HighlightUpTo(lineNumber - 1);
				IDocumentLine line = document.GetLineByNumber(lineNumber);
				HighlightedLine result = engine.HighlightLine(document, line);
				UpdateTreeList(lineNumber);
				return result;
			} finally {
				isHighlighting = false;
			}
		}
		
		/// <summary>
		/// Gets the span stack at the end of the specified line.
		/// -> GetSpanStack(1) returns the spans at the start of the second line.
		/// </summary>
		/// <remarks>
		/// GetSpanStack(0) is valid and will return <see cref="InitialSpanStack"/>.
		/// The elements are returned in inside-out order (first element of result enumerable is the color of the innermost span).
		/// </remarks>
		public SpanStack GetSpanStack(int lineNumber)
		{
			ThrowUtil.CheckInRangeInclusive(lineNumber, "lineNumber", 0, document.LineCount);
			if (firstInvalidLine <= lineNumber) {
				UpdateHighlightingState(lineNumber);
			}
			return storedSpanStacks[lineNumber];
		}
		
		/// <inheritdoc/>
		public IEnumerable<HighlightingColor> GetColorStack(int lineNumber)
		{
			return GetSpanStack(lineNumber).Select(s => s.SpanColor).Where(s => s != null);
		}
		
		void CheckIsHighlighting()
		{
			if (isDisposed) {
				throw new ObjectDisposedException("DocumentHighlighter");
			}
			if (isHighlighting) {
				throw new InvalidOperationException("Invalid call - a highlighting operation is currently running.");
			}
		}
		
		/// <inheritdoc/>
		public void UpdateHighlightingState(int lineNumber)
		{
			CheckIsHighlighting();
			isHighlighting = true;
			try {
				HighlightUpTo(lineNumber);
			} finally {
				isHighlighting = false;
			}
		}
		
		/// <summary>
		/// Sets the engine's CurrentSpanStack to the end of the target line.
		/// Updates the span stack for all lines up to (and including) the target line, if necessary.
		/// </summary>
		void HighlightUpTo(int targetLineNumber)
		{
			for (int currentLine = 0; currentLine <= targetLineNumber; currentLine++) {
				if (firstInvalidLine > currentLine) {
					// (this branch is always taken on the first loop iteration, as firstInvalidLine > 0)
					
					if (firstInvalidLine <= targetLineNumber) {
						// Skip valid lines to next invalid line:
						engine.CurrentSpanStack = storedSpanStacks[firstInvalidLine - 1];
						currentLine = firstInvalidLine;
					} else {
						// Skip valid lines to target line:
						engine.CurrentSpanStack = storedSpanStacks[targetLineNumber];
						break;
					}
				}
				Debug.Assert(EqualSpanStacks(engine.CurrentSpanStack, storedSpanStacks[currentLine - 1]));
				engine.ScanLine(document, document.GetLineByNumber(currentLine));
				UpdateTreeList(currentLine);
			}
			Debug.Assert(EqualSpanStacks(engine.CurrentSpanStack, storedSpanStacks[targetLineNumber]));
		}
		
		void UpdateTreeList(int lineNumber)
		{
			if (!EqualSpanStacks(engine.CurrentSpanStack, storedSpanStacks[lineNumber])) {
				isValid[lineNumber] = true;
				//Debug.WriteLine("Span stack in line " + lineNumber + " changed from " + storedSpanStacks[lineNumber] + " to " + spanStack);
				storedSpanStacks[lineNumber] = engine.CurrentSpanStack;
				if (lineNumber + 1 < isValid.Count) {
					isValid[lineNumber + 1] = false;
					firstInvalidLine = lineNumber + 1;
				} else {
					firstInvalidLine = int.MaxValue;
				}
				if (lineNumber + 1 < document.LineCount)
					OnHighlightStateChanged(lineNumber + 1, lineNumber + 1);
			} else if (firstInvalidLine == lineNumber) {
				isValid[lineNumber] = true;
				firstInvalidLine = isValid.IndexOf(false);
				if (firstInvalidLine < 0)
					firstInvalidLine = int.MaxValue;
			}
		}
		
		static bool EqualSpanStacks(SpanStack a, SpanStack b)
		{
			// We must use value equality between the stacks because HighlightingColorizer.OnHighlightStateChanged
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
		
		/// <inheritdoc/>
		public event HighlightingStateChangedEventHandler HighlightingStateChanged;
		
		/// <summary>
		/// Is called when the highlighting state at the end of the specified line has changed.
		/// </summary>
		/// <remarks>This callback must not call HighlightLine or InvalidateHighlighting.
		/// It may call GetSpanStack, but only for the changed line and lines above.
		/// This method must not modify the document.</remarks>
		protected virtual void OnHighlightStateChanged(int fromLineNumber, int toLineNumber)
		{
			if (HighlightingStateChanged != null)
				HighlightingStateChanged(fromLineNumber, toLineNumber);
		}
		
		/// <inheritdoc/>
		public HighlightingColor DefaultTextColor {
			get { return null; }
		}
		
		/// <inheritdoc/>
		public void BeginHighlighting()
		{
			if (isInHighlightingGroup)
				throw new InvalidOperationException("Highlighting group is already open");
			isInHighlightingGroup = true;
		}
		
		/// <inheritdoc/>
		public void EndHighlighting()
		{
			if (!isInHighlightingGroup)
				throw new InvalidOperationException("Highlighting group is not open");
			isInHighlightingGroup = false;
		}
		
		/// <inheritdoc/>
		public HighlightingColor GetNamedColor(string name)
		{
			return definition.GetNamedColor(name);
		}
	}
}

// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;
using System.Diagnostics;
using ICSharpCode.NRefactory.Editor;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Script implementation based on IDocument.
	/// </summary>
	public class DocumentScript : Script
	{
		readonly IDocument currentDocument;
		
		public IDocument CurrentDocument {
			get { return currentDocument; }
		}
		readonly IDocument originalDocument;
		
		public IDocument OriginalDocument {
			get { return originalDocument; }
		}
		readonly IDisposable undoGroup;
		
		
		public DocumentScript(IDocument document, CSharpFormattingOptions formattingOptions, TextEditorOptions options) : base(formattingOptions, options)
		{
			this.originalDocument = document.CreateDocumentSnapshot();
			this.currentDocument = document;
			Debug.Assert(currentDocument.Version.CompareAge(originalDocument.Version) == 0);
			this.undoGroup = document.OpenUndoGroup();
		}
		
		public override void Dispose()
		{
			// Since base.Dispose() reformats some nodes; we need to include it in the undo group
			base.Dispose();
			if (undoGroup != null)
				undoGroup.Dispose();
		}
		
		public override void Remove(AstNode node, bool removeEmptyLine = true)
		{
			var segment = GetSegment (node);
			int startOffset = segment.Offset;
			int endOffset = segment.EndOffset;
			var startLine = currentDocument.GetLineByOffset (startOffset);
			var endLine = currentDocument.GetLineByOffset (endOffset);
			
			if (startLine != null && endLine != null) {
				bool removeStart = string.IsNullOrWhiteSpace (currentDocument.GetText (startLine.Offset, startOffset - startLine.Offset));
				if (removeStart)
					startOffset = startLine.Offset;
				bool removeEnd = string.IsNullOrWhiteSpace (currentDocument.GetText (endOffset, endLine.EndOffset - endOffset));
				if (removeEnd)
					endOffset = endLine.EndOffset;
				
				// Remove delimiter if the whole line get's removed.
				if (removeStart && removeEnd)
					endOffset += endLine.DelimiterLength;
			}
			
			Replace (startOffset, endOffset - startOffset, string.Empty);
		}
		
		public override void Replace(int offset, int length, string newText)
		{
			currentDocument.Replace(offset, length, newText);
		}
		
		public override int GetCurrentOffset(TextLocation originalDocumentLocation)
		{
			int offset = originalDocument.GetOffset(originalDocumentLocation);
			return GetCurrentOffset(offset);
		}
		
		public override int GetCurrentOffset(int originalDocumentOffset)
		{
			return originalDocument.Version.MoveOffsetTo(currentDocument.Version, originalDocumentOffset);
		}
		
		public override void FormatText(IEnumerable<AstNode> nodes)
		{
			var syntaxTree = SyntaxTree.Parse(currentDocument, "dummy.cs");
			var formatter = new CSharpFormatter(FormattingOptions, Options);
			var segments = new List<ISegment>();
			foreach (var node in nodes.OrderByDescending (n => n.StartLocation)) {
				var segment = GetSegment(node);
				
				formatter.AddFormattingRegion (new ICSharpCode.NRefactory.TypeSystem.DomRegion (
					currentDocument.GetLocation (segment.Offset), 
					currentDocument.GetLocation (segment.EndOffset)
					));
				segments.Add(segment);
			}
			if (segments.Count == 0)
				return;
			var changes = formatter.AnalyzeFormatting (currentDocument, syntaxTree);
			foreach (var segment in segments) {
				changes.ApplyChanges(segment.Offset, segment.Length - 1);
			}
		}
		
		protected override int GetIndentLevelAt(int offset)
		{
			var line = currentDocument.GetLineByOffset(offset);
			int spaces = 0;
			int indentationLevel = 0;
			for (int i = line.Offset; i < currentDocument.TextLength; i++) {
				char c = currentDocument.GetCharAt(i);
				if (c == '\t') {
					spaces = 0;
					indentationLevel++;
				} else if (c == ' ') {
					spaces++;
					if (spaces == 4) {
						spaces = 0;
						indentationLevel++;
					}
				} else {
					break;
				}
			}
			return indentationLevel;
		}
		
		protected override ISegment CreateTrackedSegment(int offset, int length)
		{
			return new TrackedSegment(this, offset, offset + length);
		}
		
		sealed class TrackedSegment : ISegment
		{
			readonly DocumentScript script;
			readonly ITextSourceVersion originalVersion;
			readonly int originalStart;
			readonly int originalEnd;
			
			public TrackedSegment(DocumentScript script, int originalStart, int originalEnd)
			{
				this.script = script;
				this.originalVersion = script.currentDocument.Version;
				this.originalStart = originalStart;
				this.originalEnd = originalEnd;
			}
			
			public int Offset {
				get { return originalVersion.MoveOffsetTo(script.currentDocument.Version, originalStart); }
			}
			
			public int Length {
				get { return this.EndOffset - this.Offset; }
			}
			
			public int EndOffset {
				get { return originalVersion.MoveOffsetTo(script.currentDocument.Version, originalEnd); }
			}
		}
	}
}

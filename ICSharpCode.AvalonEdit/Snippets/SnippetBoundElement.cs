// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows.Documents;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Snippets
{
	/// <summary>
	/// An element that binds to a <see cref="SnippetReplaceableTextElement"/> and displays the same text.
	/// </summary>
	[Serializable]
	public class SnippetBoundElement : SnippetElement
	{
		SnippetReplaceableTextElement targetElement;
		
		/// <summary>
		/// Gets/Sets the target element.
		/// </summary>
		public SnippetReplaceableTextElement TargetElement {
			get { return targetElement; }
			set { targetElement = value; }
		}
		
		/// <summary>
		/// Converts the text before copying it.
		/// </summary>
		public virtual string ConvertText(string input)
		{
			return input;
		}
		
		/// <inheritdoc/>
		public override void Insert(InsertionContext context)
		{
			if (targetElement != null) {
				TextAnchor start = context.Document.CreateAnchor(context.InsertionPosition);
				start.MovementType = AnchorMovementType.BeforeInsertion;
				start.SurviveDeletion = true;
				string inputText = targetElement.Text;
				if (inputText != null) {
					context.InsertText(ConvertText(inputText));
				}
				TextAnchor end = context.Document.CreateAnchor(context.InsertionPosition);
				end.MovementType = AnchorMovementType.BeforeInsertion;
				end.SurviveDeletion = true;
				AnchorSegment segment = new AnchorSegment(start, end);
				context.RegisterActiveElement(this, new BoundActiveElement(context, targetElement, this, segment));
			}
		}
		
		/// <inheritdoc/>
		public override Inline ToTextRun()
		{
			if (targetElement != null) {
				string inputText = targetElement.Text;
				if (inputText != null) {
					return new Italic(new Run(ConvertText(inputText)));
				}
			}
			return base.ToTextRun();
		}
	}
	
	sealed class BoundActiveElement : IActiveElement
	{
		InsertionContext context;
		SnippetReplaceableTextElement targetSnippetElement;
		SnippetBoundElement boundElement;
		internal IReplaceableActiveElement targetElement;
		AnchorSegment segment;
		
		public BoundActiveElement(InsertionContext context, SnippetReplaceableTextElement targetSnippetElement, SnippetBoundElement boundElement, AnchorSegment segment)
		{
			this.context = context;
			this.targetSnippetElement = targetSnippetElement;
			this.boundElement = boundElement;
			this.segment = segment;
		}
		
		public void OnInsertionCompleted()
		{
			targetElement = context.GetActiveElement(targetSnippetElement) as IReplaceableActiveElement;
			if (targetElement != null) {
				targetElement.TextChanged += targetElement_TextChanged;
			}
		}
		
		void targetElement_TextChanged(object sender, EventArgs e)
		{
			// Don't copy text if the segments overlap (we would get an endless loop).
			// This can happen if the user deletes the text between the replaceable element and the bound element.
			if (segment.GetOverlap(targetElement.Segment) == SimpleSegment.Invalid) {
				int offset = segment.Offset;
				int length = segment.Length;
				string text = boundElement.ConvertText(targetElement.Text);
				if (length != text.Length || text != context.Document.GetText(offset, length)) {
					// Call replace only if we're actually changing something.
					// Without this check, we would generate an empty undo group when the user pressed undo.
					context.Document.Replace(offset, length, text);
					if (length == 0) {
						// replacing an empty anchor segment with text won't enlarge it, so we have to recreate it
						segment = new AnchorSegment(context.Document, offset, text.Length);
					}
				}
			}
		}
		
		public void Deactivate(SnippetEventArgs e)
		{
		}
		
		public bool IsEditable {
			get { return false; }
		}
		
		public ISegment Segment {
			get { return segment; }
		}
	}
}

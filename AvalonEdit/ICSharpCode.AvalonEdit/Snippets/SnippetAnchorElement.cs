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
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.NRefactory.Editor;

namespace ICSharpCode.AvalonEdit.Snippets
{
	/// <summary>
	/// Creates a named anchor that can be accessed by other SnippetElements.
	/// </summary>
	public sealed class SnippetAnchorElement : SnippetElement
	{
		/// <summary>
		/// Gets or sets the name of the anchor.
		/// </summary>
		public string Name { get; private set; }
		
		/// <summary>
		/// Creates a SnippetAnchorElement with the supplied name.
		/// </summary>
		public SnippetAnchorElement(string name)
		{
			this.Name = name;
		}
		
		/// <inheritdoc />
		public override void Insert(InsertionContext context)
		{
			TextAnchor start = context.Document.CreateAnchor(context.InsertionPosition);
			start.MovementType = AnchorMovementType.BeforeInsertion;
			start.SurviveDeletion = true;
			AnchorSegment segment = new AnchorSegment(start, start);
			context.RegisterActiveElement(this, new AnchorElement(segment, Name, context));
		}
	}
	
	/// <summary>
	/// AnchorElement created by SnippetAnchorElement.
	/// </summary>
	public sealed class AnchorElement : IActiveElement
	{
		/// <inheritdoc />
		public bool IsEditable {
			get { return false; }
		}
		
		AnchorSegment segment;
		InsertionContext context;
		
		/// <inheritdoc />
		public ISegment Segment {
			get { return segment; }
		}
		
		/// <summary>
		/// Creates a new AnchorElement.
		/// </summary>
		public AnchorElement(AnchorSegment segment, string name, InsertionContext context)
		{
			this.segment = segment;
			this.context = context;
			this.Name = name;
		}
		
		/// <summary>
		/// Gets or sets the text at the anchor.
		/// </summary>
		public string Text {
			get { return context.Document.GetText(segment); }
			set {
				int offset = segment.Offset;
				int length = segment.Length;
				context.Document.Replace(offset, length, value);
				if (length == 0) {
					// replacing an empty anchor segment with text won't enlarge it, so we have to recreate it
					segment = new AnchorSegment(context.Document, offset, value.Length);
				}
			}
		}
		
		/// <summary>
		/// Gets or sets the name of the anchor.
		/// </summary>
		public string Name { get; private set; }
		
		/// <inheritdoc />
		public void OnInsertionCompleted()
		{
		}
		
		/// <inheritdoc />
		public void Deactivate(SnippetEventArgs e)
		{
		}
	}
}

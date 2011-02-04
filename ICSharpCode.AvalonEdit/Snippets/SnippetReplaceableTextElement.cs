// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Snippets
{
	/// <summary>
	/// Text element that is supposed to be replaced by the user.
	/// Will register an <see cref="IReplaceableActiveElement"/>.
	/// </summary>
	[Serializable]
	public class SnippetReplaceableTextElement : SnippetTextElement
	{
		/// <inheritdoc/>
		public override void Insert(InsertionContext context)
		{
			int start = context.InsertionPosition;
			base.Insert(context);
			int end = context.InsertionPosition;
			context.RegisterActiveElement(this, new ReplaceableActiveElement(context, start, end));
		}
		
		/// <inheritdoc/>
		public override Inline ToTextRun()
		{
			return new Italic(base.ToTextRun());
		}
	}
	
	/// <summary>
	/// Interface for active element registered by <see cref="SnippetReplaceableTextElement"/>.
	/// </summary>
	public interface IReplaceableActiveElement : IActiveElement
	{
		/// <summary>
		/// Gets the current text inside the element.
		/// </summary>
		string Text { get; }
		
		/// <summary>
		/// Occurs when the text inside the element changes.
		/// </summary>
		event EventHandler TextChanged;
	}
	
	sealed class ReplaceableActiveElement : IReplaceableActiveElement, IWeakEventListener
	{
		readonly InsertionContext context;
		readonly int startOffset, endOffset;
		TextAnchor start, end;
		
		public ReplaceableActiveElement(InsertionContext context, int startOffset, int endOffset)
		{
			this.context = context;
			this.startOffset = startOffset;
			this.endOffset = endOffset;
		}
		
		void AnchorDeleted(object sender, EventArgs e)
		{
			context.Deactivate(new SnippetEventArgs(DeactivateReason.Deleted));
		}
		
		public void OnInsertionCompleted()
		{
			// anchors must be created in OnInsertionCompleted because they should move only
			// due to user insertions, not due to insertions of other snippet parts
			start = context.Document.CreateAnchor(startOffset);
			start.MovementType = AnchorMovementType.BeforeInsertion;
			end = context.Document.CreateAnchor(endOffset);
			end.MovementType = AnchorMovementType.AfterInsertion;
			start.Deleted += AnchorDeleted;
			end.Deleted += AnchorDeleted;
			
			// Be careful with references from the document to the editing/snippet layer - use weak events
			// to prevent memory leaks when the text area control gets dropped from the UI while the snippet is active.
			// The InsertionContext will keep us alive as long as the snippet is in interactive mode.
			TextDocumentWeakEventManager.TextChanged.AddListener(context.Document, this);
			
			background = new Renderer { Layer = KnownLayer.Background, element = this };
			foreground = new Renderer { Layer = KnownLayer.Text, element = this };
			context.TextArea.TextView.BackgroundRenderers.Add(background);
			context.TextArea.TextView.BackgroundRenderers.Add(foreground);
			context.TextArea.Caret.PositionChanged += Caret_PositionChanged;
			Caret_PositionChanged(null, null);
			
			this.Text = GetText();
		}

		public void Deactivate(SnippetEventArgs e)
		{
			TextDocumentWeakEventManager.TextChanged.RemoveListener(context.Document, this);
			context.TextArea.TextView.BackgroundRenderers.Remove(background);
			context.TextArea.TextView.BackgroundRenderers.Remove(foreground);
			context.TextArea.Caret.PositionChanged -= Caret_PositionChanged;
		}
		
		bool isCaretInside;
		
		void Caret_PositionChanged(object sender, EventArgs e)
		{
			ISegment s = this.Segment;
			if (s != null) {
				bool newIsCaretInside = s.Contains(context.TextArea.Caret.Offset);
				if (newIsCaretInside != isCaretInside) {
					isCaretInside = newIsCaretInside;
					context.TextArea.TextView.InvalidateLayer(foreground.Layer);
				}
			}
		}
		
		Renderer background, foreground;
		
		public string Text { get; private set; }
		
		string GetText()
		{
			if (start.IsDeleted || end.IsDeleted)
				return string.Empty;
			else
				return context.Document.GetText(start.Offset, Math.Max(0, end.Offset - start.Offset));
		}
		
		public event EventHandler TextChanged;
		
		bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
		{
			if (managerType == typeof(TextDocumentWeakEventManager.TextChanged)) {
				string newText = GetText();
				if (this.Text != newText) {
					this.Text = newText;
					if (TextChanged != null)
						TextChanged(this, e);
				}
				return true;
			}
			return false;
		}
		
		public bool IsEditable {
			get { return true; }
		}
		
		public ISegment Segment {
			get {
				if (start.IsDeleted || end.IsDeleted)
					return null;
				else
					return new SimpleSegment(start.Offset, Math.Max(0, end.Offset - start.Offset));
			}
		}
		
		sealed class Renderer : IBackgroundRenderer
		{
			static readonly Brush backgroundBrush = CreateBackgroundBrush();
			static readonly Pen activeBorderPen = CreateBorderPen();
			
			static Brush CreateBackgroundBrush()
			{
				SolidColorBrush b = new SolidColorBrush(Colors.LimeGreen);
				b.Opacity = 0.4;
				b.Freeze();
				return b;
			}
			
			static Pen CreateBorderPen()
			{
				Pen p = new Pen(Brushes.Black, 1);
				p.DashStyle = DashStyles.Dot;
				p.Freeze();
				return p;
			}
			
			internal ReplaceableActiveElement element;
			
			public KnownLayer Layer { get; set; }
			
			public void Draw(TextView textView, System.Windows.Media.DrawingContext drawingContext)
			{
				ISegment s = element.Segment;
				if (s != null) {
					BackgroundGeometryBuilder geoBuilder = new BackgroundGeometryBuilder();
					geoBuilder.AlignToMiddleOfPixels = true;
					if (Layer == KnownLayer.Background) {
						geoBuilder.AddSegment(textView, s);
						drawingContext.DrawGeometry(backgroundBrush, null, geoBuilder.CreateGeometry());
					} else {
						// draw foreground only if active
						if (element.isCaretInside) {
							geoBuilder.AddSegment(textView, s);
							foreach (BoundActiveElement boundElement in element.context.ActiveElements.OfType<BoundActiveElement>()) {
								if (boundElement.targetElement == element) {
									geoBuilder.AddSegment(textView, boundElement.Segment);
									geoBuilder.CloseFigure();
								}
							}
							drawingContext.DrawGeometry(null, activeBorderPen, geoBuilder.CreateGeometry());
						}
					}
				}
			}
		}
	}
}

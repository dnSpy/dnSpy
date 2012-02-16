// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Rendering
{
	// This class is internal because it does not need to be accessed by the user - it can be configured using TextEditorOptions.
	
	/// <summary>
	/// Element generator that displays · for spaces and » for tabs and a box for control characters.
	/// </summary>
	/// <remarks>
	/// This element generator is present in every TextView by default; the enabled features can be configured using the
	/// <see cref="TextEditorOptions"/>.
	/// </remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace")]
	sealed class SingleCharacterElementGenerator : VisualLineElementGenerator, IBuiltinElementGenerator
	{
		/// <summary>
		/// Gets/Sets whether to show · for spaces.
		/// </summary>
		public bool ShowSpaces { get; set; }
		
		/// <summary>
		/// Gets/Sets whether to show » for tabs.
		/// </summary>
		public bool ShowTabs { get; set; }
		
		/// <summary>
		/// Gets/Sets whether to show a box with the hex code for control characters.
		/// </summary>
		public bool ShowBoxForControlCharacters { get; set; }
		
		/// <summary>
		/// Creates a new SingleCharacterElementGenerator instance.
		/// </summary>
		public SingleCharacterElementGenerator()
		{
			this.ShowSpaces = true;
			this.ShowTabs = true;
			this.ShowBoxForControlCharacters = true;
		}
		
		void IBuiltinElementGenerator.FetchOptions(TextEditorOptions options)
		{
			this.ShowSpaces = options.ShowSpaces;
			this.ShowTabs = options.ShowTabs;
			this.ShowBoxForControlCharacters = options.ShowBoxForControlCharacters;
		}
		
		public override int GetFirstInterestedOffset(int startOffset)
		{
			DocumentLine endLine = CurrentContext.VisualLine.LastDocumentLine;
			StringSegment relevantText = CurrentContext.GetText(startOffset, endLine.EndOffset - startOffset);
			
			for (int i = 0; i < relevantText.Count; i++) {
				char c = relevantText.Text[relevantText.Offset + i];
				switch (c) {
					case ' ':
						if (ShowSpaces)
							return startOffset + i;
						break;
					case '\t':
						if (ShowTabs)
							return startOffset + i;
						break;
					default:
						if (ShowBoxForControlCharacters && char.IsControl(c)) {
							return startOffset + i;
						}
						break;
				}
			}
			return -1;
		}
		
		public override VisualLineElement ConstructElement(int offset)
		{
			char c = CurrentContext.Document.GetCharAt(offset);
			if (ShowSpaces && c == ' ') {
				return new SpaceTextElement(CurrentContext.TextView.cachedElements.GetTextForNonPrintableCharacter("\u00B7", CurrentContext));
			} else if (ShowTabs && c == '\t') {
				return new TabTextElement(CurrentContext.TextView.cachedElements.GetTextForNonPrintableCharacter("\u00BB", CurrentContext));
			} else if (ShowBoxForControlCharacters && char.IsControl(c)) {
				var p = new VisualLineElementTextRunProperties(CurrentContext.GlobalTextRunProperties);
				p.SetForegroundBrush(Brushes.White);
				var textFormatter = TextFormatterFactory.Create(CurrentContext.TextView);
				var text = FormattedTextElement.PrepareText(textFormatter,
				                                            TextUtilities.GetControlCharacterName(c), p);
				return new SpecialCharacterBoxElement(text);
			} else {
				return null;
			}
		}
		
		sealed class SpaceTextElement : FormattedTextElement
		{
			public SpaceTextElement(TextLine textLine) : base(textLine, 1)
			{
				BreakBefore = LineBreakCondition.BreakPossible;
				BreakAfter = LineBreakCondition.BreakDesired;
			}
			
			public override int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
			{
				if (mode == CaretPositioningMode.Normal)
					return base.GetNextCaretPosition(visualColumn, direction, mode);
				else
					return -1;
			}
			
			public override bool IsWhitespace(int visualColumn)
			{
				return true;
			}
		}
		
		sealed class TabTextElement : VisualLineElement
		{
			internal readonly TextLine text;
			
			public TabTextElement(TextLine text) : base(2, 1)
			{
				this.text = text;
			}
			
			public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
			{
				// the TabTextElement consists of two TextRuns:
				// first a TabGlyphRun, then TextCharacters '\t' to let WPF handle the tab indentation
				if (startVisualColumn == this.VisualColumn)
					return new TabGlyphRun(this, this.TextRunProperties);
				else if (startVisualColumn == this.VisualColumn + 1)
					return new TextCharacters("\t", 0, 1, this.TextRunProperties);
				else
					throw new ArgumentOutOfRangeException("startVisualColumn");
			}
			
			public override int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
			{
				if (mode == CaretPositioningMode.Normal)
					return base.GetNextCaretPosition(visualColumn, direction, mode);
				else
					return -1;
			}
			
			public override bool IsWhitespace(int visualColumn)
			{
				return true;
			}
		}
		
		sealed class TabGlyphRun : TextEmbeddedObject
		{
			readonly TabTextElement element;
			TextRunProperties properties;
			
			public TabGlyphRun(TabTextElement element, TextRunProperties properties)
			{
				if (properties == null)
					throw new ArgumentNullException("properties");
				this.properties = properties;
				this.element = element;
			}
			
			public override LineBreakCondition BreakBefore {
				get { return LineBreakCondition.BreakPossible; }
			}
			
			public override LineBreakCondition BreakAfter {
				get { return LineBreakCondition.BreakRestrained; }
			}
			
			public override bool HasFixedSize {
				get { return true; }
			}
			
			public override CharacterBufferReference CharacterBufferReference {
				get { return new CharacterBufferReference(); }
			}
			
			public override int Length {
				get { return 1; }
			}
			
			public override TextRunProperties Properties {
				get { return properties; }
			}
			
			public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
			{
				double width = Math.Min(0, element.text.WidthIncludingTrailingWhitespace - 1);
				return new TextEmbeddedObjectMetrics(width, element.text.Height, element.text.Baseline);
			}
			
			public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways)
			{
				double width = Math.Min(0, element.text.WidthIncludingTrailingWhitespace - 1);
				return new Rect(0, 0, width, element.text.Height);
			}
			
			public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
			{
				origin.Y -= element.text.Baseline;
				element.text.Draw(drawingContext, origin, InvertAxes.None);
			}
		}
		
		sealed class SpecialCharacterBoxElement : FormattedTextElement
		{
			public SpecialCharacterBoxElement(TextLine text) : base(text, 1)
			{
			}
			
			public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
			{
				return new SpecialCharacterTextRun(this, this.TextRunProperties);
			}
		}
		
		sealed class SpecialCharacterTextRun : FormattedTextRun
		{
			static readonly SolidColorBrush darkGrayBrush;
			
			static SpecialCharacterTextRun()
			{
				darkGrayBrush = new SolidColorBrush(Color.FromArgb(200, 128, 128, 128));
				darkGrayBrush.Freeze();
			}
			
			public SpecialCharacterTextRun(FormattedTextElement element, TextRunProperties properties)
				: base(element, properties)
			{
			}
			
			public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
			{
				Point newOrigin = new Point(origin.X + 1.5, origin.Y);
				var metrics = base.Format(double.PositiveInfinity);
				Rect r = new Rect(newOrigin.X - 0.5, newOrigin.Y - metrics.Baseline, metrics.Width + 2, metrics.Height);
				drawingContext.DrawRoundedRectangle(darkGrayBrush, null, r, 2.5, 2.5);
				base.Draw(drawingContext, newOrigin, rightToLeft, sideways);
			}
			
			public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
			{
				TextEmbeddedObjectMetrics metrics = base.Format(remainingParagraphWidth);
				return new TextEmbeddedObjectMetrics(metrics.Width + 3,
				                                     metrics.Height, metrics.Baseline);
			}
			
			public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways)
			{
				Rect r = base.ComputeBoundingBox(rightToLeft, sideways);
				r.Width += 3;
				return r;
			}
		}
	}
}

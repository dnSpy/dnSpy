// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Formatted text (not normal document text).
	/// This is used as base class for various VisualLineElements that are displayed using a
	/// FormattedText, for example newline markers or collapsed folding sections.
	/// </summary>
	public class FormattedTextElement : VisualLineElement
	{
		internal readonly FormattedText formattedText;
		internal string text;
		internal TextLine textLine;
		
		/// <summary>
		/// Creates a new FormattedTextElement that displays the specified text
		/// and occupies the specified length in the document.
		/// </summary>
		public FormattedTextElement(string text, int documentLength) : base(1, documentLength)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			this.text = text;
			this.BreakBefore = LineBreakCondition.BreakPossible;
			this.BreakAfter = LineBreakCondition.BreakPossible;
		}
		
		/// <summary>
		/// Creates a new FormattedTextElement that displays the specified text
		/// and occupies the specified length in the document.
		/// </summary>
		public FormattedTextElement(TextLine text, int documentLength) : base(1, documentLength)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			this.textLine = text;
			this.BreakBefore = LineBreakCondition.BreakPossible;
			this.BreakAfter = LineBreakCondition.BreakPossible;
		}
		
		/// <summary>
		/// Creates a new FormattedTextElement that displays the specified text
		/// and occupies the specified length in the document.
		/// </summary>
		public FormattedTextElement(FormattedText text, int documentLength) : base(1, documentLength)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			this.formattedText = text;
			this.BreakBefore = LineBreakCondition.BreakPossible;
			this.BreakAfter = LineBreakCondition.BreakPossible;
		}
		
		/// <summary>
		/// Gets/sets the line break condition before the element.
		/// The default is 'BreakPossible'.
		/// </summary>
		public LineBreakCondition BreakBefore { get; set; }
		
		/// <summary>
		/// Gets/sets the line break condition after the element.
		/// The default is 'BreakPossible'.
		/// </summary>
		public LineBreakCondition BreakAfter { get; set; }
		
		/// <inheritdoc/>
		public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
		{
			if (textLine == null) {
				var formatter = TextFormatterFactory.Create(context.TextView);
				textLine = PrepareText(formatter, this.text, this.TextRunProperties);
				this.text = null;
			}
			return new FormattedTextRun(this, this.TextRunProperties);
		}
		
		/// <summary>
		/// Constructs a TextLine from a simple text.
		/// </summary>
		public static TextLine PrepareText(TextFormatter formatter, string text, TextRunProperties properties)
		{
			if (formatter == null)
				throw new ArgumentNullException("formatter");
			if (text == null)
				throw new ArgumentNullException("text");
			if (properties == null)
				throw new ArgumentNullException("properties");
			return formatter.FormatLine(
				new SimpleTextSource(text, properties),
				0,
				32000,
				new VisualLineTextParagraphProperties {
					defaultTextRunProperties = properties,
					textWrapping = TextWrapping.NoWrap,
					tabSize = 40
				},
				null);
		}
	}
	
	/// <summary>
	/// This is the TextRun implementation used by the <see cref="FormattedTextElement"/> class.
	/// </summary>
	public class FormattedTextRun : TextEmbeddedObject
	{
		readonly FormattedTextElement element;
		TextRunProperties properties;
		
		/// <summary>
		/// Creates a new FormattedTextRun.
		/// </summary>
		public FormattedTextRun(FormattedTextElement element, TextRunProperties properties)
		{
			if (element == null)
				throw new ArgumentNullException("element");
			if (properties == null)
				throw new ArgumentNullException("properties");
			this.properties = properties;
			this.element = element;
		}
		
		/// <summary>
		/// Gets the element for which the FormattedTextRun was created.
		/// </summary>
		public FormattedTextElement Element {
			get { return element; }
		}
		
		/// <inheritdoc/>
		public override LineBreakCondition BreakBefore {
			get { return element.BreakBefore; }
		}
		
		/// <inheritdoc/>
		public override LineBreakCondition BreakAfter {
			get { return element.BreakAfter; }
		}
		
		/// <inheritdoc/>
		public override bool HasFixedSize {
			get { return true; }
		}
		
		/// <inheritdoc/>
		public override CharacterBufferReference CharacterBufferReference {
			get { return new CharacterBufferReference(); }
		}
		
		/// <inheritdoc/>
		public override int Length {
			get { return element.VisualLength; }
		}
		
		/// <inheritdoc/>
		public override TextRunProperties Properties {
			get { return properties; }
		}
		
		/// <inheritdoc/>
		public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
		{
			var formattedText = element.formattedText;
			if (formattedText != null) {
				return new TextEmbeddedObjectMetrics(formattedText.WidthIncludingTrailingWhitespace,
				                                     formattedText.Height,
				                                     formattedText.Baseline);
			} else {
				var text = element.textLine;
				return new TextEmbeddedObjectMetrics(text.WidthIncludingTrailingWhitespace,
				                                     text.Height,
				                                     text.Baseline);
			}
		}
		
		/// <inheritdoc/>
		public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways)
		{
			var formattedText = element.formattedText;
			if (formattedText != null) {
				return new Rect(0, 0, formattedText.WidthIncludingTrailingWhitespace, formattedText.Height);
			} else {
				var text = element.textLine;
				return new Rect(0, 0, text.WidthIncludingTrailingWhitespace, text.Height);
			}
		}
		
		/// <inheritdoc/>
		public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
		{
			if (element.formattedText != null) {
				origin.Y -= element.formattedText.Baseline;
				drawingContext.DrawText(element.formattedText, origin);
			} else {
				origin.Y -= element.textLine.Baseline;
				element.textLine.Draw(drawingContext, origin, InvertAxes.None);
			}
		}
	}
}

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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// A inline UIElement in the document.
	/// </summary>
	public class InlineObjectElement : VisualLineElement
	{
		/// <summary>
		/// Gets the inline element that is displayed.
		/// </summary>
		public UIElement Element { get; private set; }
		
		/// <summary>
		/// Creates a new InlineObjectElement.
		/// </summary>
		/// <param name="documentLength">The length of the element in the document. Must be non-negative.</param>
		/// <param name="element">The element to display.</param>
		public InlineObjectElement(int documentLength, UIElement element)
			: base(1, documentLength)
		{
			if (element == null)
				throw new ArgumentNullException("element");
			this.Element = element;
		}
		
		/// <inheritdoc/>
		public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			
			return new InlineObjectRun(1, this.TextRunProperties, this.Element);
		}
	}
	
	/// <summary>
	/// A text run with an embedded UIElement.
	/// </summary>
	public class InlineObjectRun : TextEmbeddedObject
	{
		UIElement element;
		int length;
		TextRunProperties properties;
		internal Size desiredSize;
		
		/// <summary>
		/// Creates a new InlineObjectRun instance.
		/// </summary>
		/// <param name="length">The length of the TextRun.</param>
		/// <param name="properties">The <see cref="TextRunProperties"/> to use.</param>
		/// <param name="element">The <see cref="UIElement"/> to display.</param>
		public InlineObjectRun(int length, TextRunProperties properties, UIElement element)
		{
			if (length <= 0)
				throw new ArgumentOutOfRangeException("length", length, "Value must be positive");
			if (properties == null)
				throw new ArgumentNullException("properties");
			if (element == null)
				throw new ArgumentNullException("element");
			
			this.length = length;
			this.properties = properties;
			this.element = element;
		}
		
		/// <summary>
		/// Gets the element displayed by the InlineObjectRun.
		/// </summary>
		public UIElement Element {
			get { return element; }
		}
		
		/// <summary>
		/// Gets the VisualLine that contains this object. This property is only available after the object
		/// was added to the text view.
		/// </summary>
		public VisualLine VisualLine { get; internal set; }
		
		/// <inheritdoc/>
		public override LineBreakCondition BreakBefore {
			get { return LineBreakCondition.BreakDesired; }
		}
		
		/// <inheritdoc/>
		public override LineBreakCondition BreakAfter {
			get { return LineBreakCondition.BreakDesired; }
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
			get { return length; }
		}
		
		/// <inheritdoc/>
		public override TextRunProperties Properties {
			get { return properties; }
		}
		
		/// <inheritdoc/>
		public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
		{
			double baseline = TextBlock.GetBaselineOffset(element);
			if (double.IsNaN(baseline))
				baseline = desiredSize.Height;
			return new TextEmbeddedObjectMetrics(desiredSize.Width, desiredSize.Height, baseline);
		}
		
		/// <inheritdoc/>
		public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways)
		{
			if (this.element.IsArrangeValid) {
				double baseline = TextBlock.GetBaselineOffset(element);
				if (double.IsNaN(baseline))
					baseline = desiredSize.Height;
				return new Rect(new Point(0, -baseline), desiredSize);
			} else {
				return Rect.Empty;
			}
		}
		
		/// <inheritdoc/>
		public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
		{
		}
	}
}

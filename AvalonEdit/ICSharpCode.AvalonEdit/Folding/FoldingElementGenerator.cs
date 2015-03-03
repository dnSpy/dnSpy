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
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Folding
{
	/// <summary>
	/// A <see cref="VisualLineElementGenerator"/> that produces line elements for folded <see cref="FoldingSection"/>s.
	/// </summary>
	public sealed class FoldingElementGenerator : VisualLineElementGenerator, ITextViewConnect
	{
		readonly List<TextView> textViews = new List<TextView>();
		FoldingManager foldingManager;
		
		#region FoldingManager property / connecting with TextView
		/// <summary>
		/// Gets/Sets the folding manager from which the foldings should be shown.
		/// </summary>
		public FoldingManager FoldingManager {
			get {
				return foldingManager;
			}
			set {
				if (foldingManager != value) {
					if (foldingManager != null) {
						foreach (TextView v in textViews)
							foldingManager.RemoveFromTextView(v);
					}
					foldingManager = value;
					if (foldingManager != null) {
						foreach (TextView v in textViews)
							foldingManager.AddToTextView(v);
					}
				}
			}
		}
		
		void ITextViewConnect.AddToTextView(TextView textView)
		{
			textViews.Add(textView);
			if (foldingManager != null)
				foldingManager.AddToTextView(textView);
		}
		
		void ITextViewConnect.RemoveFromTextView(TextView textView)
		{
			textViews.Remove(textView);
			if (foldingManager != null)
				foldingManager.RemoveFromTextView(textView);
		}
		#endregion
		
		/// <inheritdoc/>
		public override void StartGeneration(ITextRunConstructionContext context)
		{
			base.StartGeneration(context);
			if (foldingManager != null) {
				if (!foldingManager.textViews.Contains(context.TextView))
					throw new ArgumentException("Invalid TextView");
				if (context.Document != foldingManager.document)
					throw new ArgumentException("Invalid document");
			}
		}
		
		/// <inheritdoc/>
		public override int GetFirstInterestedOffset(int startOffset)
		{
			if (foldingManager != null) {
				foreach (FoldingSection fs in foldingManager.GetFoldingsContaining(startOffset)) {
					// Test whether we're currently within a folded folding (that didn't just end).
					// If so, create the fold marker immediately.
					// This is necessary if the actual beginning of the fold marker got skipped due to another VisualElementGenerator.
					if (fs.IsFolded && fs.EndOffset > startOffset) {
						//return startOffset;
					}
				}
				return foldingManager.GetNextFoldedFoldingStart(startOffset);
			} else {
				return -1;
			}
		}
		
		/// <inheritdoc/>
		public override VisualLineElement ConstructElement(int offset)
		{
			if (foldingManager == null)
				return null;
			int foldedUntil = -1;
			FoldingSection foldingSection = null;
			foreach (FoldingSection fs in foldingManager.GetFoldingsContaining(offset)) {
				if (fs.IsFolded) {
					if (fs.EndOffset > foldedUntil) {
						foldedUntil = fs.EndOffset;
						foldingSection = fs;
					}
				}
			}
			if (foldedUntil > offset && foldingSection != null) {
				// Handle overlapping foldings: if there's another folded folding
				// (starting within the foldingSection) that continues after the end of the folded section,
				// then we'll extend our fold element to cover that overlapping folding.
				bool foundOverlappingFolding;
				do {
					foundOverlappingFolding = false;
					foreach (FoldingSection fs in FoldingManager.GetFoldingsContaining(foldedUntil)) {
						if (fs.IsFolded && fs.EndOffset > foldedUntil) {
							foldedUntil = fs.EndOffset;
							foundOverlappingFolding = true;
						}
					}
				} while (foundOverlappingFolding);
				
				string title = foldingSection.Title;
				if (string.IsNullOrEmpty(title))
					title = "...";
				var p = new VisualLineElementTextRunProperties(CurrentContext.GlobalTextRunProperties);
				p.SetForegroundBrush(textBrush);
				var textFormatter = TextFormatterFactory.Create(CurrentContext.TextView);
				var text = FormattedTextElement.PrepareText(textFormatter, title, p);
				return new FoldingLineElement(foldingSection, text, foldedUntil - offset) { textBrush = textBrush };
			} else {
				return null;
			}
		}
		
		sealed class FoldingLineElement : FormattedTextElement
		{
			readonly FoldingSection fs;
			
			internal Brush textBrush;
			
			public FoldingLineElement(FoldingSection fs, TextLine text, int documentLength) : base(text, documentLength)
			{
				this.fs = fs;
			}
			
			public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
			{
				return new FoldingLineTextRun(this, this.TextRunProperties) { textBrush = textBrush };
			}
			
			protected internal override void OnMouseDown(MouseButtonEventArgs e)
			{
				if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left) {
					fs.IsFolded = false;
					e.Handled = true;
				} else {
					base.OnMouseDown(e);
				}
			}
		}
		
		sealed class FoldingLineTextRun : FormattedTextRun
		{
			internal Brush textBrush;
			
			public FoldingLineTextRun(FormattedTextElement element, TextRunProperties properties)
				: base(element, properties)
			{
			}
			
			public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
			{
				var metrics = Format(double.PositiveInfinity);
				Rect r = new Rect(origin.X, origin.Y - metrics.Baseline, metrics.Width, metrics.Height);
				drawingContext.DrawRectangle(null, new Pen(textBrush, 1), r);
				base.Draw(drawingContext, origin, rightToLeft, sideways);
			}
		}
		
		/// <summary>
		/// Default brush for folding element text. Value: Brushes.Gray
		/// </summary>
		public static readonly Brush DefaultTextBrush = Brushes.Gray;
		
		static Brush textBrush = DefaultTextBrush;
		
		/// <summary>
		/// Gets/sets the brush used for folding element text.
		/// </summary>
		public static Brush TextBrush {
			get { return textBrush; }
			set { textBrush = value; }
		}
	}
}

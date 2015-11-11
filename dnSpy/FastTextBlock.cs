/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace ICSharpCode.ILSpy {
	public class FastTextBlock : FrameworkElement {
		public string Text {
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		public static readonly DependencyProperty TextProperty;
		public static readonly DependencyProperty FontFamilyProperty;
		public static readonly DependencyProperty FontStyleProperty;
		public static readonly DependencyProperty FontWeightProperty;
		public static readonly DependencyProperty FontStretchProperty;
		public static readonly DependencyProperty FontSizeProperty;
		public static readonly DependencyProperty ForegroundProperty;

		static FastTextBlock() {
			TextProperty =
				DependencyProperty.Register("Text", typeof(string), typeof(FastTextBlock),
					new FrameworkPropertyMetadata("",
						FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
						RenderPropertyChanged));
			FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(typeof(FastTextBlock),
				new FrameworkPropertyMetadata(RenderPropertyChanged));
			FontStyleProperty = TextElement.FontStyleProperty.AddOwner(typeof(FastTextBlock),
				new FrameworkPropertyMetadata(RenderPropertyChanged));
			FontWeightProperty = TextElement.FontWeightProperty.AddOwner(typeof(FastTextBlock),
				new FrameworkPropertyMetadata(RenderPropertyChanged));
			FontStretchProperty = TextElement.FontStretchProperty.AddOwner(typeof(FastTextBlock),
				new FrameworkPropertyMetadata(RenderPropertyChanged));
			FontSizeProperty = TextElement.FontSizeProperty.AddOwner(typeof(FastTextBlock),
				new FrameworkPropertyMetadata(RenderPropertyChanged));
			ForegroundProperty = TextElement.ForegroundProperty.AddOwner(typeof(FastTextBlock),
				new FrameworkPropertyMetadata(RenderPropertyChanged));
		}

		static int H(object obj) {
			return obj == null ? 0 : obj.GetHashCode();
		}

		int CacheHash() {
			int hash = 17;
			hash = hash * 23 + H(GetValue(TextProperty));
			hash = hash * 23 + H(GetValue(FontFamilyProperty));
			hash = hash * 23 + H(GetValue(FontStyleProperty));
			hash = hash * 23 + H(GetValue(FontWeightProperty));
			hash = hash * 23 + H(GetValue(FontStretchProperty));
			hash = hash * 23 + H(GetValue(FontSizeProperty));
			hash = hash * 23 + H(GetValue(ForegroundProperty));
			hash = hash * 23 + H(FlowDirection);
			hash = hash * 23 + H(CultureInfo.CurrentUICulture);

			return hash;
		}

		int cache;
		bool invalidate = false;

		static void RenderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			((FastTextBlock)d).invalidate = true;
		}

		class TextProps : TextRunProperties {
			FastTextBlock tb;

			public TextProps(FastTextBlock tb) {
				this.tb = tb;
			}

			public override Brush BackgroundBrush {
				get { return null; }
			}

			public override CultureInfo CultureInfo {
				get { return CultureInfo.CurrentUICulture; }
			}

			public override double FontHintingEmSize {
				get { return 12; }
			}

			public override double FontRenderingEmSize {
				get { return (double)tb.GetValue(FontSizeProperty); }
			}

			public override Brush ForegroundBrush {
				get { return (Brush)tb.GetValue(ForegroundProperty); }
			}

			public override TextDecorationCollection TextDecorations {
				get { return null; }
			}

			public override TextEffectCollection TextEffects {
				get { return null; }
			}

			public override Typeface Typeface {
				get { return tb.GetTypeface(); }
			}
		}

		class TextSrc : TextSource {
			string text;
			TextProps props;

			public TextSrc(FastTextBlock tb) {
				text = tb.Text;
				props = new TextProps(tb);
			}

			public override TextRun GetTextRun(int textSourceCharacterIndex) {
				if (textSourceCharacterIndex >= text.Length) {
					return new TextEndOfParagraph(1);
				}
				return new TextCharacters(text, textSourceCharacterIndex, text.Length - textSourceCharacterIndex, props);
			}

			public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit) {
				return new TextSpan<CultureSpecificCharacterBufferRange>(0,
					new CultureSpecificCharacterBufferRange(CultureInfo.CurrentUICulture, new CharacterBufferRange(string.Empty, 0, 0)));
			}

			public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex) {
				throw new NotSupportedException();
			}
		}

		internal sealed class ParaProps : TextParagraphProperties {
			FastTextBlock tb;
			TextProps props;

			public ParaProps(FastTextBlock tb) {
				this.tb = tb;
				props = new TextProps(tb);
			}

			public override TextRunProperties DefaultTextRunProperties {
				get { return props; }
			}

			public override bool FirstLineInParagraph {
				get { return false; }
			}

			public override FlowDirection FlowDirection {
				get { return tb.FlowDirection; }
			}

			public override double Indent {
				get { return 0; }
			}

			public override double LineHeight {
				get { return 0; }
			}

			public override TextAlignment TextAlignment {
				get { return TextAlignment.Left; }
			}

			public override TextMarkerProperties TextMarkerProperties {
				get { return null; }
			}

			public override TextWrapping TextWrapping {
				get { return TextWrapping.NoWrap; }
			}
		}


		TextFormatter fmt = null;
		TextLine line = null;

		Typeface GetTypeface() {
			var fontFamily = (FontFamily)GetValue(FontFamilyProperty);
			var fontStrech = (FontStretch)GetValue(FontStretchProperty);
			var fontStyle = (FontStyle)GetValue(FontStyleProperty);
			var fontWeight = (FontWeight)GetValue(FontWeightProperty);
			return new Typeface(fontFamily, fontStyle, fontWeight, fontStrech, null);
		}

		void MakeNewText() {

			if (fmt == null)
				fmt = TextFormatter.Create(TextOptions.GetTextFormattingMode(this));

			if (line != null)
				line.Dispose();

			line = fmt.FormatLine(new TextSrc(this), 0, 0, new ParaProps(this), null);
		}


		protected override Size MeasureOverride(Size availableSize) {
			if (invalidate) {
				invalidate = false;
				var hash = CacheHash();
				if (cache != hash) {
					cache = hash;
					MakeNewText();
				}
			}

			if (line == null)
				return Size.Empty;
			return new Size(line.Width, line.Height);
		}

		protected override void OnRender(DrawingContext drawingContext) {
			if (line != null)
				line.Draw(drawingContext, new Point(0, 0), InvertAxes.None);
		}
	}
}
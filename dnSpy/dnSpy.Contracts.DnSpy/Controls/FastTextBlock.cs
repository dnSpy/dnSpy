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
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace dnSpy.Contracts.Controls {
	sealed class FastTextBlock : FrameworkElement {
		public interface IFastTextSource {
			void UpdateParent(FastTextBlock ftb);
			TextSource Source { get; }
		}

		public string Text {
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		readonly bool useNewFormatter;

		public FastTextBlock(bool useNewFormatter)
			: this(useNewFormatter, new TextSrc()) {
		}

		public FastTextBlock(bool useNewFormatter, IFastTextSource src) {
			this.useNewFormatter = useNewFormatter;
			this.src = src;
		}


		public static readonly DependencyProperty TextProperty;
		public static readonly DependencyProperty FontFamilyProperty;
		public static readonly DependencyProperty FontStyleProperty;
		public static readonly DependencyProperty FontWeightProperty;
		public static readonly DependencyProperty FontStretchProperty;
		public static readonly DependencyProperty FontSizeProperty;
		public static readonly DependencyProperty ForegroundProperty;
		public static readonly DependencyProperty BackgroundProperty;

		static FastTextBlock() {
			TextProperty =
				DependencyProperty.Register(nameof(Text), typeof(string), typeof(FastTextBlock),
					new FrameworkPropertyMetadata("",
						FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));
			FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(typeof(FastTextBlock));
			FontStyleProperty = TextElement.FontStyleProperty.AddOwner(typeof(FastTextBlock));
			FontWeightProperty = TextElement.FontWeightProperty.AddOwner(typeof(FastTextBlock));
			FontStretchProperty = TextElement.FontStretchProperty.AddOwner(typeof(FastTextBlock));
			FontSizeProperty = TextElement.FontSizeProperty.AddOwner(typeof(FastTextBlock));
			ForegroundProperty = TextElement.ForegroundProperty.AddOwner(typeof(FastTextBlock));
			BackgroundProperty = TextElement.BackgroundProperty.AddOwner(typeof(FastTextBlock));
		}

		static int H(object obj) => obj == null ? 0 : obj.GetHashCode();

		int CacheHash() {
			int hash = 17;
			hash = hash * 23 + H(GetValue(TextProperty));
			hash = hash * 23 + H(GetValue(FontFamilyProperty));
			hash = hash * 23 + H(GetValue(FontStyleProperty));
			hash = hash * 23 + H(GetValue(FontWeightProperty));
			hash = hash * 23 + H(GetValue(FontStretchProperty));
			hash = hash * 23 + H(GetValue(FontSizeProperty));
			hash = hash * 23 + H(GetValue(ForegroundProperty));
			hash = hash * 23 + H(GetValue(BackgroundProperty));
			hash = hash * 23 + H(FlowDirection);
			hash = hash * 23 + H(CultureInfo.CurrentUICulture);

			var newMode = TextOptions.GetTextFormattingMode(this);
			hash = hash * 23 + (int)newMode;

			if (textFormattingMode != newMode) {
				textFormattingMode = newMode;
				fmt = null;
				Debug.Assert(hash != cache);
			}

			return hash;
		}

		int cache;
		TextFormattingMode? textFormattingMode;

		class TextProps : TextRunProperties {
			FastTextBlock tb;

			public TextProps(FastTextBlock tb) => this.tb = tb;

			public override Brush BackgroundBrush => (Brush)tb.GetValue(BackgroundProperty);
			public override CultureInfo CultureInfo => CultureInfo.CurrentUICulture;
			public override double FontHintingEmSize => 12;
			public override double FontRenderingEmSize => (double)tb.GetValue(FontSizeProperty);
			public override Brush ForegroundBrush => (Brush)tb.GetValue(ForegroundProperty);
			public override TextDecorationCollection TextDecorations => null;
			public override TextEffectCollection TextEffects => null;
			public override Typeface Typeface => tb.GetTypeface();
		}

		class TextSrc : TextSource, IFastTextSource {
			string text;
			TextProps props;

			public override TextRun GetTextRun(int textSourceCharacterIndex) {
				if (textSourceCharacterIndex >= text.Length) {
					return new TextEndOfParagraph(1);
				}
				return new TextCharacters(text, textSourceCharacterIndex, text.Length - textSourceCharacterIndex, props);
			}

			public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit) => new TextSpan<CultureSpecificCharacterBufferRange>(0,
					new CultureSpecificCharacterBufferRange(CultureInfo.CurrentUICulture, new CharacterBufferRange(string.Empty, 0, 0)));

			public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex) => throw new NotSupportedException();

			public void UpdateParent(FastTextBlock ftb) {
				text = ftb.Text;
				props = new TextProps(ftb);
			}

			public TextSource Source => this;
		}

		internal sealed class ParaProps : TextParagraphProperties {
			FastTextBlock tb;
			TextProps props;

			public ParaProps(FastTextBlock tb) {
				this.tb = tb;
				props = new TextProps(tb);
			}

			public override TextRunProperties DefaultTextRunProperties => props;
			public override bool FirstLineInParagraph => false;
			public override FlowDirection FlowDirection => tb.FlowDirection;
			public override double Indent => 0;
			public override double LineHeight => 0;
			public override TextAlignment TextAlignment => TextAlignment.Left;
			public override TextMarkerProperties TextMarkerProperties => null;
			public override TextWrapping TextWrapping => TextWrapping.NoWrap;
		}


		ITextFormatter fmt = null;
		TextLine line = null;

		Typeface GetTypeface() {
			var fontFamily = (FontFamily)GetValue(FontFamilyProperty);
			var fontStrech = (FontStretch)GetValue(FontStretchProperty);
			var fontStyle = (FontStyle)GetValue(FontStyleProperty);
			var fontWeight = (FontWeight)GetValue(FontWeightProperty);
			return new Typeface(fontFamily, fontStyle, fontWeight, fontStrech, null);
		}

		IFastTextSource src;

		void MakeNewText() {
			if (fmt == null)
				fmt = TextFormatterFactory.Create(this, useNewFormatter);

			if (line != null)
				line.Dispose();

			src.UpdateParent(this);
			line = fmt.FormatLine(src.Source, 0, 0, new ParaProps(this), null);
		}

		void EnsureText() {
			var hash = CacheHash();
			if (cache != hash || line == null) {
				cache = hash;
				MakeNewText();
			}
		}


		protected override Size MeasureOverride(Size availableSize) {
			EnsureText();
			return new Size(line.Width, line.Height);
		}

		protected override void OnRender(DrawingContext drawingContext) {
			EnsureText();
			line.Draw(drawingContext, new Point(0, 0), InvertAxes.None);
		}
	}
}
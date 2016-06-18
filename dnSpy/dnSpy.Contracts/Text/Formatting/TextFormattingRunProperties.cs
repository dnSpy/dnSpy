/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace dnSpy.Contracts.Text.Formatting {
	/// <summary>
	/// Text formatting run properties
	/// </summary>
	public sealed class TextFormattingRunProperties : TextRunProperties, IEquatable<TextFormattingRunProperties> {
		Typeface typeface;
		bool? isBold;
		bool? isItalic;
		Brush foreground;
		Brush background;
		double? foregroundOpacity;
		double? backgroundOpacity;
		CultureInfo cultureInfo;
		double? hintingSize;
		double? renderingSize;
		TextDecorationCollection textDecorations;
		TextEffectCollection textEffects;

		/// <summary>
		/// Gets the background brush
		/// </summary>
		public override Brush BackgroundBrush => background ?? Brushes.Transparent;

		/// <summary>
		/// true if <see cref="BackgroundBrush"/> hasn't been initialized
		/// </summary>
		public bool BackgroundBrushEmpty => background == null;

		/// <summary>
		/// Gets the background opacity
		/// </summary>
		public double BackgroundOpacity => backgroundOpacity ?? 1;

		/// <summary>
		/// true if <see cref="BackgroundOpacity"/> hasn't been initialized
		/// </summary>
		public bool BackgroundOpacityEmpty => backgroundOpacity == null;

		/// <summary>
		/// true if it's bold
		/// </summary>
		public bool Bold => isBold != null && isBold.Value;

		/// <summary>
		/// true if <see cref="Bold"/> hasn't been initialized
		/// </summary>
		public bool BoldEmpty => isBold == null;

		/// <summary>
		/// Gets the culture information
		/// </summary>
		public override CultureInfo CultureInfo => cultureInfo ?? CultureInfo.CurrentCulture;

		/// <summary>
		/// true if <see cref="CultureInfo"/> hasn't been initialized
		/// </summary>
		public bool CultureInfoEmpty => cultureInfo == null;

		/// <summary>
		/// Gets the font hinting size
		/// </summary>
		public override double FontHintingEmSize => hintingSize ?? 16.0;

		/// <summary>
		/// true if <see cref="FontHintingEmSize"/> hasn't been initialized
		/// </summary>
		public bool FontHintingEmSizeEmpty => hintingSize == null;

		/// <summary>
		/// Gets the font rendering size
		/// </summary>
		public override double FontRenderingEmSize => renderingSize ?? 16.0;

		/// <summary>
		/// true if <see cref="FontRenderingEmSize"/> hasn't been initialized
		/// </summary>
		public bool FontRenderingEmSizeEmpty => renderingSize == null;

		/// <summary>
		/// Gets the foreground brush
		/// </summary>
		public override Brush ForegroundBrush => foreground ?? Brushes.Transparent;

		/// <summary>
		/// true if <see cref="ForegroundBrush"/> hasn't been initialized
		/// </summary>
		public bool ForegroundBrushEmpty => foreground == null;

		/// <summary>
		/// Gets the foreground opacity
		/// </summary>
		public double ForegroundOpacity => foregroundOpacity ?? 1;

		/// <summary>
		/// true if <see cref="ForegroundOpacity"/> hasn't been initialized
		/// </summary>
		public bool ForegroundOpacityEmpty => foregroundOpacity == null;

		/// <summary>
		/// true if italic
		/// </summary>
		public bool Italic => isItalic != null && isItalic.Value;

		/// <summary>
		/// true if <see cref="Italic"/> hasn't been initialized
		/// </summary>
		public bool ItalicEmpty => isItalic == null;

		/// <summary>
		/// Gets the decorations for the text
		/// </summary>
		public override TextDecorationCollection TextDecorations => textDecorations ?? emptyTextDecorations;
		static readonly TextDecorationCollection emptyTextDecorations;

		/// <summary>
		/// true if <see cref="TextDecorations"/> hasn't been initialized
		/// </summary>
		public bool TextDecorationsEmpty => textDecorations == null;

		/// <summary>
		/// Gets the text effects for the text
		/// </summary>
		public override TextEffectCollection TextEffects => textEffects ?? emptyTextEffects;
		static readonly TextEffectCollection emptyTextEffects;

		/// <summary>
		/// true if <see cref="TextEffects"/> hasn't been initialized
		/// </summary>
		public bool TextEffectsEmpty => textEffects == null;

		/// <summary>
		/// Gets the typeface for the text
		/// </summary>
		public override Typeface Typeface => typeface;

		/// <summary>
		/// true if <see cref="Typeface"/> hasn't been initialized
		/// </summary>
		public bool TypefaceEmpty => typeface == null;

		TextFormattingRunProperties Freeze() {
			if (foreground?.CanFreeze == true)
				foreground.Freeze();
			if (background?.CanFreeze == true)
				background.Freeze();
			if (textDecorations?.CanFreeze == true)
				textDecorations.Freeze();
			if (textEffects?.CanFreeze == true)
				textEffects.Freeze();
			return this;
		}

		static TextFormattingRunProperties() {
			emptyTextDecorations = new TextDecorationCollection();
			emptyTextDecorations.Freeze();
			emptyTextEffects = new TextEffectCollection(0);
			emptyTextEffects.Freeze();
		}

		TextFormattingRunProperties() { }

		TextFormattingRunProperties(Brush foreground, Brush background, Typeface typeface, double? renderingSize, double? hintingSize, TextDecorationCollection textDecorations, TextEffectCollection textEffects, CultureInfo cultureInfo) {
			this.foreground = foreground;
			this.background = background;
			this.typeface = typeface;
			this.renderingSize = renderingSize;
			this.hintingSize = hintingSize;
			this.textDecorations = textDecorations;
			this.textEffects = textEffects;
			this.cultureInfo = cultureInfo;
			Freeze();
		}

		TextFormattingRunProperties(TextFormattingRunProperties other) {
			this.typeface = other.typeface;
			this.isBold = other.isBold;
			this.isItalic = other.isItalic;
			this.foreground = other.foreground;
			this.background = other.background;
			this.foregroundOpacity = other.foregroundOpacity;
			this.backgroundOpacity = other.backgroundOpacity;
			this.cultureInfo = other.cultureInfo;
			this.hintingSize = other.hintingSize;
			this.renderingSize = other.renderingSize;
			this.textDecorations = other.textDecorations;
			this.textEffects = other.textEffects;
		}

		static readonly TextFormattingRunProperties empty = new TextFormattingRunProperties();

		/// <summary>
		/// Creates a new <see cref="TextFormattingRunProperties"/> instance
		/// </summary>
		/// <returns></returns>
		public static TextFormattingRunProperties CreateTextFormattingRunProperties() => empty;

		/// <summary>
		/// Creates a new <see cref="TextFormattingRunProperties"/> instance
		/// </summary>
		/// <param name="typeface"></param>
		/// <param name="size"></param>
		/// <param name="foreground"></param>
		/// <returns></returns>
		public static TextFormattingRunProperties CreateTextFormattingRunProperties(Typeface typeface, double size, System.Windows.Media.Color foreground) =>
			new TextFormattingRunProperties(new SolidColorBrush(foreground), null, typeface, size, null, null, null, null);

		/// <summary>
		/// Creates a new <see cref="TextFormattingRunProperties"/> instance
		/// </summary>
		/// <param name="foreground">Foreground brush or null</param>
		/// <param name="background">Background brush or null</param>
		/// <param name="typeface">Typeface or null</param>
		/// <param name="size">Size or null</param>
		/// <param name="hintingSize">Hinting size or null</param>
		/// <param name="textDecorations">Text decorations or null</param>
		/// <param name="textEffects">Text effects or null</param>
		/// <param name="cultureInfo">Culture info or null</param>
		/// <returns></returns>
		public static TextFormattingRunProperties CreateTextFormattingRunProperties(Brush foreground, Brush background, Typeface typeface, double? size, double? hintingSize, TextDecorationCollection textDecorations, TextEffectCollection textEffects, CultureInfo cultureInfo) =>
			new TextFormattingRunProperties(foreground, background, typeface, size, hintingSize, textDecorations, textEffects, cultureInfo);

		/// <summary>
		/// Checks whether background is the same as <paramref name="brush"/>
		/// </summary>
		/// <param name="brush">Brush</param>
		/// <returns></returns>
		public bool BackgroundBrushSame(Brush brush) => BrushUtils.Equals(brush, background);

		/// <summary>
		/// Checks whether foreground is the same as <paramref name="brush"/>
		/// </summary>
		/// <param name="brush">Brush</param>
		/// <returns></returns>
		public bool ForegroundBrushSame(Brush brush) => BrushUtils.Equals(brush, foreground);

		/// <summary>
		/// true if if rendering size is the same
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool SameSize(TextFormattingRunProperties other) => renderingSize == other?.renderingSize;

		/// <summary>
		/// Creates a new instance with background brush cleared
		/// </summary>
		/// <returns></returns>
		public TextFormattingRunProperties ClearBackgroundBrush() => new TextFormattingRunProperties(this) { background = null };

		/// <summary>
		/// Creates a new instance with background opacity cleared
		/// </summary>
		/// <returns></returns>
		public TextFormattingRunProperties ClearBackgroundOpacity() => new TextFormattingRunProperties(this) { backgroundOpacity = null };

		/// <summary>
		/// Creates a new instance with bold cleared
		/// </summary>
		/// <returns></returns>
		public TextFormattingRunProperties ClearBold() => new TextFormattingRunProperties(this) { isBold = null };

		/// <summary>
		/// Creates a new instance with culture info cleared
		/// </summary>
		/// <returns></returns>
		public TextFormattingRunProperties ClearCultureInfo() => new TextFormattingRunProperties(this) { cultureInfo = null };

		/// <summary>
		/// Creates a new instance with font hinting em size cleared
		/// </summary>
		/// <returns></returns>
		public TextFormattingRunProperties ClearFontHintingEmSize() => new TextFormattingRunProperties(this) { hintingSize = null };

		/// <summary>
		/// Creates a new instance with font rendering em size cleared
		/// </summary>
		/// <returns></returns>
		public TextFormattingRunProperties ClearFontRenderingEmSize() => new TextFormattingRunProperties(this) { renderingSize = null };

		/// <summary>
		/// Creates a new instance with foreground brush cleared
		/// </summary>
		/// <returns></returns>
		public TextFormattingRunProperties ClearForegroundBrush() => new TextFormattingRunProperties(this) { foreground = null };

		/// <summary>
		/// Creates a new instance with foreground opacity cleared
		/// </summary>
		/// <returns></returns>
		public TextFormattingRunProperties ClearForegroundOpacity() => new TextFormattingRunProperties(this) { foregroundOpacity = null };

		/// <summary>
		/// Creates a new instance with italic cleared
		/// </summary>
		/// <returns></returns>
		public TextFormattingRunProperties ClearItalic() => new TextFormattingRunProperties(this) { isItalic = null };

		/// <summary>
		/// Creates a new instance with text decorations cleared
		/// </summary>
		/// <returns></returns>
		public TextFormattingRunProperties ClearTextDecorations() => new TextFormattingRunProperties(this) { textDecorations = null };

		/// <summary>
		/// Creates a new instance with text effects cleared
		/// </summary>
		/// <returns></returns>
		public TextFormattingRunProperties ClearTextEffects() => new TextFormattingRunProperties(this) { textEffects = null };

		/// <summary>
		/// Creates a new instance with typeface cleared
		/// </summary>
		/// <returns></returns>
		public TextFormattingRunProperties ClearTypeface() => new TextFormattingRunProperties(this) { typeface = null };

		/// <summary>
		/// Creates a new instance with a new background color
		/// </summary>
		/// <param name="background">New background color</param>
		/// <returns></returns>
		public TextFormattingRunProperties SetBackground(System.Windows.Media.Color background) => SetBackgroundBrush(new SolidColorBrush(background));

		/// <summary>
		/// Creates a new instance with a new background brush
		/// </summary>
		/// <param name="brush">New background brush</param>
		/// <returns></returns>
		public TextFormattingRunProperties SetBackgroundBrush(Brush brush) => new TextFormattingRunProperties(this) { background = brush }.Freeze();

		/// <summary>
		/// Creates a new instance with a new background opactiy
		/// </summary>
		/// <param name="opacity">Opacity</param>
		/// <returns></returns>
		public TextFormattingRunProperties SetBackgroundOpacity(double opacity) => new TextFormattingRunProperties(this) { backgroundOpacity = opacity };

		/// <summary>
		/// Creates a new instance with a new bold value
		/// </summary>
		/// <param name="isBold">Bold</param>
		/// <returns></returns>
		public TextFormattingRunProperties SetBold(bool isBold) => new TextFormattingRunProperties(this) { isBold = isBold };

		/// <summary>
		/// Creates a new instance with a new culture info
		/// </summary>
		/// <param name="cultureInfo">Culture info</param>
		/// <returns></returns>
		public TextFormattingRunProperties SetCultureInfo(CultureInfo cultureInfo) => new TextFormattingRunProperties(this) { cultureInfo = cultureInfo };

		/// <summary>
		/// Creates a new instance with a new font hinting em size
		/// </summary>
		/// <param name="hintingSize">Font hinting em size</param>
		/// <returns></returns>
		public TextFormattingRunProperties SetFontHintingEmSize(double hintingSize) => new TextFormattingRunProperties(this) { hintingSize = hintingSize };

		/// <summary>
		/// Creates a new instance with a new font rendering em size
		/// </summary>
		/// <param name="renderingSize">Font rendering em size</param>
		/// <returns></returns>
		public TextFormattingRunProperties SetFontRenderingEmSize(double renderingSize) => new TextFormattingRunProperties(this) { renderingSize = renderingSize };

		/// <summary>
		/// Creates a new instance with a new foreground color
		/// </summary>
		/// <param name="foreground">Foreground color</param>
		/// <returns></returns>
		public TextFormattingRunProperties SetForeground(System.Windows.Media.Color foreground) => SetForegroundBrush(new SolidColorBrush(foreground));

		/// <summary>
		/// Creates a new instance with a new foreground brush
		/// </summary>
		/// <param name="brush">Foreground brush</param>
		/// <returns></returns>
		public TextFormattingRunProperties SetForegroundBrush(Brush brush) => new TextFormattingRunProperties(this) { foreground = brush }.Freeze();

		/// <summary>
		/// Creates a new instance with a new foreground opacity
		/// </summary>
		/// <param name="opacity">Opacity</param>
		/// <returns></returns>
		public TextFormattingRunProperties SetForegroundOpacity(double opacity) => new TextFormattingRunProperties(this) { foregroundOpacity = opacity };

		/// <summary>
		/// Creates a new instance with a new italic value
		/// </summary>
		/// <param name="isItalic">Italic</param>
		/// <returns></returns>
		public TextFormattingRunProperties SetItalic(bool isItalic) => new TextFormattingRunProperties(this) { isItalic = isItalic };

		/// <summary>
		/// Creates a new instance with new text decorations
		/// </summary>
		/// <param name="textDecorations">Text decorations</param>
		/// <returns></returns>
		public TextFormattingRunProperties SetTextDecorations(TextDecorationCollection textDecorations) => new TextFormattingRunProperties(this) { textDecorations = textDecorations }.Freeze();

		/// <summary>
		/// Creates a new instance with new text effects
		/// </summary>
		/// <param name="textEffects">Text effects</param>
		/// <returns></returns>
		public TextFormattingRunProperties SetTextEffects(TextEffectCollection textEffects) => new TextFormattingRunProperties(this) { textEffects = textEffects }.Freeze();

		/// <summary>
		/// Creates a new instance with a new typeface
		/// </summary>
		/// <param name="typeface">Typeface</param>
		/// <returns></returns>
		public TextFormattingRunProperties SetTypeface(Typeface typeface) => new TextFormattingRunProperties(this) { typeface = typeface };

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(TextFormattingRunProperties other) {
			return TypefaceEquals(typeface, other.typeface) &&
				isBold == other.isBold &&
				isItalic == other.isItalic &&
				BrushUtils.Equals(foreground, other.foreground) &&
				BrushUtils.Equals(background, other.background) &&
				foregroundOpacity == other.foregroundOpacity &&
				backgroundOpacity == other.backgroundOpacity &&
				cultureInfo == other.cultureInfo &&
				hintingSize == other.hintingSize &&
				renderingSize == other.renderingSize &&
				textDecorations == other.textDecorations &&
				textEffects == other.textEffects;
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => Equals(obj as TextFormattingRunProperties);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() {
			return GetHashCode(typeface) ^
				(isBold == null ? 0 : isBold.Value ? int.MinValue : 0) ^
				(isItalic == null ? 0 : isItalic.Value ? 0x40000000 : 0) ^
				BrushUtils.GetHashCode(foreground) ^
				BrushUtils.GetHashCode(background) ^
				(foregroundOpacity == null ? 0 : foregroundOpacity.Value.GetHashCode()) ^
				(backgroundOpacity == null ? 0 : backgroundOpacity.Value.GetHashCode()) ^
				(cultureInfo?.GetHashCode() ?? 0) ^
				(hintingSize == null ? 0 : hintingSize.Value.GetHashCode()) ^
				(renderingSize == null ? 0 : renderingSize.Value.GetHashCode()) ^
				(textDecorations?.GetHashCode() ?? 0) ^
				(textEffects?.GetHashCode() ?? 0);
		}

		int GetHashCode(Typeface typeface) => typeface?.GetHashCode() ?? 0;
		bool TypefaceEquals(Typeface a, Typeface b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			return a.Equals(b);
		}
	}
}

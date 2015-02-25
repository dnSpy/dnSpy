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
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// <see cref="TextRunProperties"/> implementation that allows changing the properties.
	/// A <see cref="VisualLineElementTextRunProperties"/> instance usually is assigned to a single
	/// <see cref="VisualLineElement"/>.
	/// </summary>
	public class VisualLineElementTextRunProperties : TextRunProperties, ICloneable
	{
		Brush backgroundBrush;
		BaselineAlignment baselineAlignment;
		CultureInfo cultureInfo;
		double fontHintingEmSize;
		double fontRenderingEmSize;
		Brush foregroundBrush;
		Typeface typeface;
		TextDecorationCollection textDecorations;
		TextEffectCollection textEffects;
		TextRunTypographyProperties typographyProperties;
		NumberSubstitution numberSubstitution;
		
		/// <summary>
		/// Creates a new VisualLineElementTextRunProperties instance that copies its values
		/// from the specified <paramref name="textRunProperties"/>.
		/// For the <see cref="TextDecorations"/> and <see cref="TextEffects"/> collections, deep copies
		/// are created if those collections are not frozen.
		/// </summary>
		public VisualLineElementTextRunProperties(TextRunProperties textRunProperties)
		{
			if (textRunProperties == null)
				throw new ArgumentNullException("textRunProperties");
			backgroundBrush = textRunProperties.BackgroundBrush;
			baselineAlignment = textRunProperties.BaselineAlignment;
			cultureInfo = textRunProperties.CultureInfo;
			fontHintingEmSize = textRunProperties.FontHintingEmSize;
			fontRenderingEmSize = textRunProperties.FontRenderingEmSize;
			foregroundBrush = textRunProperties.ForegroundBrush;
			typeface = textRunProperties.Typeface;
			textDecorations = textRunProperties.TextDecorations;
			if (textDecorations != null && !textDecorations.IsFrozen) {
				textDecorations = textDecorations.Clone();
			}
			textEffects = textRunProperties.TextEffects;
			if (textEffects != null && !textEffects.IsFrozen) {
				textEffects = textEffects.Clone();
			}
			typographyProperties = textRunProperties.TypographyProperties;
			numberSubstitution = textRunProperties.NumberSubstitution;
		}
		
		/// <summary>
		/// Creates a copy of this instance.
		/// </summary>
		public virtual VisualLineElementTextRunProperties Clone()
		{
			return new VisualLineElementTextRunProperties(this);
		}
		
		object ICloneable.Clone()
		{
			return Clone();
		}
		
		/// <inheritdoc/>
		public override Brush BackgroundBrush {
			get { return backgroundBrush; }
		}
		
		/// <summary>
		/// Sets the <see cref="BackgroundBrush"/>.
		/// </summary>
		public void SetBackgroundBrush(Brush value)
		{
			ExtensionMethods.CheckIsFrozen(value);
			backgroundBrush = value;
		}
		
		/// <inheritdoc/>
		public override BaselineAlignment BaselineAlignment {
			get { return baselineAlignment; }
		}
		
		/// <summary>
		/// Sets the <see cref="BaselineAlignment"/>.
		/// </summary>
		public void SetBaselineAlignment(BaselineAlignment value)
		{
			baselineAlignment = value;
		}
		
		/// <inheritdoc/>
		public override CultureInfo CultureInfo {
			get { return cultureInfo; }
		}
		
		/// <summary>
		/// Sets the <see cref="CultureInfo"/>.
		/// </summary>
		public void SetCultureInfo(CultureInfo value)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			cultureInfo = value;
		}
		
		/// <inheritdoc/>
		public override double FontHintingEmSize {
			get { return fontHintingEmSize; }
		}
		
		/// <summary>
		/// Sets the <see cref="FontHintingEmSize"/>.
		/// </summary>
		public void SetFontHintingEmSize(double value)
		{
			fontHintingEmSize = value;
		}
		
		/// <inheritdoc/>
		public override double FontRenderingEmSize {
			get { return fontRenderingEmSize; }
		}
		
		/// <summary>
		/// Sets the <see cref="FontRenderingEmSize"/>.
		/// </summary>
		public void SetFontRenderingEmSize(double value)
		{
			fontRenderingEmSize = value;
		}
		
		/// <inheritdoc/>
		public override Brush ForegroundBrush {
			get { return foregroundBrush; }
		}
		
		/// <summary>
		/// Sets the <see cref="ForegroundBrush"/>.
		/// </summary>
		public void SetForegroundBrush(Brush value)
		{
			ExtensionMethods.CheckIsFrozen(value);
			foregroundBrush = value;
		}
		
		/// <inheritdoc/>
		public override Typeface Typeface {
			get { return typeface; }
		}
		
		/// <summary>
		/// Sets the <see cref="Typeface"/>.
		/// </summary>
		public void SetTypeface(Typeface value)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			typeface = value;
		}
		
		/// <summary>
		/// Gets the text decorations. The value may be null, a frozen <see cref="TextDecorationCollection"/>
		/// or an unfrozen <see cref="TextDecorationCollection"/>.
		/// If the value is an unfrozen <see cref="TextDecorationCollection"/>, you may assume that the
		/// collection instance is only used for this <see cref="TextRunProperties"/> instance and it is safe
		/// to add <see cref="TextDecoration"/>s.
		/// </summary>
		public override TextDecorationCollection TextDecorations {
			get { return textDecorations; }
		}
		
		/// <summary>
		/// Sets the <see cref="TextDecorations"/>.
		/// </summary>
		public void SetTextDecorations(TextDecorationCollection value)
		{
			ExtensionMethods.CheckIsFrozen(value);
			textDecorations = value;
		}
		
		/// <summary>
		/// Gets the text effects. The value may be null, a frozen <see cref="TextEffectCollection"/>
		/// or an unfrozen <see cref="TextEffectCollection"/>.
		/// If the value is an unfrozen <see cref="TextEffectCollection"/>, you may assume that the
		/// collection instance is only used for this <see cref="TextRunProperties"/> instance and it is safe
		/// to add <see cref="TextEffect"/>s.
		/// </summary>
		public override TextEffectCollection TextEffects {
			get { return textEffects; }
		}
		
		/// <summary>
		/// Sets the <see cref="TextEffects"/>.
		/// </summary>
		public void SetTextEffects(TextEffectCollection value)
		{
			ExtensionMethods.CheckIsFrozen(value);
			textEffects = value;
		}
		
		/// <summary>
		/// Gets the typography properties for the text run.
		/// </summary>
		public override TextRunTypographyProperties TypographyProperties {
			get { return typographyProperties; }
		}
		
		/// <summary>
		/// Sets the <see cref="TypographyProperties"/>.
		/// </summary>
		public void SetTypographyProperties(TextRunTypographyProperties value)
		{
			typographyProperties = value;
		}
		
		/// <summary>
		/// Gets the number substitution settings for the text run.
		/// </summary>
		public override NumberSubstitution NumberSubstitution {
			get { return numberSubstitution; }
		}
		
		/// <summary>
		/// Sets the <see cref="NumberSubstitution"/>.
		/// </summary>
		public void SetNumberSubstitution(NumberSubstitution value)
		{
			numberSubstitution = value;
		}
	}
}

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
using dnSpy.Contracts.Text.Formatting;
using dnSpy.Contracts.Themes;

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Defines editor colors
	/// </summary>
	public abstract class EditorFormatDefinition {
		/// <summary>
		/// Creates a new <see cref="TextFormattingRunProperties"/> instance
		/// </summary>
		/// <param name="theme">Theme to use</param>
		/// <returns></returns>
		public virtual TextFormattingRunProperties CreateTextFormattingRunProperties(ITheme theme) {
			if (theme == null)
				throw new ArgumentNullException(nameof(theme));

			var typeface = GetTypeface(theme);
			var foreground = GetForeground(theme);
			var background = GetBackground(theme);
			var cultureInfo = GetCultureInfo(theme);
			var hintingSize = GetFontHintingEmSize(theme);
			var renderingSize = GetFontRenderingEmSize(theme);
			var textDecorations = GetTextDecorations(theme);
			var textEffects = GetTextEffects(theme);

			var p = TextFormattingRunProperties.CreateTextFormattingRunProperties(foreground, background, typeface, renderingSize, hintingSize, textDecorations, textEffects, cultureInfo);

			var isBold = GetIsBold(theme);
			if (typeface != null)
				isBold = typeface.Weight == FontWeights.Bold;
			if (isBold != null)
				p = p.SetBold(isBold.Value);

			var isItalic = GetIsItalic(theme);
			if (typeface != null)
				isItalic = typeface.Style == FontStyles.Italic;
			if (isItalic != null)
				p = p.SetItalic(isItalic.Value);

			var fgOpacity = GetForegroundOpacity(theme);
			if (foreground != null && foreground.Opacity != 1.0)
				fgOpacity = foreground.Opacity;
			if (fgOpacity != null)
				p = p.SetForegroundOpacity(fgOpacity.Value);

			var bgOpacity = GetBackgroundOpacity(theme);
			if (background != null && background.Opacity != 1.0)
				bgOpacity = background.Opacity;
			if (bgOpacity != null)
				p = p.SetBackgroundOpacity(bgOpacity.Value);

			return p;
		}

		/// <summary>
		/// Gets the <see cref="Typeface"/> or null
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public virtual Typeface GetTypeface(ITheme theme) => null;

		/// <summary>
		/// Gets the bold value or null
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public virtual bool? GetIsBold(ITheme theme) => null;

		/// <summary>
		/// Gets the italic value or null
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public virtual bool? GetIsItalic(ITheme theme) => null;

		/// <summary>
		/// Gets the foreground brush or null
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public virtual Brush GetForeground(ITheme theme) => null;

		/// <summary>
		/// Gets the background brush or null
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public virtual Brush GetBackground(ITheme theme) => null;

		/// <summary>
		/// Gets the foreground opacity or null
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public virtual double? GetForegroundOpacity(ITheme theme) => null;

		/// <summary>
		/// Gets the background opacity or null
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public virtual double? GetBackgroundOpacity(ITheme theme) => null;

		/// <summary>
		/// Gets the <see cref="CultureInfo"/> or null
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public virtual CultureInfo GetCultureInfo(ITheme theme) => null;

		/// <summary>
		/// Gets the font hinting em size or null
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public virtual double? GetFontHintingEmSize(ITheme theme) => null;

		/// <summary>
		/// Gets the font rendering em size or null
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public virtual double? GetFontRenderingEmSize(ITheme theme) => null;

		/// <summary>
		/// Gets the text decorations or null
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public virtual TextDecorationCollection GetTextDecorations(ITheme theme) => null;

		/// <summary>
		/// Gets the text effects or null
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public virtual TextEffectCollection GetTextEffects(ITheme theme) => null;
	}
}

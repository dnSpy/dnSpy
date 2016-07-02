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
using dnSpy.Contracts.Themes;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Text editor format definition
	/// </summary>
	public abstract class TextEditorFormatDefinition : EditorFormatDefinition, IThemeFormatDefinition {
		/// <summary>
		/// This method isn't implemented, call <see cref="CreateResourceDictionary(ITheme)"/> instead
		/// </summary>
		/// <returns></returns>
		protected override ResourceDictionary CreateResourceDictionaryFromDefinition() {
			throw new InvalidOperationException($"You must call {nameof(IThemeFormatDefinition)}.{nameof(IThemeFormatDefinition.CreateResourceDictionary)}");
		}

		/// <summary>
		/// Creates a new <see cref="ResourceDictionary"/>
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public ResourceDictionary CreateResourceDictionary(ITheme theme) {
			var res = new ResourceDictionary();

			var bg = GetWindowBackground(theme) ?? SystemColors.WindowBrush;
			res[EditorFormatMapConstants.TextViewBackgroundId] = bg;

			var typeface = GetTypeface(theme);
			var foreground = GetForeground(theme);
			var background = GetBackground(theme);
			var cultureInfo = GetCultureInfo(theme);
			var hintingSize = GetFontHintingEmSize(theme);
			var renderingSize = GetFontRenderingEmSize(theme);
			var textDecorations = GetTextDecorations(theme);
			var textEffects = GetTextEffects(theme);
			var isBold = GetIsBold(theme);
			var isItalic = GetIsItalic(theme);
			var fgOpacity = GetForegroundOpacity(theme);
			var bgOpacity = GetBackgroundOpacity(theme);

			if (typeface != null)
				res[ClassificationFormatDefinition.TypefaceId] = typeface;
			if (foreground != null)
				res[EditorFormatDefinition.ForegroundBrushId] = foreground;
			if (background != null)
				res[EditorFormatDefinition.BackgroundBrushId] = background;
			if (cultureInfo != null)
				res[ClassificationFormatDefinition.CultureInfoId] = cultureInfo;
			if (hintingSize != null)
				res[ClassificationFormatDefinition.FontHintingSizeId] = hintingSize;
			if (renderingSize != null)
				res[ClassificationFormatDefinition.FontRenderingSizeId] = renderingSize;
			if (textDecorations != null)
				res[ClassificationFormatDefinition.TextDecorationsId] = textDecorations;
			if (textEffects != null)
				res[ClassificationFormatDefinition.TextEffectsId] = textEffects;
			if (isBold != null)
				res[ClassificationFormatDefinition.IsBoldId] = isBold;
			if (isItalic != null)
				res[ClassificationFormatDefinition.IsItalicId] = isItalic;
			if (fgOpacity != null)
				res[ClassificationFormatDefinition.ForegroundOpacityId] = fgOpacity;
			if (bgOpacity != null)
				res[ClassificationFormatDefinition.BackgroundOpacityId] = bgOpacity;

			return res;
		}

		/// <summary>
		/// Gets the background brush of the window
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public virtual Brush GetWindowBackground(ITheme theme) => null;

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

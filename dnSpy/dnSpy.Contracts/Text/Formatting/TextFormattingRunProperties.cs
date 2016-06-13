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

using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace dnSpy.Contracts.Text.Formatting {
	/// <summary>
	/// Text formatting run properties
	/// </summary>
	public sealed class TextFormattingRunProperties : TextRunProperties {
		/// <summary>
		/// Gets the background brush
		/// </summary>
		public override Brush BackgroundBrush {
			get {
				throw new System.NotImplementedException();//TODO:
			}
		}

		/// <summary>
		/// Gets the culture information
		/// </summary>
		public override CultureInfo CultureInfo {
			get {
				throw new System.NotImplementedException();//TODO:
			}
		}

		/// <summary>
		/// Gets the font hinting size
		/// </summary>
		public override double FontHintingEmSize {
			get {
				throw new System.NotImplementedException();//TODO:
			}
		}

		/// <summary>
		/// Gets the font rendering size
		/// </summary>
		public override double FontRenderingEmSize {
			get {
				throw new System.NotImplementedException();//TODO:
			}
		}

		/// <summary>
		/// Gets the foreground brush
		/// </summary>
		public override Brush ForegroundBrush {
			get {
				throw new System.NotImplementedException();//TODO:
			}
		}

		/// <summary>
		/// Gets the decorations for the text
		/// </summary>
		public override TextDecorationCollection TextDecorations {
			get {
				throw new System.NotImplementedException();//TODO:
			}
		}

		/// <summary>
		/// Gets the text effects for the text
		/// </summary>
		public override TextEffectCollection TextEffects {
			get {
				throw new System.NotImplementedException();//TODO:
			}
		}

		/// <summary>
		/// Gets the typeface for the text
		/// </summary>
		public override Typeface Typeface {
			get {
				throw new System.NotImplementedException();//TODO:
			}
		}

		TextFormattingRunProperties() { }

		/// <summary>
		/// Initializes a new instance of <see cref="TextFormattingRunProperties"/>
		/// </summary>
		/// <returns></returns>
		public static TextFormattingRunProperties CreateTextFormattingRunProperties() {
			return new TextFormattingRunProperties();//TODO:
		}
	}
}

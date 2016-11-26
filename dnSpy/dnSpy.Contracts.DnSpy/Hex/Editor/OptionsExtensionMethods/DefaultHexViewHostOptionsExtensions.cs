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
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods {
	/// <summary>
	/// <see cref="DefaultHexViewHostOptions"/> extension methods
	/// </summary>
	static class DefaultHexViewHostOptionsExtensions {
		/// <summary>
		/// Returns true if the vertical scroll bar is enabled
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsVerticalScrollBarEnabled(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewHostOptions.VerticalScrollBarId);
		}

		/// <summary>
		/// Returns true if the horizontal scroll bar is enabled
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsHorizontalScrollBarEnabled(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewHostOptions.HorizontalScrollBarId);
		}

		/// <summary>
		/// Returns true if the selection margin is enabled
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsSelectionMarginEnabled(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewHostOptions.SelectionMarginId);
		}

		/// <summary>
		/// Returns true if the zoom control is enabled
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsZoomControlEnabled(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewHostOptions.ZoomControlId);
		}

		/// <summary>
		/// Returns true if the the glyph margin is enabled
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsGlyphMarginEnabled(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewHostOptions.GlyphMarginId);
		}
	}
}

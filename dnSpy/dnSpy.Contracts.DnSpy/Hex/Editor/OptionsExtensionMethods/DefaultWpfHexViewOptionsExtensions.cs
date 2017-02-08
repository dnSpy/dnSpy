/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
	/// <see cref="DefaultWpfHexViewOptions"/> extension methods
	/// </summary>
	static class DefaultWpfHexViewOptionsExtensions {
		/// <summary>
		/// Returns true if the current line should be highlighted
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsHighlightCurrentLineEnabled(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultWpfHexViewOptions.EnableHighlightCurrentLineId);
		}

		/// <summary>
		/// Returns true if simple graphics option is enabled
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsSimpleGraphicsEnabled(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultWpfHexViewOptions.EnableSimpleGraphicsId);
		}

		/// <summary>
		/// Returns true if mouse wheel zoom is enabled
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsMouseWheelZoomEnabled(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultWpfHexViewOptions.EnableMouseWheelZoomId);
		}

		/// <summary>
		/// Returns the appearance category
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static string AppearanceCategory(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultWpfHexViewOptions.AppearanceCategoryId);
		}

		/// <summary>
		/// Returns the zoom level
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static double ZoomLevel(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultWpfHexViewOptions.ZoomLevelId);
		}

		/// <summary>
		/// Returns true if clear type should be forced is enabled
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsForceClearTypeIfNeededEnabled(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultWpfHexViewOptions.ForceClearTypeIfNeededId);
		}
	}
}
